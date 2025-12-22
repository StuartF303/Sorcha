// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Sorcha.Validator.Grpc.V1;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Models;
using Google.Protobuf.WellKnownTypes;

namespace Sorcha.Validator.Service.GrpcServices;

/// <summary>
/// gRPC service implementation for validator-to-validator communication
/// </summary>
/// <remarks>
/// This service implements the ValidatorService protocol defined in validator.proto.
/// It handles:
/// <list type="bullet">
///   <item>RequestVote: Validates proposed dockets and returns signed votes</item>
///   <item>ValidateDocket: Validates confirmed dockets from peers before persistence</item>
///   <item>GetHealthStatus: Reports validator health and status</item>
/// </list>
/// </remarks>
public class ValidatorGrpcService : Sorcha.Validator.Grpc.V1.ValidatorService.ValidatorServiceBase
{
    private readonly IConsensusEngine _consensusEngine;
    private readonly ILogger<ValidatorGrpcService> _logger;

    public ValidatorGrpcService(
        IConsensusEngine consensusEngine,
        ILogger<ValidatorGrpcService> logger)
    {
        _consensusEngine = consensusEngine ?? throw new ArgumentNullException(nameof(consensusEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// RequestVote RPC: Validates a proposed docket and returns a signed vote
    /// </summary>
    /// <remarks>
    /// This method is called by a peer validator proposing a new docket for consensus.
    /// It performs independent validation and returns an approve/reject vote.
    ///
    /// <para><b>User Story:</b> US3 - Distributed Consensus Achievement (P1)</para>
    /// <para><b>Acceptance Criteria:</b> AS3 - Receive and sign votes</para>
    /// </remarks>
    public override async Task<VoteResponse> RequestVote(VoteRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "Received vote request for docket {DocketNumber} on register {RegisterId} from {ProposerId}",
            request.DocketNumber, request.RegisterId, request.ProposerValidatorId);

        try
        {
            // Convert gRPC request to domain model
            var docket = MapProtoToDocket(request);

            // Validate and vote
            var vote = await _consensusEngine.ValidateAndVoteAsync(docket, context.CancellationToken);

            // Convert domain vote to gRPC response
            var response = new VoteResponse
            {
                VoteId = vote.VoteId,
                Decision = vote.Decision == Models.VoteDecision.Approve
                    ? Grpc.V1.VoteDecision.Approve
                    : Grpc.V1.VoteDecision.Reject,
                RejectionReason = vote.RejectionReason ?? string.Empty,
                ValidatorId = vote.ValidatorId,
                VotedAt = Timestamp.FromDateTimeOffset(vote.VotedAt),
                ValidatorSignature = new Grpc.V1.Signature
                {
                    PublicKey = vote.ValidatorSignature.PublicKey,
                    SignatureValue = vote.ValidatorSignature.SignatureValue,
                    Algorithm = vote.ValidatorSignature.Algorithm
                }
            };

            _logger.LogInformation(
                "Returning {Decision} vote for docket {DocketNumber}",
                response.Decision, request.DocketNumber);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing vote request for docket {DocketNumber}", request.DocketNumber);
            throw new RpcException(new Status(StatusCode.Internal, $"Vote processing failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// ValidateDocket RPC: Validates a confirmed docket received from peers
    /// </summary>
    /// <remarks>
    /// This method is called when a peer broadcasts a confirmed docket.
    /// The validator must validate it before writing to local Register Service.
    ///
    /// <para><b>User Story:</b> US5 - Peer Docket Validation (P2)</para>
    /// <para><b>Acceptance Criteria:</b> AS1 - Validate transaction signatures, blueprint compliance, hash linkage</para>
    /// </remarks>
    public override async Task<DocketValidationResponse> ValidateDocket(
        DocketValidationRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received docket validation request for docket {DocketNumber} on register {RegisterId}",
            request.DocketNumber, request.RegisterId);

        try
        {
            // Convert gRPC request to domain model
            var docket = MapProtoToDocket(request);

            // Validate the docket independently
            var vote = await _consensusEngine.ValidateAndVoteAsync(docket, context.CancellationToken);

            // Determine if docket should be persisted
            var shouldPersist = vote.Decision == Models.VoteDecision.Approve;

            var response = new DocketValidationResponse
            {
                IsValid = shouldPersist,
                ShouldPersist = shouldPersist,
                IsFork = false // Fork detection not implemented yet (US5)
            };

            if (!shouldPersist && vote.RejectionReason != null)
            {
                response.ValidationErrors.Add(vote.RejectionReason);
            }

            _logger.LogInformation(
                "Docket {DocketNumber} validation result: {Result}",
                request.DocketNumber, shouldPersist ? "Valid" : "Invalid");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating docket {DocketNumber}", request.DocketNumber);
            throw new RpcException(new Status(StatusCode.Internal, $"Docket validation failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// GetHealthStatus RPC: Returns validator health and status information
    /// </summary>
    public override Task<HealthStatusResponse> GetHealthStatus(Empty request, ServerCallContext context)
    {
        _logger.LogDebug("Health status check received");

        // TODO: Implement proper health checks (US7 - System Wallet Management)
        // For now, return healthy status
        var response = new HealthStatusResponse
        {
            Status = Grpc.V1.HealthStatus.Healthy,
            ValidatorId = "validator-001", // TODO: Get from configuration
            ActiveRegisters = 0, // TODO: Get from ValidatorOrchestrator
            LastHeartbeat = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// Maps gRPC VoteRequest to domain Docket model
    /// </summary>
    private static Models.Docket MapProtoToDocket(VoteRequest request)
    {
        var transactions = request.Transactions.Select(MapProtoToTransaction).ToList();

        return new Models.Docket
        {
            DocketId = request.DocketId,
            RegisterId = request.RegisterId,
            DocketNumber = request.DocketNumber,
            DocketHash = request.DocketHash,
            PreviousHash = string.IsNullOrEmpty(request.PreviousHash) ? null : request.PreviousHash,
            CreatedAt = request.CreatedAt.ToDateTimeOffset(),
            Transactions = transactions,
            Status = Models.DocketStatus.Proposed,
            ProposerValidatorId = request.ProposerValidatorId,
            ProposerSignature = new Models.Signature
            {
                PublicKey = request.ProposerSignature.PublicKey,
                SignatureValue = request.ProposerSignature.SignatureValue,
                Algorithm = request.ProposerSignature.Algorithm
            },
            MerkleRoot = request.MerkleRoot
        };
    }

    /// <summary>
    /// Maps gRPC DocketValidationRequest to domain Docket model
    /// </summary>
    private static Models.Docket MapProtoToDocket(DocketValidationRequest request)
    {
        var transactions = request.Transactions.Select(MapProtoToTransaction).ToList();
        var consensusVotes = request.Votes.Select(MapProtoToConsensusVote).ToList();

        return new Models.Docket
        {
            DocketId = request.DocketId,
            RegisterId = request.RegisterId,
            DocketNumber = request.DocketNumber,
            DocketHash = request.DocketHash,
            PreviousHash = string.IsNullOrEmpty(request.PreviousHash) ? null : request.PreviousHash,
            CreatedAt = request.CreatedAt.ToDateTimeOffset(),
            Transactions = transactions,
            Status = Models.DocketStatus.Confirmed,
            ProposerValidatorId = request.ProposerValidatorId,
            ProposerSignature = new Models.Signature
            {
                PublicKey = request.ProposerSignature.PublicKey,
                SignatureValue = request.ProposerSignature.SignatureValue,
                Algorithm = request.ProposerSignature.Algorithm
            },
            MerkleRoot = request.MerkleRoot,
            Votes = consensusVotes
        };
    }

    /// <summary>
    /// Maps gRPC Transaction to domain Transaction model
    /// </summary>
    private static Models.Transaction MapProtoToTransaction(Grpc.V1.Transaction protoTx)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            protoTx.PayloadJson);

        var signatures = protoTx.Signatures.Select(s => new Models.Signature
        {
            PublicKey = s.PublicKey,
            SignatureValue = s.SignatureValue,
            Algorithm = s.Algorithm
        }).ToList();

        var metadata = new Dictionary<string, string>();
        foreach (var kvp in protoTx.Metadata)
        {
            metadata[kvp.Key] = kvp.Value;
        }

        return new Models.Transaction
        {
            TransactionId = protoTx.TransactionId,
            RegisterId = protoTx.RegisterId,
            BlueprintId = protoTx.BlueprintId,
            ActionId = protoTx.ActionId,
            Payload = payload,
            CreatedAt = protoTx.CreatedAt.ToDateTimeOffset(),
            ExpiresAt = protoTx.ExpiresAt != null ? protoTx.ExpiresAt.ToDateTimeOffset() : null,
            Signatures = signatures,
            PayloadHash = protoTx.PayloadHash,
            Priority = protoTx.Priority switch
            {
                Grpc.V1.TransactionPriority.High => Models.TransactionPriority.High,
                Grpc.V1.TransactionPriority.Normal => Models.TransactionPriority.Normal,
                Grpc.V1.TransactionPriority.Low => Models.TransactionPriority.Low,
                _ => Models.TransactionPriority.Normal
            },
            Metadata = metadata
        };
    }

    /// <summary>
    /// Maps gRPC ConsensusVote to domain ConsensusVote model
    /// </summary>
    private static Models.ConsensusVote MapProtoToConsensusVote(Grpc.V1.ConsensusVote protoVote)
    {
        return new Models.ConsensusVote
        {
            VoteId = protoVote.VoteId,
            DocketId = protoVote.DocketId,
            ValidatorId = protoVote.ValidatorId,
            Decision = protoVote.Decision == Grpc.V1.VoteDecision.Approve
                ? Models.VoteDecision.Approve
                : Models.VoteDecision.Reject,
            RejectionReason = string.IsNullOrEmpty(protoVote.RejectionReason) ? null : protoVote.RejectionReason,
            VotedAt = protoVote.VotedAt.ToDateTimeOffset(),
            ValidatorSignature = new Models.Signature
            {
                PublicKey = protoVote.ValidatorSignature.PublicKey,
                SignatureValue = protoVote.ValidatorSignature.SignatureValue,
                Algorithm = protoVote.ValidatorSignature.Algorithm
            },
            DocketHash = protoVote.DocketHash
        };
    }
}
