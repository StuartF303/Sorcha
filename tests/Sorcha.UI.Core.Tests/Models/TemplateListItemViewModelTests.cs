// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Xunit;
using FluentAssertions;
using Sorcha.UI.Core.Models.Templates;

namespace Sorcha.UI.Core.Tests.Models;

public class TemplateListItemViewModelTests
{
    [Fact]
    public void Version_DefaultsToOne()
    {
        var vm = new TemplateListItemViewModel();

        vm.Version.Should().Be(1);
    }

    [Fact]
    public void Version_CanBeSetExplicitly()
    {
        var vm = new TemplateListItemViewModel { Version = 3 };

        vm.Version.Should().Be(3);
    }

    [Fact]
    public void RecordEquality_IncludesVersion()
    {
        var a = new TemplateListItemViewModel { Id = "t1", Title = "Test", Version = 1 };
        var c = new TemplateListItemViewModel { Id = "t1", Title = "Test", Version = 2 };

        // Records with List properties use reference equality for the list,
        // so test that Version is included in equality via with-expression
        var b = a with { Version = 2 };
        b.Should().BeEquivalentTo(c);

        a.Should().NotBeEquivalentTo(c, opts => opts.ComparingByMembers<TemplateListItemViewModel>(),
            "Different Version values should make records non-equivalent");
    }
}
