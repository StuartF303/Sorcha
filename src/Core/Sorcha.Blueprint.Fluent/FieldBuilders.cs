// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Base class for field builders
/// </summary>
public abstract class FieldBuilder
{
    /// <summary>
    /// Schema dictionary containing JSON Schema properties
    /// </summary>
    protected readonly Dictionary<string, object> _schema = new();

    /// <summary>
    /// IsRequiredField indicates whether this field is required in the schema
    /// </summary>
    public bool IsRequiredField { get; protected set; }

    /// <summary>
    /// FieldBuilder constructor initializes the schema with the specified type
    /// </summary>
    /// <param name="type">JSON Schema type (e.g., "string", "number", "integer")</param>
    protected FieldBuilder(string type)
    {
        _schema["type"] = type;
    }

    /// <summary>
    /// BuildInternal returns the constructed schema dictionary
    /// </summary>
    public Dictionary<string, object> BuildInternal() => _schema;
}

/// <summary>
/// Builder for string field schemas
/// </summary>
public class StringFieldBuilder : FieldBuilder
{
    /// <summary>
    /// StringFieldBuilder constructor creates a string field builder
    /// </summary>
    public StringFieldBuilder() : base("string") { }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public StringFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public StringFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// WithMinLength sets the minimum length constraint
    /// </summary>
    public StringFieldBuilder WithMinLength(int minLength)
    {
        _schema["minLength"] = minLength;
        return this;
    }

    /// <summary>
    /// WithMaxLength sets the maximum length constraint
    /// </summary>
    public StringFieldBuilder WithMaxLength(int maxLength)
    {
        _schema["maxLength"] = maxLength;
        return this;
    }

    /// <summary>
    /// WithPattern sets a regular expression pattern for validation
    /// </summary>
    public StringFieldBuilder WithPattern(string pattern)
    {
        _schema["pattern"] = pattern;
        return this;
    }

    /// <summary>
    /// WithFormat sets the format specification (e.g., "email", "uri")
    /// </summary>
    public StringFieldBuilder WithFormat(string format)
    {
        _schema["format"] = format;
        return this;
    }

    /// <summary>
    /// WithEnum sets allowed enumeration values
    /// </summary>
    public StringFieldBuilder WithEnum(params string[] values)
    {
        _schema["enum"] = values;
        return this;
    }

    /// <summary>
    /// WithDefault sets the default value for the field
    /// </summary>
    public StringFieldBuilder WithDefault(string defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public StringFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for number field schemas
/// </summary>
public class NumberFieldBuilder : FieldBuilder
{
    /// <summary>
    /// NumberFieldBuilder constructor creates a number field builder
    /// </summary>
    public NumberFieldBuilder() : base("number") { }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public NumberFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public NumberFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// WithMinimum sets the minimum value constraint
    /// </summary>
    public NumberFieldBuilder WithMinimum(double minimum)
    {
        _schema["minimum"] = minimum;
        return this;
    }

    /// <summary>
    /// WithMaximum sets the maximum value constraint
    /// </summary>
    public NumberFieldBuilder WithMaximum(double maximum)
    {
        _schema["maximum"] = maximum;
        return this;
    }

    /// <summary>
    /// WithMultipleOf sets the value must be a multiple of this number
    /// </summary>
    public NumberFieldBuilder WithMultipleOf(double multipleOf)
    {
        _schema["multipleOf"] = multipleOf;
        return this;
    }

    /// <summary>
    /// WithDefault sets the default value for the field
    /// </summary>
    public NumberFieldBuilder WithDefault(double defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public NumberFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for integer field schemas
/// </summary>
public class IntegerFieldBuilder : FieldBuilder
{
    /// <summary>
    /// IntegerFieldBuilder constructor creates an integer field builder
    /// </summary>
    public IntegerFieldBuilder() : base("integer") { }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public IntegerFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public IntegerFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// WithMinimum sets the minimum value constraint
    /// </summary>
    public IntegerFieldBuilder WithMinimum(int minimum)
    {
        _schema["minimum"] = minimum;
        return this;
    }

    /// <summary>
    /// WithMaximum sets the maximum value constraint
    /// </summary>
    public IntegerFieldBuilder WithMaximum(int maximum)
    {
        _schema["maximum"] = maximum;
        return this;
    }

    /// <summary>
    /// WithMultipleOf sets the value must be a multiple of this number
    /// </summary>
    public IntegerFieldBuilder WithMultipleOf(int multipleOf)
    {
        _schema["multipleOf"] = multipleOf;
        return this;
    }

    /// <summary>
    /// WithDefault sets the default value for the field
    /// </summary>
    public IntegerFieldBuilder WithDefault(int defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public IntegerFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for boolean field schemas
/// </summary>
public class BooleanFieldBuilder : FieldBuilder
{
    /// <summary>
    /// BooleanFieldBuilder constructor creates a boolean field builder
    /// </summary>
    public BooleanFieldBuilder() : base("boolean") { }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public BooleanFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public BooleanFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// WithDefault sets the default value for the field
    /// </summary>
    public BooleanFieldBuilder WithDefault(bool defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public BooleanFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for date field schemas
/// </summary>
public class DateFieldBuilder : FieldBuilder
{
    /// <summary>
    /// DateFieldBuilder constructor creates a date-time field builder
    /// </summary>
    public DateFieldBuilder() : base("string")
    {
        _schema["format"] = "date-time";
    }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public DateFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public DateFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public DateFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for file field schemas
/// </summary>
public class FileFieldBuilder : FieldBuilder
{
    /// <summary>
    /// FileFieldBuilder constructor creates a file upload field builder
    /// </summary>
    public FileFieldBuilder() : base("object")
    {
        _schema["properties"] = new Dictionary<string, object>
        {
            ["fileName"] = new Dictionary<string, object> { ["type"] = "string" },
            ["fileType"] = new Dictionary<string, object> { ["type"] = "string" },
            ["fileSize"] = new Dictionary<string, object> { ["type"] = "integer" },
            ["fileExtension"] = new Dictionary<string, object> { ["type"] = "string" }
        };
    }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public FileFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public FileFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// WithMaxSize sets the maximum file size in bytes
    /// </summary>
    public FileFieldBuilder WithMaxSize(int maxSizeBytes)
    {
        var properties = (Dictionary<string, object>)_schema["properties"];
        var fileSize = (Dictionary<string, object>)properties["fileSize"];
        fileSize["maximum"] = maxSizeBytes;
        return this;
    }

    /// <summary>
    /// WithAllowedExtensions sets allowed file extensions
    /// </summary>
    public FileFieldBuilder WithAllowedExtensions(params string[] extensions)
    {
        var properties = (Dictionary<string, object>)_schema["properties"];
        var fileExtension = (Dictionary<string, object>)properties["fileExtension"];
        fileExtension["enum"] = extensions;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public FileFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for object field schemas
/// </summary>
public class ObjectFieldBuilder : FieldBuilder
{
    private readonly Dictionary<string, object> _properties = new();

    /// <summary>
    /// ObjectFieldBuilder constructor creates an object field builder
    /// </summary>
    public ObjectFieldBuilder() : base("object")
    {
        _schema["properties"] = _properties;
    }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public ObjectFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public ObjectFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// AddProperty adds a property to the object with the specified type
    /// </summary>
    public ObjectFieldBuilder AddProperty(string propName, string type)
    {
        _properties[propName] = new Dictionary<string, object> { ["type"] = type };
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public ObjectFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}

/// <summary>
/// Builder for array field schemas
/// </summary>
public class ArrayFieldBuilder : FieldBuilder
{
    /// <summary>
    /// ArrayFieldBuilder constructor creates an array field builder
    /// </summary>
    public ArrayFieldBuilder() : base("array") { }

    /// <summary>
    /// WithTitle sets the title/label for the field
    /// </summary>
    public ArrayFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    /// <summary>
    /// WithDescription sets the description for the field
    /// </summary>
    public ArrayFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    /// <summary>
    /// OfType sets the type of items in the array
    /// </summary>
    public ArrayFieldBuilder OfType(string itemType)
    {
        _schema["items"] = new Dictionary<string, object> { ["type"] = itemType };
        return this;
    }

    /// <summary>
    /// WithMinItems sets the minimum number of items constraint
    /// </summary>
    public ArrayFieldBuilder WithMinItems(int minItems)
    {
        _schema["minItems"] = minItems;
        return this;
    }

    /// <summary>
    /// WithMaxItems sets the maximum number of items constraint
    /// </summary>
    public ArrayFieldBuilder WithMaxItems(int maxItems)
    {
        _schema["maxItems"] = maxItems;
        return this;
    }

    /// <summary>
    /// IsRequired marks this field as required in the schema
    /// </summary>
    public ArrayFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}
