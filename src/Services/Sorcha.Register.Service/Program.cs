// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OData.ModelBuilder;
using MongoDB.Driver;
using Scalar.AspNetCore;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Register.Core.Events;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Core.Storage;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;
using Sorcha.Register.Service.Extensions;
using Sorcha.Register.Service.Hubs;
using Sorcha.Register.Service.Repositories;
using Sorcha.Register.Service.Services;
using Microsoft.Extensions.Options;
using Sorcha.Register.Storage.InMemory;
using Sorcha.Register.Storage.MongoDB;
using Sorcha.ServiceClients.Extensions;
using Sorcha.ServiceClients.Peer;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add rate limiting (SEC-002)
builder.AddRateLimiting();

// Add input validation (SEC-003)
builder.AddInputValidation();

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Configure OData
var modelBuilder = new ODataConventionModelBuilder();
modelBuilder.EntitySet<Sorcha.Register.Models.Register>("Registers");
modelBuilder.EntitySet<TransactionModel>("Transactions");
modelBuilder.EntitySet<Docket>("Dockets");

builder.Services.AddControllers()
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100)
        .AddRouteComponents("odata", modelBuilder.GetEdmModel()));

// Add OpenAPI services
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Sorcha Register Service API";
        document.Info.Version = "1.0.0";
        document.Info.Description = """
            # Register Service API

            ## Overview

            The Register Service provides a **distributed ledger** for storing immutable transaction records in the Sorcha platform. It implements a cryptographically-secured, append-only data structure where transactions are chained together, ensuring data integrity and non-repudiation.

            ## Primary Use Cases

            - **Transaction Storage**: Submit and store signed transactions on the distributed ledger
            - **Transaction Queries**: Retrieve transactions by ID, register, wallet, or docket
            - **Register Management**: Create and manage isolated transaction ledgers
            - **Data Verification**: Verify transaction chains and cryptographic signatures
            - **Real-time Notifications**: Subscribe to transaction confirmations via SignalR

            ## Key Concepts

            ### Registers
            A **Register** is an isolated, append-only ledger that stores related transactions:
            - **Register ID**: Unique identifier for the ledger
            - **Organization Ownership**: Each register belongs to a tenant organization
            - **Isolation**: Transactions in one register cannot reference transactions in another
            - **Purpose-Specific**: Registers can be created for different use cases (documents, workflows, audit logs)

            ### Transactions
            **Transactions** are the fundamental unit of data in a register:
            - **Immutable**: Once committed, transactions cannot be altered
            - **Chained**: Each transaction references the previous transaction hash
            - **Signed**: All transactions must be cryptographically signed by a wallet
            - **Timestamped**: UTC timestamps for ordering and auditability
            - **Payloads**: Encrypted data payloads with wallet-based access control

            ### Transaction Structure
            ```json
            {
              "txId": "unique-transaction-hash",
              "registerId": "register-id",
              "senderWallet": "wallet-address",
              "timeStamp": "2025-12-11T10:30:00Z",
              "prevTxId": "previous-transaction-hash",
              "payloads": [
                {
                  "data": "base64-encrypted-payload",
                  "walletAccess": ["wallet1", "wallet2"]
                }
              ],
              "metadata": {
                "blueprintId": "workflow-id",
                "actionId": "action-id",
                "txType": "data"
              },
              "signature": "base64-signature"
            }
            ```

            ### Dockets
            **Dockets** are logical groupings of related transactions:
            - Workflow instances reference dockets
            - All transactions for a blueprint instance share a docket
            - Enables efficient querying of related transactions

            ### Transaction Chain Integrity
            Each transaction links to the previous transaction via `prevTxId`:
            ```
            Genesis → Tx1 → Tx2 → Tx3 → ...
            (prevTxId: "") (prevTxId: Genesis) (prevTxId: Tx1)
            ```

            This creates a **Merkle chain** that ensures:
            - ✅ **Immutability**: Altering any transaction breaks the chain
            - ✅ **Auditability**: Full history is traceable
            - ✅ **Verification**: Chain integrity can be cryptographically verified

            ## Getting Started

            ### 1. Create a Register
            ```http
            POST /api/registers
            Authorization: Bearer {token}
            Content-Type: application/json

            {
              "registerId": "my-register-001",
              "organizationId": "org-123",
              "metadata": {
                "purpose": "Document Management",
                "department": "Finance"
              }
            }
            ```

            ### 2. Submit a Transaction
            ```http
            POST /api/registers/{registerId}/transactions
            Authorization: Bearer {token}
            Content-Type: application/json

            {
              "registerId": "my-register-001",
              "senderWallet": "wallet-abc123",
              "payloads": [
                {
                  "data": "base64-encrypted-data",
                  "walletAccess": ["wallet-abc123"]
                }
              ],
              "metadata": {
                "txType": "data",
                "blueprintId": "workflow-001"
              },
              "signature": "base64-signature",
              "prevTxId": "previous-tx-hash"
            }
            ```

            ### 3. Query Transactions
            ```http
            # Get all transactions in a register (OData)
            GET /odata/Transactions?$filter=RegisterId eq 'my-register-001'&$orderby=TimeStamp desc

            # Get specific transaction
            GET /api/transactions/{txId}

            # Get transactions by wallet
            GET /api/wallets/{walletId}/transactions

            # Get transactions by docket
            GET /api/dockets/{docketId}/transactions
            ```

            ### 4. Subscribe to Real-Time Updates
            ```javascript
            // SignalR connection (JavaScript example)
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/registerhub")
                .build();

            connection.on("TransactionConfirmed", (tx) => {
                console.log("Transaction confirmed:", tx.txId);
            });

            await connection.start();
            ```

            ## Transaction Lifecycle

            1. **Creation** → Transaction prepared by Blueprint Service
            2. **Signing** → Transaction signed by Wallet Service
            3. **Submission** → Transaction submitted to Register Service
            4. **Validation** → Signature and chain integrity verified
            5. **Storage** → Transaction written to ledger (immutable)
            6. **Notification** → Confirmation broadcast via SignalR
            7. **Querying** → Transaction available for retrieval

            ## Data Access Control

            ### Payload Encryption
            - Each payload is encrypted for specific wallets
            - Only wallets in `walletAccess` can decrypt the payload
            - Supports selective disclosure (different data for different participants)

            ### Query Authorization
            - Users can only query transactions they have access to
            - Wallet-based access control enforced at query time
            - Organization isolation ensures multi-tenant security

            ## OData Query Capabilities

            The Register Service supports **OData v4** for powerful querying:

            ```http
            # Filter by wallet
            GET /odata/Transactions?$filter=SenderWallet eq 'wallet-123'

            # Order by timestamp
            GET /odata/Transactions?$orderby=TimeStamp desc

            # Pagination
            GET /odata/Transactions?$top=50&$skip=100

            # Complex filters
            GET /odata/Transactions?$filter=contains(Metadata/blueprintId, 'workflow') and TimeStamp gt 2025-01-01
            ```

            ## Performance Considerations

            - **Indexing**: Transactions indexed by ID, register, wallet, docket, and timestamp
            - **Caching**: Frequently accessed transactions cached in memory
            - **Pagination**: Use `$top` and `$skip` for large result sets
            - **SignalR Backplane**: Redis used for scalable real-time notifications

            ## Security Features

            - ✅ Cryptographic signature verification
            - ✅ Transaction chain integrity checks
            - ✅ Wallet-based payload encryption
            - ✅ Organization isolation (multi-tenant)
            - ✅ Immutable audit trail
            - ✅ OWASP security headers
            - ✅ Rate limiting and DDoS protection

            ## Integration with Sorcha Platform

            ### Transaction Flow
            1. **Blueprint Service** creates transaction payloads
            2. **Wallet Service** signs the transaction
            3. **Register Service** validates and stores the transaction
            4. **Peer Service** (future) replicates to peer nodes

            ### Data Integrity
            - All transactions must have valid signatures from Wallet Service
            - Chain integrity enforced on every submission
            - Tamper detection via hash verification

            ## Target Audience

            - **Application Developers**: Building blockchain-based applications
            - **Auditors**: Verifying transaction histories
            - **System Integrators**: Integrating with external systems
            - **Compliance Officers**: Ensuring data immutability

            ## Related Services

            - **Wallet Service**: Signs transactions with cryptographic keys
            - **Blueprint Service**: Creates workflow-based transactions
            - **Tenant Service**: Provides organization isolation and access control
            - **Peer Service**: Distributes transactions across peer network (future)

            ## Common Workflows

            ### Document Timestamping
            1. Create register for document management
            2. Submit document hash as transaction
            3. Transaction provides cryptographic proof of existence at timestamp

            ### Workflow Execution
            1. Blueprint defines multi-step workflow
            2. Each action creates a transaction
            3. Transactions chained to show workflow progression
            4. Full audit trail of workflow execution

            ### Multi-Party Collaboration
            1. Multiple wallets participate in workflow
            2. Each party signs their transactions
            3. Selective disclosure controls data visibility
            4. Immutable record of all interactions
            """;

        if (document.Info.Contact == null)
        {
            document.Info.Contact = new() { };
        }
        document.Info.Contact.Name = "Sorcha Platform Team";
        document.Info.Contact.Url = new Uri("https://github.com/siccar-platform/sorcha");

        if (document.Info.License == null)
        {
            document.Info.License = new() { };
        }
        document.Info.License.Name = "MIT License";
        document.Info.License.Url = new Uri("https://opensource.org/licenses/MIT");

        return Task.CompletedTask;
    });
});

// Register storage and event infrastructure
// Smart configuration: Use MongoDB if configured, otherwise InMemory
var storageType = builder.Configuration["RegisterStorage:Type"] ?? "InMemory";
if (storageType.Equals("MongoDB", StringComparison.OrdinalIgnoreCase))
{
    // Configure MongoDB storage
    builder.Services.Configure<MongoRegisterStorageConfiguration>(
        builder.Configuration.GetSection("RegisterStorage:MongoDB"));

    builder.Services.AddSingleton<IRegisterRepository>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<MongoRegisterStorageConfiguration>>();
        var logger = sp.GetRequiredService<ILogger<MongoRegisterRepository>>();
        return new MongoRegisterRepository(options, logger);
    });

    Console.WriteLine($"✅ Register Service using MongoDB storage: {builder.Configuration["RegisterStorage:MongoDB:ConnectionString"]}");
}
else
{
    // Use in-memory storage (default)
    builder.Services.AddSingleton<IRegisterRepository, InMemoryRegisterRepository>();
    Console.WriteLine("✅ Register Service using InMemory storage (development mode)");
}

builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Register managers
builder.Services.AddScoped<RegisterManager>();
builder.Services.AddScoped<TransactionManager>();
builder.Services.AddScoped<QueryManager>();

// Register creation orchestration
builder.Services.AddScoped<IRegisterCreationOrchestrator, RegisterCreationOrchestrator>();

// Redis client for distributed state (pending registrations, caching)
builder.AddRedisClient("redis");

// Pending registration storage (Redis-backed for multi-instance deployments)
builder.Services.AddSingleton<IPendingRegistrationStore, PendingRegistrationStore>();

// Register cryptography services (from Sorcha.Cryptography)
builder.Services.AddScoped<IHashProvider, Sorcha.Cryptography.Core.HashProvider>();
builder.Services.AddScoped<ICryptoModule, Sorcha.Cryptography.Core.CryptoModule>();

// Register wallet service client
builder.Services.AddServiceClients(builder.Configuration);

// Register MongoDB for system register
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
    return new MongoClient(connectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "sorcha_system_register";
    return client.GetDatabase(databaseName);
});

// Register system register services
builder.Services.AddSingleton<ISystemRegisterRepository, MongoSystemRegisterRepository>();
builder.Services.AddSingleton<SystemRegisterService>();

// Add JWT authentication and authorization (AUTH-002)
// JWT authentication is now configured via shared ServiceDefaults with auto-key generation
builder.AddJwtAuthentication();
builder.Services.AddRegisterAuthorization();

var app = builder.Build();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

// Add OWASP security headers (SEC-004)
app.UseApiSecurityHeaders();

// Enable HTTPS enforcement with HSTS (SEC-001)
app.UseHttpsEnforcement();

// Enable input validation (SEC-003)
app.UseInputValidation();

// Configure OpenAPI
app.MapOpenApi();

// Configure Scalar API documentation UI
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Register Service")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Map SignalR hub
app.MapHub<RegisterHub>("/hubs/register");

// Add authentication and authorization middleware (AUTH-002)
app.UseAuthentication();
app.UseAuthorization();

// Enable rate limiting (SEC-002)
app.UseRateLimiting();

// ===========================
// Register Management API
// ===========================

var registersGroup = app.MapGroup("/api/registers")
    .WithTags("Registers")
    .RequireAuthorization("CanManageRegisters");

// NOTE: POST /api/registers/ (simple CRUD creation) has been removed.
// All register creation must go through the two-phase initiate/finalize flow.
// See register creation endpoints below (POST /api/registers/initiate and POST /api/registers/finalize).

/// <summary>
/// Get all registers
/// </summary>
registersGroup.MapGet("/", async (
    RegisterManager manager,
    string? tenantId = null) =>
{
    var registers = tenantId != null
        ? await manager.GetRegistersByTenantAsync(tenantId)
        : await manager.GetAllRegistersAsync();

    return Results.Ok(registers);
})
.WithName("GetAllRegisters")
.WithSummary("Get all registers")
.WithDescription("Retrieves all registers, optionally filtered by tenant.");

/// <summary>
/// Get register by ID
/// </summary>
registersGroup.MapGet("/{id}", async (
    RegisterManager manager,
    string id) =>
{
    var register = await manager.GetRegisterAsync(id);
    return register is not null ? Results.Ok(register) : Results.NotFound();
})
.WithName("GetRegister")
.WithSummary("Get register by ID")
.WithDescription("Retrieves a specific register by its unique identifier.");

/// <summary>
/// Update register
/// </summary>
registersGroup.MapPut("/{id}", async (
    RegisterManager manager,
    IPeerServiceClient peerClient,
    ILogger<Program> logger,
    string id,
    UpdateRegisterRequest request) =>
{
    var register = await manager.GetRegisterAsync(id);
    if (register is null)
        return Results.NotFound();

    var advertiseChanged = request.Advertise is not null && register.Advertise != request.Advertise.Value;

    if (request.Name is not null)
        register.Name = request.Name;
    if (request.Status is not null)
        register.Status = request.Status.Value;
    if (request.Advertise is not null)
        register.Advertise = request.Advertise.Value;

    var updated = await manager.UpdateRegisterAsync(register);

    // Notify Peer Service when advertise flag changes (fire-and-forget)
    if (advertiseChanged)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await peerClient.AdvertiseRegisterAsync(register.Id, register.Advertise);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to notify Peer Service about advertise change for register {RegisterId}",
                    register.Id);
            }
        });
    }

    return Results.Ok(updated);
})
.WithName("UpdateRegister")
.WithSummary("Update register")
.WithDescription("Updates register metadata and settings.");

/// <summary>
/// Delete register
/// </summary>
registersGroup.MapDelete("/{id}", async (
    RegisterManager manager,
    IHubContext<RegisterHub, IRegisterHubClient> hubContext,
    string id,
    string tenantId) =>
{
    try
    {
        await manager.DeleteRegisterAsync(id, tenantId);

        // Notify via SignalR
        await hubContext.Clients
            .Group($"tenant:{tenantId}")
            .RegisterDeleted(id);

        return Results.NoContent();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
})
.WithName("DeleteRegister")
.WithSummary("Delete register")
.WithDescription("Deletes a register and all associated data.");

/// <summary>
/// Get register count
/// </summary>
registersGroup.MapGet("/stats/count", async (RegisterManager manager) =>
{
    var count = await manager.GetRegisterCountAsync();
    return Results.Ok(new { count });
})
.WithName("GetRegisterCount")
.WithSummary("Get register count")
.WithDescription("Returns the total number of registers.");

// ===========================
// Register Creation with Genesis Transactions (FR-REG-001A)
// ===========================
// Separate endpoint group for register creation workflow (initiate/finalize)
// These endpoints allow anonymous access for walkthrough/testing purposes
var registerCreationGroup = app.MapGroup("/api/registers")
    .WithTags("Register Creation")
    .AllowAnonymous();

/// <summary>
/// Initiate register creation (Phase 1): Generate unsigned control record
/// </summary>
registerCreationGroup.MapPost("/initiate", async (
    IRegisterCreationOrchestrator orchestrator,
    InitiateRegisterCreationRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await orchestrator.InitiateAsync(request, cancellationToken);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message, details = "Invalid request parameters" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Register initiation failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("InitiateRegisterCreation")
.WithSummary("Initiate register creation (Phase 1)")
.WithDescription(@"
**Phase 1: Generate Unsigned Control Record**

Initiates the two-phase register creation workflow by generating a unique register ID
and unsigned control record template. The client must sign the returned `dataToSign` hash
with each admin's wallet before calling the finalize endpoint.

**Workflow:**
1. Server generates unique register ID and control record template
2. Server computes SHA-256 hash of control record for signing
3. Client signs the hash with each admin's wallet (offline/client-side)
4. Client calls /finalize with signed control record

**Control Record:**
The control record establishes administrative control with cryptographic attestations.
At least one 'owner' attestation is required.

**Expiration:**
The pending registration expires after 5 minutes. The client must finalize within this timeframe.

**Returns:**
- `registerId`: Generated unique ID
- `controlRecord`: Template with placeholder signatures
- `dataToSign`: SHA-256 hash to sign with wallets
- `expiresAt`: Expiration timestamp
- `nonce`: Replay protection nonce
");

/// <summary>
/// Finalize register creation (Phase 2): Verify signatures and create register
/// </summary>
registerCreationGroup.MapPost("/finalize", async (
    IRegisterCreationOrchestrator orchestrator,
    FinalizeRegisterCreationRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await orchestrator.FinalizeAsync(request, cancellationToken);
        return Results.Created($"/api/registers/{response.RegisterId}", response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("expired"))
    {
        return Results.Problem(
            title: "Registration expired",
            detail: ex.Message,
            statusCode: 408); // Request Timeout
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Problem(
            title: "Signature verification failed",
            detail: ex.Message,
            statusCode: 401); // Unauthorized
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message, details = "Invalid control record or signatures" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Register finalization failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("FinalizeRegisterCreation")
.WithSummary("Finalize register creation (Phase 2)")
.WithDescription(@"
**Phase 2: Verify Signatures and Create Register**

Completes the register creation workflow by verifying all attestation signatures,
creating the register in the database, and generating the genesis transaction.

**Workflow:**
1. Server retrieves pending registration by ID and nonce
2. Server validates control record against JSON Schema
3. Server verifies each attestation signature using public keys
4. Server creates register in database
5. Server creates genesis transaction with control record payload
6. Server submits genesis transaction to Validator Service
7. Validator creates genesis docket (height 0)

**Signature Verification:**
- Each attestation signature is verified using the subject's public key
- Supported algorithms: ED25519, NISTP256, RSA4096
- Signature must match the SHA-256 hash from initiation phase

**Genesis Transaction:**
The genesis transaction contains the signed control record and establishes
an immutable audit trail of register creation and ownership.

**Returns:**
- `registerId`: Created register ID
- `status`: 'created'
- `genesisTransactionId`: Genesis transaction ID
- `genesisDocketId`: '0' (genesis docket)
- `createdAt`: Creation timestamp

**Errors:**
- 400 Bad Request: Invalid control record or validation errors
- 401 Unauthorized: Signature verification failed
- 408 Request Timeout: Pending registration expired
- 500 Internal Server Error: Database or service error
");

// ===========================
// Transaction Management API
// ===========================

var transactionsGroup = app.MapGroup("/api/registers/{registerId}/transactions")
    .WithTags("Transactions")
    .RequireAuthorization("CanSubmitTransactions");

/// <summary>
/// Submit a transaction
/// </summary>
transactionsGroup.MapPost("/", async (
    TransactionManager manager,
    IHubContext<RegisterHub, IRegisterHubClient> hubContext,
    string registerId,
    TransactionModel transaction) =>
{
    try
    {
        transaction.RegisterId = registerId;
        var stored = await manager.StoreTransactionAsync(transaction);

        // Notify via SignalR
        await hubContext.Clients
            .Group($"register:{registerId}")
            .TransactionConfirmed(registerId, stored.TxId);

        return Results.Created($"/api/registers/{registerId}/transactions/{stored.TxId}", stored);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("SubmitTransaction")
.WithSummary("Submit a transaction")
.WithDescription("Stores a validated transaction in the register.");

/// <summary>
/// Get transaction by ID
/// </summary>
transactionsGroup.MapGet("/{txId}", async (
    TransactionManager manager,
    string registerId,
    string txId) =>
{
    var transaction = await manager.GetTransactionAsync(registerId, txId);
    return transaction is not null ? Results.Ok(transaction) : Results.NotFound();
})
.WithName("GetTransaction")
.WithSummary("Get transaction by ID")
.WithDescription("Retrieves a specific transaction by its ID.");

/// <summary>
/// Get all transactions for a register (queryable)
/// </summary>
transactionsGroup.MapGet("/", async (
    TransactionManager manager,
    string registerId,
    int page = 1,
    int pageSize = 20) =>
{
    var transactions = await manager.GetTransactionsAsync(registerId);
    var paged = transactions
        .OrderByDescending(t => t.TimeStamp)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Results.Ok(new
    {
        Page = page,
        PageSize = pageSize,
        Total = transactions.Count(),
        Transactions = paged
    });
})
.WithName("GetTransactions")
.WithSummary("Get all transactions")
.WithDescription("Retrieves all transactions for a register with pagination.");

// ===========================
// Query API
// ===========================

var queryGroup = app.MapGroup("/api/query")
    .WithTags("Query")
    .RequireAuthorization("CanReadTransactions");

/// <summary>
/// Query transactions by wallet address
/// </summary>
queryGroup.MapGet("/wallets/{address}/transactions", async (
    QueryManager manager,
    string address,
    string? registerId = null,
    int page = 1,
    int pageSize = 20) =>
{
    if (registerId is not null)
    {
        var result = await manager.GetTransactionsByWalletPaginatedAsync(
            registerId,
            address,
            page,
            pageSize);
        return Results.Ok(result);
    }

    // Query across all registers (future enhancement)
    return Results.BadRequest(new { error = "registerId is required" });
})
.WithName("GetTransactionsByWallet")
.WithSummary("Query transactions by wallet")
.WithDescription("Retrieves all transactions for a specific wallet address.");

/// <summary>
/// Query transactions by sender
/// </summary>
queryGroup.MapGet("/senders/{address}/transactions", async (
    QueryManager manager,
    string address,
    string registerId,
    int page = 1,
    int pageSize = 20) =>
{
    var result = await manager.GetTransactionsByWalletPaginatedAsync(
        registerId,
        address,
        page,
        pageSize,
        asSender: true,
        asRecipient: false);
    return Results.Ok(result);
})
.WithName("GetTransactionsBySender")
.WithSummary("Query transactions by sender")
.WithDescription("Retrieves all transactions sent by a specific address.");

/// <summary>
/// Query transactions by blueprint
/// </summary>
queryGroup.MapGet("/blueprints/{blueprintId}/transactions", async (
    QueryManager manager,
    string blueprintId,
    string registerId,
    string? instanceId = null) =>
{
    var result = await manager.GetTransactionsByBlueprintAsync(
        registerId,
        blueprintId,
        instanceId);

    return Results.Ok(result);
})
.WithName("GetTransactionsByBlueprint")
.WithSummary("Query transactions by blueprint")
.WithDescription("Retrieves all transactions for a specific blueprint.");

/// <summary>
/// Get transaction statistics
/// </summary>
queryGroup.MapGet("/stats", async (
    QueryManager manager,
    string registerId) =>
{
    var stats = await manager.GetTransactionStatisticsAsync(registerId);
    return Results.Ok(stats);
})
.WithName("GetTransactionStatistics")
.WithSummary("Get transaction statistics")
.WithDescription("Retrieves comprehensive statistics for a register.");

/// <summary>
/// Query transactions by previous transaction ID (for fork detection and chain traversal)
/// </summary>
queryGroup.MapGet("/previous/{prevTxId}/transactions", async (
    QueryManager manager,
    string prevTxId,
    string? registerId = null,
    int page = 1,
    int pageSize = 20) =>
{
    if (registerId is null)
    {
        return Results.BadRequest(new { error = "registerId is required" });
    }

    var result = await manager.GetTransactionsByPrevTxIdPaginatedAsync(
        registerId,
        prevTxId,
        page,
        pageSize);
    return Results.Ok(result);
})
.WithName("GetTransactionsByPrevTxId")
.WithSummary("Query transactions by previous transaction ID")
.WithDescription("Retrieves all transactions that reference a given previous transaction ID. Used for fork detection and chain integrity auditing.");

// ===========================
// Docket Management API
// ===========================

var docketsGroup = app.MapGroup("/api/registers/{registerId}/dockets")
    .WithTags("Dockets")
    .RequireAuthorization("CanReadTransactions");

/// <summary>
/// Get all dockets for a register
/// </summary>
docketsGroup.MapGet("/", async (
    IRegisterRepository repository,
    string registerId) =>
{
    var dockets = await repository.GetDocketsAsync(registerId);
    return Results.Ok(dockets);
})
.WithName("GetDockets")
.WithSummary("Get all dockets")
.WithDescription("Retrieves all dockets (blocks) for a register.");

/// <summary>
/// Get docket by ID
/// </summary>
docketsGroup.MapGet("/{docketId}", async (
    IRegisterRepository repository,
    string registerId,
    ulong docketId) =>
{
    var docket = await repository.GetDocketAsync(registerId, docketId);
    return docket is not null ? Results.Ok(docket) : Results.NotFound();
})
.WithName("GetDocket")
.WithSummary("Get docket by ID")
.WithDescription("Retrieves a specific docket by its ID (block height).");

/// <summary>
/// Get transactions in a docket
/// </summary>
docketsGroup.MapGet("/{docketId}/transactions", async (
    IRegisterRepository repository,
    string registerId,
    ulong docketId) =>
{
    var transactions = await repository.GetTransactionsByDocketAsync(registerId, docketId);
    return Results.Ok(transactions);
})
.WithName("GetDocketTransactions")
.WithSummary("Get docket transactions")
.WithDescription("Retrieves all transactions sealed in a specific docket.");

/// <summary>
/// Get the latest docket for a register
/// </summary>
docketsGroup.MapGet("/latest", async (
    IRegisterRepository repository,
    string registerId) =>
{
    var register = await repository.GetRegisterAsync(registerId);
    if (register == null)
    {
        return Results.NotFound(new { error = "Register not found" });
    }

    if (register.Height == 0)
    {
        return Results.Ok<Docket?>(null);
    }

    var docket = await repository.GetDocketAsync(registerId, register.Height);
    return docket is not null ? Results.Ok(docket) : Results.NotFound();
})
.WithName("GetLatestDocket")
.WithSummary("Get latest docket")
.WithDescription("Retrieves the most recent docket (block) for a register.");

/// <summary>
/// Write a confirmed docket to the register (Validator Service only)
/// </summary>
docketsGroup.MapPost("/", async (
    IRegisterRepository repository,
    string registerId,
    WriteDocketRequest request) =>
{
    // Validate register exists
    var register = await repository.GetRegisterAsync(registerId);
    if (register == null)
    {
        return Results.NotFound(new { error = "Register not found" });
    }

    // Create docket from request
    var docket = new Docket
    {
        Id = (ulong)request.DocketNumber,
        RegisterId = registerId,
        PreviousHash = request.PreviousHash ?? string.Empty,
        Hash = request.DocketHash,
        TransactionIds = request.TransactionIds,
        TimeStamp = request.CreatedAt.UtcDateTime,
        State = DocketState.Sealed,
        MetaData = new TransactionMetaData
        {
            RegisterId = registerId
        },
        Votes = request.ProposerValidatorId
    };

    // Insert transaction documents if provided
    if (request.Transactions is not null && request.Transactions.Any())
    {
        foreach (var tx in request.Transactions)
        {
            // Set block number for each transaction
            tx.BlockNumber = (ulong)request.DocketNumber;
            await repository.InsertTransactionAsync(tx);
        }
    }

    // Insert docket
    var inserted = await repository.InsertDocketAsync(docket);

    // Update register height
    await repository.UpdateRegisterHeightAsync(registerId, (uint)request.DocketNumber);

    return Results.Created($"/api/registers/{registerId}/dockets/{inserted.Id}", inserted);
})
.WithName("WriteDocket")
.WithSummary("Write a confirmed docket")
.WithDescription("Writes a consensus-confirmed docket to the register. Used by Validator Service.")
.RequireAuthorization("CanWriteDockets");

app.Run();

// ===========================
// Request/Response Models
// ===========================

record CreateRegisterRequest(
    string Name,
    string TenantId,
    bool Advertise = false,
    bool IsFullReplica = true);

record UpdateRegisterRequest(
    string? Name = null,
    RegisterStatus? Status = null,
    bool? Advertise = null);

record WriteDocketRequest(
    string DocketId,
    long DocketNumber,
    string? PreviousHash,
    string DocketHash,
    DateTimeOffset CreatedAt,
    List<string> TransactionIds,
    string ProposerValidatorId,
    string MerkleRoot,
    List<TransactionModel>? Transactions = null);
