// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Unit tests for SchemaValidator.
/// </summary>
public class SchemaValidatorTests
{
    private readonly ISchemaValidator _validator;

    public SchemaValidatorTests()
    {
        _validator = new SchemaValidator();
    }

    [Fact]
    public async Task ValidateAsync_ValidData_ReturnsValid()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "age": { "type": "number" }
            },
            "required": ["name"]
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 30
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_MissingRequiredField_ReturnsInvalid()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "email": { "type": "string" }
            },
            "required": ["name", "email"]
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice"
            // email is missing
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Keyword == "required");
    }

    [Fact]
    public async Task ValidateAsync_WrongType_ReturnsInvalid()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "age": { "type": "number" }
            }
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["age"] = "thirty" // string instead of number
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Keyword == "type");
    }

    [Fact]
    public async Task ValidateAsync_NestedObjects_ValidatesDeep()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "user": {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string" },
                        "address": {
                            "type": "object",
                            "properties": {
                                "city": { "type": "string" },
                                "zipCode": { "type": "string", "pattern": "^[0-9]{5}$" }
                            },
                            "required": ["city", "zipCode"]
                        }
                    },
                    "required": ["name", "address"]
                }
            },
            "required": ["user"]
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["address"] = new Dictionary<string, object>
                {
                    ["city"] = "Springfield",
                    ["zipCode"] = "12345"
                }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NestedObjectInvalidPattern_ReturnsInvalid()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "user": {
                    "type": "object",
                    "properties": {
                        "address": {
                            "type": "object",
                            "properties": {
                                "zipCode": { "type": "string", "pattern": "^[0-9]{5}$" }
                            }
                        }
                    }
                }
            }
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["address"] = new Dictionary<string, object>
                {
                    ["zipCode"] = "ABC123" // invalid pattern
                }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Keyword == "pattern");
        result.Errors.Should().Contain(e => e.InstanceLocation.Contains("zipCode"));
    }

    [Fact]
    public async Task ValidateAsync_Arrays_ValidatesItems()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "tags": {
                    "type": "array",
                    "items": { "type": "string" },
                    "minItems": 1,
                    "maxItems": 5
                }
            }
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["tags"] = new List<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ArrayItemWrongType_ReturnsInvalid()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "scores": {
                    "type": "array",
                    "items": { "type": "number" }
                }
            }
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["scores"] = new List<object> { 100, 95, "eighty" } // string in number array
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_MinimumConstraint_Validates()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "age": { 
                    "type": "number",
                    "minimum": 18
                }
            }
        }
        """);

        var validData = new Dictionary<string, object> { ["age"] = 25 };
        var invalidData = new Dictionary<string, object> { ["age"] = 15 };

        // Act
        var validResult = await _validator.ValidateAsync(validData, schema!);
        var invalidResult = await _validator.ValidateAsync(invalidData, schema!);

        // Assert
        validResult.IsValid.Should().BeTrue();
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.Errors.Should().Contain(e => e.Keyword == "minimum");
    }

    [Fact]
    public async Task ValidateAsync_StringLength_Validates()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "username": { 
                    "type": "string",
                    "minLength": 3,
                    "maxLength": 20
                }
            }
        }
        """);

        var validData = new Dictionary<string, object> { ["username"] = "alice" };
        var tooShort = new Dictionary<string, object> { ["username"] = "ab" };
        var tooLong = new Dictionary<string, object> { ["username"] = "abcdefghijklmnopqrstuvwxyz" };

        // Act
        var validResult = await _validator.ValidateAsync(validData, schema!);
        var shortResult = await _validator.ValidateAsync(tooShort, schema!);
        var longResult = await _validator.ValidateAsync(tooLong, schema!);

        // Assert
        validResult.IsValid.Should().BeTrue();
        shortResult.IsValid.Should().BeFalse();
        shortResult.Errors.Should().Contain(e => e.Keyword == "minLength");
        longResult.IsValid.Should().BeFalse();
        longResult.Errors.Should().Contain(e => e.Keyword == "maxLength");
    }

    [Fact]
    public async Task ValidateAsync_EmailFormat_Validates()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "email": { 
                    "type": "string",
                    "format": "email"
                }
            }
        }
        """);

        var validData = new Dictionary<string, object> { ["email"] = "alice@example.com" };
        var invalidData = new Dictionary<string, object> { ["email"] = "not-an-email" };

        // Act
        var validResult = await _validator.ValidateAsync(validData, schema!);
        var invalidResult = await _validator.ValidateAsync(invalidData, schema!);

        // Assert
        validResult.IsValid.Should().BeTrue();
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.Errors.Should().Contain(e => e.Keyword == "format");
    }

    [Fact]
    public async Task ValidateAsync_EmptyData_ValidatesAsEmptyObject()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object"
        }
        """);

        var data = new Dictionary<string, object>();

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ComplexSchema_ValidatesCorrectly()
    {
        // Arrange
        var schema = JsonNode.Parse("""
        {
            "type": "object",
            "properties": {
                "orderId": { "type": "string" },
                "customer": {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string" },
                        "email": { "type": "string", "format": "email" }
                    },
                    "required": ["name", "email"]
                },
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "productId": { "type": "string" },
                            "quantity": { "type": "number", "minimum": 1 },
                            "price": { "type": "number", "minimum": 0 }
                        },
                        "required": ["productId", "quantity", "price"]
                    },
                    "minItems": 1
                },
                "total": { "type": "number", "minimum": 0 }
            },
            "required": ["orderId", "customer", "items", "total"]
        }
        """);

        var data = new Dictionary<string, object>
        {
            ["orderId"] = "ORD-12345",
            ["customer"] = new Dictionary<string, object>
            {
                ["name"] = "Alice Johnson",
                ["email"] = "alice@example.com"
            },
            ["items"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["productId"] = "PROD-001",
                    ["quantity"] = 2,
                    ["price"] = 29.99
                },
                new Dictionary<string, object>
                {
                    ["productId"] = "PROD-002",
                    ["quantity"] = 1,
                    ["price"] = 49.99
                }
            },
            ["total"] = 109.97
        };

        // Act
        var result = await _validator.ValidateAsync(data, schema!);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var schema = JsonNode.Parse("""{ "type": "object" }""");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _validator.ValidateAsync(null!, schema!)
        );
    }

    [Fact]
    public async Task ValidateAsync_NullSchema_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _validator.ValidateAsync(data, null!)
        );
    }
}
