// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Json.Schema;
using Microsoft.Extensions.Options;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Register.Models.Constants;
using Sorcha.ServiceClients.Register;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services.Interfaces;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Core validation engine that validates transactions against blueprint rules,
/// cryptographic requirements, and chain integrity.
/// </summary>
public class ValidationEngine : IValidationEngine
{
    private readonly ValidationEngineConfiguration _config;
    private readonly IBlueprintCache _blueprintCache;
    private readonly IHashProvider _hashProvider;
    private readonly ICryptoModule _cryptoModule;
    private readonly IWalletUtilities _walletUtilities;
    private readonly IRegisterServiceClient _registerClient;
    private readonly IRightsEnforcementService _rightsEnforcementService;
    private readonly ILogger<ValidationEngine> _logger;

    // Statistics
    private long _totalValidated;
    private long _totalSuccessful;
    private long _totalFailed;
    private int _inProgress;
    private readonly ConcurrentDictionary<ValidationErrorCategory, long> _errorsByCategory = new();
    private readonly ConcurrentQueue<double> _durations = new();
    private readonly object _statsLock = new();

    public ValidationEngine(
        IOptions<ValidationEngineConfiguration> config,
        IBlueprintCache blueprintCache,
        IHashProvider hashProvider,
        ICryptoModule cryptoModule,
        IWalletUtilities walletUtilities,
        IRegisterServiceClient registerClient,
        IRightsEnforcementService rightsEnforcementService,
        ILogger<ValidationEngine> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _blueprintCache = blueprintCache ?? throw new ArgumentNullException(nameof(blueprintCache));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _walletUtilities = walletUtilities ?? throw new ArgumentNullException(nameof(walletUtilities));
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _rightsEnforcementService = rightsEnforcementService ?? throw new ArgumentNullException(nameof(rightsEnforcementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ValidationEngineResult> ValidateTransactionAsync(
        Transaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var sw = Stopwatch.StartNew();
        Interlocked.Increment(ref _inProgress);

        try
        {
            var errors = new List<ValidationEngineError>();

            // 1. Validate structure
            var structureResult = ValidateStructure(transaction);
            if (!structureResult.IsValid)
            {
                // Structure errors are fatal - can't continue
                return RecordResult(structureResult, sw.Elapsed);
            }

            // 2. Validate payload hash
            var hashResult = ValidatePayloadHash(transaction);
            if (!hashResult.IsValid)
            {
                errors.AddRange(hashResult.Errors);
                // Hash mismatch is fatal
                return RecordResult(CreateFailureResult(transaction, sw.Elapsed, errors), sw.Elapsed);
            }

            // 3. Validate schema (if enabled)
            if (_config.EnableSchemaValidation)
            {
                var schemaResult = await ValidateSchemaAsync(transaction, ct);
                if (!schemaResult.IsValid)
                {
                    errors.AddRange(schemaResult.Errors);
                }
            }

            // 4. Verify signatures (if enabled)
            if (_config.EnableSignatureVerification)
            {
                var sigResult = await VerifySignaturesAsync(transaction, ct);
                if (!sigResult.IsValid)
                {
                    errors.AddRange(sigResult.Errors);
                }
            }

            // 4b. Validate blueprint conformance (if enabled)
            if (_config.EnableBlueprintConformance)
            {
                var bpResult = await ValidateBlueprintConformanceAsync(transaction, ct);
                if (!bpResult.IsValid)
                {
                    errors.AddRange(bpResult.Errors);
                }
            }

            // 4c. Validate governance rights for Control transactions (if enabled)
            if (_config.EnableGovernanceValidation)
            {
                var govResult = await _rightsEnforcementService.ValidateGovernanceRightsAsync(transaction, ct);
                if (!govResult.IsValid)
                {
                    errors.AddRange(govResult.Errors);
                }
            }

            // 5. Validate chain (if enabled)
            if (_config.EnableChainValidation)
            {
                var chainResult = await ValidateChainAsync(transaction, ct);
                if (!chainResult.IsValid)
                {
                    errors.AddRange(chainResult.Errors);
                }
            }

            // 6. Validate timing
            var timingResult = ValidateTiming(transaction);
            if (!timingResult.IsValid)
            {
                errors.AddRange(timingResult.Errors);
            }

            if (errors.Count > 0)
            {
                return RecordResult(CreateFailureResult(transaction, sw.Elapsed, errors), sw.Elapsed);
            }

            var result = ValidationEngineResult.Success(
                transaction.TransactionId,
                transaction.RegisterId,
                sw.Elapsed);

            return RecordResult(result, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating transaction {TransactionId}", transaction.TransactionId);

            var result = ValidationEngineResult.Failure(
                transaction.TransactionId,
                transaction.RegisterId,
                sw.Elapsed,
                new ValidationEngineError
                {
                    Code = "VAL_INTERNAL",
                    Message = $"Internal validation error: {ex.Message}",
                    Category = ValidationErrorCategory.Internal,
                    IsFatal = true
                });

            return RecordResult(result, sw.Elapsed);
        }
        finally
        {
            Interlocked.Decrement(ref _inProgress);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ValidationEngineResult>> ValidateBatchAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (transactions.Count == 0)
            return [];

        _logger.LogDebug("Validating batch of {Count} transactions", transactions.Count);

        if (_config.EnableParallelValidation && transactions.Count > 1)
        {
            var tasks = transactions.Select(tx => ValidateTransactionAsync(tx, ct));
            var results = await Task.WhenAll(tasks);
            return results;
        }

        // Sequential validation
        var resultList = new List<ValidationEngineResult>();
        foreach (var tx in transactions)
        {
            var result = await ValidateTransactionAsync(tx, ct);
            resultList.Add(result);
        }

        return resultList;
    }

    /// <inheritdoc/>
    public ValidationEngineResult ValidateStructure(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var sw = Stopwatch.StartNew();
        var errors = new List<ValidationEngineError>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(transaction.TransactionId))
        {
            errors.Add(CreateError("VAL_STRUCT_001", "Transaction ID is required",
                ValidationErrorCategory.Structure, "TransactionId", true));
        }

        if (string.IsNullOrWhiteSpace(transaction.RegisterId))
        {
            errors.Add(CreateError("VAL_STRUCT_002", "Register ID is required",
                ValidationErrorCategory.Structure, "RegisterId", true));
        }

        if (string.IsNullOrWhiteSpace(transaction.BlueprintId))
        {
            errors.Add(CreateError("VAL_STRUCT_003", "Blueprint ID is required",
                ValidationErrorCategory.Structure, "BlueprintId", true));
        }

        if (string.IsNullOrWhiteSpace(transaction.ActionId))
        {
            errors.Add(CreateError("VAL_STRUCT_004", "Action ID is required",
                ValidationErrorCategory.Structure, "ActionId", true));
        }

        if (transaction.Payload.ValueKind == JsonValueKind.Undefined ||
            transaction.Payload.ValueKind == JsonValueKind.Null)
        {
            errors.Add(CreateError("VAL_STRUCT_005", "Payload is required",
                ValidationErrorCategory.Structure, "Payload", true));
        }

        if (string.IsNullOrWhiteSpace(transaction.PayloadHash))
        {
            errors.Add(CreateError("VAL_STRUCT_006", "Payload hash is required",
                ValidationErrorCategory.Structure, "PayloadHash", true));
        }

        if (transaction.Signatures == null || transaction.Signatures.Count == 0)
        {
            errors.Add(CreateError("VAL_STRUCT_007", "At least one signature is required",
                ValidationErrorCategory.Structure, "Signatures", true));
        }
        else
        {
            for (int i = 0; i < transaction.Signatures.Count; i++)
            {
                var sig = transaction.Signatures[i];
                if (sig.PublicKey == null || sig.PublicKey.Length == 0)
                {
                    errors.Add(CreateError("VAL_STRUCT_008",
                        $"Signature {i} is missing public key",
                        ValidationErrorCategory.Structure, $"Signatures[{i}].PublicKey"));
                }

                if (sig.SignatureValue == null || sig.SignatureValue.Length == 0)
                {
                    errors.Add(CreateError("VAL_STRUCT_009",
                        $"Signature {i} is missing signature value",
                        ValidationErrorCategory.Structure, $"Signatures[{i}].SignatureValue"));
                }

                if (string.IsNullOrWhiteSpace(sig.Algorithm))
                {
                    errors.Add(CreateError("VAL_STRUCT_010",
                        $"Signature {i} is missing algorithm",
                        ValidationErrorCategory.Structure, $"Signatures[{i}].Algorithm"));
                }
            }
        }

        if (errors.Count > 0)
        {
            return CreateFailureResult(transaction, sw.Elapsed, errors);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    /// <inheritdoc/>
    public async Task<ValidationEngineResult> ValidateSchemaAsync(
        Transaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var sw = Stopwatch.StartNew();
        var errors = new List<ValidationEngineError>();

        // Config check: skip if disabled
        if (!_config.EnableSchemaValidation)
        {
            _logger.LogDebug("Schema validation disabled by configuration");
            return ValidationEngineResult.Success(
                transaction.TransactionId,
                transaction.RegisterId,
                sw.Elapsed);
        }

        try
        {
            // Skip schema validation for genesis/control transactions
            if (IsGenesisOrControlTransaction(transaction))
            {
                _logger.LogDebug("Skipping schema validation for genesis/control transaction {TransactionId}",
                    transaction.TransactionId);
                return ValidationEngineResult.Success(
                    transaction.TransactionId,
                    transaction.RegisterId,
                    sw.Elapsed);
            }

            // Get the blueprint
            var blueprint = await _blueprintCache.GetBlueprintAsync(transaction.BlueprintId, ct);
            if (blueprint == null)
            {
                errors.Add(CreateError("VAL_SCHEMA_001",
                    $"Blueprint '{transaction.BlueprintId}' not found",
                    ValidationErrorCategory.Blueprint, "BlueprintId", true));
                return CreateFailureResult(transaction, sw.Elapsed, errors);
            }

            // Find the action
            if (!int.TryParse(transaction.ActionId, out var actionIdInt))
            {
                errors.Add(CreateError("VAL_SCHEMA_002",
                    $"Invalid action ID format: '{transaction.ActionId}'",
                    ValidationErrorCategory.Blueprint, "ActionId", true));
                return CreateFailureResult(transaction, sw.Elapsed, errors);
            }

            var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionIdInt);
            if (action == null)
            {
                errors.Add(CreateError("VAL_SCHEMA_003",
                    $"Action {transaction.ActionId} not found in blueprint '{transaction.BlueprintId}'",
                    ValidationErrorCategory.Blueprint, "ActionId", true));
                return CreateFailureResult(transaction, sw.Elapsed, errors);
            }

            // Skip schema validation if no schemas defined (FR-006)
            if (action.DataSchemas == null || !action.DataSchemas.Any())
            {
                _logger.LogDebug(
                    "No schemas defined for action {ActionId} in blueprint {BlueprintId}, skipping schema validation",
                    transaction.ActionId, transaction.BlueprintId);
                return ValidationEngineResult.Success(
                    transaction.TransactionId,
                    transaction.RegisterId,
                    sw.Elapsed);
            }

            // Evaluate payload against all schemas (payload must pass ALL schemas)
            var evalOptions = new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true
            };

            foreach (var schemaDoc in action.DataSchemas)
            {
                JsonSchema jsonSchema;
                try
                {
                    var schemaText = schemaDoc.RootElement.GetRawText();
                    jsonSchema = JsonSchema.FromText(schemaText);
                }
                catch (Exception ex)
                {
                    errors.Add(CreateError("VAL_SCHEMA_005",
                        $"Malformed JSON schema in blueprint '{transaction.BlueprintId}' action {transaction.ActionId}: {ex.Message}",
                        ValidationErrorCategory.Blueprint, "DataSchemas", true));
                    continue;
                }

                var result = jsonSchema.Evaluate(transaction.Payload, evalOptions);

                if (!result.IsValid)
                {
                    // Collect all violations from the evaluation
                    if (result.Details != null)
                    {
                        foreach (var detail in result.Details.Where(d => !d.IsValid && d.Errors != null))
                        {
                            foreach (var error in detail.Errors!)
                            {
                                var instanceLocation = detail.InstanceLocation.ToString();
                                errors.Add(CreateError("VAL_SCHEMA_004",
                                    $"Schema violation at '{instanceLocation}': {error.Value}",
                                    ValidationErrorCategory.Schema, instanceLocation));
                            }
                        }
                    }

                    // If no details were extracted, add a generic error
                    if (errors.Count == 0)
                    {
                        errors.Add(CreateError("VAL_SCHEMA_004",
                            "Payload does not conform to the required schema",
                            ValidationErrorCategory.Schema, "Payload"));
                    }
                }
            }

            _logger.LogDebug(
                "Schema validation for transaction {TransactionId} against blueprint {BlueprintId} action {ActionId}: {ViolationCount} violations",
                transaction.TransactionId, transaction.BlueprintId, transaction.ActionId, errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating schema for transaction {TransactionId}", transaction.TransactionId);
            errors.Add(CreateError("VAL_SCHEMA_ERR",
                $"Schema validation error: {ex.Message}",
                ValidationErrorCategory.Schema, isFatal: true));
        }

        if (errors.Count > 0)
        {
            return CreateFailureResult(transaction, sw.Elapsed, errors);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    /// <inheritdoc/>
    public async Task<ValidationEngineResult> VerifySignaturesAsync(
        Transaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var sw = Stopwatch.StartNew();
        var errors = new List<ValidationEngineError>();

        try
        {
            // The data that was signed is the transaction ID + payload hash
            var signedData = $"{transaction.TransactionId}:{transaction.PayloadHash}";
            var signedHash = _hashProvider.ComputeHash(
                Encoding.UTF8.GetBytes(signedData),
                HashType.SHA256);

            foreach (var signature in transaction.Signatures)
            {
                try
                {
                    // Parse the algorithm
                    var network = ParseAlgorithmToNetwork(signature.Algorithm);
                    if (network == null)
                    {
                        errors.Add(CreateError("VAL_SIG_001",
                            $"Unsupported signature algorithm: {signature.Algorithm}",
                            ValidationErrorCategory.Cryptographic,
                            $"Signatures.Algorithm"));
                        continue;
                    }

                    // Verify the signature
                    var verifyResult = await _cryptoModule.VerifyAsync(
                        signature.SignatureValue,
                        signedHash,
                        (byte)network.Value,
                        signature.PublicKey,
                        ct);

                    if (verifyResult != CryptoStatus.Success)
                    {
                        var publicKeyHex = Convert.ToHexString(signature.PublicKey);
                        errors.Add(CreateError("VAL_SIG_002",
                            $"Invalid signature from public key {publicKeyHex[..Math.Min(20, publicKeyHex.Length)]}...",
                            ValidationErrorCategory.Cryptographic,
                            "Signatures"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to verify signature for transaction {TransactionId}",
                        transaction.TransactionId);
                    errors.Add(CreateError("VAL_SIG_003",
                        $"Signature verification failed: {ex.Message}",
                        ValidationErrorCategory.Cryptographic,
                        "Signatures"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signatures for transaction {TransactionId}",
                transaction.TransactionId);
            errors.Add(CreateError("VAL_SIG_ERR",
                $"Signature verification error: {ex.Message}",
                ValidationErrorCategory.Cryptographic, isFatal: true));
        }

        if (errors.Count > 0)
        {
            return CreateFailureResult(transaction, sw.Elapsed, errors);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    /// <inheritdoc/>
    public async Task<ValidationEngineResult> ValidateChainAsync(
        Transaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var sw = Stopwatch.StartNew();
        var errors = new List<ValidationEngineError>();

        // Config check: skip if disabled
        if (!_config.EnableChainValidation)
        {
            _logger.LogDebug("Chain validation disabled by configuration");
            return ValidationEngineResult.Success(
                transaction.TransactionId,
                transaction.RegisterId,
                sw.Elapsed);
        }

        try
        {
            // 1. Transaction-level chain validation
            var previousTxId = transaction.PreviousTransactionId;
            if (!string.IsNullOrWhiteSpace(previousTxId))
            {
                var previousTx = await _registerClient.GetTransactionAsync(
                    transaction.RegisterId, previousTxId, ct);

                if (previousTx == null)
                {
                    errors.Add(CreateError("VAL_CHAIN_001",
                        $"Previous transaction '{previousTxId}' not found in register '{transaction.RegisterId}'",
                        ValidationErrorCategory.Chain, "PreviousTransactionId"));
                }
                else if (!string.Equals(previousTx.RegisterId, transaction.RegisterId, StringComparison.Ordinal))
                {
                    errors.Add(CreateError("VAL_CHAIN_002",
                        $"Previous transaction '{previousTxId}' belongs to register '{previousTx.RegisterId}', expected '{transaction.RegisterId}'",
                        ValidationErrorCategory.Chain, "PreviousTransactionId"));
                }

                // 3. Fork detection — check if other transactions already reference the same predecessor
                var existingSuccessors = await _registerClient.GetTransactionsByPrevTxIdAsync(
                    transaction.RegisterId, previousTxId, 1, 1, ct);

                if (existingSuccessors.Total > 0)
                {
                    errors.Add(CreateError("VAL_CHAIN_FORK",
                        $"Fork detected: {existingSuccessors.Total} existing transaction(s) already reference previous transaction '{previousTxId}' in register '{transaction.RegisterId}'",
                        ValidationErrorCategory.Chain, "PreviousTransactionId"));
                }
            }

            // 2. Docket-level chain validation
            var height = await _registerClient.GetRegisterHeightAsync(transaction.RegisterId, ct);

            if (height > 0)
            {
                var latestDocket = await _registerClient.ReadDocketAsync(
                    transaction.RegisterId, height, ct);

                if (latestDocket != null && height > 1)
                {
                    var predecessorDocket = await _registerClient.ReadDocketAsync(
                        transaction.RegisterId, height - 1, ct);

                    if (predecessorDocket == null)
                    {
                        errors.Add(CreateError("VAL_CHAIN_003",
                            $"Docket gap detected: docket {height - 1} not found in register '{transaction.RegisterId}'",
                            ValidationErrorCategory.Chain, "DocketNumber"));
                    }
                    else
                    {
                        // Verify hash linkage
                        if (!string.Equals(latestDocket.PreviousHash, predecessorDocket.DocketHash, StringComparison.Ordinal))
                        {
                            errors.Add(CreateError("VAL_CHAIN_004",
                                $"Docket hash chain broken: docket {height} PreviousHash does not match docket {height - 1} DocketHash",
                                ValidationErrorCategory.Chain, "DocketHash"));
                        }

                        // Verify sequential numbering
                        if (latestDocket.DocketNumber != predecessorDocket.DocketNumber + 1)
                        {
                            errors.Add(CreateError("VAL_CHAIN_003",
                                $"Docket numbering gap: expected {predecessorDocket.DocketNumber + 1}, found {latestDocket.DocketNumber}",
                                ValidationErrorCategory.Chain, "DocketNumber"));
                        }
                    }
                }
            }

            _logger.LogDebug(
                "Chain validation for transaction {TransactionId} in register {RegisterId}: {ErrorCount} errors",
                transaction.TransactionId, transaction.RegisterId, errors.Count);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Register Service unavailable during chain validation for transaction {TransactionId}",
                transaction.TransactionId);
            errors.Add(CreateError("VAL_CHAIN_TRANSIENT",
                $"Register Service unavailable: {ex.Message}",
                ValidationErrorCategory.Chain, isFatal: false));
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Register Service timed out during chain validation for transaction {TransactionId}",
                transaction.TransactionId);
            errors.Add(CreateError("VAL_CHAIN_TRANSIENT",
                $"Register Service timed out: {ex.Message}",
                ValidationErrorCategory.Chain, isFatal: false));
        }

        if (errors.Count > 0)
        {
            return CreateFailureResult(transaction, sw.Elapsed, errors);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    /// <inheritdoc/>
    public ValidationEngineStats GetStats()
    {
        double avgDuration = 0;
        lock (_statsLock)
        {
            if (_durations.Count > 0)
            {
                avgDuration = _durations.Average();
            }
        }

        return new ValidationEngineStats
        {
            TotalValidated = Interlocked.Read(ref _totalValidated),
            TotalSuccessful = Interlocked.Read(ref _totalSuccessful),
            TotalFailed = Interlocked.Read(ref _totalFailed),
            AverageValidationDuration = TimeSpan.FromMilliseconds(avgDuration),
            ErrorsByCategory = new Dictionary<ValidationErrorCategory, long>(_errorsByCategory),
            InProgress = _inProgress
        };
    }

    /// <inheritdoc/>
    public async Task<ValidationEngineResult> ValidateBlueprintConformanceAsync(
        Transaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var sw = Stopwatch.StartNew();
        var errors = new List<ValidationEngineError>();

        // Skip for genesis/control transactions
        if (IsGenesisOrControlTransaction(transaction))
        {
            return ValidationEngineResult.Success(
                transaction.TransactionId,
                transaction.RegisterId,
                sw.Elapsed);
        }

        try
        {
            // Blueprint + action lookup (reuse logic from ValidateSchemaAsync)
            var blueprint = await _blueprintCache.GetBlueprintAsync(transaction.BlueprintId, ct);
            if (blueprint == null)
            {
                errors.Add(CreateError("VAL_SCHEMA_001",
                    $"Blueprint '{transaction.BlueprintId}' not found",
                    ValidationErrorCategory.Blueprint, "BlueprintId", true));
                return CreateFailureResult(transaction, sw.Elapsed, errors);
            }

            if (!int.TryParse(transaction.ActionId, out var actionIdInt))
            {
                errors.Add(CreateError("VAL_SCHEMA_002",
                    $"Invalid action ID format: '{transaction.ActionId}'",
                    ValidationErrorCategory.Blueprint, "ActionId", true));
                return CreateFailureResult(transaction, sw.Elapsed, errors);
            }

            var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionIdInt);
            if (action == null)
            {
                errors.Add(CreateError("VAL_SCHEMA_003",
                    $"Action {transaction.ActionId} not found in blueprint '{transaction.BlueprintId}'",
                    ValidationErrorCategory.Blueprint, "ActionId", true));
                return CreateFailureResult(transaction, sw.Elapsed, errors);
            }

            // 1. Starting action validation
            if (string.IsNullOrWhiteSpace(transaction.PreviousTransactionId))
            {
                if (!action.IsStartingAction)
                {
                    errors.Add(CreateError("VAL_BP_001",
                        $"Action {actionIdInt} is not a starting action but has no previous transaction",
                        ValidationErrorCategory.Blueprint, "ActionId"));
                }
            }

            // 2. Sender authorization — derive wallet from signature and compare to participant
            if (transaction.Signatures.Count > 0)
            {
                var firstSig = transaction.Signatures[0];
                var network = ParseAlgorithmToNetwork(firstSig.Algorithm);

                if (network != null)
                {
                    var derivedWallet = _walletUtilities.PublicKeyToWallet(firstSig.PublicKey, (byte)network.Value);
                    var participant = blueprint.Participants.FirstOrDefault(p =>
                        string.Equals(p.Id, action.Sender, StringComparison.OrdinalIgnoreCase));

                    if (participant != null && !string.IsNullOrWhiteSpace(participant.WalletAddress))
                    {
                        if (!string.Equals(derivedWallet, participant.WalletAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(CreateError("VAL_BP_002",
                                $"Signer wallet {derivedWallet} does not match authorized sender {participant.WalletAddress} for action {actionIdInt}",
                                ValidationErrorCategory.Permission, "Signatures"));
                        }
                    }
                    else if (participant != null && string.IsNullOrWhiteSpace(participant.WalletAddress))
                    {
                        _logger.LogDebug(
                            "Participant {ParticipantId} has no wallet address set, skipping sender authorization for action {ActionId}",
                            action.Sender, actionIdInt);
                    }
                }
            }

            // 3. Action sequencing — if PreviousTransactionId is set, validate route reachability
            if (!string.IsNullOrWhiteSpace(transaction.PreviousTransactionId))
            {
                var previousTx = await _registerClient.GetTransactionAsync(
                    transaction.RegisterId, transaction.PreviousTransactionId, ct);

                if (previousTx?.MetaData?.ActionId != null)
                {
                    var previousActionId = (int)previousTx.MetaData.ActionId.Value;
                    var previousAction = blueprint.Actions.FirstOrDefault(a => a.Id == previousActionId);

                    if (previousAction != null)
                    {
                        var routes = previousAction.Routes?.ToList();
                        if (routes != null && routes.Count > 0)
                        {
                            var reachableActionIds = routes
                                .SelectMany(r => r.NextActionIds)
                                .ToHashSet();

                            // Also check rejection routing
                            if (previousAction.RejectionConfig != null)
                            {
                                reachableActionIds.Add(previousAction.RejectionConfig.TargetActionId);
                            }

                            if (!reachableActionIds.Contains(actionIdInt))
                            {
                                errors.Add(CreateError("VAL_BP_003",
                                    $"Action {actionIdInt} is not reachable from action {previousActionId} via blueprint routes",
                                    ValidationErrorCategory.Blueprint, "ActionId"));
                            }
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Previous action {PreviousActionId} has no routes defined, skipping sequence check for action {ActionId}",
                                previousActionId, actionIdInt);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Previous transaction {PrevTxId} missing ActionId in metadata, skipping sequence check",
                        transaction.PreviousTransactionId);
                }
            }

            _logger.LogDebug(
                "Blueprint conformance validation for transaction {TransactionId}: {ErrorCount} errors",
                transaction.TransactionId, errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating blueprint conformance for transaction {TransactionId}",
                transaction.TransactionId);
            errors.Add(CreateError("VAL_BP_ERR",
                $"Blueprint conformance validation error: {ex.Message}",
                ValidationErrorCategory.Blueprint, isFatal: false));
        }

        if (errors.Count > 0)
        {
            return CreateFailureResult(transaction, sw.Elapsed, errors);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    #region Private Methods

    private static bool IsGenesisOrControlTransaction(Transaction transaction)
    {
        if (string.Equals(transaction.BlueprintId, GenesisConstants.BlueprintId, StringComparison.OrdinalIgnoreCase))
            return true;

        if (transaction.Metadata.TryGetValue("Type", out var typeStr) &&
            (string.Equals(typeStr, "Genesis", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(typeStr, "Control", StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private ValidationEngineResult ValidatePayloadHash(Transaction transaction)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var payloadJson = transaction.Payload.GetRawText();
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var computedHash = _hashProvider.ComputeHash(payloadBytes, HashType.SHA256);
            var computedHashHex = Convert.ToHexString(computedHash).ToLowerInvariant();

            if (!string.Equals(computedHashHex, transaction.PayloadHash, StringComparison.OrdinalIgnoreCase))
            {
                return CreateFailureResult(transaction, sw.Elapsed,
                [
                    CreateError("VAL_HASH_001",
                        $"Payload hash mismatch. Expected: {transaction.PayloadHash}, Computed: {computedHashHex}",
                        ValidationErrorCategory.Cryptographic, "PayloadHash", true)
                ]);
            }
        }
        catch (Exception ex)
        {
            return CreateFailureResult(transaction, sw.Elapsed,
            [
                CreateError("VAL_HASH_ERR",
                    $"Error computing payload hash: {ex.Message}",
                    ValidationErrorCategory.Cryptographic, "PayloadHash", true)
            ]);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    private ValidationEngineResult ValidateTiming(Transaction transaction)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<ValidationEngineError>();
        var now = DateTimeOffset.UtcNow;

        // Check for future timestamps
        if (transaction.CreatedAt > now.Add(_config.MaxClockSkew))
        {
            errors.Add(CreateError("VAL_TIME_001",
                "Transaction timestamp is in the future",
                ValidationErrorCategory.Timing, "CreatedAt"));
        }

        // Check for expired transactions
        if (transaction.CreatedAt < now.Subtract(_config.MaxTransactionAge))
        {
            errors.Add(CreateError("VAL_TIME_002",
                $"Transaction is too old (max age: {_config.MaxTransactionAge})",
                ValidationErrorCategory.Timing, "CreatedAt"));
        }

        // Check explicit expiration
        if (transaction.ExpiresAt.HasValue && transaction.ExpiresAt.Value <= now)
        {
            errors.Add(CreateError("VAL_TIME_003",
                "Transaction has expired",
                ValidationErrorCategory.Timing, "ExpiresAt"));
        }

        if (errors.Count > 0)
        {
            return CreateFailureResult(transaction, sw.Elapsed, errors);
        }

        return ValidationEngineResult.Success(
            transaction.TransactionId,
            transaction.RegisterId,
            sw.Elapsed);
    }

    private static WalletNetworks? ParseAlgorithmToNetwork(string algorithm)
    {
        return algorithm?.ToUpperInvariant() switch
        {
            "ED25519" => WalletNetworks.ED25519,
            "NIST-P256" or "NISTP256" or "P256" or "ECDSA-P256" => WalletNetworks.NISTP256,
            "RSA-4096" or "RSA4096" => WalletNetworks.RSA4096,
            _ => null
        };
    }

    private static ValidationEngineError CreateError(
        string code,
        string message,
        ValidationErrorCategory category,
        string? field = null,
        bool isFatal = false)
    {
        return new ValidationEngineError
        {
            Code = code,
            Message = message,
            Category = category,
            Field = field,
            IsFatal = isFatal
        };
    }

    private static ValidationEngineResult CreateFailureResult(
        Transaction transaction,
        TimeSpan duration,
        List<ValidationEngineError> errors)
    {
        return ValidationEngineResult.Failure(
            transaction.TransactionId,
            transaction.RegisterId,
            duration,
            errors.ToArray());
    }

    private ValidationEngineResult RecordResult(ValidationEngineResult result, TimeSpan duration)
    {
        Interlocked.Increment(ref _totalValidated);

        if (result.IsValid)
        {
            Interlocked.Increment(ref _totalSuccessful);
        }
        else
        {
            Interlocked.Increment(ref _totalFailed);

            // Track error categories
            foreach (var error in result.Errors)
            {
                _errorsByCategory.AddOrUpdate(error.Category, 1, (_, count) => count + 1);
            }
        }

        // Track duration (keep last 1000)
        lock (_statsLock)
        {
            _durations.Enqueue(duration.TotalMilliseconds);
            while (_durations.Count > 1000)
            {
                _durations.TryDequeue(out _);
            }
        }

        return result;
    }

    #endregion
}
