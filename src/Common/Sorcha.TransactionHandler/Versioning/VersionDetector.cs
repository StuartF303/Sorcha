using System;
using System.Text.Json;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Interfaces;

namespace Sorcha.TransactionHandler.Versioning;

/// <summary>
/// Detects transaction versions from binary or JSON data.
/// </summary>
public class VersionDetector : IVersionDetector
{
    /// <inheritdoc/>
    public TransactionVersion DetectVersion(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length < 4)
            throw new ArgumentException("Insufficient data to detect version", nameof(data));

        // Read version from first 4 bytes (little-endian)
        uint version = BitConverter.ToUInt32(data, 0);

        return version switch
        {
            1 => TransactionVersion.V1,
            _ => throw new NotSupportedException($"Transaction version {version} is not supported")
        };
    }

    /// <inheritdoc/>
    public TransactionVersion DetectVersion(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentNullException(nameof(json));

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("version", out var versionProperty))
                throw new ArgumentException("JSON does not contain a 'version' property", nameof(json));

            var version = versionProperty.GetUInt32();

            return version switch
            {
                1 => TransactionVersion.V1,
                _ => throw new NotSupportedException($"Transaction version {version} is not supported")
            };
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format", nameof(json), ex);
        }
    }
}
