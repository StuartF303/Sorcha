// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Unit tests for DisclosureProcessor.
/// </summary>
public class DisclosureProcessorTests
{
    private readonly IDisclosureProcessor _processor;

    public DisclosureProcessorTests()
    {
        _processor = new DisclosureProcessor();
    }

    #region ApplyDisclosure - Simple Fields

    [Fact]
    public void ApplyDisclosure_AllFieldsWildcard_ReturnsAllData()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["email"] = "alice@example.com",
            ["age"] = 30
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/*" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainKey("name");
        result.Should().ContainKey("email");
        result.Should().ContainKey("age");
        result["name"].Should().Be("Alice");
    }

    [Fact]
    public void ApplyDisclosure_HashAllFieldsWildcard_ReturnsAllData()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field1"] = "value1",
            ["field2"] = "value2"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "#/*" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("field1");
        result.Should().ContainKey("field2");
    }

    [Fact]
    public void ApplyDisclosure_SpecificField_ReturnsOnlyThatField()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["email"] = "alice@example.com",
            ["ssn"] = "123-45-6789"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/name" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("name");
        result["name"].Should().Be("Alice");
        result.Should().NotContainKey("email");
        result.Should().NotContainKey("ssn");
    }

    [Fact]
    public void ApplyDisclosure_MultipleFields_ReturnsAllSpecified()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["email"] = "alice@example.com",
            ["ssn"] = "123-45-6789",
            ["salary"] = 100000
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string>
        {
            "/name",
            "/email"
        });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("name");
        result.Should().ContainKey("email");
        result.Should().NotContainKey("ssn");
        result.Should().NotContainKey("salary");
    }

    [Fact]
    public void ApplyDisclosure_WithHashPrefix_Works()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field1"] = "value1",
            ["field2"] = "value2"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "#/field1" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("field1");
        result["field1"].Should().Be("value1");
    }

    [Fact]
    public void ApplyDisclosure_NonExistentField_ReturnsEmpty()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/nonexistent" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ApplyDisclosure - Nested Fields

    [Fact]
    public void ApplyDisclosure_NestedField_ExtractsCorrectly()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["email"] = "alice@example.com",
                ["address"] = new Dictionary<string, object>
                {
                    ["city"] = "Springfield",
                    ["zipCode"] = "12345"
                }
            }
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/user" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("user");
        var user = result["user"] as Dictionary<string, object>;
        user.Should().NotBeNull();
        user!["name"].Should().Be("Alice");
    }

    [Fact]
    public void ApplyDisclosure_DeepNestedField_Works()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["company"] = new Dictionary<string, object>
            {
                ["name"] = "Acme Corp",
                ["address"] = new Dictionary<string, object>
                {
                    ["street"] = "123 Main St",
                    ["city"] = "Springfield"
                }
            }
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/company" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().ContainKey("company");
        var company = result["company"] as Dictionary<string, object>;
        company.Should().NotBeNull();
    }

    #endregion

    #region ApplyDisclosure - Arrays

    [Fact]
    public void ApplyDisclosure_ArrayField_ReturnsEntireArray()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["tags"] = new List<string> { "tag1", "tag2", "tag3" }
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/tags" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().ContainKey("tags");
        var tags = result["tags"] as List<string>;
        tags.Should().NotBeNull();
        tags.Should().HaveCount(3);
        tags.Should().Contain("tag1");
    }

    [Fact]
    public void ApplyDisclosure_ComplexData_FiltersCorrectly()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["orderId"] = "ORD-123",
            ["customer"] = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["email"] = "alice@example.com"
            },
            ["items"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["productId"] = "PROD-001",
                    ["quantity"] = 2,
                    ["price"] = 29.99
                }
            },
            ["total"] = 59.98,
            ["paymentInfo"] = new Dictionary<string, object>
            {
                ["cardNumber"] = "****-****-****-1234"
            }
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string>
        {
            "/orderId",
            "/customer",
            "/items",
            "/total"
        });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(4);
        result.Should().ContainKey("orderId");
        result.Should().ContainKey("customer");
        result.Should().ContainKey("items");
        result.Should().ContainKey("total");
        result.Should().NotContainKey("paymentInfo");
    }

    #endregion

    #region CreateDisclosures

    [Fact]
    public void CreateDisclosures_SingleParticipant_CreatesOne()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["email"] = "alice@example.com"
        };

        var disclosures = new[]
        {
            new BpModels.Disclosure("participant1", new List<string> { "/name", "/email" })
        };

        // Act
        var results = _processor.CreateDisclosures(data, disclosures);

        // Assert
        results.Should().HaveCount(1);
        results[0].ParticipantId.Should().Be("participant1");
        results[0].DisclosedData.Should().HaveCount(2);
        results[0].DisclosedData.Should().ContainKey("name");
        results[0].DisclosedData.Should().ContainKey("email");
    }

    [Fact]
    public void CreateDisclosures_MultipleParticipants_CreatesDifferentViews()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["orderId"] = "ORD-123",
            ["productId"] = "PROD-001",
            ["quantity"] = 5,
            ["price"] = 100.0,
            ["buyerAddress"] = "123 Main St",
            ["sellerAddress"] = "456 Oak Ave"
        };

        var disclosures = new[]
        {
            // Buyer sees: order ID, product, quantity, price, buyer address
            new BpModels.Disclosure("buyer", new List<string>
            {
                "/orderId",
                "/productId",
                "/quantity",
                "/price",
                "/buyerAddress"
            }),
            // Seller sees: order ID, product, quantity, seller address
            new BpModels.Disclosure("seller", new List<string>
            {
                "/orderId",
                "/productId",
                "/quantity",
                "/sellerAddress"
            }),
            // Auditor sees everything
            new BpModels.Disclosure("auditor", new List<string> { "/*" })
        };

        // Act
        var results = _processor.CreateDisclosures(data, disclosures);

        // Assert
        results.Should().HaveCount(3);

        // Buyer
        var buyer = results.FirstOrDefault(r => r.ParticipantId == "buyer");
        buyer.Should().NotBeNull();
        buyer!.DisclosedData.Should().HaveCount(5);
        buyer.DisclosedData.Should().ContainKey("buyerAddress");
        buyer.DisclosedData.Should().NotContainKey("sellerAddress");

        // Seller
        var seller = results.FirstOrDefault(r => r.ParticipantId == "seller");
        seller.Should().NotBeNull();
        seller!.DisclosedData.Should().HaveCount(4);
        seller.DisclosedData.Should().ContainKey("sellerAddress");
        seller.DisclosedData.Should().NotContainKey("buyerAddress");
        seller.DisclosedData.Should().NotContainKey("price");

        // Auditor
        var auditor = results.FirstOrDefault(r => r.ParticipantId == "auditor");
        auditor.Should().NotBeNull();
        auditor!.DisclosedData.Should().HaveCount(6);
        auditor.DisclosedData.Should().ContainKeys("orderId", "productId", "quantity", "price", "buyerAddress", "sellerAddress");
    }

    [Fact]
    public void CreateDisclosures_EmptyDisclosures_ReturnsEmpty()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field"] = "value"
        };

        var disclosures = Array.Empty<Disclosure>();

        // Act
        var results = _processor.CreateDisclosures(data, disclosures);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateDisclosures_ParticipantWithNoFields_ReturnsEmptyData()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field1"] = "value1"
        };

        var disclosures = new[]
        {
            new BpModels.Disclosure("participant1", new List<string> { "/nonexistent" })
        };

        // Act
        var results = _processor.CreateDisclosures(data, disclosures);

        // Assert
        results.Should().HaveCount(1);
        results[0].ParticipantId.Should().Be("participant1");
        results[0].DisclosedData.Should().BeEmpty();
    }

    #endregion

    #region Pointer Escaping

    [Fact]
    public void ApplyDisclosure_FieldWithSlash_HandlesEscaping()
    {
        // Arrange - field name contains '/'
        var data = new Dictionary<string, object>
        {
            ["a/b"] = "value"
        };

        // JSON Pointer escape: '/' becomes '~1'
        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/a~1b" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().ContainKey("a/b");
        result["a/b"].Should().Be("value");
    }

    [Fact]
    public void ApplyDisclosure_FieldWithTilde_HandlesEscaping()
    {
        // Arrange - field name contains '~'
        var data = new Dictionary<string, object>
        {
            ["a~b"] = "value"
        };

        // JSON Pointer escape: '~' becomes '~0'
        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/a~0b" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().ContainKey("a~b");
        result["a~b"].Should().Be("value");
    }

    #endregion

    #region Error Handling

    [Fact]
    public void ApplyDisclosure_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/field" });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.ApplyDisclosure(null!, disclosure));
    }

    [Fact]
    public void ApplyDisclosure_NullDisclosure_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.ApplyDisclosure(data, null!));
    }

    [Fact]
    public void CreateDisclosures_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var disclosures = new[] { new Disclosure("p1", new List<string> { "/f" }) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.CreateDisclosures(null!, disclosures));
    }

    [Fact]
    public void CreateDisclosures_NullDisclosures_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.CreateDisclosures(data, null!));
    }

    [Fact]
    public void ApplyDisclosure_EmptyPointerList_ReturnsEmpty()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field"] = "value"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string>());

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplyDisclosure_WhitespacePointer_IgnoresIt()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field"] = "value"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "   ", "/field" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("field");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ApplyDisclosure_EmptyData_ReturnsEmpty()
    {
        // Arrange
        var data = new Dictionary<string, object>();
        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/field" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplyDisclosure_DuplicatePointers_NoDuplicateFields()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field"] = "value"
        };

        var disclosure = new BpModels.Disclosure("participant1", new List<string> { "/field", "/field" });

        // Act
        var result = _processor.ApplyDisclosure(data, disclosure);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("field");
    }

    #endregion
}
