// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Text.Json;
using FluentAssertions;
using Sorcha.Register.Models;
using Sorcha.TransactionHandler.Services;
using Xunit;

namespace Sorcha.TransactionHandler.Tests.Serialization;

/// <summary>
/// Tests for ContentType and ContentEncoding fields in PayloadModel JSON serialization.
/// </summary>
public class ContentTypeSerializationTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PayloadModel_WithContentType_SerializesToJson()
    {
        var payload = new PayloadModel
        {
            Data = "dGVzdA",
            Hash = "aGFzaA",
            ContentType = "application/json",
            ContentEncoding = ContentEncodings.Base64Url
        };

        var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("contentType").GetString().Should().Be("application/json");
        root.GetProperty("contentEncoding").GetString().Should().Be("base64url");
    }

    [Fact]
    public void PayloadModel_WithContentType_DeserializesFromJson()
    {
        var json = """
        {
            "data": "dGVzdA",
            "hash": "aGFzaA",
            "contentType": "application/json",
            "contentEncoding": "base64url"
        }
        """;

        var payload = JsonSerializer.Deserialize<PayloadModel>(json, CamelCaseOptions);

        payload.Should().NotBeNull();
        payload!.ContentType.Should().Be("application/json");
        payload.ContentEncoding.Should().Be("base64url");
    }

    [Fact]
    public void PayloadModel_LegacyWithoutContentFields_DefaultsToNull()
    {
        var json = """
        {
            "data": "SGVsbG8=",
            "hash": "aGFzaA"
        }
        """;

        var payload = JsonSerializer.Deserialize<PayloadModel>(json, CamelCaseOptions);

        payload.Should().NotBeNull();
        payload!.ContentType.Should().BeNull();
        payload.ContentEncoding.Should().BeNull();
    }

    [Fact]
    public void PayloadModel_ContentType_RoundTrips()
    {
        var original = new PayloadModel
        {
            Data = "dGVzdA",
            Hash = "aGFzaA",
            ContentType = "application/pdf",
            ContentEncoding = ContentEncodings.Base64Url,
            WalletAccess = new[] { "wallet1" }
        };

        var json = JsonSerializer.Serialize(original, CamelCaseOptions);
        var roundTripped = JsonSerializer.Deserialize<PayloadModel>(json, CamelCaseOptions);

        roundTripped.Should().NotBeNull();
        roundTripped!.ContentType.Should().Be("application/pdf");
        roundTripped.ContentEncoding.Should().Be(ContentEncodings.Base64Url);
        roundTripped.Data.Should().Be("dGVzdA");
    }

    [Fact]
    public void PayloadModel_NullContentType_NotSerializedWhenNull()
    {
        var payload = new PayloadModel
        {
            Data = "dGVzdA",
            Hash = "aGFzaA"
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        json.Should().NotContain("contentType");
        json.Should().NotContain("contentEncoding");
    }

    [Fact]
    public void PayloadModel_CompressedEncoding_SerializesCorrectly()
    {
        var payload = new PayloadModel
        {
            Data = "Y29tcHJlc3NlZA",
            Hash = "aGFzaA",
            ContentType = "application/json",
            ContentEncoding = ContentEncodings.BrotliBase64Url
        };

        var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
        var deserialized = JsonSerializer.Deserialize<PayloadModel>(json, CamelCaseOptions);

        deserialized!.ContentEncoding.Should().Be("br+base64url");
    }
}
