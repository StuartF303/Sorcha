using Microsoft.Extensions.Logging;
using Sorcha.Wallet.Service.Domain.Entities;
using Sorcha.Wallet.Service.Domain.Events;
using Sorcha.Wallet.Service.Domain.ValueObjects;
using Sorcha.Wallet.Service.Events.Interfaces;
using Sorcha.Wallet.Service.Repositories.Interfaces;
using Sorcha.Wallet.Service.Services.Interfaces;

namespace Sorcha.Wallet.Service.Services.Implementation;

/// <summary>
/// Main wallet service implementation that orchestrates all wallet operations.
/// </summary>
public class WalletManager : IWalletService
{
    private readonly IKeyManagementService _keyManagement;
    private readonly ITransactionService _transactionService;
    private readonly IDelegationService _delegationService;
    private readonly IWalletRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WalletManager> _logger;

    public WalletManager(
        IKeyManagementService keyManagement,
        ITransactionService transactionService,
        IDelegationService delegationService,
        IWalletRepository repository,
        IEventPublisher eventPublisher,
        ILogger<WalletManager> logger)
    {
        _keyManagement = keyManagement ?? throw new ArgumentNullException(nameof(keyManagement));
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _delegationService = delegationService ?? throw new ArgumentNullException(nameof(delegationService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<(Wallet Wallet, Mnemonic Mnemonic)> CreateWalletAsync(
        string name,
        string algorithm,
        string owner,
        string tenant,
        int wordCount = 12,
        string? passphrase = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            throw new ArgumentException("Algorithm cannot be empty", nameof(algorithm));
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner cannot be empty", nameof(owner));
        if (string.IsNullOrWhiteSpace(tenant))
            throw new ArgumentException("Tenant cannot be empty", nameof(tenant));

        try
        {
            _logger.LogInformation("Creating wallet for owner {Owner} using {Algorithm}", owner, algorithm);

            // Generate mnemonic
            var mnemonic = Mnemonic.Generate(wordCount);

            // Derive master key from mnemonic
            var masterKey = await _keyManagement.DeriveMasterKeyAsync(mnemonic, passphrase);

            // Derive first key at BIP44 path m/44'/0'/0'/0/0
            var path = DerivationPath.CreateBip44(0, 0, 0, 0);
            var (privateKey, publicKey) = await _keyManagement.DeriveKeyAtPathAsync(
                masterKey, path, algorithm);

            // Generate wallet address
            var address = await _keyManagement.GenerateAddressAsync(publicKey, algorithm);

            // Encrypt private key
            var (encryptedKey, keyId) = await _keyManagement.EncryptPrivateKeyAsync(
                privateKey, string.Empty);

            // Create wallet entity
            var wallet = new Wallet
            {
                Address = address,
                PublicKey = Convert.ToBase64String(publicKey),
                EncryptedPrivateKey = encryptedKey,
                EncryptionKeyId = keyId,
                Algorithm = algorithm,
                Owner = owner,
                Tenant = tenant,
                Name = name,
                Status = WalletStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["WordCount"] = mnemonic.WordCount.ToString(),
                    ["DerivationPath"] = path.Path
                }
            };

            // Save to repository
            await _repository.AddAsync(wallet, cancellationToken);

            // Publish event
            await _eventPublisher.PublishAsync(new WalletCreatedEvent
            {
                WalletAddress = wallet.Address,
                OccurredAt = wallet.CreatedAt,
                Owner = wallet.Owner,
                Tenant = wallet.Tenant,
                Algorithm = wallet.Algorithm,
                Name = wallet.Name
            }, cancellationToken);

            _logger.LogInformation("Created wallet {Address} for owner {Owner}", wallet.Address, owner);
            return (wallet, mnemonic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create wallet for owner {Owner}", owner);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Wallet> RecoverWalletAsync(
        Mnemonic mnemonic,
        string name,
        string algorithm,
        string owner,
        string tenant,
        string? passphrase = null,
        CancellationToken cancellationToken = default)
    {
        if (mnemonic == null)
            throw new ArgumentNullException(nameof(mnemonic));
        if (string.IsNullOrWhiteSpace(algorithm))
            throw new ArgumentException("Algorithm cannot be empty", nameof(algorithm));
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner cannot be empty", nameof(owner));
        if (string.IsNullOrWhiteSpace(tenant))
            throw new ArgumentException("Tenant cannot be empty", nameof(tenant));

        try
        {
            _logger.LogInformation("Recovering wallet for owner {Owner} using {Algorithm}", owner, algorithm);

            // Derive master key
            var masterKey = await _keyManagement.DeriveMasterKeyAsync(mnemonic, passphrase);

            // Derive first address to use as primary wallet address
            var primaryPath = DerivationPath.CreateBip44(0, 0, 0, 0);
            var (primaryPrivateKey, primaryPublicKey) = await _keyManagement.DeriveKeyAtPathAsync(
                masterKey, primaryPath, algorithm);

            var address = await _keyManagement.GenerateAddressAsync(primaryPublicKey, algorithm);

            // Check if wallet already exists
            var existing = await _repository.GetByAddressAsync(address, false, false, false, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException($"Wallet {address} already exists");
            }

            // Encrypt private key
            var (encryptedKey, keyId) = await _keyManagement.EncryptPrivateKeyAsync(
                primaryPrivateKey, string.Empty);

            // Create wallet entity
            var wallet = new Wallet
            {
                Address = address,
                PublicKey = Convert.ToBase64String(primaryPublicKey),
                EncryptedPrivateKey = encryptedKey,
                EncryptionKeyId = keyId,
                Algorithm = algorithm,
                Owner = owner,
                Tenant = tenant,
                Name = name,
                Status = WalletStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["WordCount"] = mnemonic.WordCount.ToString(),
                    ["Recovered"] = "true"
                }
            };

            // Save to repository
            await _repository.AddAsync(wallet, cancellationToken);

            // Publish event
            await _eventPublisher.PublishAsync(new WalletRecoveredEvent
            {
                WalletAddress = wallet.Address,
                OccurredAt = wallet.CreatedAt,
                Owner = wallet.Owner,
                Tenant = wallet.Tenant,
                Algorithm = wallet.Algorithm
            }, cancellationToken);

            _logger.LogInformation("Recovered wallet {Address} for owner {Owner}", wallet.Address, owner);
            return wallet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover wallet for owner {Owner}", owner);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Wallet?> GetWalletAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        try
        {
            var wallet = await _repository.GetByAddressAsync(address, false, false, false, cancellationToken);

            if (wallet != null)
            {
                // Update last accessed time
                wallet.LastAccessedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(wallet, cancellationToken);
            }

            return wallet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wallet {Address}", address);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Wallet>> GetWalletsByOwnerAsync(
        string owner,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner cannot be empty", nameof(owner));
        if (string.IsNullOrWhiteSpace(tenant))
            throw new ArgumentException("Tenant cannot be empty", nameof(tenant));

        try
        {
            var wallets = await _repository.GetByOwnerAsync(owner, tenant, cancellationToken);
            return wallets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wallets for owner {Owner}", owner);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Wallet> UpdateWalletAsync(
        string address,
        string? name = null,
        string? description = null,
        Dictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        try
        {
            var wallet = await _repository.GetByAddressAsync(address, false, false, false, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {address} not found");
            }

            // Update fields
            if (name != null)
                wallet.Name = name;

            if (description != null)
                wallet.Description = description;

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    wallet.Metadata[tag.Key] = tag.Value;
                }
            }

            wallet.LastAccessedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(wallet, cancellationToken);

            _logger.LogDebug("Updated wallet {Address}", wallet.Address);
            return wallet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update wallet {Address}", address);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteWalletAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        try
        {
            var wallet = await _repository.GetByAddressAsync(address, false, false, false, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {address} not found");
            }

            // Soft delete by changing status
            wallet.Status = WalletStatus.Deleted;
            wallet.LastAccessedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(wallet, cancellationToken);

            await _eventPublisher.PublishAsync(new WalletStatusChangedEvent
            {
                WalletAddress = wallet.Address,
                OldStatus = WalletStatus.Active,
                NewStatus = WalletStatus.Deleted
            }, cancellationToken);

            _logger.LogInformation("Deleted wallet {Address}", address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete wallet {Address}", address);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<WalletAddress> GenerateAddressAsync(
        string walletAddress,
        int index,
        bool isChange = false,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));

        try
        {
            var wallet = await _repository.GetByAddressAsync(walletAddress, false, false, false, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {walletAddress} not found");
            }

            // This requires the mnemonic or master key which we don't store
            throw new NotImplementedException(
                "Address generation requires the wallet's mnemonic or master key. " +
                "This should be provided by the caller or stored securely.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate address for wallet {Address}", walletAddress);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> SignTransactionAsync(
        string walletAddress,
        byte[] transactionData,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));
        if (transactionData == null || transactionData.Length == 0)
            throw new ArgumentException("Transaction data cannot be empty", nameof(transactionData));

        try
        {
            var wallet = await _repository.GetByAddressAsync(walletAddress, false, false, false, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {walletAddress} not found");
            }

            // Decrypt private key
            var privateKey = await _keyManagement.DecryptPrivateKeyAsync(
                wallet.EncryptedPrivateKey,
                wallet.EncryptionKeyId);

            // Sign transaction
            var signature = await _transactionService.SignTransactionAsync(
                transactionData, privateKey, wallet.Algorithm);

            // Publish event
            await _eventPublisher.PublishAsync(new TransactionSignedEvent
            {
                WalletAddress = walletAddress,
                TransactionId = Convert.ToBase64String(signature[..Math.Min(32, signature.Length)]),
                SignedBy = walletAddress
            }, cancellationToken);

            _logger.LogInformation("Signed transaction for wallet {Address}", walletAddress);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign transaction for wallet {Address}", walletAddress);
            throw;
        }
    }

    /// <summary>
    /// Decrypts a payload using the wallet's private key
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="encryptedPayload">Encrypted payload bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted payload</returns>
    public async Task<byte[]> DecryptPayloadAsync(
        string walletAddress,
        byte[] encryptedPayload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            throw new ArgumentException("Wallet address cannot be empty", nameof(walletAddress));
        if (encryptedPayload == null || encryptedPayload.Length == 0)
            throw new ArgumentException("Encrypted payload cannot be empty", nameof(encryptedPayload));

        try
        {
            var wallet = await _repository.GetByAddressAsync(walletAddress, false, false, false, cancellationToken);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {walletAddress} not found");
            }

            // Decrypt private key
            var privateKey = await _keyManagement.DecryptPrivateKeyAsync(
                wallet.EncryptedPrivateKey,
                wallet.EncryptionKeyId);

            // Decrypt payload
            var decryptedPayload = await _transactionService.DecryptPayloadAsync(
                encryptedPayload, privateKey, wallet.Algorithm);

            _logger.LogInformation("Decrypted payload for wallet {Address}", walletAddress);
            return decryptedPayload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload for wallet {Address}", walletAddress);
            throw;
        }
    }

    /// <summary>
    /// Encrypts a payload for a recipient wallet
    /// </summary>
    /// <param name="recipientAddress">Recipient wallet address</param>
    /// <param name="payload">Payload bytes to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted payload</returns>
    public async Task<byte[]> EncryptPayloadAsync(
        string recipientAddress,
        byte[] payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientAddress))
            throw new ArgumentException("Recipient address cannot be empty", nameof(recipientAddress));
        if (payload == null || payload.Length == 0)
            throw new ArgumentException("Payload cannot be empty", nameof(payload));

        try
        {
            var recipientWallet = await _repository.GetByAddressAsync(recipientAddress, false, false, false, cancellationToken);
            if (recipientWallet == null)
            {
                throw new InvalidOperationException($"Recipient wallet {recipientAddress} not found");
            }

            // Get recipient's public key
            var publicKey = Convert.FromHexString(recipientWallet.PublicKey);

            // Encrypt payload
            var encryptedPayload = await _transactionService.EncryptPayloadAsync(
                payload, publicKey, recipientWallet.Algorithm);

            _logger.LogInformation("Encrypted payload for recipient {Address}", recipientAddress);
            return encryptedPayload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt payload for recipient {Address}", recipientAddress);
            throw;
        }
    }
}
