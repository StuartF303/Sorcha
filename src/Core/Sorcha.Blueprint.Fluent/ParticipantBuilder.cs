// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating Participant instances
/// </summary>
public class ParticipantBuilder
{
    private readonly Participant _participant;

    internal ParticipantBuilder(string participantId)
    {
        _participant = new Participant
        {
            Id = participantId
        };
    }

    /// <summary>
    /// Sets the participant's friendly name
    /// </summary>
    public ParticipantBuilder Named(string name)
    {
        _participant.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the participant's organization
    /// </summary>
    public ParticipantBuilder FromOrganisation(string organisation)
    {
        _participant.Organisation = organisation;
        return this;
    }

    /// <summary>
    /// Sets the participant's wallet address
    /// </summary>
    public ParticipantBuilder WithWallet(string walletAddress)
    {
        _participant.WalletAddress = walletAddress;
        return this;
    }

    /// <summary>
    /// Sets the participant's DID URI
    /// </summary>
    public ParticipantBuilder WithDidUri(string didUri)
    {
        _participant.DidUri = didUri;
        return this;
    }

    /// <summary>
    /// Enables stealth address for privacy
    /// </summary>
    public ParticipantBuilder UseStealthAddress(bool useStealth = true)
    {
        _participant.UseStealthAddress = useStealth;
        return this;
    }

    internal Participant Build() => _participant;
}
