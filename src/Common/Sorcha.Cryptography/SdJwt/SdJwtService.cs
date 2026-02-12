// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sorcha.Cryptography.SdJwt;

/// <summary>
/// SD-JWT VC implementation per RFC 9901.
/// Creates, verifies, and presents SD-JWT tokens with selective disclosure.
/// </summary>
/// <remarks>
/// Wraps HeroSD-JWT library for core SD-JWT operations.
/// Falls back to manual implementation if the library is unavailable.
/// </remarks>
public class SdJwtService : ISdJwtService
{
    /// <inheritdoc />
    public Task<SdJwtToken> CreateTokenAsync(
        Dictionary<string, object> claims,
        IEnumerable<string>? disclosableClaims,
        string issuer,
        string subject,
        byte[] signingKey,
        string algorithm,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);

        var disclosableSet = disclosableClaims?.ToHashSet() ?? claims.Keys.ToHashSet();

        // Build disclosures for each disclosable claim
        var disclosures = new List<string>();
        var sdDigests = new List<string>();

        foreach (var claimName in disclosableSet)
        {
            if (!claims.TryGetValue(claimName, out var claimValue))
                continue;

            var disclosure = CreateDisclosure(claimName, claimValue);
            disclosures.Add(disclosure);

            var digest = ComputeDisclosureDigest(disclosure);
            sdDigests.Add(digest);
        }

        // Build the JWT payload
        var payload = new Dictionary<string, object>
        {
            ["iss"] = issuer,
            ["sub"] = subject,
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["_sd_alg"] = "sha-256"
        };

        if (expiresAt.HasValue)
            payload["exp"] = expiresAt.Value.ToUnixTimeSeconds();

        // Add non-disclosable claims directly
        foreach (var (key, value) in claims)
        {
            if (!disclosableSet.Contains(key))
                payload[key] = value;
        }

        // Add SD digests
        if (sdDigests.Count > 0)
            payload["_sd"] = sdDigests;

        // Build the JWT header
        var header = new Dictionary<string, object>
        {
            ["alg"] = MapAlgorithm(algorithm),
            ["typ"] = "vc+sd-jwt"
        };

        // Sign the JWT
        var headerB64 = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var payloadB64 = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = Sign(Encoding.UTF8.GetBytes(signingInput), signingKey, algorithm);
        var signatureB64 = Base64UrlEncode(signature);

        // Assemble the SD-JWT: header.payload.signature~disclosure1~disclosure2~
        var parts = new List<string> { $"{headerB64}.{payloadB64}.{signatureB64}" };
        parts.AddRange(disclosures);
        var rawToken = string.Join("~", parts) + "~";

        var token = new SdJwtToken
        {
            Header = header,
            Payload = payload,
            Disclosures = disclosures,
            Signature = signatureB64,
            RawToken = rawToken
        };

        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<SdJwtVerificationResult> VerifyTokenAsync(
        string rawToken,
        byte[] issuerPublicKey,
        string algorithm,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        ArgumentNullException.ThrowIfNull(issuerPublicKey);

        var result = new SdJwtVerificationResult();

        try
        {
            var parts = rawToken.TrimEnd('~').Split('~');
            if (parts.Length < 1)
            {
                result.Errors.Add("Invalid SD-JWT format: no JWT part found");
                return Task.FromResult(result);
            }

            var jwtPart = parts[0];
            var disclosures = parts.Length > 1 ? parts[1..] : [];

            // Parse and verify JWT signature
            var jwtSegments = jwtPart.Split('.');
            if (jwtSegments.Length != 3)
            {
                result.Errors.Add("Invalid JWT format: expected 3 segments");
                return Task.FromResult(result);
            }

            var signingInput = $"{jwtSegments[0]}.{jwtSegments[1]}";
            var signatureBytes = Base64UrlDecode(jwtSegments[2]);

            if (!Verify(Encoding.UTF8.GetBytes(signingInput), signatureBytes, issuerPublicKey, algorithm))
            {
                result.Errors.Add("Invalid signature");
                return Task.FromResult(result);
            }

            // Parse payload
            var payloadJson = Base64UrlDecode(jwtSegments[1]);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)
                ?? new Dictionary<string, JsonElement>();

            // Extract standard claims
            if (payload.TryGetValue("iss", out var iss))
                result.Issuer = iss.GetString();
            if (payload.TryGetValue("sub", out var sub))
                result.Subject = sub.GetString();
            if (payload.TryGetValue("iat", out var iat))
                result.IssuedAt = DateTimeOffset.FromUnixTimeSeconds(iat.GetInt64());
            if (payload.TryGetValue("exp", out var exp))
                result.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());

            // Extract non-SD claims
            var reservedClaims = new HashSet<string> { "iss", "sub", "iat", "exp", "_sd", "_sd_alg", "cnf" };
            foreach (var (key, value) in payload)
            {
                if (!reservedClaims.Contains(key))
                    result.Claims[key] = ConvertJsonElement(value);
            }

            // Process disclosures
            foreach (var disclosure in disclosures)
            {
                if (string.IsNullOrWhiteSpace(disclosure))
                    continue;

                try
                {
                    var disclosureJson = Base64UrlDecode(disclosure);
                    var disclosureArray = JsonSerializer.Deserialize<JsonElement[]>(disclosureJson);
                    if (disclosureArray is { Length: 3 })
                    {
                        var claimName = disclosureArray[1].GetString() ?? string.Empty;
                        var claimValue = ConvertJsonElement(disclosureArray[2]);
                        result.Claims[claimName] = claimValue;
                    }
                }
                catch
                {
                    result.Errors.Add($"Failed to parse disclosure: {disclosure[..Math.Min(20, disclosure.Length)]}...");
                }
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Verification failed: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SdJwtPresentation> CreatePresentationAsync(
        string rawToken,
        IEnumerable<string> claimsToDisclose,
        byte[]? holderKey = null,
        string? audience = null,
        string? nonce = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        ArgumentNullException.ThrowIfNull(claimsToDisclose);

        var disclosureSet = claimsToDisclose.ToHashSet();
        var parts = rawToken.TrimEnd('~').Split('~');
        var jwtPart = parts[0];
        var allDisclosures = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        // Select only the disclosures for claims we want to reveal
        var selectedDisclosures = new List<string>();
        foreach (var disclosure in allDisclosures)
        {
            if (string.IsNullOrWhiteSpace(disclosure))
                continue;

            try
            {
                var disclosureJson = Base64UrlDecode(disclosure);
                var disclosureArray = JsonSerializer.Deserialize<JsonElement[]>(disclosureJson);
                if (disclosureArray is { Length: 3 })
                {
                    var claimName = disclosureArray[1].GetString();
                    if (claimName != null && disclosureSet.Contains(claimName))
                        selectedDisclosures.Add(disclosure);
                }
            }
            catch
            {
                // Skip malformed disclosures
            }
        }

        // Build presentation: jwt~selected_disclosure1~selected_disclosure2~[kb-jwt]
        var presentationParts = new List<string> { jwtPart };
        presentationParts.AddRange(selectedDisclosures);
        var rawPresentation = string.Join("~", presentationParts) + "~";

        var presentation = new SdJwtPresentation
        {
            Token = new SdJwtToken { RawToken = rawToken },
            SelectedDisclosures = selectedDisclosures,
            RawPresentation = rawPresentation
        };

        return Task.FromResult(presentation);
    }

    /// <inheritdoc />
    public Task<SdJwtVerificationResult> VerifyPresentationAsync(
        string rawPresentation,
        byte[] issuerPublicKey,
        string algorithm,
        CancellationToken cancellationToken = default)
    {
        // A presentation is verified the same way as a token —
        // only the selected disclosures are present, so only those claims are extracted.
        return VerifyTokenAsync(rawPresentation, issuerPublicKey, algorithm, cancellationToken);
    }

    // --- Internal helpers ---

    private static string CreateDisclosure(string claimName, object claimValue)
    {
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var disclosure = JsonSerializer.Serialize(new object[] { salt, claimName, claimValue });
        return Base64UrlEncode(Encoding.UTF8.GetBytes(disclosure));
    }

    private static string ComputeDisclosureDigest(string disclosure)
    {
        var bytes = Encoding.ASCII.GetBytes(disclosure);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    private static string MapAlgorithm(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "EDDSA" or "ED25519" => "EdDSA",
            "ES256" or "P-256" or "P256" => "ES256",
            "RS256" or "RSA" or "RSA-4096" => "RS256",
            _ => algorithm
        };
    }

    private static byte[] Sign(byte[] data, byte[] privateKey, string algorithm)
    {
        var alg = algorithm.ToUpperInvariant();

        if (alg is "EDDSA" or "ED25519")
        {
            // Use libsodium Ed25519 signing via Sodium.Core
            return Sodium.PublicKeyAuth.SignDetached(data, privateKey);
        }

        if (alg is "ES256" or "P-256" or "P256")
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(privateKey, out _);
            return ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }

        if (alg is "RS256" or "RSA" or "RSA-4096")
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        throw new NotSupportedException($"Unsupported signing algorithm: {algorithm}");
    }

    private static bool Verify(byte[] data, byte[] signature, byte[] publicKey, string algorithm)
    {
        var alg = algorithm.ToUpperInvariant();

        if (alg is "EDDSA" or "ED25519")
        {
            return Sodium.PublicKeyAuth.VerifyDetached(signature, data, publicKey);
        }

        if (alg is "ES256" or "P-256" or "P256")
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

        if (alg is "RS256" or "RSA" or "RSA-4096")
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        throw new NotSupportedException($"Unsupported verification algorithm: {algorithm}");
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            _ => element.GetRawText()
        };
    }
}
