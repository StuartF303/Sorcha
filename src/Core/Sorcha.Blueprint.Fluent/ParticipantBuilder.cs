// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Models.JsonLd;

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

    /// <summary>
    /// Sets the JSON-LD type (Person or Organization)
    /// </summary>
    public ParticipantBuilder AsJsonLdType(string type)
    {
        _participant.JsonLdType = type;
        return this;
    }

    /// <summary>
    /// Automatically sets JSON-LD type based on organisation name
    /// </summary>
    public ParticipantBuilder WithAutoJsonLdType()
    {
        _participant.JsonLdType = JsonLdTypeHelper.GetParticipantType(_participant.Organisation);
        return this;
    }

    /// <summary>
    /// Sets the participant as a Person (JSON-LD type)
    /// </summary>
    public ParticipantBuilder AsPerson()
    {
        _participant.JsonLdType = JsonLdTypes.Person;
        return this;
    }

    /// <summary>
    /// Sets the participant as an Organization (JSON-LD type)
    /// </summary>
    public ParticipantBuilder AsOrganization()
    {
        _participant.JsonLdType = JsonLdTypes.Organization;
        return this;
    }

    /// <summary>
    /// Adds a verifiable credential to the participant
    /// </summary>
    public ParticipantBuilder WithVerifiableCredential(JsonNode credential)
    {
        _participant.VerifiableCredential = credential.DeepClone();
        return this;
    }

    /// <summary>
    /// Adds additional JSON-LD properties
    /// </summary>
    public ParticipantBuilder WithAdditionalProperty(string key, JsonNode value)
    {
        _participant.AdditionalProperties ??= new Dictionary<string, JsonNode>();
        _participant.AdditionalProperties[key] = value.DeepClone();
        return this;
    }

    internal Participant Build()
    {
        return _participant;
    }
}
