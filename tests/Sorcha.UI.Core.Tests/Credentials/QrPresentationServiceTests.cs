// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Credentials;

public class QrPresentationServiceTests
{
    private readonly QrPresentationService _service = new();

    [Fact]
    public void BuildOid4vpUrl_ValidInputs_ReturnsCorrectUrl()
    {
        var url = _service.BuildOid4vpUrl(
            "https://sorcha.example/api/v1/presentations/req-123",
            "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4");

        url.Should().StartWith("openid4vp://authorize?request_uri=");
        url.Should().Contain("nonce=a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4");
        url.Should().Contain(Uri.EscapeDataString("https://sorcha.example/api/v1/presentations/req-123"));
    }

    [Fact]
    public void BuildOid4vpUrl_EmptyRequestUrl_Throws()
    {
        var act = () => _service.BuildOid4vpUrl("", "nonce123");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildOid4vpUrl_EmptyNonce_Throws()
    {
        var act = () => _service.BuildOid4vpUrl("https://example.com/req-1", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSvg_ValidInputs_ReturnsSvgMarkup()
    {
        var svg = _service.GenerateSvg(
            "https://sorcha.example/api/v1/presentations/req-123",
            "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4");

        svg.Should().NotBeNullOrEmpty();
        svg.Should().Contain("<svg");
        svg.Should().Contain("</svg>");
    }

    [Fact]
    public void GeneratePngDataUri_ValidInputs_ReturnsDataUri()
    {
        var dataUri = _service.GeneratePngDataUri(
            "https://sorcha.example/api/v1/presentations/req-123",
            "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4");

        dataUri.Should().StartWith("data:image/png;base64,");
        dataUri.Length.Should().BeGreaterThan(50);
    }

    [Fact]
    public void GenerateSvg_DifferentUrls_ProduceDifferentOutput()
    {
        var svg1 = _service.GenerateSvg("https://example.com/req-1", "nonce1nonce1nonce1nonce1nonce1no");
        var svg2 = _service.GenerateSvg("https://example.com/req-2", "nonce2nonce2nonce2nonce2nonce2no");

        svg1.Should().NotBe(svg2);
    }

    [Fact]
    public void GenerateSvg_CustomPixelsPerModule_ProducesOutput()
    {
        var svg = _service.GenerateSvg(
            "https://example.com/req-1",
            "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4",
            pixelsPerModule: 20);

        svg.Should().Contain("<svg");
    }
}
