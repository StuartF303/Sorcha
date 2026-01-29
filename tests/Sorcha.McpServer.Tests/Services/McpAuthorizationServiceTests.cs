// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.McpServer.Services;

namespace Sorcha.McpServer.Tests.Services;

public class McpAuthorizationServiceTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<ILogger<McpAuthorizationService>> _loggerMock;
    private readonly McpAuthorizationService _service;

    public McpAuthorizationServiceTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _loggerMock = new Mock<ILogger<McpAuthorizationService>>();
        _service = new McpAuthorizationService(_sessionServiceMock.Object, _loggerMock.Object);
    }

    private void SetupSession(string[] roles, bool isExpired = false)
    {
        var session = new McpSession
        {
            UserId = "test-user",
            TenantId = "test-tenant",
            Roles = roles,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _sessionServiceMock.Setup(x => x.CurrentSession).Returns(session);
        _sessionServiceMock.Setup(x => x.IsTokenExpired()).Returns(isExpired);
    }

    #region Admin Tools

    [Theory]
    [InlineData("sorcha_health_check")]
    [InlineData("sorcha_log_query")]
    [InlineData("sorcha_metrics")]
    [InlineData("sorcha_tenant_list")]
    [InlineData("sorcha_tenant_create")]
    [InlineData("sorcha_tenant_update")]
    [InlineData("sorcha_user_list")]
    [InlineData("sorcha_user_manage")]
    [InlineData("sorcha_peer_status")]
    [InlineData("sorcha_validator_status")]
    [InlineData("sorcha_register_stats")]
    [InlineData("sorcha_audit_query")]
    [InlineData("sorcha_token_revoke")]
    public void CanInvokeTool_AdminTool_AdminRole_ReturnsTrue(string toolName)
    {
        // Arrange
        SetupSession(["sorcha:admin"]);

        // Act
        var result = _service.CanInvokeTool(toolName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("sorcha_health_check")]
    [InlineData("sorcha_tenant_list")]
    public void CanInvokeTool_AdminTool_DesignerRole_ReturnsFalse(string toolName)
    {
        // Arrange
        SetupSession(["sorcha:designer"]);

        // Act
        var result = _service.CanInvokeTool(toolName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Designer Tools

    [Theory]
    [InlineData("sorcha_blueprint_list")]
    [InlineData("sorcha_blueprint_get")]
    [InlineData("sorcha_blueprint_create")]
    [InlineData("sorcha_blueprint_update")]
    [InlineData("sorcha_blueprint_validate")]
    [InlineData("sorcha_blueprint_simulate")]
    [InlineData("sorcha_disclosure_analysis")]
    [InlineData("sorcha_blueprint_diff")]
    [InlineData("sorcha_blueprint_export")]
    [InlineData("sorcha_schema_validate")]
    [InlineData("sorcha_schema_generate")]
    [InlineData("sorcha_jsonlogic_test")]
    [InlineData("sorcha_workflow_instances")]
    public void CanInvokeTool_DesignerTool_DesignerRole_ReturnsTrue(string toolName)
    {
        // Arrange
        SetupSession(["sorcha:designer"]);

        // Act
        var result = _service.CanInvokeTool(toolName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("sorcha_blueprint_create")]
    [InlineData("sorcha_blueprint_validate")]
    public void CanInvokeTool_DesignerTool_ParticipantRole_ReturnsFalse(string toolName)
    {
        // Arrange
        SetupSession(["sorcha:participant"]);

        // Act
        var result = _service.CanInvokeTool(toolName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Participant Tools

    [Theory]
    [InlineData("sorcha_inbox_list")]
    [InlineData("sorcha_action_details")]
    [InlineData("sorcha_action_submit")]
    [InlineData("sorcha_action_validate")]
    [InlineData("sorcha_transaction_history")]
    [InlineData("sorcha_workflow_status")]
    [InlineData("sorcha_disclosed_data")]
    [InlineData("sorcha_wallet_info")]
    [InlineData("sorcha_wallet_sign")]
    [InlineData("sorcha_register_query")]
    public void CanInvokeTool_ParticipantTool_ParticipantRole_ReturnsTrue(string toolName)
    {
        // Arrange
        SetupSession(["sorcha:participant"]);

        // Act
        var result = _service.CanInvokeTool(toolName);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Multiple Roles

    [Fact]
    public void CanInvokeTool_MultipleRoles_CanAccessAllRoleTools()
    {
        // Arrange
        SetupSession(["sorcha:admin", "sorcha:designer", "sorcha:participant"]);

        // Act & Assert
        _service.CanInvokeTool("sorcha_health_check").Should().BeTrue(); // admin
        _service.CanInvokeTool("sorcha_blueprint_create").Should().BeTrue(); // designer
        _service.CanInvokeTool("sorcha_inbox_list").Should().BeTrue(); // participant
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CanInvokeTool_NoSession_ReturnsFalse()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.CurrentSession).Returns((McpSession?)null);

        // Act
        var result = _service.CanInvokeTool("sorcha_health_check");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanInvokeTool_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        SetupSession(["sorcha:admin"], isExpired: true);

        // Act
        var result = _service.CanInvokeTool("sorcha_health_check");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanInvokeTool_UnknownTool_ReturnsFalse()
    {
        // Arrange
        SetupSession(["sorcha:admin"]);

        // Act
        var result = _service.CanInvokeTool("sorcha_unknown_tool");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanInvokeTool_NoRoles_ReturnsFalse()
    {
        // Arrange
        SetupSession([]);

        // Act
        var result = _service.CanInvokeTool("sorcha_health_check");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetAuthorizedTools

    [Fact]
    public void GetAuthorizedTools_AdminRole_ReturnsAdminTools()
    {
        // Arrange
        SetupSession(["sorcha:admin"]);

        // Act
        var tools = _service.GetAuthorizedTools();

        // Assert
        tools.Should().Contain("sorcha_health_check");
        tools.Should().Contain("sorcha_tenant_list");
        tools.Should().NotContain("sorcha_blueprint_create");
        tools.Should().NotContain("sorcha_inbox_list");
    }

    [Fact]
    public void GetAuthorizedTools_MultipleRoles_ReturnsCombinedTools()
    {
        // Arrange
        SetupSession(["sorcha:admin", "sorcha:designer"]);

        // Act
        var tools = _service.GetAuthorizedTools();

        // Assert
        tools.Should().Contain("sorcha_health_check"); // admin
        tools.Should().Contain("sorcha_blueprint_create"); // designer
        tools.Should().NotContain("sorcha_inbox_list"); // participant only
    }

    [Fact]
    public void GetAuthorizedTools_NoSession_ReturnsEmpty()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.CurrentSession).Returns((McpSession?)null);

        // Act
        var tools = _service.GetAuthorizedTools();

        // Assert
        tools.Should().BeEmpty();
    }

    [Fact]
    public void GetAuthorizedTools_ExpiredToken_ReturnsEmpty()
    {
        // Arrange
        SetupSession(["sorcha:admin"], isExpired: true);

        // Act
        var tools = _service.GetAuthorizedTools();

        // Assert
        tools.Should().BeEmpty();
    }

    #endregion
}
