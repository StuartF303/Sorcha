using Microsoft.AspNetCore.Mvc;
using Sorcha.WalletService.Api.Mappers;
using Sorcha.WalletService.Api.Models;
using Sorcha.WalletService.Domain.ValueObjects;
using Sorcha.WalletService.Services.Implementation;
using System.Security.Claims;

namespace Sorcha.WalletService.Api.Controllers;

/// <summary>
/// Wallet management endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class WalletsController : ControllerBase
{
    private readonly WalletManager _walletManager;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(
        WalletManager walletManager,
        ILogger<WalletsController> logger)
    {
        _walletManager = walletManager ?? throw new ArgumentNullException(nameof(walletManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new wallet
    /// </summary>
    /// <param name="request">Wallet creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created wallet with mnemonic phrase</returns>
    /// <response code="201">Wallet created successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWalletResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateWalletResponse>> CreateWallet(
        [FromBody] CreateWalletRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var owner = GetCurrentUser();
            var tenant = GetCurrentTenant();

            _logger.LogInformation("Creating wallet for user {Owner} in tenant {Tenant}", owner, tenant);

            var (wallet, mnemonic) = await _walletManager.CreateWalletAsync(
                request.Name,
                request.Algorithm,
                owner,
                tenant,
                request.WordCount,
                request.Passphrase,
                cancellationToken);

            var response = new CreateWalletResponse
            {
                Wallet = wallet.ToDto(),
                MnemonicWords = mnemonic.Phrase.Split(' ')
            };

            return CreatedAtAction(
                nameof(GetWallet),
                new { address = wallet.Address },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid wallet creation request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create wallet");
            return Problem(
                title: "Wallet Creation Failed",
                detail: "An error occurred while creating the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Recover a wallet from mnemonic phrase
    /// </summary>
    /// <param name="request">Recovery parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovered wallet</returns>
    /// <response code="200">Wallet recovered successfully</response>
    /// <response code="400">Invalid mnemonic or parameters</response>
    /// <response code="409">Wallet already exists</response>
    [HttpPost("recover")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WalletDto>> RecoverWallet(
        [FromBody] RecoverWalletRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var owner = GetCurrentUser();
            var tenant = GetCurrentTenant();

            _logger.LogInformation("Recovering wallet for user {Owner} in tenant {Tenant}", owner, tenant);

            var mnemonic = new Mnemonic(string.Join(" ", request.MnemonicWords));

            var wallet = await _walletManager.RecoverWalletAsync(
                mnemonic,
                request.Name,
                request.Algorithm,
                owner,
                tenant,
                request.Passphrase,
                cancellationToken);

            return Ok(wallet.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid wallet recovery request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "Wallet already exists");
            return Conflict(new ProblemDetails
            {
                Title = "Wallet Already Exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover wallet");
            return Problem(
                title: "Wallet Recovery Failed",
                detail: "An error occurred while recovering the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get wallet by address
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet details</returns>
    /// <response code="200">Wallet found</response>
    /// <response code="404">Wallet not found</response>
    [HttpGet("{address}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletDto>> GetWallet(
        string address,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _walletManager.GetWalletAsync(address, cancellationToken);

        if (wallet == null)
        {
            return NotFound();
        }

        // TODO: Add authorization check - user should own the wallet or have delegated access
        return Ok(wallet.ToDto());
    }

    /// <summary>
    /// List wallets for current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of wallets</returns>
    /// <response code="200">Wallets retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WalletDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WalletDto>>> ListWallets(
        CancellationToken cancellationToken = default)
    {
        var owner = GetCurrentUser();
        var tenant = GetCurrentTenant();

        var wallets = await _walletManager.GetWalletsByOwnerAsync(owner, tenant, cancellationToken);

        return Ok(wallets.Select(w => w.ToDto()));
    }

    /// <summary>
    /// Update wallet metadata
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="request">Update parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated wallet</returns>
    /// <response code="200">Wallet updated successfully</response>
    /// <response code="404">Wallet not found</response>
    [HttpPatch("{address}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletDto>> UpdateWallet(
        string address,
        [FromBody] UpdateWalletRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _walletManager.UpdateWalletAsync(
                address,
                request.Name,
                tags: request.Tags,
                cancellationToken: cancellationToken);

            return Ok(wallet.ToDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update wallet {Address}", address);
            return Problem(
                title: "Wallet Update Failed",
                detail: "An error occurred while updating the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Delete (soft delete) a wallet
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Wallet deleted successfully</response>
    /// <response code="404">Wallet not found</response>
    [HttpDelete("{address}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWallet(
        string address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _walletManager.DeleteWalletAsync(address, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete wallet {Address}", address);
            return Problem(
                title: "Wallet Deletion Failed",
                detail: "An error occurred while deleting the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Sign a transaction with a wallet
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="request">Transaction data to sign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Digital signature</returns>
    /// <response code="200">Transaction signed successfully</response>
    /// <response code="404">Wallet not found</response>
    [HttpPost("{address}/sign")]
    [ProducesResponseType(typeof(SignTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SignTransactionResponse>> SignTransaction(
        string address,
        [FromBody] SignTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transactionData = Convert.FromBase64String(request.TransactionData);
            var signature = await _walletManager.SignTransactionAsync(
                address,
                transactionData,
                cancellationToken);

            var response = new SignTransactionResponse
            {
                Signature = Convert.ToBase64String(signature),
                SignedBy = address,
                SignedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid base64 transaction data");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Transaction data must be valid base64 encoded string",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign transaction for wallet {Address}", address);
            return Problem(
                title: "Transaction Signing Failed",
                detail: "An error occurred while signing the transaction",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Decrypt a payload using a wallet's private key
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="request">Encrypted payload data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted payload</returns>
    /// <response code="200">Payload decrypted successfully</response>
    /// <response code="400">Invalid encrypted payload</response>
    /// <response code="404">Wallet not found</response>
    [HttpPost("{address}/decrypt")]
    [ProducesResponseType(typeof(DecryptPayloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecryptPayloadResponse>> DecryptPayload(
        string address,
        [FromBody] DecryptPayloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encryptedPayload = Convert.FromBase64String(request.EncryptedPayload);
            var decryptedPayload = await _walletManager.DecryptPayloadAsync(
                address,
                encryptedPayload,
                cancellationToken);

            var response = new DecryptPayloadResponse
            {
                DecryptedPayload = Convert.ToBase64String(decryptedPayload),
                DecryptedBy = address,
                DecryptedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid base64 encrypted payload");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Encrypted payload must be valid base64 encoded string",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload for wallet {Address}", address);
            return Problem(
                title: "Payload Decryption Failed",
                detail: "An error occurred while decrypting the payload",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Encrypt a payload for a recipient wallet
    /// </summary>
    /// <param name="address">Recipient wallet address (if RecipientAddress not in request body)</param>
    /// <param name="request">Payload data to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted payload</returns>
    /// <response code="200">Payload encrypted successfully</response>
    /// <response code="400">Invalid payload data</response>
    /// <response code="404">Recipient wallet not found</response>
    [HttpPost("{address}/encrypt")]
    [ProducesResponseType(typeof(EncryptPayloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EncryptPayloadResponse>> EncryptPayload(
        string address,
        [FromBody] EncryptPayloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use RecipientAddress from request body if provided, otherwise use address from route
            var recipientAddress = request.RecipientAddress ?? address;

            var payload = Convert.FromBase64String(request.Payload);
            var encryptedPayload = await _walletManager.EncryptPayloadAsync(
                recipientAddress,
                payload,
                cancellationToken);

            var response = new EncryptPayloadResponse
            {
                EncryptedPayload = Convert.ToBase64String(encryptedPayload),
                RecipientAddress = recipientAddress,
                EncryptedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid base64 payload");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Payload must be valid base64 encoded string",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt payload for recipient {Address}", address);
            return Problem(
                title: "Payload Encryption Failed",
                detail: "An error occurred while encrypting the payload",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Generate a new address for a wallet
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="request">Address generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated address</returns>
    /// <response code="200">Address generated successfully</response>
    /// <response code="404">Wallet not found</response>
    /// <response code="501">Not implemented (requires mnemonic)</response>
    [HttpPost("{address}/addresses")]
    [ProducesResponseType(typeof(GenerateAddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<GenerateAddressResponse>> GenerateAddress(
        string address,
        [FromBody] GenerateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse derivation path or use default
            // Note: This functionality requires the wallet's mnemonic which is not stored
            // This is a placeholder for future implementation
            throw new NotImplementedException(
                "Address generation requires the wallet's mnemonic, which is not stored for security. " +
                "Consider implementing this via a secure enclave or requiring the user to provide their mnemonic.");
        }
        catch (NotImplementedException ex)
        {
            _logger.LogWarning("Address generation attempted but not implemented: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
            {
                Title = "Not Implemented",
                Detail = ex.Message,
                Status = StatusCodes.Status501NotImplemented
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate address for wallet {Address}", address);
            return Problem(
                title: "Address Generation Failed",
                detail: "An error occurred while generating the address",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // Helper methods for authentication/authorization
    private string GetCurrentUser()
    {
        // TODO: Extract from JWT claims
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    private string GetCurrentTenant()
    {
        // TODO: Extract from JWT claims or headers
        return User.FindFirstValue("tenant") ?? "default";
    }
}
