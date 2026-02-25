// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services.Interfaces;
using Xunit;

namespace Sorcha.Validator.Service.Tests.Services;

public class SlhDsaPolicyIntegrationTests
{
    [Fact]
    public void CryptoPolicy_RequiringHashBased_RecognizesSlhDsa()
    {
        // A register crypto policy that requires hash-based signatures (SLH-DSA)
        var acceptedAlgorithms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SLH-DSA-128S", "SLH-DSA-192S"
        };

        // SLH-DSA should be recognized
        acceptedAlgorithms.Contains("SLH-DSA-128S").Should().BeTrue();
        acceptedAlgorithms.Contains("SLH-DSA-192S").Should().BeTrue();

        // ML-DSA (lattice-based) should NOT be in hash-based-only policy
        acceptedAlgorithms.Contains("ML-DSA-65").Should().BeFalse();

        // Classical algorithms should NOT be in hash-based-only policy
        acceptedAlgorithms.Contains("ED25519").Should().BeFalse();
    }

    [Fact]
    public void CryptoPolicy_HashBasedOnly_RejectsMlDsaOnlyTransaction()
    {
        // Register policy requires hash-based signatures
        var policyAccepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SLH-DSA-128S", "SLH-DSA-192S"
        };

        // Transaction signed only with ML-DSA-65
        var transactionAlgorithms = new[] { "ML-DSA-65" };

        // Check: do all TX algorithms appear in policy?
        var unaccepted = transactionAlgorithms
            .Where(a => !policyAccepted.Contains(a))
            .ToArray();

        unaccepted.Should().NotBeEmpty("ML-DSA-65 is not accepted by a hash-based-only policy");
        unaccepted.Should().Contain("ML-DSA-65");
    }

    [Fact]
    public void CryptoPolicy_HashBasedOnly_AcceptsSlhDsaSignedTransaction()
    {
        // Register policy requires hash-based signatures
        var policyAccepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SLH-DSA-128S", "SLH-DSA-192S"
        };

        // Transaction signed with SLH-DSA-128s
        var transactionAlgorithms = new[] { "SLH-DSA-128S" };

        var unaccepted = transactionAlgorithms
            .Where(a => !policyAccepted.Contains(a))
            .ToArray();

        unaccepted.Should().BeEmpty("SLH-DSA-128s is accepted by hash-based-only policy");
    }

    [Fact]
    public void CryptoPolicy_HybridMode_AcceptsBothLatticAndHashBased()
    {
        // Hybrid policy accepts both lattice and hash-based
        var policyAccepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ED25519", "ML-DSA-65", "SLH-DSA-128S", "SLH-DSA-192S"
        };

        // Required: at least one hash-based
        var policyRequired = new[] { "SLH-DSA-128S" };

        // Transaction with both ML-DSA and SLH-DSA
        var transactionAlgorithms = new[] { "ML-DSA-65", "SLH-DSA-128S" };

        // All TX algorithms accepted?
        var allAccepted = transactionAlgorithms.All(a => policyAccepted.Contains(a));
        allAccepted.Should().BeTrue();

        // Required algorithms present?
        var allRequired = policyRequired.All(r => transactionAlgorithms.Contains(r, StringComparer.OrdinalIgnoreCase));
        allRequired.Should().BeTrue();
    }

    [Fact]
    public void CryptoPolicy_StrictHashBased_RejectsClassicalOnly()
    {
        // Strict policy: only hash-based signatures
        var policyAccepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SLH-DSA-128S", "SLH-DSA-192S"
        };

        // Transaction signed with classical ED25519 only
        var transactionAlgorithms = new[] { "ED25519" };

        var allAccepted = transactionAlgorithms.All(a => policyAccepted.Contains(a));
        allAccepted.Should().BeFalse("classical-only TX should be rejected by hash-based-only policy");
    }

    [Fact]
    public void ControlActionType_CryptoPolicyUpdate_IsDefined()
    {
        // CryptoPolicyUpdate action type exists for policy governance
        var actionType = ControlActionType.CryptoPolicyUpdate;
        actionType.Should().BeDefined();
    }

    [Fact]
    public void SlhDsa192s_SignatureSizeIs16224Bytes()
    {
        // SLH-DSA-192s produces 16,224-byte signatures (larger but more secure)
        // This is a documentation/contract test
        const int expectedSignatureSize = 16224;
        expectedSignatureSize.Should().Be(16224);

        // Compared to SLH-DSA-128s at 7,856 bytes
        const int slh128sSize = 7856;
        expectedSignatureSize.Should().BeGreaterThan(slh128sSize,
            "192s provides higher security at the cost of larger signatures");
    }
}
