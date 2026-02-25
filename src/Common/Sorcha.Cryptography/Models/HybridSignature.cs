// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Composite signature containing both classical and post-quantum components.
/// At least one of <see cref="Classical"/> or <see cref="Pqc"/> must be present.
/// Wire format: JSON serialized to string in TransactionModel.Signature field.
/// </summary>
public class HybridSignature
{
    /// <summary>
    /// Base64-encoded classical signature (ED25519/P-256/RSA).
    /// </summary>
    [JsonPropertyName("classical")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Classical { get; set; }

    /// <summary>
    /// Algorithm identifier for the classical signature.
    /// Required when <see cref="Classical"/> is present.
    /// </summary>
    [JsonPropertyName("classicalAlgorithm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassicalAlgorithm { get; set; }

    /// <summary>
    /// Base64-encoded post-quantum signature (ML-DSA/SLH-DSA).
    /// </summary>
    [JsonPropertyName("pqc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pqc { get; set; }

    /// <summary>
    /// Algorithm identifier for the PQC signature.
    /// Required when <see cref="Pqc"/> is present.
    /// </summary>
    [JsonPropertyName("pqcAlgorithm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PqcAlgorithm { get; set; }

    /// <summary>
    /// Base64-encoded full PQC public key for address-key binding verification.
    /// Required when <see cref="Pqc"/> is present (PQC addresses are hash-based).
    /// </summary>
    [JsonPropertyName("witnessPublicKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WitnessPublicKey { get; set; }

    /// <summary>
    /// Validates that at least one signature component is present with its required fields.
    /// </summary>
    public bool IsValid()
    {
        bool hasClassical = !string.IsNullOrEmpty(Classical);
        bool hasPqc = !string.IsNullOrEmpty(Pqc);

        if (!hasClassical && !hasPqc)
            return false;

        if (hasClassical && string.IsNullOrEmpty(ClassicalAlgorithm))
            return false;

        if (hasPqc && (string.IsNullOrEmpty(PqcAlgorithm) || string.IsNullOrEmpty(WitnessPublicKey)))
            return false;

        return true;
    }

    /// <summary>
    /// Serializes this hybrid signature to a JSON string for wire transport.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, HybridSignatureJsonContext.Default.HybridSignature);
    }

    /// <summary>
    /// Deserializes a hybrid signature from a JSON string.
    /// Returns null if the input is not a valid HybridSignature JSON.
    /// </summary>
    public static HybridSignature? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize(json, HybridSignatureJsonContext.Default.HybridSignature);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to detect whether a signature string is a HybridSignature JSON.
    /// Legacy signatures are plain Base64; hybrid signatures are JSON objects.
    /// </summary>
    public static bool IsHybridFormat(string signature)
    {
        return !string.IsNullOrEmpty(signature) && signature.TrimStart().StartsWith('{');
    }
}

/// <summary>
/// Source-generated JSON serializer context for <see cref="HybridSignature"/>.
/// </summary>
[JsonSerializable(typeof(HybridSignature))]
internal partial class HybridSignatureJsonContext : JsonSerializerContext;
