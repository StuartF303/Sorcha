// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Sorcha.UI.Core.Services.Forms;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Forms;

public class SchemaValidationTests
{
    private readonly FormSchemaService _sut = new();

    #region ValidateData — Full Form Validation

    [Fact]
    public void ValidateData_AllRequiredPresent_NoErrors()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "age": {"type": "integer"}
            },
            "required": ["name", "age"]
        }
        """);

        var data = new Dictionary<string, object?>
        {
            ["/name"] = "John",
            ["/age"] = 25
        };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateData_MultipleMissingRequired_MultipleErrors()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "firstName": {"type": "string"},
                "lastName": {"type": "string"},
                "email": {"type": "string"}
            },
            "required": ["firstName", "lastName", "email"]
        }
        """);

        var errors = _sut.ValidateData(schema, new Dictionary<string, object?>());

        errors.Should().HaveCount(3);
        errors.Should().ContainKey("/firstName");
        errors.Should().ContainKey("/lastName");
        errors.Should().ContainKey("/email");
    }

    [Fact]
    public void ValidateData_EmptyStringForRequired_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "required": ["name"]
        }
        """);

        var data = new Dictionary<string, object?> { ["/name"] = "" };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/name");
    }

    [Fact]
    public void ValidateData_NullValueForRequired_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "required": ["name"]
        }
        """);

        var data = new Dictionary<string, object?> { ["/name"] = null };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/name");
    }

    [Fact]
    public void ValidateData_NullSchema_ReturnsEmpty()
    {
        var errors = _sut.ValidateData(null, new Dictionary<string, object?> { ["/x"] = "value" });
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateData_StringTooShort_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "code": {"type": "string", "minLength": 5}
            }
        }
        """);

        var data = new Dictionary<string, object?> { ["/code"] = "AB" };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/code");
        errors["/code"].Should().Contain(e => e.Contains("at least 5"));
    }

    [Fact]
    public void ValidateData_StringTooLong_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "code": {"type": "string", "maxLength": 3}
            }
        }
        """);

        var data = new Dictionary<string, object?> { ["/code"] = "ABCDE" };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/code");
        errors["/code"].Should().Contain(e => e.Contains("at most 3"));
    }

    [Fact]
    public void ValidateData_NumberBelowMinimum_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "amount": {"type": "number", "minimum": 0.01}
            }
        }
        """);

        var data = new Dictionary<string, object?> { ["/amount"] = -10m };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/amount");
    }

    [Fact]
    public void ValidateData_NumberAboveMaximum_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "quantity": {"type": "integer", "maximum": 100}
            }
        }
        """);

        var data = new Dictionary<string, object?> { ["/quantity"] = 150m };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/quantity");
    }

    [Fact]
    public void ValidateData_InvalidEnumValue_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "currency": {"type": "string", "enum": ["USD", "EUR", "GBP"]}
            }
        }
        """);

        var data = new Dictionary<string, object?> { ["/currency"] = "JPY" };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().ContainKey("/currency");
        errors["/currency"].Should().Contain(e => e.Contains("Must be one of"));
    }

    [Fact]
    public void ValidateData_ValidEnumValue_NoError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "currency": {"type": "string", "enum": ["USD", "EUR", "GBP"]}
            }
        }
        """);

        var data = new Dictionary<string, object?> { ["/currency"] = "USD" };

        var errors = _sut.ValidateData(schema, data);
        errors.Should().NotContainKey("/currency");
    }

    #endregion

    #region ValidateField — Single Field Validation

    [Fact]
    public void ValidateField_ValidStringValue_NoErrors()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string", "minLength": 2}},
            "required": ["name"]
        }
        """);

        var errors = _sut.ValidateField(schema, "/name", "John");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateField_PatternMismatch_ReturnsError()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"email": {"type": "string", "pattern": "^\\S+@\\S+$"}}
        }
        """);

        var errors = _sut.ValidateField(schema, "/email", "invalid-email");
        errors.Should().Contain(e => e.Contains("pattern"));
    }

    [Fact]
    public void ValidateField_PatternMatch_NoErrors()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"code": {"type": "string", "pattern": "^[A-Z]{3}$"}}
        }
        """);

        var errors = _sut.ValidateField(schema, "/code", "USD");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateField_NullSchema_ReturnsEmpty()
    {
        var errors = _sut.ValidateField(null, "/field", "value");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateField_NonexistentScope_ReturnsEmpty()
    {
        var schema = JsonDocument.Parse("""{"type": "object", "properties": {"name": {"type": "string"}}}""");
        var errors = _sut.ValidateField(schema, "/nonexistent", "value");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateField_OptionalFieldEmpty_NoErrors()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"notes": {"type": "string"}}
        }
        """);

        var errors = _sut.ValidateField(schema, "/notes", "");
        errors.Should().BeEmpty();
    }

    #endregion

    #region GetSchemaForScope — Scope Resolution

    [Fact]
    public void GetSchemaForScope_TopLevelProperty_ReturnsSchema()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "amount": {"type": "number", "minimum": 0, "maximum": 99999}
            }
        }
        """);

        var result = _sut.GetSchemaForScope(schema, "/amount");

        result.Should().NotBeNull();
        result!.Value.GetProperty("type").GetString().Should().Be("number");
        result!.Value.GetProperty("minimum").GetInt32().Should().Be(0);
        result!.Value.GetProperty("maximum").GetInt32().Should().Be(99999);
    }

    [Fact]
    public void GetSchemaForScope_WithoutLeadingSlash_StillWorks()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string"}}
        }
        """);

        // NormalizeScope is called internally — this should still work
        var result = _sut.GetSchemaForScope(schema, "name");
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetSchemaForScope_NullSchema_ReturnsNull()
    {
        _sut.GetSchemaForScope(null, "/anything").Should().BeNull();
    }

    [Fact]
    public void GetSchemaForScope_EmptyScope_ReturnsNull()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{}}""");
        _sut.GetSchemaForScope(schema, "").Should().BeNull();
    }

    #endregion

    #region GetEnumValues — Enum Extraction

    [Fact]
    public void GetEnumValues_StringEnums_ReturnsList()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "status": {"type": "string", "enum": ["active", "inactive", "pending"]}
            }
        }
        """);

        var values = _sut.GetEnumValues(schema, "/status");
        values.Should().BeEquivalentTo(["active", "inactive", "pending"]);
    }

    [Fact]
    public void GetEnumValues_NoEnumProperty_ReturnsEmpty()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string"}}
        }
        """);

        _sut.GetEnumValues(schema, "/name").Should().BeEmpty();
    }

    [Fact]
    public void GetEnumValues_NullSchema_ReturnsEmpty()
    {
        _sut.GetEnumValues(null, "/field").Should().BeEmpty();
    }

    #endregion

    #region IsRequired — Required Field Detection

    [Fact]
    public void IsRequired_InRequiredArray_ReturnsTrue()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "required": ["name"]
        }
        """);

        _sut.IsRequired(schema, "/name").Should().BeTrue();
    }

    [Fact]
    public void IsRequired_NotInRequiredArray_ReturnsFalse()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {"name": {"type": "string"}, "notes": {"type": "string"}},
            "required": ["name"]
        }
        """);

        _sut.IsRequired(schema, "/notes").Should().BeFalse();
    }

    [Fact]
    public void IsRequired_NoRequiredArray_ReturnsFalse()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""");

        _sut.IsRequired(schema, "/name").Should().BeFalse();
    }

    [Fact]
    public void IsRequired_NullSchema_ReturnsFalse()
    {
        _sut.IsRequired(null, "/name").Should().BeFalse();
    }

    #endregion

    #region Invoice Approval Scenario — Integration

    [Fact]
    public void InvoiceApproval_SubmitInvoice_ValidatesCorrectly()
    {
        // Mirrors the simple-invoice-approval.json action 0 schema
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "invoiceNumber": {"type": "string", "minLength": 3, "maxLength": 50},
                "invoiceDate": {"type": "string", "format": "date"},
                "dueDate": {"type": "string", "format": "date"},
                "description": {"type": "string", "minLength": 5, "maxLength": 500},
                "amount": {"type": "number", "minimum": 0.01},
                "currency": {"type": "string", "enum": ["USD", "EUR", "GBP", "CAD"]},
                "taxAmount": {"type": "number", "minimum": 0}
            },
            "required": ["invoiceNumber", "invoiceDate", "dueDate", "description", "amount", "currency"]
        }
        """);

        // Valid submission
        var validData = new Dictionary<string, object?>
        {
            ["/invoiceNumber"] = "INV-001",
            ["/invoiceDate"] = "2026-02-12",
            ["/dueDate"] = "2026-03-12",
            ["/description"] = "Web development services",
            ["/amount"] = 5000m,
            ["/currency"] = "USD",
            ["/taxAmount"] = 500m
        };

        var errors = _sut.ValidateData(schema, validData);
        errors.Should().BeEmpty();

        // Missing required fields
        var invalidData = new Dictionary<string, object?>
        {
            ["/invoiceNumber"] = "AB",  // too short (minLength 3)
            ["/amount"] = -1m           // below minimum
            // Missing: invoiceDate, dueDate, description, currency
        };

        var errors2 = _sut.ValidateData(schema, invalidData);
        errors2.Should().ContainKey("/invoiceDate");
        errors2.Should().ContainKey("/dueDate");
        errors2.Should().ContainKey("/description");
        errors2.Should().ContainKey("/currency");
        errors2.Should().ContainKey("/invoiceNumber"); // too short
        errors2.Should().ContainKey("/amount"); // below minimum
    }

    [Fact]
    public void InvoiceApproval_AutoGenerate_CreatesCorrectControls()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "invoiceNumber": {"type": "string", "title": "Invoice Number"},
                "invoiceDate": {"type": "string", "format": "date", "title": "Invoice Date"},
                "description": {"type": "string", "title": "Description", "maxLength": 500},
                "amount": {"type": "number", "title": "Amount"},
                "currency": {"type": "string", "enum": ["USD", "EUR"], "title": "Currency"},
                "approved": {"type": "boolean", "title": "Approved"}
            }
        }
        """);

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements.Should().HaveCount(6);

        // Check each control type was inferred correctly
        var controls = form.Elements.ToDictionary(c => c.Scope, c => c);
        controls["/invoiceNumber"].ControlType.Should().Be(Sorcha.Blueprint.Models.ControlTypes.TextLine);
        controls["/invoiceDate"].ControlType.Should().Be(Sorcha.Blueprint.Models.ControlTypes.DateTime);
        controls["/description"].ControlType.Should().Be(Sorcha.Blueprint.Models.ControlTypes.TextLine); // maxLength <= 500
        controls["/amount"].ControlType.Should().Be(Sorcha.Blueprint.Models.ControlTypes.Numeric);
        controls["/currency"].ControlType.Should().Be(Sorcha.Blueprint.Models.ControlTypes.Selection);
        controls["/approved"].ControlType.Should().Be(Sorcha.Blueprint.Models.ControlTypes.Checkbox);
    }

    #endregion
}
