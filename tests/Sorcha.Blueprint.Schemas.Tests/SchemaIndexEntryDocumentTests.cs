// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Schemas.Repositories;

namespace Sorcha.Blueprint.Schemas.Tests;

public class SchemaIndexEntryDocumentTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var doc = new SchemaIndexEntryDocument
        {
            SourceProvider = "TestProvider",
            SourceUri = "http://example.com/schema",
            Title = "Test Schema",
            SectorTags = ["general"]
        };

        doc.SchemaVersion.Should().Be("1.0.0");
        doc.JsonSchemaDraft.Should().Be("2020-12");
        doc.Status.Should().Be(nameof(SchemaIndexStatus.Active));
        doc.FieldCount.Should().Be(0);
        doc.Id.Should().BeNull();
    }

    [Fact]
    public void Status_CanBeSetToAllValues()
    {
        var doc = CreateTestDocument();

        doc.Status = nameof(SchemaIndexStatus.Active);
        doc.Status.Should().Be("Active");

        doc.Status = nameof(SchemaIndexStatus.Deprecated);
        doc.Status.Should().Be("Deprecated");

        doc.Status = nameof(SchemaIndexStatus.Unavailable);
        doc.Status.Should().Be("Unavailable");
    }

    [Fact]
    public void Document_WithAllFieldsPopulated_CanBeCreated()
    {
        var doc = new SchemaIndexEntryDocument
        {
            Id = "test-id",
            SourceProvider = "HL7 FHIR",
            SourceUri = "http://hl7.org/fhir/Patient",
            Title = "FHIR Patient",
            Description = "A patient resource from HL7 FHIR",
            SectorTags = ["healthcare"],
            Keywords = ["patient", "demographics", "healthcare"],
            FieldCount = 25,
            FieldNames = ["name", "birthDate", "gender"],
            RequiredFields = ["name"],
            SchemaVersion = "R5",
            JsonSchemaDraft = "2020-12",
            ContentHash = "abc123def456",
            Status = "Active",
            LastFetchedAt = DateTimeOffset.UtcNow,
            DateAdded = DateTimeOffset.UtcNow.AddDays(-30),
            DateModified = DateTimeOffset.UtcNow
        };

        doc.SourceProvider.Should().Be("HL7 FHIR");
        doc.FieldCount.Should().Be(25);
        doc.SectorTags.Should().Contain("healthcare");
    }

    [Fact]
    public void SearchResult_ContainsCorrectData()
    {
        var docs = new List<SchemaIndexEntryDocument>
        {
            CreateTestDocument("Provider1", "uri1"),
            CreateTestDocument("Provider2", "uri2")
        };

        var result = new SchemaIndexSearchResult(docs, 2, null);

        result.Results.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public void SearchResult_WithCursor_IndicatesMoreResults()
    {
        var docs = new List<SchemaIndexEntryDocument>
        {
            CreateTestDocument()
        };

        var result = new SchemaIndexSearchResult(docs, 100, "next-page-cursor");

        result.NextCursor.Should().Be("next-page-cursor");
        result.TotalCount.Should().Be(100);
    }

    private static SchemaIndexEntryDocument CreateTestDocument(
        string provider = "TestProvider",
        string uri = "http://example.com/schema") => new()
    {
        SourceProvider = provider,
        SourceUri = uri,
        Title = "Test Schema",
        SectorTags = ["general"]
    };
}
