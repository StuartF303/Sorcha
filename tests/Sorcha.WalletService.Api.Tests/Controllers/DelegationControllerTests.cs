using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sorcha.WalletService.Domain.Entities;

namespace Sorcha.WalletService.Api.Tests.Controllers;

public class DelegationControllerTests
{
    private readonly Mock<DelegationService> _mockDelegationService;
    private readonly Mock<ILogger<DelegationController>> _mockLogger;
    private readonly DelegationController _controller;

    public DelegationControllerTests()
    {
        _mockDelegationService = new Mock<DelegationService>(
            Mock.Of<IWalletRepository>(),
            Mock.Of<ILogger<DelegationService>>());

        _mockLogger = new Mock<ILogger<DelegationController>>();
        _controller = new DelegationController(_mockDelegationService.Object, _mockLogger.Object);

        // Setup controller context with user claims
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim("tenant", "test-tenant")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region GrantAccess Tests

    [Fact]
    public async Task GrantAccess_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var request = new GrantAccessRequest
        {
            Subject = "user123",
            AccessRight = "ReadWrite",
            Reason = "Testing access"
        };

        var walletAccess = new WalletAccess
        {
            WalletAddress = walletAddress,
            Subject = request.Subject,
            AccessRight = AccessRight.ReadWrite,
            GrantedBy = "test-user",
            GrantedAt = DateTime.UtcNow,
            Reason = request.Reason
        };

        _mockDelegationService
            .Setup(x => x.GrantAccessAsync(
                walletAddress,
                request.Subject,
                AccessRight.ReadWrite,
                "test-user",
                request.Reason,
                request.ExpiresAt,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(walletAccess);

        // Act
        var result = await _controller.GrantAccess(walletAddress, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        var dto = createdResult.Value.Should().BeOfType<WalletAccessDto>().Subject;
        dto.WalletAddress.Should().Be(walletAddress);
        dto.Subject.Should().Be(request.Subject);
        dto.AccessRight.Should().Be("ReadWrite");
    }

    [Fact]
    public async Task GrantAccess_ShouldReturnBadRequest_WhenInvalidAccessRight()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var request = new GrantAccessRequest
        {
            Subject = "user123",
            AccessRight = "InvalidRight",
            Reason = "Testing"
        };

        // Act
        var result = await _controller.GrantAccess(walletAddress, request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Invalid access right");
    }

    [Fact]
    public async Task GrantAccess_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var walletAddress = "ws1nonexistent";
        var request = new GrantAccessRequest
        {
            Subject = "user123",
            AccessRight = "ReadOnly"
        };

        _mockDelegationService
            .Setup(x => x.GrantAccessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AccessRight>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet not found"));

        // Act
        var result = await _controller.GrantAccess(walletAddress, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GrantAccess_ShouldReturnConflict_WhenAccessAlreadyExists()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var request = new GrantAccessRequest
        {
            Subject = "user123",
            AccessRight = "ReadWrite"
        };

        _mockDelegationService
            .Setup(x => x.GrantAccessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AccessRight>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Access already exists"));

        // Act
        var result = await _controller.GrantAccess(walletAddress, request);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task GrantAccess_ShouldReturnBadRequest_WhenArgumentException()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var request = new GrantAccessRequest
        {
            Subject = "",
            AccessRight = "ReadOnly"
        };

        _mockDelegationService
            .Setup(x => x.GrantAccessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AccessRight>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Subject cannot be empty"));

        // Act
        var result = await _controller.GrantAccess(walletAddress, request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GrantAccess_ShouldSupportExpiringAccess()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var request = new GrantAccessRequest
        {
            Subject = "user123",
            AccessRight = "ReadWrite",
            ExpiresAt = expiresAt
        };

        var walletAccess = new WalletAccess
        {
            WalletAddress = walletAddress,
            Subject = request.Subject,
            AccessRight = AccessRight.ReadWrite,
            GrantedBy = "test-user",
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        _mockDelegationService
            .Setup(x => x.GrantAccessAsync(
                walletAddress,
                request.Subject,
                AccessRight.ReadWrite,
                "test-user",
                request.Reason,
                expiresAt,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(walletAccess);

        // Act
        var result = await _controller.GrantAccess(walletAddress, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = createdResult.Value.Should().BeOfType<WalletAccessDto>().Subject;
        dto.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region GetAccess Tests

    [Fact]
    public async Task GetAccess_ShouldReturnOk_WithAccessList()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var accessList = new List<WalletAccess>
        {
            new WalletAccess
            {
                WalletAddress = walletAddress,
                Subject = "user1",
                AccessRight = AccessRight.ReadWrite,
                GrantedBy = "owner",
                GrantedAt = DateTime.UtcNow
            },
            new WalletAccess
            {
                WalletAddress = walletAddress,
                Subject = "user2",
                AccessRight = AccessRight.ReadOnly,
                GrantedBy = "owner",
                GrantedAt = DateTime.UtcNow
            }
        };

        _mockDelegationService
            .Setup(x => x.GetActiveAccessAsync(walletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessList);

        // Act
        var result = await _controller.GetAccess(walletAddress);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = okResult.Value.Should().BeAssignableTo<IEnumerable<WalletAccessDto>>().Subject;
        dtos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccess_ShouldReturnEmptyList_WhenNoAccess()
    {
        // Arrange
        var walletAddress = "ws1test123";

        _mockDelegationService
            .Setup(x => x.GetActiveAccessAsync(walletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WalletAccess>());

        // Act
        var result = await _controller.GetAccess(walletAddress);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = okResult.Value.Should().BeAssignableTo<IEnumerable<WalletAccessDto>>().Subject;
        dtos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccess_ShouldReturnProblem_WhenException()
    {
        // Arrange
        var walletAddress = "ws1test123";

        _mockDelegationService
            .Setup(x => x.GetActiveAccessAsync(walletAddress, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAccess(walletAddress);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region RevokeAccess Tests

    [Fact]
    public async Task RevokeAccess_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";

        _mockDelegationService
            .Setup(x => x.RevokeAccessAsync(
                walletAddress,
                subject,
                "test-user",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RevokeAccess(walletAddress, subject);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RevokeAccess_ShouldReturnNotFound_WhenAccessDoesNotExist()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";

        _mockDelegationService
            .Setup(x => x.RevokeAccessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Access not found"));

        // Act
        var result = await _controller.RevokeAccess(walletAddress, subject);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RevokeAccess_ShouldReturnProblem_WhenException()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";

        _mockDelegationService
            .Setup(x => x.RevokeAccessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.RevokeAccess(walletAddress, subject);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CheckAccess Tests

    [Fact]
    public async Task CheckAccess_ShouldReturnOk_WithTrueWhenHasAccess()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";
        var requiredRight = "ReadOnly";

        _mockDelegationService
            .Setup(x => x.HasAccessAsync(
                walletAddress,
                subject,
                AccessRight.ReadOnly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckAccess(walletAddress, subject, requiredRight);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessCheckResponse>().Subject;
        response.WalletAddress.Should().Be(walletAddress);
        response.Subject.Should().Be(subject);
        response.RequiredRight.Should().Be(requiredRight);
        response.HasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAccess_ShouldReturnOk_WithFalseWhenNoAccess()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";
        var requiredRight = "ReadWrite";

        _mockDelegationService
            .Setup(x => x.HasAccessAsync(
                walletAddress,
                subject,
                AccessRight.ReadWrite,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckAccess(walletAddress, subject, requiredRight);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessCheckResponse>().Subject;
        response.HasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAccess_ShouldReturnBadRequest_WhenInvalidAccessRight()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";
        var requiredRight = "InvalidRight";

        // Act
        var result = await _controller.CheckAccess(walletAddress, subject, requiredRight);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CheckAccess_ShouldUseReadOnlyAsDefault()
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";

        _mockDelegationService
            .Setup(x => x.HasAccessAsync(
                walletAddress,
                subject,
                AccessRight.ReadOnly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - not specifying requiredRight, should default to ReadOnly
        var result = await _controller.CheckAccess(walletAddress, subject);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessCheckResponse>().Subject;
        response.RequiredRight.Should().Be("ReadOnly");
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("ReadWrite")]
    [InlineData("ReadOnly")]
    public async Task CheckAccess_ShouldSupportAllAccessRights(string accessRight)
    {
        // Arrange
        var walletAddress = "ws1test123";
        var subject = "user123";
        var expectedAccessRight = Enum.Parse<AccessRight>(accessRight);

        _mockDelegationService
            .Setup(x => x.HasAccessAsync(
                walletAddress,
                subject,
                expectedAccessRight,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckAccess(walletAddress, subject, accessRight);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessCheckResponse>().Subject;
        response.RequiredRight.Should().Be(accessRight);
        response.HasAccess.Should().BeTrue();
    }

    #endregion
}
