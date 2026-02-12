// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Forms;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Forms;

public class FormContextTests
{
    [Fact]
    public void SetValue_StringValue_StoresInFormData()
    {
        var ctx = new FormContext();
        ctx.SetValue("/name", "John");

        ctx.FormData["/name"].Should().Be("John");
    }

    [Fact]
    public void GetValue_ExistingKey_ReturnsTyped()
    {
        var ctx = new FormContext();
        ctx.FormData["/amount"] = 42.5m;

        ctx.GetValue<decimal>("/amount").Should().Be(42.5m);
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsDefault()
    {
        var ctx = new FormContext();

        ctx.GetValue<string>("/missing").Should().BeNull();
        ctx.GetValue<decimal>("/missing").Should().Be(0);
    }

    [Fact]
    public void SetValue_TriggersOnDataChanged()
    {
        var ctx = new FormContext();
        var fired = false;
        ctx.OnDataChanged += () => fired = true;

        ctx.SetValue("/field", "value");

        fired.Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_AllFieldsDisclosed_ReturnsTrue()
    {
        var ctx = new FormContext { AllFieldsDisclosed = true };

        ctx.IsDisclosed("/anything").Should().BeTrue();
        ctx.IsDisclosed("/nested/field").Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_ExactMatch_ReturnsTrue()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/amount");

        ctx.IsDisclosed("/amount").Should().BeTrue();
        ctx.IsDisclosed("/other").Should().BeFalse();
    }

    [Fact]
    public void IsDisclosed_WildcardMatch_ReturnsTrue()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/address/*");

        ctx.IsDisclosed("/address/city").Should().BeTrue();
        ctx.IsDisclosed("/address/zip").Should().BeTrue();
        ctx.IsDisclosed("/name").Should().BeFalse();
    }

    [Fact]
    public void IsDisclosed_EmptyScope_ReturnsTrue()
    {
        var ctx = new FormContext();
        ctx.IsDisclosed("").Should().BeTrue();
        ctx.IsDisclosed(null!).Should().BeTrue();
    }

    [Fact]
    public void SetErrors_AddsErrors()
    {
        var ctx = new FormContext();
        ctx.SetErrors("/name", ["Name is required"]);

        ctx.HasErrors.Should().BeTrue();
        ctx.GetErrors("/name").Should().Contain("Name is required");
    }

    [Fact]
    public void SetErrors_EmptyList_RemovesErrors()
    {
        var ctx = new FormContext();
        ctx.SetErrors("/name", ["Name is required"]);
        ctx.SetErrors("/name", []);

        ctx.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ClearErrors_RemovesAllErrors()
    {
        var ctx = new FormContext();
        ctx.SetErrors("/a", ["err1"]);
        ctx.SetErrors("/b", ["err2"]);

        ctx.ClearErrors();

        ctx.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void SetErrors_TriggersOnValidationChanged()
    {
        var ctx = new FormContext();
        var fired = false;
        ctx.OnValidationChanged += () => fired = true;

        ctx.SetErrors("/name", ["required"]);

        fired.Should().BeTrue();
    }
}
