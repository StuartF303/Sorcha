// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Sorcha.Blueprint.Models;
using Sorcha.UI.Core.Services.Forms;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Forms;

public class AutoGenerationTests
{
    private readonly FormSchemaService _sut = new();

    [Fact]
    public void AutoGenerateForm_StringProperty_GeneratesTextLine()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string","title":"Full Name"}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.ControlType.Should().Be(ControlTypes.Layout);
        form.Elements.Should().HaveCount(1);
        form.Elements[0].ControlType.Should().Be(ControlTypes.TextLine);
        form.Elements[0].Title.Should().Be("Full Name");
        form.Elements[0].Scope.Should().Be("/name");
    }

    [Fact]
    public void AutoGenerateForm_NumberProperty_GeneratesNumeric()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"amount":{"type":"number"}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].ControlType.Should().Be(ControlTypes.Numeric);
    }

    [Fact]
    public void AutoGenerateForm_IntegerProperty_GeneratesNumeric()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"count":{"type":"integer"}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].ControlType.Should().Be(ControlTypes.Numeric);
    }

    [Fact]
    public void AutoGenerateForm_BooleanProperty_GeneratesCheckbox()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"approved":{"type":"boolean"}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].ControlType.Should().Be(ControlTypes.Checkbox);
    }

    [Fact]
    public void AutoGenerateForm_DateFormat_GeneratesDateTime()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"dueDate":{"type":"string","format":"date"}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].ControlType.Should().Be(ControlTypes.DateTime);
    }

    [Fact]
    public void AutoGenerateForm_EnumProperty_GeneratesSelection()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"currency":{"type":"string","enum":["USD","EUR"]}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].ControlType.Should().Be(ControlTypes.Selection);
    }

    [Fact]
    public void AutoGenerateForm_LongString_GeneratesTextArea()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"description":{"type":"string","maxLength":1000}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].ControlType.Should().Be(ControlTypes.TextArea);
    }

    [Fact]
    public void AutoGenerateForm_NoTitle_HumanizesPropertyName()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"invoiceNumber":{"type":"string"}}}""");

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements[0].Title.Should().Be("Invoice Number");
    }

    [Fact]
    public void AutoGenerateForm_NullSchemas_ReturnsEmptyLayout()
    {
        var form = _sut.AutoGenerateForm(null);

        form.ControlType.Should().Be(ControlTypes.Layout);
        form.Elements.Should().BeEmpty();
    }

    [Fact]
    public void AutoGenerateForm_MultipleProperties_GeneratesAllControls()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "amount": {"type": "number"},
                "approved": {"type": "boolean"}
            }
        }
        """);

        var form = _sut.AutoGenerateForm([schema]);

        form.Elements.Should().HaveCount(3);
    }
}
