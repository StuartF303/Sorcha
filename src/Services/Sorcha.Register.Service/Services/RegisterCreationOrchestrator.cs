// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Validator;

namespace Sorcha.Register.Service.Services;

/// <summary>
/// Orchestrates the two-phase register creation workflow with genesis transactions
/// </summary>
public class RegisterCreationOrchestrator : IRegisterCreationOrchestrator
{
    private readonly ILogger<RegisterCreationOrchestrator> _logger;
    private readonly RegisterManager _registerManager;
    private readonly IWalletServiceClient _walletClient;
    private readonly IHashProvider _hashProvider;
    private readonly ICryptoModule _cryptoModule;
    private readonly IValidatorServiceClient _validatorClient;

    // Thread-safe in-memory storage for pending registrations
    // TODO: Replace with distributed cache (Redis) for production multi-instance deployments
    private readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations = new();

    private readonly TimeSpan _pendingExpirationTime = TimeSpan.FromMinutes(5);
    private readonly JsonSerializerOptions _canonicalJsonOptions;

    public RegisterCreationOrchestrator(
        ILogger<RegisterCreationOrchestrator> logger,
        RegisterManager registerManager,
        IWalletServiceClient walletClient,
        IHashProvider hashProvider,
        ICryptoModule cryptoModule,
        IValidatorServiceClient validatorClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registerManager = registerManager ?? throw new ArgumentNullException(nameof(registerManager));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _validatorClient = validatorClient ?? throw new ArgumentNullException(nameof(validatorClient));

        // Configure JSON serialization for canonical form (RFC 8785)
        _canonicalJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Initiates register creation (Phase 1): generates unsigned control record
    /// </summary>
    public async Task<InitiateRegisterCreationResponse> InitiateAsync(
        InitiateRegisterCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Initiating register creation for name '{Name}' in tenant '{TenantId}'",
            request.Name,
            request.TenantId);

        // Generate unique register ID (GUID without hyphens)
        var registerId = Guid.NewGuid().ToString("N");
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.Add(_pendingExpirationTime);
        var nonce = GenerateNonce();

        // Create control record template with creator as owner
        var controlRecord = new RegisterControlRecord
        {
            RegisterId = registerId,
            Name = request.Name,
            Description = request.Description,
            TenantId = request.TenantId,
            CreatedAt = createdAt,
            Metadata = request.Metadata,
            Attestations = new List<RegisterAttestation>
            {
                new RegisterAttestation
                {
                    Role = RegisterRole.Owner,
                    Subject = $"did:sorcha:{request.Creator.UserId}",
                    PublicKey = "[to-be-filled]",
                    Signature = "[to-be-signed]",
                    Algorithm = SignatureAlgorithm.ED25519, // Default, client can override
                    GrantedAt = createdAt
                }
            }
        };

        // Add additional administrators if specified
        if (request.AdditionalAdmins != null)
        {
            foreach (var admin in request.AdditionalAdmins)
            {
                controlRecord.Attestations.Add(new RegisterAttestation
                {
                    Role = admin.Role,
                    Subject = $"did:sorcha:{admin.UserId}",
                    PublicKey = "[to-be-filled]",
                    Signature = "[to-be-signed]",
                    Algorithm = SignatureAlgorithm.ED25519,
                    GrantedAt = createdAt
                });
            }
        }

        // Compute canonical JSON hash for signing
        var canonicalJson = JsonSerializer.Serialize(controlRecord, _canonicalJsonOptions);
        var hashBytes = _hashProvider.ComputeHash(
            Encoding.UTF8.GetBytes(canonicalJson),
            Sorcha.Cryptography.Enums.HashType.SHA256);
        var controlRecordHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Store pending registration
        var pending = new PendingRegistration
        {
            RegisterId = registerId,
            ControlRecord = controlRecord,
            ControlRecordHash = controlRecordHash,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Nonce = nonce
        };

        _pendingRegistrations[registerId] = pending;

        // Schedule cleanup of expired pending registrations
        _ = Task.Run(async () => await CleanupExpiredPendingRegistrationsAsync(), cancellationToken);

        _logger.LogInformation(
            "Register initiation created with ID {RegisterId}, expires at {ExpiresAt}",
            registerId,
            expiresAt);

        return new InitiateRegisterCreationResponse
        {
            RegisterId = registerId,
            ControlRecord = controlRecord,
            DataToSign = controlRecordHash,
            ExpiresAt = expiresAt,
            Nonce = nonce
        };
    }

    /// <summary>
    /// Finalizes register creation (Phase 2): verifies signatures and creates register
    /// </summary>
    public async Task<FinalizeRegisterCreationResponse> FinalizeAsync(
        FinalizeRegisterCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Finalizing register creation for ID {RegisterId}",
            request.RegisterId);

        // Retrieve pending registration
        if (!_pendingRegistrations.TryGetValue(request.RegisterId, out var pending))
        {
            _logger.LogWarning("Pending registration not found for ID {RegisterId}", request.RegisterId);
            throw new InvalidOperationException($"Pending registration not found for register ID {request.RegisterId}");
        }

        // Verify nonce (replay protection)
        if (pending.Nonce != request.Nonce)
        {
            _logger.LogWarning(
                "Invalid nonce for register {RegisterId}: expected {Expected}, got {Actual}",
                request.RegisterId,
                pending.Nonce,
                request.Nonce);
            throw new UnauthorizedAccessException("Invalid nonce - possible replay attack");
        }

        // Check expiration
        if (pending.IsExpired())
        {
            _pendingRegistrations.TryRemove(request.RegisterId, out _);
            _logger.LogWarning("Pending registration expired for ID {RegisterId}", request.RegisterId);
            throw new InvalidOperationException($"Pending registration expired for register ID {request.RegisterId}");
        }

        // Validate control record structure
        var validationErrors = ValidateControlRecord(request.ControlRecord);
        if (validationErrors.Any())
        {
            _logger.LogWarning(
                "Control record validation failed for {RegisterId}: {Errors}",
                request.RegisterId,
                string.Join(", ", validationErrors));
            throw new ArgumentException($"Control record validation failed: {string.Join(", ", validationErrors)}");
        }

        // Verify all attestation signatures
        await VerifyAttestationsAsync(request.ControlRecord, pending.ControlRecordHash, cancellationToken);

        // Create register in database
        var register = await _registerManager.CreateRegisterAsync(
            request.ControlRecord.Name,
            request.ControlRecord.TenantId,
            advertise: false, // Default to private
            isFullReplica: true,
            cancellationToken);

        _logger.LogInformation("Created register {RegisterId} in database", register.Id);

        // Create genesis transaction with control record payload
        var genesisTransaction = CreateGenesisTransaction(register.Id, request.ControlRecord);

        _logger.LogInformation(
            "Created genesis transaction {TransactionId} for register {RegisterId}",
            genesisTransaction.TxId,
            register.Id);

        // Submit genesis transaction to Validator Service mempool
        var controlRecordJson = JsonSerializer.Serialize(request.ControlRecord, _canonicalJsonOptions);
        var submissionRequest = new GenesisTransactionSubmission
        {
            TransactionId = genesisTransaction.TxId,
            RegisterId = register.Id,
            ControlRecordPayload = JsonDocument.Parse(controlRecordJson).RootElement,
            PayloadHash = genesisTransaction.Payloads[0].Hash,
            Signatures = request.ControlRecord.Attestations.Select(a => new GenesisSignature
            {
                PublicKey = a.PublicKey,
                SignatureValue = a.Signature,
                Algorithm = a.Algorithm.ToString()
            }).ToList(),
            CreatedAt = request.ControlRecord.CreatedAt,
            RegisterName = request.ControlRecord.Name,
            TenantId = request.ControlRecord.TenantId
        };

        var submitted = await _validatorClient.SubmitGenesisTransactionAsync(submissionRequest, cancellationToken);

        if (!submitted)
        {
            _logger.LogWarning(
                "Failed to submit genesis transaction {TransactionId} to Validator Service",
                genesisTransaction.TxId);
            // Note: Register is already created in database, but genesis transaction submission failed
            // This is a recoverable state - the genesis transaction can be resubmitted
        }
        else
        {
            _logger.LogInformation(
                "Genesis transaction {TransactionId} submitted to Validator Service successfully",
                genesisTransaction.TxId);
        }

        // Remove from pending registrations
        _pendingRegistrations.TryRemove(request.RegisterId, out _);

        return new FinalizeRegisterCreationResponse
        {
            RegisterId = register.Id,
            Status = "created",
            GenesisTransactionId = genesisTransaction.TxId,
            GenesisDocketId = "0",
            CreatedAt = register.CreatedAt
        };
    }

    /// <summary>
    /// Validates control record structure
    /// </summary>
    private List<string> ValidateControlRecord(RegisterControlRecord controlRecord)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(controlRecord.RegisterId))
            errors.Add("RegisterId is required");

        if (string.IsNullOrWhiteSpace(controlRecord.Name))
            errors.Add("Name is required");

        if (controlRecord.Name.Length > 38)
            errors.Add("Name must be 38 characters or less");

        if (controlRecord.Description?.Length > 500)
            errors.Add("Description must be 500 characters or less");

        if (!controlRecord.Attestations.Any())
            errors.Add("At least one attestation is required");

        if (!controlRecord.HasOwnerAttestation())
            errors.Add("At least one Owner attestation is required");

        if (controlRecord.Attestations.Count > 10)
            errors.Add("Maximum 10 attestations allowed");

        // Validate each attestation
        foreach (var attestation in controlRecord.Attestations)
        {
            if (string.IsNullOrWhiteSpace(attestation.Subject))
                errors.Add($"Attestation subject is required");

            if (string.IsNullOrWhiteSpace(attestation.PublicKey))
                errors.Add($"Attestation public key is required for {attestation.Subject}");

            if (string.IsNullOrWhiteSpace(attestation.Signature))
                errors.Add($"Attestation signature is required for {attestation.Subject}");
        }

        return errors;
    }

    /// <summary>
    /// Verifies all attestation signatures
    /// </summary>
    private async Task VerifyAttestationsAsync(
        RegisterControlRecord controlRecord,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Verifying {Count} attestations for register {RegisterId}",
            controlRecord.Attestations.Count,
            controlRecord.RegisterId);

        foreach (var attestation in controlRecord.Attestations)
        {
            try
            {
                // Convert base64 public key and signature to bytes
                var publicKeyBytes = Convert.FromBase64String(attestation.PublicKey);
                var signatureBytes = Convert.FromBase64String(attestation.Signature);
                var hashBytes = Convert.FromHexString(expectedHash);

                // Verify signature using Sorcha.Cryptography
                var verifyResult = await _cryptoModule.VerifyAsync(
                    signatureBytes,
                    hashBytes,
                    MapAlgorithm(attestation.Algorithm),
                    publicKeyBytes,
                    cancellationToken);

                if (verifyResult != Sorcha.Cryptography.Enums.CryptoStatus.Success)
                {
                    _logger.LogWarning(
                        "Signature verification failed for attestation: subject={Subject}, role={Role}",
                        attestation.Subject,
                        attestation.Role);
                    throw new UnauthorizedAccessException(
                        $"Invalid signature for attestation: {attestation.Subject} ({attestation.Role})");
                }

                _logger.LogDebug(
                    "Verified signature for {Subject} ({Role})",
                    attestation.Subject,
                    attestation.Role);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 encoding in attestation for {Subject}", attestation.Subject);
                throw new ArgumentException($"Invalid base64 encoding in attestation for {attestation.Subject}", ex);
            }
        }

        _logger.LogInformation(
            "All {Count} attestations verified successfully for register {RegisterId}",
            controlRecord.Attestations.Count,
            controlRecord.RegisterId);
    }

    /// <summary>
    /// Creates a genesis transaction with control record payload
    /// </summary>
    private TransactionModel CreateGenesisTransaction(string registerId, RegisterControlRecord controlRecord)
    {
        var controlRecordJson = JsonSerializer.Serialize(controlRecord, _canonicalJsonOptions);
        var controlRecordBytes = Encoding.UTF8.GetBytes(controlRecordJson);

        return new TransactionModel
        {
            TxId = $"genesis-{registerId}",
            RegisterId = registerId,
            SenderWallet = "system", // System transaction
            TimeStamp = controlRecord.CreatedAt.UtcDateTime,
            PrevTxId = string.Empty, // Genesis has no previous transaction
            Payloads = new[]
            {
                new PayloadModel
                {
                    Data = Convert.ToBase64String(controlRecordBytes),
                    WalletAccess = controlRecord.Attestations.Select(a => a.Subject).ToArray(),
                    Hash = string.Empty // Will be computed later
                }
            },
            MetaData = new TransactionMetaData
            {
                RegisterId = registerId,
                TransactionType = TransactionType.Genesis
            },
            Version = 1,
            Signature = string.Empty // Will be signed later
        };
    }

    /// <summary>
    /// Maps SignatureAlgorithm to WalletNetworks (byte) for Sorcha.Cryptography
    /// </summary>
    private byte MapAlgorithm(SignatureAlgorithm algorithm)
    {
        return algorithm switch
        {
            SignatureAlgorithm.ED25519 => (byte)Sorcha.Cryptography.Enums.WalletNetworks.ED25519,
            SignatureAlgorithm.NISTP256 => (byte)Sorcha.Cryptography.Enums.WalletNetworks.NISTP256,
            SignatureAlgorithm.RSA4096 => (byte)Sorcha.Cryptography.Enums.WalletNetworks.RSA4096,
            _ => throw new ArgumentException($"Unsupported signature algorithm: {algorithm}")
        };
    }

    /// <summary>
    /// Generates a cryptographic nonce for replay protection
    /// </summary>
    private string GenerateNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Cleans up expired pending registrations
    /// </summary>
    private async Task CleanupExpiredPendingRegistrationsAsync()
    {
        await Task.Delay(TimeSpan.FromMinutes(1)); // Run every minute

        var expired = _pendingRegistrations
            .Where(kvp => kvp.Value.IsExpired())
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var registerId in expired)
        {
            if (_pendingRegistrations.TryRemove(registerId, out _))
            {
                _logger.LogDebug("Removed expired pending registration {RegisterId}", registerId);
            }
        }

        if (expired.Any())
        {
            _logger.LogInformation("Cleaned up {Count} expired pending registrations", expired.Count);
        }
    }
}

/// <summary>
/// Interface for register creation orchestration
/// </summary>
public interface IRegisterCreationOrchestrator
{
    /// <summary>
    /// Initiates register creation (Phase 1): generates unsigned control record
    /// </summary>
    Task<InitiateRegisterCreationResponse> InitiateAsync(
        InitiateRegisterCreationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes register creation (Phase 2): verifies signatures and creates register
    /// </summary>
    Task<FinalizeRegisterCreationResponse> FinalizeAsync(
        FinalizeRegisterCreationRequest request,
        CancellationToken cancellationToken = default);
}
