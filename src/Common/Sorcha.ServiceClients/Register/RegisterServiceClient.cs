// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sorcha.Register.Models;
using Sorcha.ServiceClients.Auth;

namespace Sorcha.ServiceClients.Register;

/// <summary>
/// HTTP client for Register Service operations with JWT authentication support
/// </summary>
public class RegisterServiceClient : IRegisterServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceAuthClient _serviceAuth;
    private readonly ILogger<RegisterServiceClient> _logger;
    private readonly string _serviceAddress;
    private readonly bool _useGrpc;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RegisterServiceClient(
        HttpClient httpClient,
        IServiceAuthClient serviceAuth,
        IConfiguration configuration,
        ILogger<RegisterServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviceAuth = serviceAuth ?? throw new ArgumentNullException(nameof(serviceAuth));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serviceAddress = configuration["ServiceClients:RegisterService:Address"]
            ?? configuration["GrpcClients:RegisterService:Address"]
            ?? throw new InvalidOperationException("Register Service address not configured");

        _useGrpc = configuration.GetValue<bool>("ServiceClients:RegisterService:UseGrpc", false);

        // Configure HttpClient base address
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_serviceAddress.TrimEnd('/') + "/");
        }

        _logger.LogInformation(
            "RegisterServiceClient initialized (Address: {Address}, Protocol: {Protocol})",
            _serviceAddress, _useGrpc ? "gRPC" : "HTTP");
    }

    /// <summary>
    /// Sets the JWT authentication header for service-to-service calls
    /// </summary>
    private async Task SetAuthHeaderAsync(CancellationToken cancellationToken)
    {
        var token = await _serviceAuth.GetTokenAsync(cancellationToken);
        if (token is not null)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("No auth token available for Register Service call");
        }
    }

    // =========================================================================
    // Docket Operations
    // =========================================================================

    public async Task<bool> WriteDocketAsync(
        DocketModel docket,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Writing docket {DocketNumber} to register {RegisterId}",
                docket.DocketNumber, docket.RegisterId);

            await SetAuthHeaderAsync(cancellationToken);

            var request = new WriteDocketRequest
            {
                DocketId = docket.DocketId,
                DocketNumber = docket.DocketNumber,
                PreviousHash = docket.PreviousHash,
                DocketHash = docket.DocketHash,
                CreatedAt = docket.CreatedAt,
                TransactionIds = docket.Transactions.Select(t => t.TxId ?? t.Id ?? string.Empty).ToList(),
                ProposerValidatorId = docket.ProposerValidatorId,
                MerkleRoot = docket.MerkleRoot,
                Transactions = docket.Transactions
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"api/registers/{Uri.EscapeDataString(docket.RegisterId)}/dockets",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to write docket {DocketNumber} to register {RegisterId}: {StatusCode}",
                    docket.DocketNumber, docket.RegisterId, response.StatusCode);
                return false;
            }

            _logger.LogInformation(
                "Successfully wrote docket {DocketNumber} to register {RegisterId}",
                docket.DocketNumber, docket.RegisterId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error writing docket {DocketNumber} to register {RegisterId}",
                docket.DocketNumber, docket.RegisterId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to write docket {DocketNumber} to register {RegisterId}",
                docket.DocketNumber, docket.RegisterId);
            return false;
        }
    }

    public async Task<DocketModel?> ReadDocketAsync(
        string registerId,
        long docketNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Reading docket {DocketNumber} from register {RegisterId}",
                docketNumber, registerId);

            var response = await _httpClient.GetAsync(
                $"api/registers/{Uri.EscapeDataString(registerId)}/dockets/{docketNumber}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Docket {DocketNumber} not found in register {RegisterId}", docketNumber, registerId);
                    return null;
                }

                _logger.LogWarning(
                    "Failed to read docket {DocketNumber} from register {RegisterId}: {StatusCode}",
                    docketNumber, registerId, response.StatusCode);
                return null;
            }

            var docket = await response.Content.ReadFromJsonAsync<DocketResponse>(JsonOptions, cancellationToken);
            if (docket == null)
            {
                return null;
            }

            return MapToDocketModel(docket, registerId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error reading docket {DocketNumber} from register {RegisterId}",
                docketNumber, registerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to read docket {DocketNumber} from register {RegisterId}",
                docketNumber, registerId);
            return null;
        }
    }

    public async Task<DocketModel?> ReadLatestDocketAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Reading latest docket from register {RegisterId}", registerId);

            var response = await _httpClient.GetAsync(
                $"api/registers/{Uri.EscapeDataString(registerId)}/dockets/latest",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("No dockets found for register {RegisterId}", registerId);
                    return null;
                }

                _logger.LogWarning(
                    "Failed to read latest docket from register {RegisterId}: {StatusCode}",
                    registerId, response.StatusCode);
                return null;
            }

            var docket = await response.Content.ReadFromJsonAsync<DocketResponse>(JsonOptions, cancellationToken);
            if (docket == null)
            {
                return null;
            }

            return MapToDocketModel(docket, registerId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error reading latest docket from register {RegisterId}", registerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read latest docket from register {RegisterId}", registerId);
            return null;
        }
    }

    public async Task<long> GetRegisterHeightAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting register height for {RegisterId}", registerId);

            var register = await GetRegisterAsync(registerId, cancellationToken);
            if (register == null)
            {
                _logger.LogWarning("Register {RegisterId} not found", registerId);
                return -1L;
            }

            return register.Height;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get register height for {RegisterId}", registerId);
            return -1L;
        }
    }

    private static DocketModel MapToDocketModel(DocketResponse docket, string registerId)
    {
        return new DocketModel
        {
            DocketId = docket.Id.ToString(),
            RegisterId = registerId,
            DocketNumber = (long)docket.Id,
            PreviousHash = docket.PreviousHash,
            DocketHash = docket.Hash,
            CreatedAt = docket.TimeStamp,
            Transactions = [], // Transactions need to be fetched separately if needed
            ProposerValidatorId = docket.Votes ?? string.Empty,
            MerkleRoot = string.Empty // Not stored in Register.Models.Docket
        };
    }

    // =========================================================================
    // Transaction Operations
    // =========================================================================

    public async Task<TransactionModel> SubmitTransactionAsync(
        string registerId,
        TransactionModel transaction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Submitting transaction {TransactionId} to register {RegisterId}",
                transaction.Id, registerId);

            await SetAuthHeaderAsync(cancellationToken);

            var response = await _httpClient.PostAsJsonAsync(
                $"api/registers/{Uri.EscapeDataString(registerId)}/transactions",
                transaction,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to submit transaction {TransactionId} to register {RegisterId}: {StatusCode} - {Error}",
                    transaction.Id, registerId, response.StatusCode, error);
                throw new HttpRequestException($"Failed to submit transaction: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<TransactionModel>(JsonOptions, cancellationToken);
            _logger.LogInformation(
                "Successfully submitted transaction {TransactionId} to register {RegisterId}",
                transaction.Id, registerId);
            return result ?? transaction;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to submit transaction {TransactionId} to register {RegisterId}",
                transaction.Id, registerId);
            throw;
        }
    }

    public async Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Getting transaction {TransactionId} from register {RegisterId}",
                transactionId, registerId);

            await SetAuthHeaderAsync(cancellationToken);

            var response = await _httpClient.GetAsync(
                $"api/registers/{Uri.EscapeDataString(registerId)}/transactions/{Uri.EscapeDataString(transactionId)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Transaction {TransactionId} not found in register {RegisterId}", transactionId, registerId);
                    return null;
                }

                _logger.LogWarning(
                    "Failed to get transaction {TransactionId} from register {RegisterId}: {StatusCode}",
                    transactionId, registerId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TransactionModel>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error getting transaction {TransactionId} from register {RegisterId}",
                transactionId, registerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get transaction {TransactionId} from register {RegisterId}",
                transactionId, registerId);
            return null;
        }
    }

    public async Task<TransactionPage> GetTransactionsAsync(
        string registerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Getting transactions from register {RegisterId} (page {Page}, size {PageSize})",
                registerId, page, pageSize);

            var response = await _httpClient.GetAsync(
                $"api/registers/{Uri.EscapeDataString(registerId)}/transactions?$skip={(page - 1) * pageSize}&$top={pageSize}&$count=true",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get transactions from register {RegisterId}: {StatusCode}",
                    registerId, response.StatusCode);
                return new TransactionPage { Page = page, PageSize = pageSize };
            }

            var transactions = await response.Content.ReadFromJsonAsync<List<TransactionModel>>(JsonOptions, cancellationToken);
            return new TransactionPage
            {
                Page = page,
                PageSize = pageSize,
                Transactions = transactions ?? []
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting transactions from register {RegisterId}", registerId);
            return new TransactionPage { Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions from register {RegisterId}", registerId);
            throw;
        }
    }

    public async Task<TransactionPage> GetTransactionsByWalletAsync(
        string registerId,
        string walletAddress,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Getting transactions for wallet {WalletAddress} from register {RegisterId}",
                walletAddress, registerId);

            var response = await _httpClient.GetAsync(
                $"api/query/wallet/{Uri.EscapeDataString(walletAddress)}/transactions/{Uri.EscapeDataString(registerId)}?$skip={(page - 1) * pageSize}&$top={pageSize}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get transactions for wallet {WalletAddress} from register {RegisterId}: {StatusCode}",
                    walletAddress, registerId, response.StatusCode);
                return new TransactionPage { Page = page, PageSize = pageSize };
            }

            var transactions = await response.Content.ReadFromJsonAsync<List<TransactionModel>>(JsonOptions, cancellationToken);
            return new TransactionPage
            {
                Page = page,
                PageSize = pageSize,
                Transactions = transactions ?? []
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error getting transactions for wallet {WalletAddress} from register {RegisterId}",
                walletAddress, registerId);
            return new TransactionPage { Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get transactions for wallet {WalletAddress} from register {RegisterId}",
                walletAddress, registerId);
            throw;
        }
    }

    public async Task<TransactionPage> GetTransactionsByPrevTxIdAsync(
        string registerId,
        string prevTxId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Getting transactions by prevTxId {PrevTxId} from register {RegisterId}",
                prevTxId, registerId);

            await SetAuthHeaderAsync(cancellationToken);

            var response = await _httpClient.GetAsync(
                $"api/query/previous/{Uri.EscapeDataString(prevTxId)}/transactions?registerId={Uri.EscapeDataString(registerId)}&$skip={(page - 1) * pageSize}&$top={pageSize}&$count=true",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug(
                        "No transactions found for prevTxId {PrevTxId} in register {RegisterId}",
                        prevTxId, registerId);
                    return new TransactionPage { Page = page, PageSize = pageSize };
                }

                _logger.LogWarning(
                    "Failed to get transactions by prevTxId {PrevTxId} from register {RegisterId}: {StatusCode}",
                    prevTxId, registerId, response.StatusCode);
                return new TransactionPage { Page = page, PageSize = pageSize };
            }

            var result = await response.Content.ReadFromJsonAsync<PrevTxIdQueryResponse>(JsonOptions, cancellationToken);
            if (result == null)
            {
                return new TransactionPage { Page = page, PageSize = pageSize };
            }

            return new TransactionPage
            {
                Page = result.Page,
                PageSize = result.PageSize,
                Total = result.TotalCount,
                Transactions = result.Items ?? []
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error getting transactions by prevTxId {PrevTxId} from register {RegisterId}",
                prevTxId, registerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get transactions by prevTxId {PrevTxId} from register {RegisterId}",
                prevTxId, registerId);
            throw;
        }
    }

    public async Task<List<TransactionModel>> GetTransactionsByInstanceIdAsync(
        string registerId,
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Getting transactions for instance {InstanceId} from register {RegisterId}",
                instanceId, registerId);

            var response = await _httpClient.GetAsync(
                $"api/query/instance/{Uri.EscapeDataString(instanceId)}/transactions/{Uri.EscapeDataString(registerId)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get transactions for instance {InstanceId} from register {RegisterId}: {StatusCode}",
                    instanceId, registerId, response.StatusCode);
                return [];
            }

            var transactions = await response.Content.ReadFromJsonAsync<List<TransactionModel>>(JsonOptions, cancellationToken);
            return transactions ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error getting transactions for instance {InstanceId} from register {RegisterId}",
                instanceId, registerId);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get transactions for instance {InstanceId} from register {RegisterId}",
                instanceId, registerId);
            throw;
        }
    }

    // =========================================================================
    // Register Management
    // =========================================================================

    public async Task<Sorcha.Register.Models.Register?> GetRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting register info for {RegisterId}", registerId);

            await SetAuthHeaderAsync(cancellationToken);

            var response = await _httpClient.GetAsync(
                $"api/registers/{Uri.EscapeDataString(registerId)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Register {RegisterId} not found", registerId);
                    return null;
                }

                _logger.LogWarning(
                    "Failed to get register info for {RegisterId}: {StatusCode}",
                    registerId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Sorcha.Register.Models.Register>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting register info for {RegisterId}", registerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get register info for {RegisterId}", registerId);
            return null;
        }
    }

    public async Task<Sorcha.Register.Models.Register> CreateRegisterAsync(
        string registerId,
        string name,
        string blueprintId,
        string owner,
        string tenant,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating register {RegisterId} with name {Name}", registerId, name);

            var request = new CreateRegisterRequest
            {
                Name = name,
                TenantId = tenant,
                Advertise = true,
                IsFullReplica = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/registers",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to create register {RegisterId}: {StatusCode} - {Error}",
                    registerId, response.StatusCode, error);
                throw new HttpRequestException($"Failed to create register: {response.StatusCode}");
            }

            var register = await response.Content.ReadFromJsonAsync<Sorcha.Register.Models.Register>(JsonOptions, cancellationToken);
            _logger.LogInformation("Successfully created register {RegisterId}", registerId);
            return register ?? throw new InvalidOperationException("Failed to deserialize register response");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create register {RegisterId}", registerId);
            throw;
        }
    }

    // =========================================================================
    // Internal DTOs
    // =========================================================================

    private record WriteDocketRequest
    {
        public required string DocketId { get; init; }
        public required long DocketNumber { get; init; }
        public string? PreviousHash { get; init; }
        public required string DocketHash { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required List<string> TransactionIds { get; init; }
        public required string ProposerValidatorId { get; init; }
        public required string MerkleRoot { get; init; }
        public List<Sorcha.Register.Models.TransactionModel>? Transactions { get; init; }
    }

    private record CreateRegisterRequest
    {
        public required string Name { get; init; }
        public required string TenantId { get; init; }
        public bool Advertise { get; init; } = false;
        public bool IsFullReplica { get; init; } = true;
    }

    private record PrevTxIdQueryResponse
    {
        public List<TransactionModel> Items { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
    }

    private record DocketResponse
    {
        public ulong Id { get; init; }
        public string RegisterId { get; init; } = string.Empty;
        public string PreviousHash { get; init; } = string.Empty;
        public string Hash { get; init; } = string.Empty;
        public List<string> TransactionIds { get; init; } = [];
        public DateTimeOffset TimeStamp { get; init; }
        public string? Votes { get; init; }
    }
}
