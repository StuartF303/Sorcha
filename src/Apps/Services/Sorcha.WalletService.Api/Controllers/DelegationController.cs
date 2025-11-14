using Microsoft.AspNetCore.Mvc;
using Sorcha.WalletService.Api.Mappers;
using Sorcha.WalletService.Api.Models;
using Sorcha.WalletService.Domain;
using Sorcha.WalletService.Services.Implementation;
using System.Security.Claims;

namespace Sorcha.WalletService.Api.Controllers;

/// <summary>
/// Wallet access control and delegation endpoints
/// </summary>
[ApiController]
[Route("api/v1/wallets/{walletAddress}/access")]
[Produces("application/json")]
public class DelegationController : ControllerBase
{
    private readonly DelegationService _delegationService;
    private readonly ILogger<DelegationController> _logger;

    public DelegationController(
        DelegationService delegationService,
        ILogger<DelegationController> logger)
    {
        _delegationService = delegationService ?? throw new ArgumentNullException(nameof(delegationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Grant access to a wallet
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="request">Access grant parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created access entry</returns>
    /// <response code="201">Access granted successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Wallet not found</response>
    /// <response code="409">Access already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(WalletAccessDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WalletAccessDto>> GrantAccess(
        string walletAddress,
        [FromBody] GrantAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var grantedBy = GetCurrentUser();

            if (!Enum.TryParse<AccessRight>(request.AccessRight, out var accessRight))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Access Right",
                    Detail = $"Invalid access right: {request.AccessRight}. Valid values are: Owner, ReadWrite, ReadOnly",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation(
                "Granting {AccessRight} access on wallet {WalletAddress} to {Subject}",
                accessRight, walletAddress, request.Subject);

            var access = await _delegationService.GrantAccessAsync(
                walletAddress,
                request.Subject,
                accessRight,
                grantedBy,
                request.Reason,
                request.ExpiresAt,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetAccess),
                new { walletAddress },
                access.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid access grant request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Access Already Exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant access on wallet {WalletAddress}", walletAddress);
            return Problem(
                title: "Access Grant Failed",
                detail: "An error occurred while granting access",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// List active access grants for a wallet
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active access grants</returns>
    /// <response code="200">Access list retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WalletAccessDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WalletAccessDto>>> GetAccess(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _delegationService.GetActiveAccessAsync(walletAddress, cancellationToken);
            return Ok(access.Select(a => a.ToDto()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access for wallet {WalletAddress}", walletAddress);
            return Problem(
                title: "Failed to Retrieve Access",
                detail: "An error occurred while retrieving access grants",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Revoke access to a wallet
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="subject">Subject whose access to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Access revoked successfully</response>
    /// <response code="404">Wallet or access not found</response>
    [HttpDelete("{subject}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAccess(
        string walletAddress,
        string subject,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var revokedBy = GetCurrentUser();

            _logger.LogInformation(
                "Revoking access for {Subject} on wallet {WalletAddress}",
                subject, walletAddress);

            await _delegationService.RevokeAccessAsync(
                walletAddress,
                subject,
                revokedBy,
                cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke access for {Subject} on wallet {WalletAddress}",
                subject, walletAddress);
            return Problem(
                title: "Access Revocation Failed",
                detail: "An error occurred while revoking access",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Check if a subject has access to a wallet
    /// </summary>
    /// <param name="walletAddress">Wallet address</param>
    /// <param name="subject">Subject identifier</param>
    /// <param name="requiredRight">Required access right (Owner, ReadWrite, ReadOnly)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access check result</returns>
    /// <response code="200">Access check completed</response>
    [HttpGet("{subject}/check")]
    [ProducesResponseType(typeof(AccessCheckResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessCheckResponse>> CheckAccess(
        string walletAddress,
        string subject,
        [FromQuery] string requiredRight = "ReadOnly",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<AccessRight>(requiredRight, out var accessRight))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Access Right",
                    Detail = $"Invalid access right: {requiredRight}. Valid values are: Owner, ReadWrite, ReadOnly",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var hasAccess = await _delegationService.HasAccessAsync(
                walletAddress,
                subject,
                accessRight,
                cancellationToken);

            return Ok(new AccessCheckResponse
            {
                WalletAddress = walletAddress,
                Subject = subject,
                RequiredRight = requiredRight,
                HasAccess = hasAccess
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check access for {Subject} on wallet {WalletAddress}",
                subject, walletAddress);
            return Problem(
                title: "Access Check Failed",
                detail: "An error occurred while checking access",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private string GetCurrentUser()
    {
        // TODO: Extract from JWT claims
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }
}

/// <summary>
/// Response model for access check
/// </summary>
public class AccessCheckResponse
{
    /// <summary>
    /// Wallet address
    /// </summary>
    public required string WalletAddress { get; set; }

    /// <summary>
    /// Subject identifier
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Required access right
    /// </summary>
    public required string RequiredRight { get; set; }

    /// <summary>
    /// Whether subject has the required access
    /// </summary>
    public bool HasAccess { get; set; }
}
