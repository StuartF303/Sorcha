// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Register.Service.Services;
using Sorcha.ServiceClients.Register.Models;
using Xunit;

namespace Sorcha.Register.Service.Tests.Services;

public class ParticipantIndexServiceTests
{
    private readonly ParticipantIndexService _service;

    public ParticipantIndexServiceTests()
    {
        var loggerMock = new Mock<ILogger<ParticipantIndexService>>();
        _service = new ParticipantIndexService(loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ParticipantIndexService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region IndexParticipant Tests

    [Fact]
    public void IndexParticipant_ValidPayload_IndexesParticipant()
    {
        // Arrange
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true));

        // Act
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Assert
        var record = _service.GetById("reg-1", "part-1");
        record.Should().NotBeNull();
        record!.ParticipantName.Should().Be("Alice");
        record.OrganizationName.Should().Be("Acme");
        record.Status.Should().Be("Active");
        record.Version.Should().Be(1);
        record.LatestTxId.Should().Be("tx-1");
    }

    [Fact]
    public void IndexParticipant_IndexesAllAddresses()
    {
        // Arrange
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true),
            ("addr-2", "key-2", "P-256", false));

        // Act
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Assert
        _service.GetByAddress("reg-1", "addr-1").Should().NotBeNull();
        _service.GetByAddress("reg-1", "addr-2").Should().NotBeNull();
    }

    [Fact]
    public void IndexParticipant_HigherVersion_UpdatesRecord()
    {
        // Arrange
        var v1 = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true));
        var v2 = CreatePayloadElement("part-1", "Alice Updated", "Acme", "Active", 2,
            ("addr-1", "key-1", "ED25519", true));

        // Act
        _service.IndexParticipant("reg-1", "tx-1", v1, DateTimeOffset.UtcNow);
        _service.IndexParticipant("reg-1", "tx-2", v2, DateTimeOffset.UtcNow);

        // Assert
        var record = _service.GetById("reg-1", "part-1");
        record!.ParticipantName.Should().Be("Alice Updated");
        record.Version.Should().Be(2);
        record.LatestTxId.Should().Be("tx-2");
    }

    [Fact]
    public void IndexParticipant_LowerVersion_SkipsUpdate()
    {
        // Arrange
        var v2 = CreatePayloadElement("part-1", "Alice v2", "Acme", "Active", 2,
            ("addr-1", "key-1", "ED25519", true));
        var v1 = CreatePayloadElement("part-1", "Alice v1", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true));

        // Act
        _service.IndexParticipant("reg-1", "tx-2", v2, DateTimeOffset.UtcNow);
        _service.IndexParticipant("reg-1", "tx-1", v1, DateTimeOffset.UtcNow);

        // Assert
        var record = _service.GetById("reg-1", "part-1");
        record!.ParticipantName.Should().Be("Alice v2");
        record.Version.Should().Be(2);
    }

    [Fact]
    public void IndexParticipant_UpdateRemovesOldAddresses()
    {
        // Arrange
        var v1 = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-old", "key-1", "ED25519", true));
        var v2 = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 2,
            ("addr-new", "key-2", "ED25519", true));

        // Act
        _service.IndexParticipant("reg-1", "tx-1", v1, DateTimeOffset.UtcNow);
        _service.IndexParticipant("reg-1", "tx-2", v2, DateTimeOffset.UtcNow);

        // Assert
        _service.GetByAddress("reg-1", "addr-old").Should().BeNull();
        _service.GetByAddress("reg-1", "addr-new").Should().NotBeNull();
    }

    [Fact]
    public void IndexParticipant_InvalidPayload_DoesNotThrow()
    {
        // Arrange
        var invalidPayload = JsonSerializer.Deserialize<JsonElement>("\"not-an-object\"");

        // Act
        var act = () => _service.IndexParticipant("reg-1", "tx-1", invalidPayload, DateTimeOffset.UtcNow);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IndexParticipant_DifferentRegisters_AreIsolated()
    {
        // Arrange
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true));

        // Act
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Assert
        _service.GetById("reg-1", "part-1").Should().NotBeNull();
        _service.GetById("reg-2", "part-1").Should().BeNull();
    }

    #endregion

    #region GetByAddress Tests

    [Fact]
    public void GetByAddress_ExistingAddress_ReturnsRecord()
    {
        // Arrange
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true));
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Act
        var record = _service.GetByAddress("reg-1", "addr-1");

        // Assert
        record.Should().NotBeNull();
        record!.ParticipantId.Should().Be("part-1");
    }

    [Fact]
    public void GetByAddress_NonExistentAddress_ReturnsNull()
    {
        var record = _service.GetByAddress("reg-1", "unknown-addr");
        record.Should().BeNull();
    }

    [Fact]
    public void GetByAddress_NonExistentRegister_ReturnsNull()
    {
        var record = _service.GetByAddress("unknown-reg", "addr-1");
        record.Should().BeNull();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public void GetById_ExistingParticipant_ReturnsLatestVersion()
    {
        // Arrange
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-1", "key-1", "ED25519", true));
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Act
        var record = _service.GetById("reg-1", "part-1");

        // Assert
        record.Should().NotBeNull();
        record!.ParticipantId.Should().Be("part-1");
    }

    [Fact]
    public void GetById_NonExistentParticipant_ReturnsNull()
    {
        var record = _service.GetById("reg-1", "unknown-part");
        record.Should().BeNull();
    }

    #endregion

    #region List Tests

    [Fact]
    public void List_ReturnsActiveByDefault()
    {
        // Arrange
        IndexParticipant("reg-1", "p1", "Alice", "Active", 1);
        IndexParticipant("reg-1", "p2", "Bob", "Revoked", 1);
        IndexParticipant("reg-1", "p3", "Charlie", "Active", 1);

        // Act
        var page = _service.List("reg-1");

        // Assert
        page.Total.Should().Be(2);
        page.Participants.Should().HaveCount(2);
        page.Participants.Should().OnlyContain(p => p.Status == "Active");
    }

    [Fact]
    public void List_StatusAll_ReturnsAllStatuses()
    {
        // Arrange
        IndexParticipant("reg-1", "p1", "Alice", "Active", 1);
        IndexParticipant("reg-1", "p2", "Bob", "Revoked", 1);
        IndexParticipant("reg-1", "p3", "Charlie", "Deprecated", 1);

        // Act
        var page = _service.List("reg-1", statusFilter: "all");

        // Assert
        page.Total.Should().Be(3);
    }

    [Fact]
    public void List_Pagination_ReturnsCorrectSubset()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            IndexParticipant("reg-1", $"p{i}", $"Participant {i}", "Active", 1);

        // Act
        var page = _service.List("reg-1", skip: 2, top: 2);

        // Assert
        page.Total.Should().Be(5);
        page.Participants.Should().HaveCount(2);
        page.PageSize.Should().Be(2);
    }

    [Fact]
    public void List_EmptyRegister_ReturnsEmptyPage()
    {
        var page = _service.List("empty-reg");
        page.Total.Should().Be(0);
        page.Participants.Should().BeEmpty();
    }

    [Fact]
    public void List_StatusFilter_Revoked_ReturnsOnlyRevoked()
    {
        // Arrange
        IndexParticipant("reg-1", "p1", "Alice", "Active", 1);
        IndexParticipant("reg-1", "p2", "Bob", "Revoked", 1);

        // Act
        var page = _service.List("reg-1", statusFilter: "revoked");

        // Assert
        page.Total.Should().Be(1);
        page.Participants.Should().OnlyContain(p => p.Status == "Revoked");
    }

    #endregion

    #region Public Key Resolution Support Tests

    [Fact]
    public void GetByAddress_RevokedParticipant_StillReturnsRecord()
    {
        // Arrange — revoked participants should still be returned (with status visible)
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Revoked", 2,
            ("addr-1", "key-1", "ED25519", true));
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Act
        var record = _service.GetByAddress("reg-1", "addr-1");

        // Assert
        record.Should().NotBeNull();
        record!.Status.Should().Be("Revoked");
    }

    [Fact]
    public void IndexParticipant_MultipleAddressesDifferentAlgorithms_AllIndexed()
    {
        // Arrange
        var payload = CreatePayloadElement("part-1", "Alice", "Acme", "Active", 1,
            ("addr-ed", "key-ed", "ED25519", true),
            ("addr-p256", "key-p256", "P-256", false));
        _service.IndexParticipant("reg-1", "tx-1", payload, DateTimeOffset.UtcNow);

        // Act
        var byEd = _service.GetByAddress("reg-1", "addr-ed");
        var byP256 = _service.GetByAddress("reg-1", "addr-p256");

        // Assert
        byEd.Should().NotBeNull();
        byEd!.Addresses.Should().HaveCount(2);
        byP256.Should().NotBeNull();
        byP256!.ParticipantId.Should().Be("part-1");
    }

    #endregion

    #region Helper Methods

    private void IndexParticipant(string registerId, string participantId, string name, string status, int version)
    {
        var payload = CreatePayloadElement(participantId, name, "TestOrg", status, version,
            ($"addr-{participantId}", $"key-{participantId}", "ED25519", true));
        _service.IndexParticipant(registerId, $"tx-{participantId}-v{version}", payload, DateTimeOffset.UtcNow);
    }

    private static JsonElement CreatePayloadElement(
        string participantId,
        string participantName,
        string organizationName,
        string status,
        int version,
        params (string addr, string key, string algo, bool primary)[] addresses)
    {
        var addrArray = addresses.Select(a => new
        {
            walletAddress = a.addr,
            publicKey = a.key,
            algorithm = a.algo,
            primary = a.primary
        });

        var payload = new
        {
            participantId,
            participantName,
            organizationName,
            status,
            version,
            addresses = addrArray
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    #endregion
}
