// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Did;
using Sorcha.Wallet.Service.Credentials;
using Sorcha.Wallet.Service.Models;

namespace Sorcha.Wallet.Service.Services;

/// <summary>
/// Manages OID4VP presentation requests — creation, matching, verification, and lifecycle.
/// </summary>
public interface IPresentationRequestService
{
    /// <summary>
    /// Creates a new presentation request.
    /// </summary>
    Task<PresentationRequest> CreateRequestAsync(CreatePresentationRequestDto dto, CancellationToken ct = default);

    /// <summary>
    /// Gets a presentation request by ID. Returns null if not found.
    /// </summary>
    Task<PresentationRequest?> GetRequestAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    /// Finds credentials matching the request in the target wallet.
    /// </summary>
    Task<IReadOnlyList<MatchedCredentialInfo>> FindMatchingCredentialsAsync(
        PresentationRequest request, string walletAddress, CancellationToken ct = default);

    /// <summary>
    /// Submits a presentation for verification.
    /// </summary>
    Task<PresentationRequest> SubmitPresentationAsync(
        string requestId, string credentialId, string[] disclosedClaims, string vpToken,
        CancellationToken ct = default);

    /// <summary>
    /// Denies a presentation request.
    /// </summary>
    Task<PresentationRequest?> DenyRequestAsync(string requestId, CancellationToken ct = default);
}

/// <summary>
/// DTO for creating a presentation request.
/// </summary>
public class CreatePresentationRequestDto
{
    public required string CredentialType { get; init; }
    public string[]? AcceptedIssuers { get; init; }
    public ClaimConstraint[]? RequiredClaims { get; init; }
    public required string CallbackUrl { get; init; }
    public string? TargetWalletAddress { get; init; }
    public int TtlSeconds { get; init; } = 300;
    public string VerifierIdentity { get; init; } = "Unknown Verifier";
}

/// <summary>
/// Info about a credential that matches a presentation request.
/// </summary>
public class MatchedCredentialInfo
{
    public required string CredentialId { get; init; }
    public required string Type { get; init; }
    public required string IssuerDid { get; init; }
    public required string[] DisclosableClaims { get; init; }
    public required string[] RequestedClaims { get; init; }
}

public class PresentationRequestService : IPresentationRequestService
{
    private readonly ConcurrentDictionary<string, PresentationRequest> _requests = new();
    private readonly ICredentialStore _credentialStore;
    private readonly IDidResolverRegistry? _didRegistry;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<PresentationRequestService> _logger;

    public PresentationRequestService(
        ICredentialStore credentialStore,
        ILogger<PresentationRequestService> logger,
        IDidResolverRegistry? didRegistry = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _credentialStore = credentialStore;
        _logger = logger;
        _didRegistry = didRegistry;
        _httpClientFactory = httpClientFactory;
    }

    public Task<PresentationRequest> CreateRequestAsync(
        CreatePresentationRequestDto dto, CancellationToken ct = default)
    {
        var request = new PresentationRequest
        {
            VerifierIdentity = dto.VerifierIdentity,
            CredentialType = dto.CredentialType,
            AcceptedIssuers = dto.AcceptedIssuers,
            RequiredClaims = dto.RequiredClaims,
            CallbackUrl = dto.CallbackUrl,
            TargetWalletAddress = dto.TargetWalletAddress,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(dto.TtlSeconds)
        };

        _requests[request.Id] = request;

        _logger.LogInformation(
            "Created presentation request {RequestId} for {CredentialType} from {Verifier}, expires {ExpiresAt}",
            request.Id, request.CredentialType, request.VerifierIdentity, request.ExpiresAt);

        return Task.FromResult(request);
    }

    public Task<PresentationRequest?> GetRequestAsync(string requestId, CancellationToken ct = default)
    {
        _requests.TryGetValue(requestId, out var request);

        if (request is { Status: PresentationStatus.Pending, IsExpired: true })
        {
            request.Status = PresentationStatus.Expired;
        }

        return Task.FromResult(request);
    }

    public async Task<IReadOnlyList<MatchedCredentialInfo>> FindMatchingCredentialsAsync(
        PresentationRequest request, string walletAddress, CancellationToken ct = default)
    {
        var credentials = await _credentialStore.MatchAsync(
            walletAddress,
            request.CredentialType,
            request.AcceptedIssuers,
            ct);

        var result = new List<MatchedCredentialInfo>();

        foreach (var cred in credentials)
        {
            var claims = ParseClaims(cred.ClaimsJson);
            if (claims == null) continue;

            var disclosable = claims.Keys.ToArray();
            var requested = request.RequiredClaims?
                .Select(c => c.ClaimName)
                .Where(n => claims.ContainsKey(n))
                .ToArray() ?? [];

            result.Add(new MatchedCredentialInfo
            {
                CredentialId = cred.Id,
                Type = cred.Type,
                IssuerDid = cred.IssuerDid,
                DisclosableClaims = disclosable,
                RequestedClaims = requested
            });
        }

        return result;
    }

    public async Task<PresentationRequest> SubmitPresentationAsync(
        string requestId, string credentialId, string[] disclosedClaims, string vpToken,
        CancellationToken ct = default)
    {
        if (!_requests.TryGetValue(requestId, out var request))
            throw new KeyNotFoundException($"Presentation request '{requestId}' not found");

        if (request.IsExpired)
        {
            request.Status = PresentationStatus.Expired;
            throw new InvalidOperationException("Presentation request has expired");
        }

        if (request.Status != PresentationStatus.Pending)
            throw new InvalidOperationException($"Request is in '{request.Status}' state, expected Pending");

        request.VpToken = vpToken;
        request.Status = PresentationStatus.Submitted;

        // Verify the presentation
        var verification = await VerifyPresentationAsync(request, credentialId, ct);
        request.VerificationResult = JsonSerializer.Serialize(verification);

        if (verification.IsValid)
        {
            request.Status = PresentationStatus.Verified;

            // Record usage against credential
            await _credentialStore.RecordPresentationAsync(credentialId, ct);

            _logger.LogInformation(
                "Presentation request {RequestId} verified successfully for credential {CredentialId}",
                requestId, credentialId);
        }
        else
        {
            request.Status = PresentationStatus.Denied;

            _logger.LogWarning(
                "Presentation request {RequestId} verification failed for credential {CredentialId}: {Errors}",
                requestId, credentialId,
                string.Join(", ", verification.Errors?.Select(e => e.Message) ?? []));
        }

        return request;
    }

    public Task<PresentationRequest?> DenyRequestAsync(string requestId, CancellationToken ct = default)
    {
        if (!_requests.TryGetValue(requestId, out var request))
            return Task.FromResult<PresentationRequest?>(null);

        if (request.Status == PresentationStatus.Pending)
        {
            request.Status = PresentationStatus.Denied;
            _logger.LogInformation("Presentation request {RequestId} denied by holder", requestId);
        }

        return Task.FromResult<PresentationRequest?>(request);
    }

    private async Task<VerificationResult> VerifyPresentationAsync(
        PresentationRequest request, string credentialId, CancellationToken ct)
    {
        var errors = new List<VerificationError>();

        // 1. Look up credential
        var credential = await _credentialStore.GetByIdAsync(credentialId, ct);
        if (credential == null)
        {
            errors.Add(new VerificationError
            {
                RequirementType = request.CredentialType,
                FailureReason = "NotFound",
                Message = "Credential not found"
            });
            return new VerificationResult { IsValid = false, Errors = errors };
        }

        // 2. Type match
        if (!string.Equals(credential.Type, request.CredentialType, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new VerificationError
            {
                RequirementType = request.CredentialType,
                FailureReason = "TypeMismatch",
                Message = $"Expected '{request.CredentialType}', got '{credential.Type}'"
            });
        }

        // 3. Issuer check
        if (request.AcceptedIssuers is { Length: > 0 } &&
            !request.AcceptedIssuers.Contains(credential.IssuerDid))
        {
            errors.Add(new VerificationError
            {
                RequirementType = request.CredentialType,
                FailureReason = "UntrustedIssuer",
                Message = $"Issuer '{credential.IssuerDid}' not in accepted issuers list"
            });
        }

        // 4. Status check (active)
        if (credential.Status != "Active")
        {
            errors.Add(new VerificationError
            {
                RequirementType = request.CredentialType,
                FailureReason = credential.Status,
                Message = $"Credential has been {credential.Status.ToLowerInvariant()} by issuer"
            });
        }

        // 5. Expiry check
        if (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            errors.Add(new VerificationError
            {
                RequirementType = request.CredentialType,
                FailureReason = "Expired",
                Message = "Credential has expired"
            });
        }

        // 6. Status list check (if configured)
        var statusListCheck = "NotConfigured";
        if (!string.IsNullOrEmpty(credential.StatusListUrl) && credential.StatusListIndex.HasValue)
        {
            statusListCheck = await CheckStatusListAsync(
                credential.StatusListUrl, credential.StatusListIndex.Value, ct);

            if (statusListCheck == "Revoked" || statusListCheck == "Suspended")
            {
                errors.Add(new VerificationError
                {
                    RequirementType = request.CredentialType,
                    FailureReason = statusListCheck,
                    Message = $"Credential status list reports: {statusListCheck}"
                });
            }
        }
        else
        {
            statusListCheck = "Active";
        }

        // 7. Required claims verification
        var verifiedClaims = new Dictionary<string, object>();
        if (request.RequiredClaims is { Length: > 0 })
        {
            var claims = ParseClaims(credential.ClaimsJson);
            if (claims != null)
            {
                foreach (var constraint in request.RequiredClaims)
                {
                    if (!claims.TryGetValue(constraint.ClaimName, out var value))
                    {
                        errors.Add(new VerificationError
                        {
                            RequirementType = request.CredentialType,
                            FailureReason = "MissingClaim",
                            Message = $"Required claim '{constraint.ClaimName}' not present"
                        });
                        continue;
                    }

                    if (constraint.ExpectedValue != null)
                    {
                        var actualStr = value?.ToString();
                        if (!string.Equals(constraint.ExpectedValue, actualStr, StringComparison.Ordinal))
                        {
                            errors.Add(new VerificationError
                            {
                                RequirementType = request.CredentialType,
                                FailureReason = "ClaimValueMismatch",
                                Message = $"Claim '{constraint.ClaimName}' expected '{constraint.ExpectedValue}', got '{actualStr}'"
                            });
                            continue;
                        }
                    }

                    verifiedClaims[constraint.ClaimName] = value!;
                }
            }
        }
        else
        {
            // No required claims — include all available claims
            var claims = ParseClaims(credential.ClaimsJson);
            if (claims != null)
            {
                foreach (var kvp in claims)
                    verifiedClaims[kvp.Key] = kvp.Value;
            }
        }

        // 8. DID resolution verification (if DID registry available)
        if (_didRegistry != null && errors.Count == 0)
        {
            try
            {
                var didDoc = await _didRegistry.ResolveAsync(credential.IssuerDid, ct);
                if (didDoc == null)
                {
                    _logger.LogWarning(
                        "DID resolution failed for issuer {IssuerDid} — continuing without DID verification",
                        credential.IssuerDid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DID resolution error for {IssuerDid}", credential.IssuerDid);
            }
        }

        if (errors.Count > 0)
        {
            return new VerificationResult
            {
                IsValid = false,
                Errors = errors,
                CredentialType = credential.Type,
                IssuerDid = credential.IssuerDid,
                StatusListCheck = statusListCheck
            };
        }

        return new VerificationResult
        {
            IsValid = true,
            VerifiedClaims = verifiedClaims,
            CredentialType = credential.Type,
            IssuerDid = credential.IssuerDid,
            StatusListCheck = statusListCheck
        };
    }

    private async Task<string> CheckStatusListAsync(
        string statusListUrl, int index, CancellationToken ct)
    {
        try
        {
            if (_httpClientFactory == null) return "Active";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetFromJsonAsync<JsonDocument>(statusListUrl, ct);
            if (response == null) return "Unknown";

            // Navigate W3C BitstringStatusListCredential envelope
            var root = response.RootElement;
            if (root.TryGetProperty("credentialSubject", out var subject) &&
                subject.TryGetProperty("encodedList", out var encodedList))
            {
                var encoded = encodedList.GetString();
                if (!string.IsNullOrEmpty(encoded))
                {
                    var compressed = Convert.FromBase64String(encoded);
                    using var ms = new MemoryStream(compressed);
                    using var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    await gzip.CopyToAsync(output, ct);
                    var bytes = output.ToArray();

                    var byteIndex = index / 8;
                    var bitIndex = 7 - (index % 8); // MSB-first

                    if (byteIndex < bytes.Length)
                    {
                        var isSet = (bytes[byteIndex] & (1 << bitIndex)) != 0;
                        return isSet ? "Revoked" : "Active";
                    }
                }
            }

            return "Active";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check status list at {Url}", statusListUrl);
            return "Unknown";
        }
    }

    private static Dictionary<string, object>? ParseClaims(string claimsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(claimsJson);
        }
        catch
        {
            return null;
        }
    }
}
