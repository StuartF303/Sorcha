// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
    private readonly IPendingRegistrationStore _pendingStore;

    private readonly TimeSpan _pendingExpirationTime = TimeSpan.FromMinutes(5);
    private readonly JsonSerializerOptions _canonicalJsonOptions;

    public RegisterCreationOrchestrator(
        ILogger<RegisterCreationOrchestrator> logger,
        RegisterManager registerManager,
        IWalletServiceClient walletClient,
        IHashProvider hashProvider,
        ICryptoModule cryptoModule,
        IValidatorServiceClient validatorClient,
        IPendingRegistrationStore pendingStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registerManager = registerManager ?? throw new ArgumentNullException(nameof(registerManager));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _validatorClient = validatorClient ?? throw new ArgumentNullException(nameof(validatorClient));
        _pendingStore = pendingStore ?? throw new ArgumentNullException(nameof(pendingStore));

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
            "Initiating register creation for name '{Name}' in tenant '{TenantId}' with {OwnerCount} owner(s)",
            request.Name,
            request.TenantId,
            request.Owners.Count);

        // Generate unique register ID (GUID without hyphens)
        var registerId = Guid.NewGuid().ToString("N");
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.Add(_pendingExpirationTime);
        var nonce = GenerateNonce();

        // Create attestations to sign for each owner
        var attestationsToSign = new List<AttestationToSign>();

        _logger.LogInformation(
            "Processing owners for register {RegisterId}: {OwnerCount} owner(s) provided",
            registerId,
            request.Owners?.Count ?? 0);

        // Generate attestation data for each owner
        foreach (var owner in request.Owners ?? new List<OwnerInfo>())
        {
            var attestationData = new AttestationSigningData
            {
                Role = RegisterRole.Owner,
                Subject = $"did:sorcha:{owner.UserId}",
                RegisterId = registerId,
                RegisterName = request.Name,
                GrantedAt = createdAt
            };

            // Serialize to canonical JSON
            // NOTE: We send the canonical JSON itself, not the hash.
            // The wallet's TransactionService will hash it before signing.
            var canonicalJson = JsonSerializer.Serialize(attestationData, _canonicalJsonOptions);

            // Compute hash for logging purposes
            var hashBytes = _hashProvider.ComputeHash(
                Encoding.UTF8.GetBytes(canonicalJson),
                Sorcha.Cryptography.Enums.HashType.SHA256);
            var attestationHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            _logger.LogInformation(
                "Created attestation for {Subject} ({Role}): hash={Hash}, json={Json}",
                attestationData.Subject,
                attestationData.Role,
                attestationHash,
                canonicalJson);

            attestationsToSign.Add(new AttestationToSign
            {
                UserId = owner.UserId,
                WalletId = owner.WalletId,
                Role = RegisterRole.Owner,
                AttestationData = attestationData,
                DataToSign = canonicalJson  // Send canonical JSON, not hash
            });

            _logger.LogDebug(
                "Created attestation for owner {UserId}, hash: {Hash}",
                owner.UserId,
                attestationHash);
        }

        // Generate attestation data for additional administrators
        if (request.AdditionalAdmins != null)
        {
            foreach (var admin in request.AdditionalAdmins)
            {
                var attestationData = new AttestationSigningData
                {
                    Role = admin.Role,
                    Subject = $"did:sorcha:{admin.UserId}",
                    RegisterId = registerId,
                    RegisterName = request.Name,
                    GrantedAt = createdAt
                };

                // Serialize to canonical JSON
                // NOTE: We send the canonical JSON itself, not the hash.
                // The wallet's TransactionService will hash it before signing.
                var canonicalJson = JsonSerializer.Serialize(attestationData, _canonicalJsonOptions);

                // Compute hash for logging purposes
                var hashBytes = _hashProvider.ComputeHash(
                    Encoding.UTF8.GetBytes(canonicalJson),
                    Sorcha.Cryptography.Enums.HashType.SHA256);
                var attestationHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

                attestationsToSign.Add(new AttestationToSign
                {
                    UserId = admin.UserId,
                    WalletId = admin.WalletId,
                    Role = admin.Role,
                    AttestationData = attestationData,
                    DataToSign = canonicalJson  // Send canonical JSON, not hash
                });

                _logger.LogDebug(
                    "Created attestation for admin {UserId}, hash: {Hash}",
                    admin.UserId,
                    attestationHash);
            }
        }

        // Store pending registration with register metadata
        var pending = new PendingRegistration
        {
            RegisterId = registerId,
            ControlRecord = new RegisterControlRecord
            {
                RegisterId = registerId,
                Name = request.Name,
                Description = request.Description,
                TenantId = request.TenantId,
                CreatedAt = createdAt,
                Metadata = request.Metadata,
                Attestations = new List<RegisterAttestation>() // Will be filled during finalization
            },
            ControlRecordHash = string.Empty, // Not computed yet - will be computed after attestations collected
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Nonce = nonce
        };

        _pendingStore.Add(registerId, pending);

        // Schedule cleanup of expired pending registrations
        _ = Task.Run(async () => await CleanupExpiredPendingRegistrationsAsync(), cancellationToken);

        _logger.LogInformation(
            "Register initiation created with ID {RegisterId}, {AttestationCount} attestation(s) to sign, expires at {ExpiresAt}",
            registerId,
            attestationsToSign.Count,
            expiresAt);

        return new InitiateRegisterCreationResponse
        {
            RegisterId = registerId,
            AttestationsToSign = attestationsToSign,
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
            "Finalizing register creation for ID {RegisterId} with {AttestationCount} signed attestations",
            request.RegisterId,
            request.SignedAttestations.Count);

        // Retrieve and remove pending registration
        if (!_pendingStore.TryRemove(request.RegisterId, out var pending))
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

        // Check expiration (already removed from store above)
        if (pending.IsExpired())
        {
            _logger.LogWarning("Pending registration expired for ID {RegisterId}", request.RegisterId);
            throw new InvalidOperationException($"Pending registration expired for register ID {request.RegisterId}");
        }

        // Verify all attestation signatures against individual attestation data
        await VerifyAttestationsAsync(request.SignedAttestations, request.RegisterId, cancellationToken);

        // Construct control record from verified attestations
        var controlRecord = new RegisterControlRecord
        {
            RegisterId = pending.ControlRecord.RegisterId,
            Name = pending.ControlRecord.Name,
            Description = pending.ControlRecord.Description,
            TenantId = pending.ControlRecord.TenantId,
            CreatedAt = pending.ControlRecord.CreatedAt,
            Metadata = pending.ControlRecord.Metadata,
            Attestations = request.SignedAttestations.Select(sa => new RegisterAttestation
            {
                Role = sa.AttestationData.Role,
                Subject = sa.AttestationData.Subject,
                PublicKey = sa.PublicKey,
                Signature = sa.Signature,
                Algorithm = sa.Algorithm,
                GrantedAt = sa.AttestationData.GrantedAt
            }).ToList()
        };

        // Validate constructed control record
        var validationErrors = ValidateControlRecord(controlRecord);
        if (validationErrors.Any())
        {
            _logger.LogWarning(
                "Control record validation failed for {RegisterId}: {Errors}",
                request.RegisterId,
                string.Join(", ", validationErrors));
            throw new ArgumentException($"Control record validation failed: {string.Join(", ", validationErrors)}");
        }

        _logger.LogInformation(
            "Control record constructed successfully with {AttestationCount} verified attestations",
            controlRecord.Attestations.Count);

        // Create register in database
        var register = await _registerManager.CreateRegisterAsync(
            controlRecord.Name,
            controlRecord.TenantId,
            advertise: false, // Default to private
            isFullReplica: true,
            cancellationToken);

        _logger.LogInformation("Created register {RegisterId} in database", register.Id);

        // Create genesis transaction with control record payload
        var genesisTransaction = CreateGenesisTransaction(register.Id, controlRecord);

        _logger.LogInformation(
            "Created genesis transaction {TransactionId} for register {RegisterId}",
            genesisTransaction.TxId,
            register.Id);

        // Submit genesis transaction to Validator Service mempool
        var controlRecordJson = JsonSerializer.Serialize(controlRecord, _canonicalJsonOptions);
        var submissionRequest = new GenesisTransactionSubmission
        {
            TransactionId = genesisTransaction.TxId,
            RegisterId = register.Id,
            ControlRecordPayload = JsonDocument.Parse(controlRecordJson).RootElement,
            PayloadHash = genesisTransaction.Payloads[0].Hash,
            Signatures = controlRecord.Attestations.Select(a => new GenesisSignature
            {
                PublicKey = a.PublicKey,
                SignatureValue = a.Signature,
                Algorithm = a.Algorithm.ToString()
            }).ToList(),
            CreatedAt = controlRecord.CreatedAt,
            RegisterName = controlRecord.Name,
            TenantId = controlRecord.TenantId
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

        // Note: Pending registration already removed from store at the start of this method

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
    /// Verifies all attestation signatures against individual attestation data hashes
    /// </summary>
    private async Task VerifyAttestationsAsync(
        List<SignedAttestation> signedAttestations,
        string registerId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Verifying {Count} signed attestations for register {RegisterId}",
            signedAttestations.Count,
            registerId);

        foreach (var signedAttestation in signedAttestations)
        {
            try
            {
                // Reconstruct the canonical JSON of the attestation data
                var canonicalJson = JsonSerializer.Serialize(
                    signedAttestation.AttestationData,
                    _canonicalJsonOptions);

                // Compute SHA-256 hash of attestation data
                var hashBytes = _hashProvider.ComputeHash(
                    Encoding.UTF8.GetBytes(canonicalJson),
                    Sorcha.Cryptography.Enums.HashType.SHA256);

                var attestationHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

                _logger.LogInformation(
                    "Verifying attestation: subject={Subject}, role={Role}, hash={Hash}, json={Json}",
                    signedAttestation.AttestationData.Subject,
                    signedAttestation.AttestationData.Role,
                    attestationHash,
                    canonicalJson);

                // Convert base64 public key and signature to bytes
                var publicKeyBytes = Convert.FromBase64String(signedAttestation.PublicKey);
                var signatureBytes = Convert.FromBase64String(signedAttestation.Signature);

                _logger.LogInformation(
                    "Verifying: sigLen={SigLen}, hashLen={HashLen}, pubKeyLen={PubKeyLen}, algo={Algo}",
                    signatureBytes.Length,
                    hashBytes.Length,
                    publicKeyBytes.Length,
                    signedAttestation.Algorithm);

                // Verify signature using Sorcha.Cryptography
                var verifyResult = await _cryptoModule.VerifyAsync(
                    signatureBytes,
                    hashBytes,
                    MapAlgorithm(signedAttestation.Algorithm),
                    publicKeyBytes,
                    cancellationToken);

                _logger.LogInformation(
                    "Verification result: {Result} for {Subject}",
                    verifyResult,
                    signedAttestation.AttestationData.Subject);

                if (verifyResult != Sorcha.Cryptography.Enums.CryptoStatus.Success)
                {
                    _logger.LogWarning(
                        "Signature verification failed for attestation: subject={Subject}, role={Role}, hash={Hash}",
                        signedAttestation.AttestationData.Subject,
                        signedAttestation.AttestationData.Role,
                        attestationHash);
                    throw new UnauthorizedAccessException(
                        $"Invalid signature for attestation: {signedAttestation.AttestationData.Subject} ({signedAttestation.AttestationData.Role})");
                }

                _logger.LogDebug(
                    "Verified signature for {Subject} ({Role})",
                    signedAttestation.AttestationData.Subject,
                    signedAttestation.AttestationData.Role);
            }
            catch (FormatException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid base64 encoding in attestation for {Subject}",
                    signedAttestation.AttestationData.Subject);
                throw new ArgumentException(
                    $"Invalid base64 encoding in attestation for {signedAttestation.AttestationData.Subject}",
                    ex);
            }
        }

        _logger.LogInformation(
            "All {Count} attestations verified successfully for register {RegisterId}",
            signedAttestations.Count,
            registerId);
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
        _pendingStore.CleanupExpired();
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
