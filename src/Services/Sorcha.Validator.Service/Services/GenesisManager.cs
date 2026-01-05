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
/// Manages genesis docket creation for new registers
/// </summary>
public class GenesisManager : IGenesisManager
{
    private readonly IRegisterServiceClient _registerClient;
    private readonly IWalletServiceClient _walletClient;
    private readonly MerkleTree _merkleTree;
    private readonly DocketHasher _docketHasher;
    private readonly ValidatorConfiguration _config;
    private readonly ILogger<GenesisManager> _logger;

    public GenesisManager(
        IRegisterServiceClient registerClient,
        IWalletServiceClient walletClient,
        MerkleTree merkleTree,
        DocketHasher docketHasher,
        IOptions<ValidatorConfiguration> config,
        ILogger<GenesisManager> logger)
    {
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _merkleTree = merkleTree ?? throw new ArgumentNullException(nameof(merkleTree));
        _docketHasher = docketHasher ?? throw new ArgumentNullException(nameof(docketHasher));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a genesis docket (first docket in a register's chain)
    /// </summary>
    public async Task<Docket> CreateGenesisDocketAsync(
        string registerId,
        List<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating genesis docket for register {RegisterId} with {TransactionCount} transactions",
            registerId, transactions.Count);

        try
        {
            var createdAt = DateTimeOffset.UtcNow;

            // Compute Merkle root
            string merkleRoot;
            if (transactions.Count == 0)
            {
                // Empty Merkle root
                merkleRoot = _merkleTree.ComputeMerkleRoot(new List<string>());
                _logger.LogInformation("Genesis docket for register {RegisterId} has no transactions", registerId);
            }
            else
            {
                // Hash each transaction
                var txHashes = transactions.Select(tx =>
                    _docketHasher.ComputeTransactionHash(tx.TransactionId, tx.PayloadHash, tx.CreatedAt)
                ).ToList();

                merkleRoot = _merkleTree.ComputeMerkleRoot(txHashes);
                _logger.LogDebug("Computed Merkle root for {Count} transactions: {MerkleRoot}",
                    transactions.Count, merkleRoot);
            }

            // Compute docket hash (genesis has null previous hash)
            var docketHash = _docketHasher.ComputeDocketHash(
                registerId,
                docketNumber: 0,
                previousHash: null,
                merkleRoot,
                createdAt);

            // Sign docket with system wallet
            var systemWalletAddress = _config.SystemWalletAddress;

            var signature = await _walletClient.SignDataAsync(systemWalletAddress, docketHash, cancellationToken);

            // TODO: Replace with proper wallet integration using IWalletIntegrationService
            // For now, convert string representations to byte arrays
            var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(systemWalletAddress);
            var signatureBytes = System.Text.Encoding.UTF8.GetBytes(signature);

            // Create genesis docket
            var genesisDocket = new Docket
            {
                DocketId = docketHash,
                RegisterId = registerId,
                DocketNumber = 0,
                PreviousHash = null, // Genesis docket has no previous
                DocketHash = docketHash,
                CreatedAt = createdAt,
                Transactions = transactions,
                Status = DocketStatus.Proposed,
                ProposerValidatorId = _config.ValidatorId,
                ProposerSignature = new Signature
                {
                    PublicKey = publicKeyBytes,
                    SignatureValue = signatureBytes,
                    Algorithm = "ED25519", // TODO: Get from wallet service
                    SignedAt = createdAt
                },
                MerkleRoot = merkleRoot
            };

            _logger.LogInformation("Created genesis docket for register {RegisterId} with hash {DocketHash}",
                registerId, docketHash);

            return genesisDocket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create genesis docket for register {RegisterId}", registerId);
            throw;
        }
    }

    /// <summary>
    /// Checks if a register needs a genesis docket (height = 0)
    /// </summary>
    public async Task<bool> NeedsGenesisDocketAsync(string registerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var height = await _registerClient.GetRegisterHeightAsync(registerId, cancellationToken);
            var needsGenesis = height == 0;

            if (needsGenesis)
            {
                _logger.LogInformation("Register {RegisterId} needs genesis docket (height = 0)", registerId);
            }

            return needsGenesis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if register {RegisterId} needs genesis docket", registerId);
            return false;
        }
    }
}
