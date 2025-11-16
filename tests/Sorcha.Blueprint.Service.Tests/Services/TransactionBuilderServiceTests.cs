// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Service.Services.Implementation;
using Sorcha.Blueprint.Service.Services.Interfaces;
using Sorcha.Cryptography.Interfaces;
using Sorcha.TransactionHandler.Enums;
using System.Text;
using System.Text.Json;

namespace Sorcha.Blueprint.Service.Tests.Services;

public class TransactionBuilderServiceTests
{
    private readonly Mock<ICryptoModule> _mockCryptoModule;
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly Mock<ILogger<TransactionBuilderService>> _mockLogger;
    private readonly TransactionBuilderService _service;

    public TransactionBuilderServiceTests()
    {
        _mockCryptoModule = new Mock<ICryptoModule>();
        _mockHashProvider = new Mock<IHashProvider>();
        _mockLogger = new Mock<ILogger<TransactionBuilderService>>();
        _service = new TransactionBuilderService(
            _mockCryptoModule.Object,
            _mockHashProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task BuildActionTransactionAsync_WithValidData_CreatesTransaction()
    {
        // Arrange
        var blueprintId = "blueprint-1";
        var actionId = "action-1";
        var instanceId = "instance-123";
        var previousTxHash = "prev-tx-hash";
        var encryptedPayloads = new Dictionary<string, byte[]>
        {
            ["wallet-alice"] = Encoding.UTF8.GetBytes("encrypted-data-alice"),
            ["wallet-bob"] = Encoding.UTF8.GetBytes("encrypted-data-bob")
        };
        var senderWallet = "wallet-sender";
        var registerAddress = "register-1";

        // Act
        var result = await _service.BuildActionTransactionAsync(
            blueprintId,
            actionId,
            instanceId,
            previousTxHash,
            encryptedPayloads,
            senderWallet,
            registerAddress);

        // Assert
        result.Should().NotBeNull();
        result.SenderWallet.Should().Be(senderWallet);
        result.Recipients.Should().HaveCount(2);
        result.Recipients.Should().Contain("wallet-alice");
        result.Recipients.Should().Contain("wallet-bob");
        result.PreviousTxHash.Should().Be(previousTxHash);
        result.RegisterId.Should().Be(registerAddress);
        result.Version.Should().Be(TransactionVersion.V4);

        // Verify metadata
        result.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<JsonElement>(result.Metadata!);
        metadata.GetProperty("type").GetString().Should().Be("action");
        metadata.GetProperty("blueprintId").GetString().Should().Be(blueprintId);
        metadata.GetProperty("actionId").GetString().Should().Be(actionId);
        metadata.GetProperty("instanceId").GetString().Should().Be(instanceId);
    }

    [Fact]
    public async Task BuildActionTransactionAsync_WithoutInstanceId_GeneratesNew()
    {
        // Arrange
        var blueprintId = "blueprint-1";
        var actionId = "action-1";
        var encryptedPayloads = new Dictionary<string, byte[]>
        {
            ["wallet-alice"] = Encoding.UTF8.GetBytes("data")
        };
        var senderWallet = "wallet-sender";
        var registerAddress = "register-1";

        // Act
        var result = await _service.BuildActionTransactionAsync(
            blueprintId,
            actionId,
            null, // No instance ID
            null,
            encryptedPayloads,
            senderWallet,
            registerAddress);

        // Assert
        result.Should().NotBeNull();
        var metadata = JsonSerializer.Deserialize<JsonElement>(result.Metadata!);
        var instanceId = metadata.GetProperty("instanceId").GetString();
        instanceId.Should().NotBeNullOrEmpty();
        Guid.TryParse(instanceId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task BuildActionTransactionAsync_WithNullBlueprintId_ThrowsArgumentException()
    {
        // Arrange
        var encryptedPayloads = new Dictionary<string, byte[]> { ["wallet"] = new byte[1] };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BuildActionTransactionAsync(
                null!,
                "action-1",
                null,
                null,
                encryptedPayloads,
                "wallet-sender",
                "register-1"));
    }

    [Fact]
    public async Task BuildActionTransactionAsync_WithNullActionId_ThrowsArgumentException()
    {
        // Arrange
        var encryptedPayloads = new Dictionary<string, byte[]> { ["wallet"] = new byte[1] };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BuildActionTransactionAsync(
                "blueprint-1",
                null!,
                null,
                null,
                encryptedPayloads,
                "wallet-sender",
                "register-1"));
    }

    [Fact]
    public async Task BuildActionTransactionAsync_WithEmptyPayloads_ThrowsArgumentException()
    {
        // Arrange
        var emptyPayloads = new Dictionary<string, byte[]>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BuildActionTransactionAsync(
                "blueprint-1",
                "action-1",
                null,
                null,
                emptyPayloads,
                "wallet-sender",
                "register-1"));
    }

    [Fact]
    public async Task BuildRejectionTransactionAsync_WithValidData_CreatesRejectionTransaction()
    {
        // Arrange
        var originalTxHash = "original-tx-hash";
        var reason = "Invalid data provided";
        var senderWallet = "wallet-sender";
        var registerAddress = "register-1";

        // Act
        var result = await _service.BuildRejectionTransactionAsync(
            originalTxHash,
            reason,
            senderWallet,
            registerAddress);

        // Assert
        result.Should().NotBeNull();
        result.SenderWallet.Should().Be(senderWallet);
        result.Recipients.Should().BeEmpty();
        result.PreviousTxHash.Should().Be(originalTxHash);
        result.RegisterId.Should().Be(registerAddress);

        // Verify metadata
        var metadata = JsonSerializer.Deserialize<JsonElement>(result.Metadata!);
        metadata.GetProperty("type").GetString().Should().Be("rejection");
        metadata.GetProperty("rejectedTransactionHash").GetString().Should().Be(originalTxHash);
        metadata.GetProperty("reason").GetString().Should().Be(reason);
    }

    [Fact]
    public async Task BuildRejectionTransactionAsync_WithNullOriginalTxHash_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BuildRejectionTransactionAsync(
                null!,
                "reason",
                "wallet-sender",
                "register-1"));
    }

    [Fact]
    public async Task BuildRejectionTransactionAsync_WithNullReason_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BuildRejectionTransactionAsync(
                "tx-hash",
                null!,
                "wallet-sender",
                "register-1"));
    }

    [Fact]
    public async Task BuildFileTransactionsAsync_WithValidFiles_CreatesFileTransactions()
    {
        // Arrange
        var files = new List<FileAttachment>
        {
            new FileAttachment("document.pdf", "application/pdf", Encoding.UTF8.GetBytes("PDF content")),
            new FileAttachment("image.jpg", "image/jpeg", Encoding.UTF8.GetBytes("JPG content"))
        };
        var parentTxHash = "parent-tx-hash";
        var senderWallet = "wallet-sender";
        var registerAddress = "register-1";

        // Act
        var result = await _service.BuildFileTransactionsAsync(
            files,
            parentTxHash,
            senderWallet,
            registerAddress);

        // Assert
        result.Should().HaveCount(2);

        var pdfTx = result[0];
        pdfTx.PreviousTxHash.Should().Be(parentTxHash);
        pdfTx.SenderWallet.Should().Be(senderWallet);

        var pdfMetadata = JsonSerializer.Deserialize<JsonElement>(pdfTx.Metadata!);
        pdfMetadata.GetProperty("type").GetString().Should().Be("file");
        pdfMetadata.GetProperty("fileName").GetString().Should().Be("document.pdf");
        pdfMetadata.GetProperty("contentType").GetString().Should().Be("application/pdf");
        pdfMetadata.GetProperty("size").GetInt32().Should().Be(11);

        var jpgTx = result[1];
        var jpgMetadata = JsonSerializer.Deserialize<JsonElement>(jpgTx.Metadata!);
        jpgMetadata.GetProperty("fileName").GetString().Should().Be("image.jpg");
    }

    [Fact]
    public async Task BuildFileTransactionsAsync_WithEmptyFiles_ReturnsEmptyList()
    {
        // Arrange
        var files = new List<FileAttachment>();
        var parentTxHash = "parent-tx-hash";
        var senderWallet = "wallet-sender";
        var registerAddress = "register-1";

        // Act
        var result = await _service.BuildFileTransactionsAsync(
            files,
            parentTxHash,
            senderWallet,
            registerAddress);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildFileTransactionsAsync_WithEmptyFileContent_SkipsFile()
    {
        // Arrange
        var files = new List<FileAttachment>
        {
            new FileAttachment("valid.pdf", "application/pdf", Encoding.UTF8.GetBytes("content")),
            new FileAttachment("empty.txt", "text/plain", Array.Empty<byte>()) // Empty file
        };
        var parentTxHash = "parent-tx-hash";
        var senderWallet = "wallet-sender";
        var registerAddress = "register-1";

        // Act
        var result = await _service.BuildFileTransactionsAsync(
            files,
            parentTxHash,
            senderWallet,
            registerAddress);

        // Assert
        result.Should().HaveCount(1);
        var metadata = JsonSerializer.Deserialize<JsonElement>(result[0].Metadata!);
        metadata.GetProperty("fileName").GetString().Should().Be("valid.pdf");
    }

    [Fact]
    public async Task BuildFileTransactionsAsync_WithNullFiles_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.BuildFileTransactionsAsync(
                null!,
                "parent-tx",
                "wallet",
                "register"));
    }

    [Fact]
    public async Task BuildFileTransactionsAsync_WithNullParentTxHash_ThrowsArgumentException()
    {
        // Arrange
        var files = new List<FileAttachment>
        {
            new FileAttachment("file.txt", "text/plain", Encoding.UTF8.GetBytes("content"))
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BuildFileTransactionsAsync(
                files,
                null!,
                "wallet",
                "register"));
    }

    [Fact]
    public async Task BuildActionTransactionAsync_IncludesTimestamp()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;
        var encryptedPayloads = new Dictionary<string, byte[]>
        {
            ["wallet"] = Encoding.UTF8.GetBytes("data")
        };

        // Act
        var result = await _service.BuildActionTransactionAsync(
            "blueprint-1",
            "action-1",
            null,
            null,
            encryptedPayloads,
            "wallet-sender",
            "register-1");

        var afterTime = DateTime.UtcNow;

        // Assert
        result.Timestamp.Should().NotBeNull();
        result.Timestamp.Should().BeOnOrAfter(beforeTime);
        result.Timestamp.Should().BeOnOrBefore(afterTime);
    }
}
