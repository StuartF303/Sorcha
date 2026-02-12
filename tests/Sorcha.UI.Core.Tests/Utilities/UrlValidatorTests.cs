// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Utilities;
using Xunit;

namespace Sorcha.UI.Core.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="UrlValidator"/> security validation.
/// </summary>
public class UrlValidatorTests
{
    private static readonly Uri BaseUri = new("https://localhost:5001/");

    #region Valid Relative Paths

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/app/registers/123")]
    [InlineData("/app/wallets")]
    [InlineData("/")]
    [InlineData("/auth/logout")]
    [InlineData("/some/deep/nested/path")]
    public void IsValidReturnUrl_ValidRelativePath_ReturnsTrue(string url)
    {
        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeTrue($"'{url}' is a valid relative path");
    }

    #endregion

    #region Invalid Relative Paths

    [Theory]
    [InlineData("dashboard")]  // Not a path (missing leading /)
    [InlineData("app/registers")]  // Not a path (missing leading /)
    [InlineData("//evil.com/path")]  // Protocol-relative URL
    [InlineData("//localhost/path")]  // Protocol-relative even to same host
    public void IsValidReturnUrl_InvalidRelativePath_ReturnsFalse(string url)
    {
        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeFalse($"'{url}' is not a valid relative path");
    }

    #endregion

    #region Same-Origin Absolute URLs

    [Fact]
    public void IsValidReturnUrl_SameOriginAbsoluteUrl_ReturnsTrue()
    {
        // Arrange
        var url = "https://localhost:5001/dashboard";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeTrue("same-origin absolute URLs are safe");
    }

    [Fact]
    public void IsValidReturnUrl_SameOriginWithPath_ReturnsTrue()
    {
        // Arrange
        var url = "https://localhost:5001/app/registers/123";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeTrue("same-origin absolute URLs with paths are safe");
    }

    #endregion

    #region Different Origin URLs

    [Theory]
    [InlineData("https://evil.com/")]
    [InlineData("https://evil.com/dashboard")]
    [InlineData("http://localhost:5001/")]  // Different scheme
    [InlineData("https://localhost:5002/")]  // Different port
    [InlineData("https://different.host:5001/")]  // Different host
    public void IsValidReturnUrl_DifferentOrigin_ReturnsFalse(string url)
    {
        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeFalse($"'{url}' is a different origin and should be rejected");
    }

    #endregion

    #region Dangerous Schemes

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("javascript:void(0)")]
    [InlineData("JAVASCRIPT:alert('xss')")]  // Case insensitive
    [InlineData("JavaScript:document.cookie")]
    public void IsValidReturnUrl_JavaScriptScheme_ReturnsFalse(string url)
    {
        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeFalse($"'{url}' uses javascript: scheme and should be rejected");
    }

    [Theory]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("DATA:text/plain,test")]  // Case insensitive
    public void IsValidReturnUrl_DataScheme_ReturnsFalse(string url)
    {
        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeFalse($"'{url}' uses data: scheme and should be rejected");
    }

    #endregion

    #region Null/Empty/Whitespace Handling

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void IsValidReturnUrl_NullOrWhitespace_ReturnsFalse(string? url)
    {
        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeFalse("null/empty/whitespace URLs should be rejected");
    }

    #endregion

    #region String BaseUri Overload

    [Fact]
    public void IsValidReturnUrl_StringBaseUri_ValidUrl_ReturnsTrue()
    {
        // Arrange
        var url = "/dashboard";
        var baseUri = "https://localhost:5001/";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, baseUri);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidReturnUrl_InvalidStringBaseUri_ReturnsFalse()
    {
        // Arrange
        var url = "/dashboard";
        var baseUri = "not-a-valid-uri";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, baseUri);

        // Assert
        result.Should().BeFalse("invalid base URI should cause validation to fail");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsValidReturnUrl_UrlWithQueryString_ReturnsTrue()
    {
        // Arrange
        var url = "/app/registers?page=1&size=10";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeTrue("relative URLs with query strings are safe");
    }

    [Fact]
    public void IsValidReturnUrl_UrlWithFragment_ReturnsTrue()
    {
        // Arrange
        var url = "/app/registers#section1";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeTrue("relative URLs with fragments are safe");
    }

    [Fact]
    public void IsValidReturnUrl_UrlEncodedPath_ReturnsTrue()
    {
        // Arrange
        var url = "/app/registers/some%20name";

        // Act
        var result = UrlValidator.IsValidReturnUrl(url, BaseUri);

        // Assert
        result.Should().BeTrue("URL-encoded relative paths are safe");
    }

    #endregion
}
