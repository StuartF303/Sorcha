// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Buffers.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Sorcha.Register.Storage.MongoDB.Serialization;

/// <summary>
/// Custom BSON serializer that writes string values as BSON Binary (base64url-decoded)
/// and reads both BSON Binary (new format) and BSON String (legacy format).
/// </summary>
/// <remarks>
/// Write path: base64url/base64 string → decode to byte[] → BSON Binary subtype 0x00
/// Read path (new): BSON Binary → byte[]
/// Read path (legacy): BSON String → smart-decode (base64/base64url) → byte[]
/// </remarks>
public class Base64UrlBinarySerializer : SerializerBase<byte[]>
{
    public override byte[] Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        return bsonType switch
        {
            BsonType.Binary => context.Reader.ReadBinaryData().Bytes,
            BsonType.String => DecodeBase64Auto(context.Reader.ReadString()),
            BsonType.Null => HandleNull(context),
            _ => throw new BsonSerializationException(
                $"Cannot deserialize BsonType.{bsonType} to byte[]. Expected Binary or String.")
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, byte[] value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteBinaryData(new BsonBinaryData(value, BsonBinarySubType.Binary));
    }

    private static byte[] HandleNull(BsonDeserializationContext context)
    {
        context.Reader.ReadNull();
        return [];
    }

    /// <summary>
    /// Smart decode: detects legacy Base64 (+, /, =) vs Base64url.
    /// </summary>
    private static byte[] DecodeBase64Auto(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return [];

        if (encoded.Contains('+') || encoded.Contains('/') || encoded.Contains('='))
            return Convert.FromBase64String(encoded);

        return Base64Url.DecodeFromChars(encoded);
    }
}

/// <summary>
/// Custom BSON serializer for string properties that need to read both BSON Binary (new format)
/// and BSON String (legacy format). Used on TransactionModel/PayloadModel/Challenge class maps
/// so that read operations transparently handle documents stored with BSON Binary fields.
/// </summary>
/// <remarks>
/// Read path (new): BSON Binary byte[] → Base64Url.EncodeToString → string
/// Read path (legacy): BSON String → string (pass-through)
/// Write path: string → BSON String (when writing via TransactionModel, not MongoTransactionDocument)
/// </remarks>
public class BinaryAwareStringSerializer : SerializerBase<string>
{
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        return bsonType switch
        {
            BsonType.String => context.Reader.ReadString(),
            BsonType.Binary => Base64Url.EncodeToString(context.Reader.ReadBinaryData().Bytes),
            BsonType.Null => HandleNull(context),
            _ => throw new BsonSerializationException(
                $"Cannot deserialize BsonType.{bsonType} to string. Expected String or Binary.")
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteString(value);
    }

    private static string HandleNull(BsonDeserializationContext context)
    {
        context.Reader.ReadNull();
        return string.Empty;
    }
}

/// <summary>
/// Nullable variant of <see cref="Base64UrlBinarySerializer"/> for optional byte[] fields.
/// </summary>
public class NullableBase64UrlBinarySerializer : SerializerBase<byte[]?>
{
    private static readonly Base64UrlBinarySerializer Inner = new();

    public override byte[]? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        if (bsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }

        return Inner.Deserialize(context, args);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, byte[]? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        Inner.Serialize(context, args, value);
    }
}
