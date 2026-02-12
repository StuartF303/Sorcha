// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Forms;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Forms;

public class DisclosureFilterTests
{
    [Fact]
    public void IsDisclosed_AllFieldsDisclosed_AlwaysReturnsTrue()
    {
        var ctx = new FormContext { AllFieldsDisclosed = true };

        ctx.IsDisclosed("/field1").Should().BeTrue();
        ctx.IsDisclosed("/deeply/nested/field").Should().BeTrue();
        ctx.IsDisclosed("/anything").Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_ExactPointerMatch_ReturnsTrue()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/approved");
        ctx.DisclosureFilter.Add("/paymentDate");

        ctx.IsDisclosed("/approved").Should().BeTrue();
        ctx.IsDisclosed("/paymentDate").Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_NoMatch_ReturnsFalse()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/approved");

        ctx.IsDisclosed("/secret").Should().BeFalse();
        ctx.IsDisclosed("/other").Should().BeFalse();
    }

    [Fact]
    public void IsDisclosed_RootWildcard_MatchesAll()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/*");

        ctx.IsDisclosed("/field1").Should().BeTrue();
        ctx.IsDisclosed("/field2").Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_NestedWildcard_MatchesChildren()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/address/*");

        ctx.IsDisclosed("/address/city").Should().BeTrue();
        ctx.IsDisclosed("/address/street").Should().BeTrue();
        ctx.IsDisclosed("/address/zip").Should().BeTrue();
        ctx.IsDisclosed("/name").Should().BeFalse();
    }

    [Fact]
    public void IsDisclosed_EmptyScope_AlwaysReturnsTrue()
    {
        var ctx = new FormContext();
        // Layout controls have empty scope — always shown
        ctx.IsDisclosed("").Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_NullScope_AlwaysReturnsTrue()
    {
        var ctx = new FormContext();
        ctx.IsDisclosed(null!).Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_EmptyFilter_NotAllDisclosed_ReturnsFalse()
    {
        var ctx = new FormContext();
        // No disclosures set, not all fields disclosed
        ctx.IsDisclosed("/someField").Should().BeFalse();
    }

    [Fact]
    public void IsDisclosed_DeepNestedWildcard_MatchesGrandchildren()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/address/*");

        // /address/* should match /address/anything
        ctx.IsDisclosed("/address/country").Should().BeTrue();
    }

    [Fact]
    public void IsDisclosed_MixedExactAndWildcard_BothWork()
    {
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/name");
        ctx.DisclosureFilter.Add("/address/*");

        ctx.IsDisclosed("/name").Should().BeTrue();
        ctx.IsDisclosed("/address/city").Should().BeTrue();
        ctx.IsDisclosed("/email").Should().BeFalse();
    }

    [Fact]
    public void DisclosureFilter_VendorScenario_SeesCorrectFields()
    {
        // From simple-invoice-approval: vendor sees /approved, /paymentDate, /paymentMethod, /notes
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/approved");
        ctx.DisclosureFilter.Add("/paymentDate");
        ctx.DisclosureFilter.Add("/paymentMethod");
        ctx.DisclosureFilter.Add("/notes");

        ctx.IsDisclosed("/approved").Should().BeTrue();
        ctx.IsDisclosed("/paymentDate").Should().BeTrue();
        ctx.IsDisclosed("/paymentMethod").Should().BeTrue();
        ctx.IsDisclosed("/notes").Should().BeTrue();
        ctx.IsDisclosed("/internalComments").Should().BeFalse();
    }

    [Fact]
    public void DisclosureFilter_AccountsPayableWildcard_SeesAll()
    {
        // From simple-invoice-approval: AP sees /* (all fields)
        var ctx = new FormContext();
        ctx.DisclosureFilter.Add("/*");

        ctx.IsDisclosed("/invoiceNumber").Should().BeTrue();
        ctx.IsDisclosed("/amount").Should().BeTrue();
        ctx.IsDisclosed("/anyField").Should().BeTrue();
    }
}
