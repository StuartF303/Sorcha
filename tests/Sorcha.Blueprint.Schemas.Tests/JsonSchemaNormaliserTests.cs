// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests;

public class JsonSchemaNormaliserTests
{
    [Fact]
    public void Normalise_Draft04_RenamesIdToJsonId()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-04/schema#",
                "id": "http://example.com/test",
                "type": "object"
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var root = result.RootElement;

        root.TryGetProperty("$id", out var id).Should().BeTrue();
        id.GetString().Should().Be("http://example.com/test");
        root.TryGetProperty("id", out _).Should().BeFalse();
        root.GetProperty("$schema").GetString().Should().Contain("2020-12");
    }

    [Fact]
    public void Normalise_Draft04_RenamesDefinitionsToJsonDefs()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-04/schema#",
                "type": "object",
                "definitions": {
                    "address": { "type": "object" }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var root = result.RootElement;

        root.TryGetProperty("$defs", out _).Should().BeTrue();
        root.TryGetProperty("definitions", out _).Should().BeFalse();
    }

    [Fact]
    public void Normalise_Draft04_ConvertsExclusiveMinimumBooleanToNumber()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-04/schema#",
                "type": "object",
                "properties": {
                    "age": {
                        "type": "integer",
                        "minimum": 0,
                        "exclusiveMinimum": true
                    }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var ageProps = result.RootElement.GetProperty("properties").GetProperty("age");

        ageProps.GetProperty("exclusiveMinimum").GetDouble().Should().Be(0);
        ageProps.TryGetProperty("minimum", out _).Should().BeFalse();
    }

    [Fact]
    public void Normalise_Draft04_ConvertsExclusiveMaximumBooleanToNumber()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-04/schema#",
                "type": "object",
                "properties": {
                    "score": {
                        "type": "number",
                        "maximum": 100,
                        "exclusiveMaximum": true
                    }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var scoreProps = result.RootElement.GetProperty("properties").GetProperty("score");

        scoreProps.GetProperty("exclusiveMaximum").GetDouble().Should().Be(100);
        scoreProps.TryGetProperty("maximum", out _).Should().BeFalse();
    }

    [Fact]
    public void Normalise_Draft06_ConvertsExclusiveMinimumBoolean()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-06/schema#",
                "type": "object",
                "properties": {
                    "quantity": {
                        "type": "integer",
                        "minimum": 1,
                        "exclusiveMinimum": true
                    }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var quantity = result.RootElement.GetProperty("properties").GetProperty("quantity");

        quantity.GetProperty("exclusiveMinimum").GetDouble().Should().Be(1);
    }

    [Fact]
    public void Normalise_Draft07_RenamesDefinitionsToJsonDefs()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-07/schema#",
                "type": "object",
                "definitions": {
                    "name": { "type": "string" }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var root = result.RootElement;

        root.TryGetProperty("$defs", out _).Should().BeTrue();
        root.TryGetProperty("definitions", out _).Should().BeFalse();
    }

    [Fact]
    public void Normalise_Already202012_NoChangesExceptSchemaUri()
    {
        var schema = CreateSchema("""
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "$id": "http://example.com/test",
                "type": "object",
                "$defs": {
                    "address": { "type": "object" }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var root = result.RootElement;

        root.GetProperty("$id").GetString().Should().Be("http://example.com/test");
        root.TryGetProperty("$defs", out _).Should().BeTrue();
    }

    [Fact]
    public void ExtractMetadata_ReturnsFieldCountAndNames()
    {
        var schema = CreateSchema("""
            {
                "type": "object",
                "title": "Invoice Document",
                "description": "A commercial invoice for trade",
                "properties": {
                    "invoiceNumber": { "type": "string" },
                    "amount": { "type": "number" },
                    "currency": { "type": "string" }
                },
                "required": ["invoiceNumber", "amount"]
            }
            """);

        var metadata = JsonSchemaNormaliser.ExtractMetadata(schema);

        metadata.FieldCount.Should().Be(3);
        metadata.FieldNames.Should().Contain("invoiceNumber", "amount", "currency");
        metadata.RequiredFields.Should().Contain("invoiceNumber", "amount");
        metadata.RequiredFields.Should().NotContain("currency");
    }

    [Fact]
    public void ExtractMetadata_ExtractsKeywordsFromTitleAndDescription()
    {
        var schema = CreateSchema("""
            {
                "type": "object",
                "title": "Patient Resource",
                "description": "Healthcare patient demographics information"
            }
            """);

        var metadata = JsonSchemaNormaliser.ExtractMetadata(schema);

        metadata.Keywords.Should().Contain("patient");
        metadata.Keywords.Should().Contain("healthcare");
    }

    [Fact]
    public void ExtractMetadata_NoProperties_ReturnsZeroFieldCount()
    {
        var schema = CreateSchema("""{ "type": "string" }""");

        var metadata = JsonSchemaNormaliser.ExtractMetadata(schema);

        metadata.FieldCount.Should().Be(0);
        metadata.FieldNames.Should().BeEmpty();
    }

    [Fact]
    public void ComputeContentHash_ReturnsSha256Hex()
    {
        var schema = CreateSchema("""{ "type": "object" }""");

        var hash = JsonSchemaNormaliser.ComputeContentHash(schema);

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ComputeContentHash_SameContent_ReturnsSameHash()
    {
        var schema1 = CreateSchema("""{ "type": "object" }""");
        var schema2 = CreateSchema("""{ "type": "object" }""");

        JsonSchemaNormaliser.ComputeContentHash(schema1)
            .Should().Be(JsonSchemaNormaliser.ComputeContentHash(schema2));
    }

    [Fact]
    public void DetectDraft_IdentifiesDraft04()
    {
        var root = JsonNode.Parse("""{ "$schema": "http://json-schema.org/draft-04/schema#" }""")!.AsObject();
        JsonSchemaNormaliser.DetectDraft(root).Should().Be("draft-04");
    }

    [Fact]
    public void DetectDraft_IdentifiesDraft07()
    {
        var root = JsonNode.Parse("""{ "$schema": "http://json-schema.org/draft-07/schema#" }""")!.AsObject();
        JsonSchemaNormaliser.DetectDraft(root).Should().Be("draft-07");
    }

    [Fact]
    public void DetectDraft_InfersDraft04FromIdKeyword()
    {
        var root = JsonNode.Parse("""{ "id": "http://example.com/schema", "type": "object" }""")!.AsObject();
        JsonSchemaNormaliser.DetectDraft(root).Should().Be("draft-04");
    }

    [Fact]
    public void Normalise_NestedDraft04_NormalisesRecursively()
    {
        var schema = CreateSchema("""
            {
                "$schema": "http://json-schema.org/draft-04/schema#",
                "type": "object",
                "properties": {
                    "address": {
                        "type": "object",
                        "properties": {
                            "zip": {
                                "type": "integer",
                                "minimum": 10000,
                                "exclusiveMinimum": true
                            }
                        }
                    }
                }
            }
            """);

        var result = JsonSchemaNormaliser.Normalise(schema);
        var zip = result.RootElement
            .GetProperty("properties")
            .GetProperty("address")
            .GetProperty("properties")
            .GetProperty("zip");

        zip.GetProperty("exclusiveMinimum").GetDouble().Should().Be(10000);
        zip.TryGetProperty("minimum", out _).Should().BeFalse();
    }

    private static JsonDocument CreateSchema(string json) => JsonDocument.Parse(json);
}
