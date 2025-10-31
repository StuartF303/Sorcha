// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Base class for field builders
/// </summary>
public abstract class FieldBuilder
{
    protected readonly Dictionary<string, object> _schema = new();
    public bool IsRequiredField { get; protected set; }

    protected FieldBuilder(string type)
    {
        _schema["type"] = type;
    }

    public Dictionary<string, object> BuildInternal() => _schema;
}

/// <summary>
/// Builder for string field schemas
/// </summary>
public class StringFieldBuilder : FieldBuilder
{
    public StringFieldBuilder() : base("string") { }

    public StringFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public StringFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public StringFieldBuilder WithMinLength(int minLength)
    {
        _schema["minLength"] = minLength;
        return this;
    }

    public StringFieldBuilder WithMaxLength(int maxLength)
    {
        _schema["maxLength"] = maxLength;
        return this;
    }

    public StringFieldBuilder WithPattern(string pattern)
    {
        _schema["pattern"] = pattern;
        return this;
    }

    public StringFieldBuilder WithFormat(string format)
    {
        _schema["format"] = format;
        return this;
    }

    public StringFieldBuilder WithEnum(params string[] values)
    {
        _schema["enum"] = values;
        return this;
    }

    public StringFieldBuilder WithDefault(string defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

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
    public NumberFieldBuilder() : base("number") { }

    public NumberFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public NumberFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public NumberFieldBuilder WithMinimum(double minimum)
    {
        _schema["minimum"] = minimum;
        return this;
    }

    public NumberFieldBuilder WithMaximum(double maximum)
    {
        _schema["maximum"] = maximum;
        return this;
    }

    public NumberFieldBuilder WithMultipleOf(double multipleOf)
    {
        _schema["multipleOf"] = multipleOf;
        return this;
    }

    public NumberFieldBuilder WithDefault(double defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

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
    public IntegerFieldBuilder() : base("integer") { }

    public IntegerFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public IntegerFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public IntegerFieldBuilder WithMinimum(int minimum)
    {
        _schema["minimum"] = minimum;
        return this;
    }

    public IntegerFieldBuilder WithMaximum(int maximum)
    {
        _schema["maximum"] = maximum;
        return this;
    }

    public IntegerFieldBuilder WithMultipleOf(int multipleOf)
    {
        _schema["multipleOf"] = multipleOf;
        return this;
    }

    public IntegerFieldBuilder WithDefault(int defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

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
    public BooleanFieldBuilder() : base("boolean") { }

    public BooleanFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public BooleanFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public BooleanFieldBuilder WithDefault(bool defaultValue)
    {
        _schema["default"] = defaultValue;
        return this;
    }

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
    public DateFieldBuilder() : base("string")
    {
        _schema["format"] = "date-time";
    }

    public DateFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public DateFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

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

    public FileFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public FileFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public FileFieldBuilder WithMaxSize(int maxSizeBytes)
    {
        var properties = (Dictionary<string, object>)_schema["properties"];
        var fileSize = (Dictionary<string, object>)properties["fileSize"];
        fileSize["maximum"] = maxSizeBytes;
        return this;
    }

    public FileFieldBuilder WithAllowedExtensions(params string[] extensions)
    {
        var properties = (Dictionary<string, object>)_schema["properties"];
        var fileExtension = (Dictionary<string, object>)properties["fileExtension"];
        fileExtension["enum"] = extensions;
        return this;
    }

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

    public ObjectFieldBuilder() : base("object")
    {
        _schema["properties"] = _properties;
    }

    public ObjectFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public ObjectFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public ObjectFieldBuilder AddProperty(string propName, string type)
    {
        _properties[propName] = new Dictionary<string, object> { ["type"] = type };
        return this;
    }

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
    public ArrayFieldBuilder() : base("array") { }

    public ArrayFieldBuilder WithTitle(string title)
    {
        _schema["title"] = title;
        return this;
    }

    public ArrayFieldBuilder WithDescription(string description)
    {
        _schema["description"] = description;
        return this;
    }

    public ArrayFieldBuilder OfType(string itemType)
    {
        _schema["items"] = new Dictionary<string, object> { ["type"] = itemType };
        return this;
    }

    public ArrayFieldBuilder WithMinItems(int minItems)
    {
        _schema["minItems"] = minItems;
        return this;
    }

    public ArrayFieldBuilder WithMaxItems(int maxItems)
    {
        _schema["maxItems"] = maxItems;
        return this;
    }

    public ArrayFieldBuilder IsRequired()
    {
        IsRequiredField = true;
        return this;
    }
}
