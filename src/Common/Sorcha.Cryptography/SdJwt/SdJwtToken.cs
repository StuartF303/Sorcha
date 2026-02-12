// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Generic;

namespace Sorcha.Cryptography.SdJwt;

/// <summary>
/// Represents a complete SD-JWT VC token with all disclosures.
/// </summary>
public class SdJwtToken
{
    /// <summary>
    /// JWT header containing algorithm and type.
    /// </summary>
    public Dictionary<string, object> Header { get; set; } = new();

    /// <summary>
    /// JWT payload containing claims and _sd digest arrays.
    /// </summary>
    public Dictionary<string, object> Payload { get; set; } = new();

    /// <summary>
    /// All disclosures (base64url-encoded JSON arrays: [salt, claim_name, claim_value]).
    /// </summary>
    public List<string> Disclosures { get; set; } = new();

    /// <summary>
    /// The JWS signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// The complete serialized SD-JWT token (issuer-jwt~disclosure1~disclosure2~).
    /// </summary>
    public string RawToken { get; set; } = string.Empty;
}
