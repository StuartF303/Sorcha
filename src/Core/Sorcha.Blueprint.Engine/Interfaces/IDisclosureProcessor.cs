// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// Selective disclosure processor that filters data based on disclosure rules.
/// </summary>
/// <remarks>
/// Implements privacy-preserving selective data disclosure using JSON Pointers
/// to specify which fields each participant can see.
/// 
/// Disclosure rules define:
/// - Which participant receives the disclosure
/// - Which fields from the action data are included
/// - Optional transformations (redaction, hashing)
/// 
/// This enables workflows where different participants see different
/// subsets of the data, preserving privacy while maintaining workflow integrity.
/// 
/// Example:
/// - Buyer sees: productId, price, quantity
/// - Seller sees: productId, quantity, buyerAddress
/// - Auditor sees: all fields
/// </remarks>
public interface IDisclosureProcessor
{
    /// <summary>
    /// Apply a single disclosure rule to filter data for one participant.
    /// </summary>
    /// <param name="data">The complete action data.</param>
    /// <param name="disclosure">The disclosure rule defining which fields to include.</param>
    /// <returns>
    /// A dictionary containing only the fields specified in the disclosure rule.
    /// Fields are selected using JSON Pointers from the disclosure.
    /// </returns>
    /// <remarks>
    /// The disclosure rule contains a list of JSON Pointers (RFC 6901) that
    /// specify which fields to extract from the data.
    /// 
    /// Example disclosure rule:
    /// {
    ///   "participantId": "buyer-wallet",
    ///   "fields": ["/productId", "/price", "/quantity"]
    /// }
    /// 
    /// If a pointer references a non-existent field, it is silently ignored.
    /// </remarks>
    Dictionary<string, object> ApplyDisclosure(
        Dictionary<string, object> data,
        Disclosure disclosure);

    /// <summary>
    /// Create disclosure results for all participants in an action.
    /// </summary>
    /// <param name="data">The complete action data.</param>
    /// <param name="disclosures">The disclosure rules for all participants.</param>
    /// <returns>
    /// A list of disclosure results, one per participant, each containing
    /// the participant ID and their filtered data.
    /// </returns>
    /// <remarks>
    /// This method processes all disclosures in parallel for efficiency.
    /// Each disclosure result can be used to:
    /// - Create encrypted payloads for each participant
    /// - Generate transaction metadata
    /// - Send real-time notifications
    /// </remarks>
    List<DisclosureResult> CreateDisclosures(
        Dictionary<string, object> data,
        IEnumerable<Disclosure> disclosures);
}
