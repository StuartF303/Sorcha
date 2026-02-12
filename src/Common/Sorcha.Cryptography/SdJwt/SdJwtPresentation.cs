// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Generic;

namespace Sorcha.Cryptography.SdJwt;

/// <summary>
/// Represents an SD-JWT presentation with selected disclosures and optional key binding.
/// </summary>
public class SdJwtPresentation
{
    /// <summary>
    /// The original SD-JWT token (issuer-signed JWT).
    /// </summary>
    public SdJwtToken Token { get; set; } = new();

    /// <summary>
    /// Only the disclosures selected for presentation (subset of Token.Disclosures).
    /// </summary>
    public List<string> SelectedDisclosures { get; set; } = new();

    /// <summary>
    /// Key binding JWT proving the presenter holds the credential key.
    /// </summary>
    public string? KeyBindingJwt { get; set; }

    /// <summary>
    /// The serialized presentation token (issuer-jwt~selected-disclosure1~...~kb-jwt).
    /// </summary>
    public string RawPresentation { get; set; } = string.Empty;
}
