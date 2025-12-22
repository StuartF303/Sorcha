// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.Cryptography.Utilities;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Builds dockets from pending transactions with hybrid triggering
/// </summary>
public class DocketBuilder : IDocketBuilder
{
    private readonly IMemPoolManager _memPoolManager;
    private readonly IRegisterServiceClient _registerClient;
    private readonly IWalletServiceClient _walletClient;
    private readonly IGenesisManager _genesisManager;
    private readonly MerkleTree _merkleTree;
    private readonly DocketHasher _docketHasher;
    private readonly ValidatorConfiguration _validatorConfig;
    private readonly DocketBuildConfiguration _buildConfig;
    private readonly ILogger<DocketBuilder> _logger;

    public DocketBuilder(
        IMemPoolManager memPoolManager,
        IRegisterServiceClient registerClient,
        IWalletServiceClient walletClient,
        IGenesisManager genesisManager,
        MerkleTree merkleTree,
        DocketHasher docketHasher,
        IOptions<ValidatorConfiguration> validatorConfig,
        IOptions<DocketBuildConfiguration> buildConfig,
        ILogger<DocketBuilder> logger)
    {
        _memPoolManager = memPoolManager ?? throw new ArgumentNullException(nameof(memPoolManager));
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _genesisManager = genesisManager ?? throw new ArgumentNullException(nameof(genesisManager));
        _merkleTree = merkleTree ?? throw new ArgumentNullException(nameof(merkleTree));
        _docketHasher = docketHasher ?? throw new ArgumentNullException(nameof(docketHasher));
        _validatorConfig = validatorConfig?.Value ?? throw new ArgumentNullException(nameof(validatorConfig));
        _buildConfig = buildConfig?.Value ?? throw new ArgumentNullException(nameof(buildConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a docket from pending transactions
    /// </summary>
    public async Task<Docket?> BuildDocketAsync(
        string registerId,
        bool forceBuild = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building docket for register {RegisterId} (forced: {ForceBuild})",
            registerId, forceBuild);

        try
        {
            // Check if register needs genesis docket
            var needsGenesis = await _genesisManager.NeedsGenesisDocketAsync(registerId, cancellationToken);
            if (needsGenesis)
            {
                _logger.LogInformation("Register {RegisterId} needs genesis docket", registerId);
                var transactions = await _memPoolManager.GetPendingTransactionsAsync(
                    registerId,
                    _buildConfig.MaxTransactionsPerDocket,
                    cancellationToken);

                return await _genesisManager.CreateGenesisDocketAsync(registerId, transactions, cancellationToken);
            }

            // Get pending transactions from memory pool
            var pendingTransactions = await _memPoolManager.GetPendingTransactionsAsync(
                registerId,
                _buildConfig.MaxTransactionsPerDocket,
                cancellationToken);

            // Check if we have transactions to build
            if (pendingTransactions.Count == 0)
            {
                if (!_buildConfig.AllowEmptyDockets)
                {
                    _logger.LogDebug("No pending transactions for register {RegisterId} and empty dockets not allowed",
                        registerId);
                    return null;
                }

                _logger.LogWarning("Building empty docket for register {RegisterId}", registerId);
            }

            // Get previous docket info
            var latestDocket = await _registerClient.ReadLatestDocketAsync(registerId, cancellationToken);
            var docketNumber = (latestDocket?.DocketNumber ?? -1) + 1;
            var previousHash = latestDocket?.DocketHash;

            _logger.LogInformation("Building docket {DocketNumber} for register {RegisterId} with {TransactionCount} transactions",
                docketNumber, registerId, pendingTransactions.Count);

            // Compute Merkle root
            string merkleRoot;
            if (pendingTransactions.Count == 0)
            {
                merkleRoot = _merkleTree.ComputeMerkleRoot(new List<string>());
            }
            else
            {
                var txHashes = pendingTransactions.Select(tx =>
                    _docketHasher.ComputeTransactionHash(tx.TransactionId, tx.PayloadHash, tx.CreatedAt)
                ).ToList();

                merkleRoot = _merkleTree.ComputeMerkleRoot(txHashes);
                _logger.LogDebug("Computed Merkle root: {MerkleRoot}", merkleRoot);
            }

            var createdAt = DateTimeOffset.UtcNow;

            // Compute docket hash
            var docketHash = _docketHasher.ComputeDocketHash(
                registerId,
                docketNumber,
                previousHash,
                merkleRoot,
                createdAt);

            // Sign docket with system wallet
            var systemWalletId = _validatorConfig.SystemWalletId
                ?? await _walletClient.CreateOrRetrieveSystemWalletAsync(_validatorConfig.ValidatorId, cancellationToken);

            var signature = await _walletClient.SignDataAsync(systemWalletId, docketHash, cancellationToken);

            // Create docket
            var docket = new Docket
            {
                DocketId = docketHash,
                RegisterId = registerId,
                DocketNumber = docketNumber,
                PreviousHash = previousHash,
                DocketHash = docketHash,
                CreatedAt = createdAt,
                Transactions = pendingTransactions,
                Status = DocketStatus.Proposed,
                ProposerValidatorId = _validatorConfig.ValidatorId,
                ProposerSignature = new Signature
                {
                    PublicKey = systemWalletId,
                    SignatureValue = signature,
                    Algorithm = "ED25519" // TODO: Get from wallet service
                },
                MerkleRoot = merkleRoot
            };

            _logger.LogInformation("Built docket {DocketNumber} for register {RegisterId} with hash {DocketHash}",
                docketNumber, registerId, docketHash);

            return docket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build docket for register {RegisterId}", registerId);
            return null;
        }
    }

    /// <summary>
    /// Checks if a register should build a docket based on hybrid triggers
    /// </summary>
    public async Task<bool> ShouldBuildDocketAsync(
        string registerId,
        DateTimeOffset lastBuildTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check time threshold (hybrid trigger 1)
            var timeSinceLastBuild = DateTimeOffset.UtcNow - lastBuildTime;
            if (timeSinceLastBuild >= _buildConfig.TimeThreshold)
            {
                _logger.LogDebug("Time threshold met for register {RegisterId} ({TimeSinceLastBuild} >= {TimeThreshold})",
                    registerId, timeSinceLastBuild, _buildConfig.TimeThreshold);
                return true;
            }

            // Check size threshold (hybrid trigger 2)
            var transactionCount = await _memPoolManager.GetTransactionCountAsync(registerId, cancellationToken);
            if (transactionCount >= _buildConfig.SizeThreshold)
            {
                _logger.LogDebug("Size threshold met for register {RegisterId} ({TransactionCount} >= {SizeThreshold})",
                    registerId, transactionCount, _buildConfig.SizeThreshold);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if register {RegisterId} should build docket", registerId);
            return false;
        }
    }
}
