// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Cryptography;
using FluentAssertions;
using Sorcha.Cryptography.SdJwt;
using Xunit;

namespace Sorcha.Cryptography.Tests.SdJwt;

/// <summary>
/// Tests for SD-JWT selective disclosure: create token with multiple claims,
/// present with only a subset, verify that only disclosed claims are visible.
/// </summary>
public class SdJwtSelectiveDisclosureTests
{
    private readonly SdJwtService _service = new();

    private static (byte[] privateKey, byte[] publicKey) GenerateP256KeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (ecdsa.ExportECPrivateKey(), ecdsa.ExportSubjectPublicKeyInfo());
    }

    [Fact]
    public async Task CreatePresentation_WithSubsetOfClaims_OnlyDisclosedClaimsVisible()
    {
        // Arrange — Create token with 5 disclosable claims
        var (privateKey, publicKey) = GenerateP256KeyPair();
        var claims = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 30,
            ["license_type"] = "ClassA",
            ["email"] = "alice@example.com",
            ["address"] = "123 Main St"
        };

        var token = await _service.CreateTokenAsync(
            claims,
            disclosableClaims: ["name", "age", "license_type", "email", "address"],
            issuer: "did:issuer:1",
            subject: "did:holder:alice",
            signingKey: privateKey,
            algorithm: "ES256");

        token.Disclosures.Should().HaveCount(5);

        // Act — Create presentation with only 2 claims disclosed
        var presentation = await _service.CreatePresentationAsync(
            token.RawToken,
            claimsToDisclose: ["name", "license_type"]);

        // Assert — Only 2 disclosures selected
        presentation.SelectedDisclosures.Should().HaveCount(2);
        presentation.RawPresentation.Should().NotBeNullOrWhiteSpace();

        // Verify the presentation — only disclosed claims should be visible
        var verifyResult = await _service.VerifyPresentationAsync(
            presentation.RawPresentation,
            publicKey,
            "ES256");

        verifyResult.IsValid.Should().BeTrue();
        verifyResult.Claims.Should().ContainKey("name").WhoseValue.Should().Be("Alice");
        verifyResult.Claims.Should().ContainKey("license_type").WhoseValue.Should().Be("ClassA");
        verifyResult.Claims.Should().NotContainKey("age");
        verifyResult.Claims.Should().NotContainKey("email");
        verifyResult.Claims.Should().NotContainKey("address");
    }

    [Fact]
    public async Task CreatePresentation_AllClaimsDisclosed_AllVisible()
    {
        // Arrange
        var (privateKey, publicKey) = GenerateP256KeyPair();
        var claims = new Dictionary<string, object>
        {
            ["name"] = "Bob",
            ["role"] = "engineer",
            ["clearance"] = "top-secret"
        };

        var token = await _service.CreateTokenAsync(
            claims,
            disclosableClaims: ["name", "role", "clearance"],
            issuer: "did:issuer:1",
            subject: "did:holder:bob",
            signingKey: privateKey,
            algorithm: "ES256");

        // Act — Disclose all claims
        var presentation = await _service.CreatePresentationAsync(
            token.RawToken,
            claimsToDisclose: ["name", "role", "clearance"]);

        // Assert
        var verifyResult = await _service.VerifyPresentationAsync(
            presentation.RawPresentation, publicKey, "ES256");

        verifyResult.IsValid.Should().BeTrue();
        verifyResult.Claims.Should().ContainKey("name").WhoseValue.Should().Be("Bob");
        verifyResult.Claims.Should().ContainKey("role").WhoseValue.Should().Be("engineer");
        verifyResult.Claims.Should().ContainKey("clearance").WhoseValue.Should().Be("top-secret");
    }

    [Fact]
    public async Task CreatePresentation_NoClaimsDisclosed_NoneVisible()
    {
        // Arrange
        var (privateKey, publicKey) = GenerateP256KeyPair();
        var claims = new Dictionary<string, object>
        {
            ["name"] = "Charlie",
            ["license_type"] = "ClassB"
        };

        var token = await _service.CreateTokenAsync(
            claims,
            disclosableClaims: ["name", "license_type"],
            issuer: "did:issuer:1",
            subject: "did:holder:charlie",
            signingKey: privateKey,
            algorithm: "ES256");

        // Act — Disclose no claims (empty list)
        var presentation = await _service.CreatePresentationAsync(
            token.RawToken,
            claimsToDisclose: []);

        // Assert — No disclosable claims visible
        presentation.SelectedDisclosures.Should().BeEmpty();
        var verifyResult = await _service.VerifyPresentationAsync(
            presentation.RawPresentation, publicKey, "ES256");

        verifyResult.IsValid.Should().BeTrue();
        verifyResult.Claims.Should().NotContainKey("name");
        verifyResult.Claims.Should().NotContainKey("license_type");
    }

    [Fact]
    public async Task CreatePresentation_MixedDisclosableAndNonDisclosable_CorrectVisibility()
    {
        // Arrange — "type" and "vct" are non-disclosable (always visible),
        // other claims are selectively disclosable
        var (privateKey, publicKey) = GenerateP256KeyPair();
        var claims = new Dictionary<string, object>
        {
            ["type"] = "LicenseCredential",
            ["vct"] = "LicenseCredential",
            ["holder_name"] = "Alice",
            ["license_type"] = "ClassA",
            ["license_number"] = "LIC-001"
        };

        var token = await _service.CreateTokenAsync(
            claims,
            disclosableClaims: ["holder_name", "license_type", "license_number"], // type/vct NOT in disclosable list
            issuer: "did:issuer:gov",
            subject: "did:holder:alice",
            signingKey: privateKey,
            algorithm: "ES256");

        // Act — Disclose only holder_name
        var presentation = await _service.CreatePresentationAsync(
            token.RawToken,
            claimsToDisclose: ["holder_name"]);

        // Assert
        var verifyResult = await _service.VerifyPresentationAsync(
            presentation.RawPresentation, publicKey, "ES256");

        verifyResult.IsValid.Should().BeTrue();
        // Non-disclosable claims always visible in JWT payload
        verifyResult.Claims.Should().ContainKey("type").WhoseValue.Should().Be("LicenseCredential");
        verifyResult.Claims.Should().ContainKey("vct").WhoseValue.Should().Be("LicenseCredential");
        // Disclosed claim visible
        verifyResult.Claims.Should().ContainKey("holder_name").WhoseValue.Should().Be("Alice");
        // Non-disclosed disclosable claims hidden
        verifyResult.Claims.Should().NotContainKey("license_type");
        verifyResult.Claims.Should().NotContainKey("license_number");
    }

    [Fact]
    public async Task VerifyPresentation_SignatureStillValid_AfterSelectiveDisclosure()
    {
        // Arrange — The signature covers the JWT (header.payload), not the disclosures.
        // Removing disclosures should not affect signature validity.
        var (privateKey, publicKey) = GenerateP256KeyPair();
        var claims = new Dictionary<string, object>
        {
            ["claim1"] = "value1",
            ["claim2"] = "value2",
            ["claim3"] = "value3",
            ["claim4"] = "value4",
            ["claim5"] = "value5"
        };

        var token = await _service.CreateTokenAsync(
            claims,
            disclosableClaims: null, // all disclosable
            issuer: "did:issuer:1",
            subject: "did:holder:1",
            signingKey: privateKey,
            algorithm: "ES256");

        // Act — Create multiple presentations with different subsets
        var presentation1 = await _service.CreatePresentationAsync(
            token.RawToken, claimsToDisclose: ["claim1", "claim2"]);
        var presentation2 = await _service.CreatePresentationAsync(
            token.RawToken, claimsToDisclose: ["claim3", "claim4", "claim5"]);
        var presentation3 = await _service.CreatePresentationAsync(
            token.RawToken, claimsToDisclose: ["claim1"]);

        // Assert — All presentations verify successfully
        var result1 = await _service.VerifyPresentationAsync(
            presentation1.RawPresentation, publicKey, "ES256");
        var result2 = await _service.VerifyPresentationAsync(
            presentation2.RawPresentation, publicKey, "ES256");
        var result3 = await _service.VerifyPresentationAsync(
            presentation3.RawPresentation, publicKey, "ES256");

        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
        result3.IsValid.Should().BeTrue();

        result1.Claims.Should().HaveCount(2);
        result2.Claims.Should().HaveCount(3);
        result3.Claims.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreatePresentation_NonExistentClaim_IgnoredGracefully()
    {
        // Arrange
        var (privateKey, publicKey) = GenerateP256KeyPair();
        var claims = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["role"] = "engineer"
        };

        var token = await _service.CreateTokenAsync(
            claims,
            disclosableClaims: ["name", "role"],
            issuer: "did:issuer:1",
            subject: "did:holder:alice",
            signingKey: privateKey,
            algorithm: "ES256");

        // Act — Request to disclose a claim that doesn't exist
        var presentation = await _service.CreatePresentationAsync(
            token.RawToken,
            claimsToDisclose: ["name", "nonexistent_claim"]);

        // Assert — Only the existing claim is disclosed
        presentation.SelectedDisclosures.Should().HaveCount(1); // only "name"
        var verifyResult = await _service.VerifyPresentationAsync(
            presentation.RawPresentation, publicKey, "ES256");

        verifyResult.IsValid.Should().BeTrue();
        verifyResult.Claims.Should().ContainKey("name");
        verifyResult.Claims.Should().NotContainKey("nonexistent_claim");
    }
}
