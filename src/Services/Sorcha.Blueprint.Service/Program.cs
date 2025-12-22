// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.OutputCaching;
using Scalar.AspNetCore;
using System.Collections.Concurrent;
using Sorcha.Blueprint.Service.Extensions;
using Sorcha.Blueprint.Service.JsonLd;
using Sorcha.Cryptography.Core;
using Sorcha.ServiceClients.Extensions;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Redis output caching
builder.AddRedisOutputCache("redis");

// Add Redis distributed cache for IDistributedCache dependency
builder.AddRedisDistributedCache("redis");

// Add OpenAPI services
builder.Services.AddOpenApi();

// Add in-memory storage (later: replace with EF Core + PostgreSQL)
builder.Services.AddSingleton<IBlueprintStore, InMemoryBlueprintStore>();
builder.Services.AddSingleton<IPublishedBlueprintStore, InMemoryPublishedBlueprintStore>();

// Add Blueprint services
builder.Services.AddScoped<IBlueprintService, BlueprintService>();
builder.Services.AddScoped<IPublishService, PublishService>();

// Add Template services
builder.Services.AddSingleton<Sorcha.Blueprint.Engine.Interfaces.IJsonEEvaluator, Sorcha.Blueprint.Engine.Implementation.JsonEEvaluator>();
builder.Services.AddSingleton<Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService, Sorcha.Blueprint.Service.Templates.BlueprintTemplateService>();

// Add Cryptography services (required for transaction building)
builder.Services.AddScoped<Sorcha.Cryptography.Interfaces.ICryptoModule, Sorcha.Cryptography.Core.CryptoModule>();
builder.Services.AddScoped<Sorcha.Cryptography.Interfaces.IHashProvider, Sorcha.Cryptography.Core.HashProvider>();

// Add Execution Engine services (Sprint 5)
builder.Services.AddScoped<Sorcha.Blueprint.Engine.Interfaces.ISchemaValidator, Sorcha.Blueprint.Engine.Implementation.SchemaValidator>();
builder.Services.AddScoped<Sorcha.Blueprint.Engine.Interfaces.IJsonLogicEvaluator, Sorcha.Blueprint.Engine.Implementation.JsonLogicEvaluator>();
builder.Services.AddScoped<Sorcha.Blueprint.Engine.Interfaces.IDisclosureProcessor, Sorcha.Blueprint.Engine.Implementation.DisclosureProcessor>();
builder.Services.AddScoped<Sorcha.Blueprint.Engine.Interfaces.IRoutingEngine, Sorcha.Blueprint.Engine.Implementation.RoutingEngine>();
builder.Services.AddScoped<Sorcha.Blueprint.Engine.Interfaces.IActionProcessor, Sorcha.Blueprint.Engine.Implementation.ActionProcessor>();
builder.Services.AddScoped<Sorcha.Blueprint.Engine.Interfaces.IExecutionEngine, Sorcha.Blueprint.Engine.Implementation.ExecutionEngine>();

// Add Action service layer (Sprint 3)
builder.Services.AddScoped<Sorcha.Blueprint.Service.Services.Interfaces.IActionResolverService, Sorcha.Blueprint.Service.Services.Implementation.ActionResolverService>();
builder.Services.AddScoped<Sorcha.Blueprint.Service.Services.Interfaces.IPayloadResolverService, Sorcha.Blueprint.Service.Services.Implementation.PayloadResolverService>();
builder.Services.AddScoped<Sorcha.Blueprint.Service.Services.Interfaces.ITransactionBuilderService, Sorcha.Blueprint.Service.Services.Implementation.TransactionBuilderService>();

// Add consolidated service clients (Sprint 6)
builder.Services.AddServiceClients(builder.Configuration);

// Add Action storage (Sprint 4)
builder.Services.AddSingleton<Sorcha.Blueprint.Service.Storage.IActionStore, Sorcha.Blueprint.Service.Storage.InMemoryActionStore>();

// Add Instance storage (Sprint 6 - Orchestration)
builder.Services.AddSingleton<Sorcha.Blueprint.Service.Storage.IInstanceStore, Sorcha.Blueprint.Service.Storage.InMemoryInstanceStore>();

// Add Orchestration services (Sprint 6)
builder.Services.AddScoped<Sorcha.Blueprint.Service.Services.Interfaces.IStateReconstructionService,
    Sorcha.Blueprint.Service.Services.Implementation.StateReconstructionService>();
builder.Services.AddScoped<Sorcha.Blueprint.Service.Services.Interfaces.IActionExecutionService,
    Sorcha.Blueprint.Service.Services.Implementation.ActionExecutionService>();

// Add SignalR (Sprint 5)
// TODO: Add Redis backplane when Microsoft.AspNetCore.SignalR.StackExchangeRedis package is added
builder.Services.AddSignalR();

// Add Notification service (Sprint 5)
builder.Services.AddScoped<Sorcha.Blueprint.Service.Services.Interfaces.INotificationService,
    Sorcha.Blueprint.Service.Services.Implementation.NotificationService>();

// Add JWT authentication and authorization (AUTH-002)
// JWT authentication is now configured via shared ServiceDefaults with auto-key generation
builder.AddJwtAuthentication();
builder.Services.AddBlueprintAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

// Add OWASP security headers (SEC-004)
app.UseApiSecurityHeaders();

// Configure OpenAPI (available in all environments for API consumers)
app.MapOpenApi();

// Configure Scalar API documentation UI (development only)
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Blueprint Service")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseOutputCache();

// Enable JSON-LD content negotiation
app.UseJsonLdContentNegotiation();

// Add authentication and authorization middleware (AUTH-002)
app.UseAuthentication();
app.UseAuthorization();

// Add Delegation Token Middleware (Sprint 6 - Orchestration)
app.UseMiddleware<Sorcha.Blueprint.Service.Middleware.DelegationTokenMiddleware>();

// Map SignalR hub (Sprint 5)
app.MapHub<Sorcha.Blueprint.Service.Hubs.ActionsHub>("/actionshub");

// ===========================
// Blueprint CRUD Endpoints
// ===========================

var blueprintGroup = app.MapGroup("/api/blueprints")
    .WithTags("Blueprints")
    .WithOpenApi()
    .RequireAuthorization("CanManageBlueprints");

/// <summary>
/// Get all blueprints with pagination
/// Supports JSON-LD via Accept: application/ld+json header
/// </summary>
blueprintGroup.MapGet("/", async (
    HttpContext context,
    IBlueprintService service,
    int page = 1,
    int pageSize = 20,
    string? search = null,
    string? status = null) =>
{
    var blueprints = await service.GetAllAsync(page, pageSize, search, status);
    return Results.Ok(blueprints);
})
.WithName("GetBlueprints")
.WithSummary("Get all blueprints")
.WithDescription("Retrieve a paginated list of blueprints with optional search and status filtering. Supports JSON-LD via Accept: application/ld+json header.")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

/// <summary>
/// Get blueprint by ID
/// Supports JSON-LD via Accept: application/ld+json header
/// </summary>
blueprintGroup.MapGet("/{id}", async (HttpContext context, string id, IBlueprintService service) =>
{
    var blueprint = await service.GetByIdAsync(id);
    if (blueprint is null) return Results.NotFound();

    // Add JSON-LD context if requested
    if (context.AcceptsJsonLd())
    {
        blueprint = JsonLdHelper.EnsureJsonLdContext(blueprint);
    }

    return Results.Ok(blueprint);
})
.WithName("GetBlueprintById")
.WithSummary("Get blueprint by ID")
.WithDescription("Retrieve a specific blueprint by its unique identifier. Supports JSON-LD via Accept: application/ld+json header.")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

/// <summary>
/// Create new blueprint
/// Supports JSON-LD via Accept: application/ld+json header
/// </summary>
blueprintGroup.MapPost("/", async (
    HttpContext context,
    BlueprintModel blueprint,
    IBlueprintService service,
    IOutputCacheStore cache) =>
{
    var created = await service.CreateAsync(blueprint);
    await cache.EvictByTagAsync("blueprints", default);

    // Add JSON-LD context if requested
    if (context.AcceptsJsonLd())
    {
        created = JsonLdHelper.EnsureJsonLdContext(created);
    }

    return Results.Created($"/api/blueprints/{created.Id}", created);
})
.WithName("CreateBlueprint")
.WithSummary("Create new blueprint")
.WithDescription("Create a new blueprint with the provided details. Supports JSON-LD via Accept: application/ld+json header.");

/// <summary>
/// Update existing blueprint
/// </summary>
blueprintGroup.MapPut("/{id}", async (string id, BlueprintModel blueprint, IBlueprintService service, IOutputCacheStore cache) =>
{
    var updated = await service.UpdateAsync(id, blueprint);
    if (updated is null) return Results.NotFound();

    await cache.EvictByTagAsync("blueprints", default);
    return Results.Ok(updated);
})
.WithName("UpdateBlueprint")
.WithSummary("Update blueprint")
.WithDescription("Update an existing blueprint with new details");

/// <summary>
/// Delete blueprint (soft delete)
/// </summary>
blueprintGroup.MapDelete("/{id}", async (string id, IBlueprintService service, IOutputCacheStore cache) =>
{
    var deleted = await service.DeleteAsync(id);
    if (!deleted) return Results.NotFound();

    await cache.EvictByTagAsync("blueprints", default);
    return Results.NoContent();
})
.WithName("DeleteBlueprint")
.WithSummary("Delete blueprint")
.WithDescription("Soft delete a blueprint (can be recovered)");

// ===========================
// Blueprint Publishing Endpoints
// ===========================

/// <summary>
/// Publish blueprint
/// </summary>
blueprintGroup.MapPost("/{id}/publish", async (string id, IPublishService service, IOutputCacheStore cache) =>
{
    var result = await service.PublishAsync(id);

    if (!result.IsSuccess)
    {
        return Results.BadRequest(new { errors = result.Errors });
    }

    await cache.EvictByTagAsync("blueprints", default);
    await cache.EvictByTagAsync("published", default);

    return Results.Ok(result.PublishedBlueprint);
})
.WithName("PublishBlueprint")
.WithSummary("Publish blueprint")
.WithDescription("Validate and publish a blueprint to make it available for use")
.RequireAuthorization("CanPublishBlueprints");

/// <summary>
/// Get all published versions of a blueprint
/// </summary>
blueprintGroup.MapGet("/{id}/versions", async (string id, IPublishedBlueprintStore store) =>
{
    var versions = await store.GetVersionsAsync(id);
    return Results.Ok(versions);
})
.WithName("GetBlueprintVersions")
.WithSummary("Get blueprint versions")
.WithDescription("Retrieve all published versions of a blueprint")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("published"));

/// <summary>
/// Get specific published version
/// </summary>
blueprintGroup.MapGet("/{id}/versions/{version}", async (string id, int version, IPublishedBlueprintStore store) =>
{
    var published = await store.GetVersionAsync(id, version);
    return published is not null ? Results.Ok(published) : Results.NotFound();
})
.WithName("GetBlueprintVersion")
.WithSummary("Get specific version")
.WithDescription("Retrieve a specific published version of a blueprint (immutable)")
.CacheOutput(policy => policy.Expire(TimeSpan.FromDays(365)).Tag("published")); // Cache permanently - immutable

// ===========================
// Schema Endpoints
// ===========================

var schemaGroup = app.MapGroup("/api/schemas")
    .WithTags("Schemas")
    .WithOpenApi()
    .RequireAuthorization();

/// <summary>
/// Get all available schemas
/// </summary>
schemaGroup.MapGet("/", async (string? category = null, string? source = null, string? search = null) =>
{
    // TODO: Implement schema repository integration
    return Results.Ok(new { message = "Schema endpoint - coming soon" });
})
.WithName("GetSchemas")
.WithSummary("Get schemas")
.WithDescription("Retrieve available data schemas with optional filtering")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(15)).Tag("schemas"));

// ===========================
// Template Endpoints
// ===========================

var templateGroup = app.MapGroup("/api/templates")
    .WithTags("Templates")
    .WithOpenApi()
    .RequireAuthorization();

/// <summary>
/// Get all published templates
/// </summary>
templateGroup.MapGet("/", async (Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service, string? category = null) =>
{
    var templates = category != null
        ? await service.GetTemplatesByCategoryAsync(category)
        : await service.GetPublishedTemplatesAsync();

    return Results.Ok(templates);
})
.WithName("GetTemplates")
.WithSummary("Get all published templates")
.WithDescription("Retrieve all published blueprint templates, optionally filtered by category")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("templates"));

/// <summary>
/// Get template by ID
/// </summary>
templateGroup.MapGet("/{id}", async (string id, Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service) =>
{
    var template = await service.GetTemplateAsync(id);
    return template is not null ? Results.Ok(template) : Results.NotFound();
})
.WithName("GetTemplateById")
.WithSummary("Get template by ID")
.WithDescription("Retrieve a specific blueprint template by its unique identifier")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("templates"));

/// <summary>
/// Create or update a template
/// </summary>
templateGroup.MapPost("/", async (
    Sorcha.Blueprint.Models.BlueprintTemplate template,
    Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service,
    IOutputCacheStore cache) =>
{
    var saved = await service.SaveTemplateAsync(template);
    await cache.EvictByTagAsync("templates", default);

    return Results.Ok(saved);
})
.WithName("SaveTemplate")
.WithSummary("Create or update template")
.WithDescription("Create a new template or update an existing one");

/// <summary>
/// Delete a template
/// </summary>
templateGroup.MapDelete("/{id}", async (
    string id,
    Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service,
    IOutputCacheStore cache) =>
{
    var deleted = await service.DeleteTemplateAsync(id);
    if (!deleted) return Results.NotFound();

    await cache.EvictByTagAsync("templates", default);
    return Results.NoContent();
})
.WithName("DeleteTemplate")
.WithSummary("Delete template")
.WithDescription("Delete a blueprint template");

/// <summary>
/// Evaluate a template with parameters to generate a blueprint
/// </summary>
templateGroup.MapPost("/evaluate", async (
    Sorcha.Blueprint.Models.TemplateEvaluationRequest request,
    Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service) =>
{
    var result = await service.EvaluateTemplateAsync(request);

    if (!result.Success)
    {
        return Results.BadRequest(result);
    }

    return Results.Ok(result);
})
.WithName("EvaluateTemplate")
.WithSummary("Evaluate template")
.WithDescription("Evaluate a blueprint template with specific parameters to generate a blueprint");

/// <summary>
/// Validate template parameters
/// </summary>
templateGroup.MapPost("/{id}/validate", async (
    string id,
    Dictionary<string, object> parameters,
    Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service) =>
{
    var result = await service.ValidateParametersAsync(id, parameters);

    return Results.Ok(new
    {
        valid = result.IsValid,
        errors = result.Errors,
        warnings = result.Warnings
    });
})
.WithName("ValidateTemplateParameters")
.WithSummary("Validate parameters")
.WithDescription("Validate parameters against a template's parameter schema");

/// <summary>
/// Evaluate a template example
/// </summary>
templateGroup.MapGet("/{id}/examples/{exampleName}", async (
    string id,
    string exampleName,
    Sorcha.Blueprint.Service.Templates.IBlueprintTemplateService service) =>
{
    var result = await service.EvaluateExampleAsync(id, exampleName);

    if (!result.Success)
    {
        return Results.BadRequest(result);
    }

    return Results.Ok(result);
})
.WithName("EvaluateTemplateExample")
.WithSummary("Evaluate template example")
.WithDescription("Evaluate a predefined example from the template");

// ===========================
// Action API Endpoints (Sprint 4)
// ===========================

var actionsGroup = app.MapGroup("/api/actions")
    .WithTags("Actions")
    .WithOpenApi()
    .RequireAuthorization("CanExecuteBlueprints");

/// <summary>
/// Get available blueprints for a wallet/register combination
/// </summary>
actionsGroup.MapGet("/{wallet}/{register}/blueprints", async (
    string wallet,
    string register,
    IPublishedBlueprintStore publishedStore,
    IBlueprintStore blueprintStore) =>
{
    // Get all published blueprints
    var blueprints = await blueprintStore.GetAllAsync();
    var availableBlueprints = new List<Sorcha.Blueprint.Service.Models.Responses.BlueprintInfo>();

    foreach (var blueprint in blueprints)
    {
        var versions = await publishedStore.GetVersionsAsync(blueprint.Id);
        var latestVersion = versions.OrderByDescending(v => v.Version).FirstOrDefault();

        if (latestVersion != null)
        {
            // For MVP, all actions are available
            // In future, apply routing rules to filter actions based on workflow state
            var availableActions = blueprint.Actions
                .Select(a => new Sorcha.Blueprint.Service.Models.Responses.ActionInfo
                {
                    ActionId = a.Id.ToString(),
                    Title = a.Title,
                    Description = a.Description,
                    IsAvailable = true, // TODO: Apply routing rules
                    DataSchema = a.DataSchemas?.FirstOrDefault()?.RootElement.GetProperty("$id").GetString()
                })
                .ToList();

            availableBlueprints.Add(new Sorcha.Blueprint.Service.Models.Responses.BlueprintInfo
            {
                BlueprintId = blueprint.Id,
                Title = blueprint.Title,
                Description = blueprint.Description,
                Version = latestVersion.Version,
                AvailableActions = availableActions
            });
        }
    }

    var response = new Sorcha.Blueprint.Service.Models.Responses.AvailableBlueprintsResponse
    {
        WalletAddress = wallet,
        RegisterAddress = register,
        Blueprints = availableBlueprints
    };

    return Results.Ok(response);
})
.WithName("GetAvailableBlueprints")
.WithSummary("Get available blueprints")
.WithDescription("Retrieve blueprints and actions available to a specific wallet/register combination")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

/// <summary>
/// Get actions for a wallet/register (paginated)
/// </summary>
actionsGroup.MapGet("/{wallet}/{register}", async (
    string wallet,
    string register,
    Sorcha.Blueprint.Service.Storage.IActionStore actionStore,
    int page = 1,
    int pageSize = 20) =>
{
    var skip = (page - 1) * pageSize;
    var actions = await actionStore.GetActionsAsync(wallet, register, skip, pageSize);
    var totalCount = await actionStore.GetActionCountAsync(wallet, register);

    var result = new PagedResult<Sorcha.Blueprint.Service.Models.Responses.ActionDetailsResponse>
    {
        Items = actions,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };

    return Results.Ok(result);
})
.WithName("GetActions")
.WithSummary("Get actions for wallet/register")
.WithDescription("Retrieve paginated list of actions for a specific wallet and register");

/// <summary>
/// Get a specific action by transaction hash
/// </summary>
actionsGroup.MapGet("/{wallet}/{register}/{tx}", async (
    string wallet,
    string register,
    string tx,
    Sorcha.Blueprint.Service.Storage.IActionStore actionStore) =>
{
    var action = await actionStore.GetActionAsync(tx);

    if (action == null)
    {
        return Results.NotFound(new { error = "Action not found" });
    }

    // Verify the action belongs to this wallet/register
    if (action.SenderWallet != wallet || action.RegisterAddress != register)
    {
        return Results.NotFound(new { error = "Action not found" });
    }

    return Results.Ok(action);
})
.WithName("GetActionDetails")
.WithSummary("Get action details")
.WithDescription("Retrieve detailed information about a specific action transaction");

/// <summary>
/// Submit a new action
/// </summary>
actionsGroup.MapPost("/", async (
    Sorcha.Blueprint.Service.Models.Requests.ActionSubmissionRequest request,
    Sorcha.Blueprint.Service.Services.Interfaces.IActionResolverService actionResolver,
    Sorcha.Blueprint.Service.Services.Interfaces.IPayloadResolverService payloadResolver,
    Sorcha.Blueprint.Service.Services.Interfaces.ITransactionBuilderService txBuilder,
    Sorcha.ServiceClients.Wallet.IWalletServiceClient walletClient,
    Sorcha.ServiceClients.Register.IRegisterServiceClient registerClient,
    Sorcha.Blueprint.Service.Storage.IActionStore actionStore,
    Sorcha.Cryptography.Interfaces.IHashProvider hashProvider) =>
{
    try
    {
        // 1. Get blueprint
        var blueprint = await actionResolver.GetBlueprintAsync(request.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // 2. Get action definition
        var actionDef = actionResolver.GetActionDefinition(blueprint, request.ActionId);
        if (actionDef == null)
        {
            return Results.BadRequest(new { error = "Action not found in blueprint" });
        }

        // 3. Determine participants who will receive payloads
        // For MVP, encrypt payload for the sender wallet
        // TODO: In full implementation, process disclosure rules to determine
        // which data each participant should receive
        var participantWalletMap = new Dictionary<string, string>
        {
            [request.SenderWallet] = request.SenderWallet
        };

        // Simple disclosure: all participants get the full payload
        // In production, use disclosure rules from actionDef
        var disclosureResults = new Dictionary<string, object>
        {
            [request.SenderWallet] = request.PayloadData
        };

        // 4. Create encrypted payloads using Wallet Service
        var encryptedPayloads = await payloadResolver.CreateEncryptedPayloadsAsync(
            disclosureResults,
            participantWalletMap,
            request.SenderWallet);

        // 5. Build transaction
        var transaction = await txBuilder.BuildActionTransactionAsync(
            request.BlueprintId,
            request.ActionId,
            request.InstanceId,
            request.PreviousTransactionHash,
            encryptedPayloads,
            request.SenderWallet,
            request.RegisterAddress);

        // 6. Calculate transaction hash
        var txHashBytes = System.Text.Encoding.UTF8.GetBytes(transaction.TxId ?? Guid.NewGuid().ToString());
        using var txHashStream = new System.IO.MemoryStream(txHashBytes);
        var txHash = await hashProvider.ComputeHashAsync(txHashStream);
        var txHashHex = BitConverter.ToString(txHash).Replace("-", "").ToLowerInvariant();

        // 7. Sign the transaction with Wallet Service
        var transactionBytes = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(transaction));
        var signature = await walletClient.SignTransactionAsync(
            request.SenderWallet,
            transactionBytes);

        // 8. Convert to Register TransactionModel and submit to Register Service
        var registerTransaction = new Sorcha.Register.Models.TransactionModel
        {
            TxId = txHashHex,
            RegisterId = request.RegisterAddress,
            SenderWallet = request.SenderWallet,
            TimeStamp = DateTime.UtcNow,
            PrevTxId = request.PreviousTransactionHash ?? string.Empty,
            MetaData = transaction.Metadata != null ?
                System.Text.Json.JsonSerializer.Deserialize<Sorcha.Register.Models.TransactionMetaData>(transaction.Metadata) : null,
            Payloads = encryptedPayloads.Select(kvp => new Sorcha.Register.Models.PayloadModel
            {
                Data = Convert.ToBase64String(kvp.Value),
                WalletAccess = new[] { kvp.Key }
            }).ToArray(),
            PayloadCount = (ulong)encryptedPayloads.Count,
            // Add signature to transaction (Base64 encoded)
            Signature = Convert.ToBase64String(signature)
        };

        // Submit to Register Service
        await registerClient.SubmitTransactionAsync(request.RegisterAddress, registerTransaction);

        // 9. Build file transactions if any
        List<string>? fileHashes = null;
        if (request.Files != null && request.Files.Any())
        {
            var fileAttachments = request.Files.Select(f => new Sorcha.Blueprint.Service.Services.Interfaces.FileAttachment(
                f.FileName,
                f.ContentType,
                Convert.FromBase64String(f.ContentBase64)
            )).ToList();

            var fileTxs = await txBuilder.BuildFileTransactionsAsync(
                fileAttachments,
                txHashHex,
                request.SenderWallet,
                request.RegisterAddress);

            fileHashes = new List<string>();
            var fileMetadataList = new List<Sorcha.Blueprint.Service.Models.Responses.FileMetadata>();

            for (int i = 0; i < fileTxs.Count; i++)
            {
                var fileTx = fileTxs[i];
                var fileHashBytes = System.Text.Encoding.UTF8.GetBytes(fileTx.TxId ?? Guid.NewGuid().ToString());
                using var fileHashStream = new System.IO.MemoryStream(fileHashBytes);
                var fileHash = await hashProvider.ComputeHashAsync(fileHashStream);
                var fileHashHex = BitConverter.ToString(fileHash).Replace("-", "").ToLowerInvariant();
                fileHashes.Add(fileHashHex);

                // Store file content and metadata
                var fileAttachment = fileAttachments[i];
                await actionStore.StoreFileContentAsync(fileHashHex, fileAttachment.Content);

                var fileMeta = new Sorcha.Blueprint.Service.Models.Responses.FileMetadata
                {
                    FileId = fileHashHex,
                    FileName = fileAttachment.FileName,
                    ContentType = fileAttachment.ContentType,
                    Size = fileAttachment.Content.Length
                };

                await actionStore.StoreFileMetadataAsync(txHashHex, fileHashHex, fileMeta);
                fileMetadataList.Add(fileMeta);
            }
        }

        // 10. Generate instance ID if needed
        var instanceId = request.InstanceId ?? Guid.NewGuid().ToString();

        // 11. Store action locally
        var actionDetails = new Sorcha.Blueprint.Service.Models.Responses.ActionDetailsResponse
        {
            TransactionHash = txHashHex,
            BlueprintId = request.BlueprintId,
            ActionId = request.ActionId,
            InstanceId = instanceId,
            SenderWallet = request.SenderWallet,
            RegisterAddress = request.RegisterAddress,
            PayloadData = request.PayloadData,
            Timestamp = DateTimeOffset.UtcNow,
            PreviousTransactionHash = request.PreviousTransactionHash
        };

        await actionStore.StoreActionAsync(actionDetails);

        // 12. Return response
        var response = new Sorcha.Blueprint.Service.Models.Responses.ActionSubmissionResponse
        {
            TransactionId = txHashHex,
            InstanceId = instanceId,
            SerializedTransaction = System.Text.Json.JsonSerializer.Serialize(transaction),
            FileTransactionHashes = fileHashes,
            Timestamp = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("SubmitAction")
.WithSummary("Submit an action")
.WithDescription("Submit a new action for execution in a blueprint workflow");

/// <summary>
/// Reject a pending action
/// </summary>
actionsGroup.MapPost("/reject", async (
    Sorcha.Blueprint.Service.Models.Requests.ActionRejectionRequest request,
    Sorcha.Blueprint.Service.Services.Interfaces.ITransactionBuilderService txBuilder,
    Sorcha.ServiceClients.Register.IRegisterServiceClient registerClient,
    Sorcha.Blueprint.Service.Storage.IActionStore actionStore,
    Sorcha.Cryptography.Interfaces.IHashProvider hashProvider) =>
{
    try
    {
        // 1. Verify original transaction exists
        var originalAction = await actionStore.GetActionAsync(request.TransactionHash);
        if (originalAction == null)
        {
            return Results.NotFound(new { error = "Original transaction not found" });
        }

        // 2. Build rejection transaction
        var rejectionTx = await txBuilder.BuildRejectionTransactionAsync(
            request.TransactionHash,
            request.Reason,
            request.SenderWallet,
            request.RegisterAddress);

        // 3. Calculate rejection transaction hash
        var rejectionHashBytes = System.Text.Encoding.UTF8.GetBytes(rejectionTx.TxId ?? Guid.NewGuid().ToString());
        using var rejectionHashStream = new System.IO.MemoryStream(rejectionHashBytes);
        var rejectionHash = await hashProvider.ComputeHashAsync(rejectionHashStream);
        var rejectionHashHex = BitConverter.ToString(rejectionHash).Replace("-", "").ToLowerInvariant();

        // 4. Convert to Register TransactionModel and submit to Register Service
        var registerRejection = new Sorcha.Register.Models.TransactionModel
        {
            TxId = rejectionHashHex,
            RegisterId = request.RegisterAddress,
            SenderWallet = request.SenderWallet,
            TimeStamp = DateTime.UtcNow,
            PrevTxId = request.TransactionHash,
            MetaData = rejectionTx.Metadata != null ?
                System.Text.Json.JsonSerializer.Deserialize<Sorcha.Register.Models.TransactionMetaData>(rejectionTx.Metadata) : null,
            Payloads = Array.Empty<Sorcha.Register.Models.PayloadModel>()
        };

        // Submit rejection to Register Service
        await registerClient.SubmitTransactionAsync(request.RegisterAddress, registerRejection);

        // 5. Store rejection action locally
        var rejectionDetails = new Sorcha.Blueprint.Service.Models.Responses.ActionDetailsResponse
        {
            TransactionHash = rejectionHashHex,
            BlueprintId = originalAction.BlueprintId,
            ActionId = "rejection",
            InstanceId = originalAction.InstanceId,
            SenderWallet = request.SenderWallet,
            RegisterAddress = request.RegisterAddress,
            PayloadData = new Dictionary<string, object>
            {
                ["rejectedTransactionHash"] = request.TransactionHash,
                ["reason"] = request.Reason
            },
            Timestamp = DateTimeOffset.UtcNow,
            PreviousTransactionHash = request.TransactionHash
        };

        await actionStore.StoreActionAsync(rejectionDetails);

        // 6. Return response
        var response = new Sorcha.Blueprint.Service.Models.Responses.ActionSubmissionResponse
        {
            TransactionId = rejectionHashHex,
            InstanceId = rejectionDetails.InstanceId,
            SerializedTransaction = System.Text.Json.JsonSerializer.Serialize(rejectionTx),
            Timestamp = DateTimeOffset.UtcNow
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("RejectAction")
.WithSummary("Reject an action")
.WithDescription("Reject a pending action with a reason");

/// <summary>
/// Get file content by file ID
/// </summary>
app.MapGet("/api/files/{wallet}/{register}/{tx}/{fileId}", async (
    string wallet,
    string register,
    string tx,
    string fileId,
    Sorcha.Blueprint.Service.Storage.IActionStore actionStore) =>
{
    // 1. Verify action exists and belongs to wallet/register
    var action = await actionStore.GetActionAsync(tx);
    if (action == null || action.SenderWallet != wallet || action.RegisterAddress != register)
    {
        return Results.NotFound(new { error = "Action not found" });
    }

    // 2. Get file metadata
    var metadata = await actionStore.GetFileMetadataAsync(tx, fileId);
    if (metadata == null)
    {
        return Results.NotFound(new { error = "File not found" });
    }

    // 3. Get file content
    var content = await actionStore.GetFileContentAsync(fileId);
    if (content == null)
    {
        return Results.NotFound(new { error = "File content not found" });
    }

    // 4. Return file
    return Results.File(content, metadata.ContentType, metadata.FileName);
})
.WithName("GetFile")
.WithSummary("Get file attachment")
.WithDescription("Retrieve a file attachment from an action transaction")
.WithTags("Actions")
.WithOpenApi();

// ===========================
// Execution Helper Endpoints (Sprint 5)
// ===========================

var executionGroup = app.MapGroup("/api/execution")
    .WithTags("Execution")
    .WithOpenApi()
    .RequireAuthorization("CanExecuteBlueprints");

/// <summary>
/// Validate action data against schema (helper endpoint)
/// </summary>
executionGroup.MapPost("/validate", async (
    ValidateRequest request,
    IBlueprintStore blueprintStore,
    Sorcha.Blueprint.Engine.Interfaces.IExecutionEngine executionEngine) =>
{
    try
    {
        // Get blueprint
        var blueprint = await blueprintStore.GetAsync(request.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // Get action (parse ActionId string to int)
        if (!int.TryParse(request.ActionId, out var actionIdInt))
        {
            return Results.BadRequest(new { error = "Invalid action ID format" });
        }

        var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionIdInt);
        if (action == null)
        {
            return Results.BadRequest(new { error = "Action not found in blueprint" });
        }

        // Validate
        var result = await executionEngine.ValidateAsync(request.Data, action);

        return Results.Ok(new
        {
            isValid = result.IsValid,
            errors = result.Errors.Select(e => new
            {
                path = e.InstanceLocation,
                message = e.Message,
                schemaLocation = e.SchemaLocation,
                keyword = e.Keyword
            })
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ValidateAction")
.WithSummary("Validate action data")
.WithDescription("Validate action data against the action's JSON Schema without executing the full workflow");

/// <summary>
/// Apply calculations to action data (helper endpoint)
/// </summary>
executionGroup.MapPost("/calculate", async (
    CalculateRequest request,
    IBlueprintStore blueprintStore,
    Sorcha.Blueprint.Engine.Interfaces.IExecutionEngine executionEngine) =>
{
    try
    {
        // Get blueprint
        var blueprint = await blueprintStore.GetAsync(request.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // Get action (parse ActionId string to int)
        if (!int.TryParse(request.ActionId, out var actionIdInt))
        {
            return Results.BadRequest(new { error = "Invalid action ID format" });
        }

        var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionIdInt);
        if (action == null)
        {
            return Results.BadRequest(new { error = "Action not found in blueprint" });
        }

        // Apply calculations
        var result = await executionEngine.ApplyCalculationsAsync(request.Data, action);

        return Results.Ok(new
        {
            processedData = result,
            calculatedFields = result.Keys.Except(request.Data.Keys).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CalculateAction")
.WithSummary("Apply calculations")
.WithDescription("Apply JSON Logic calculations to action data without executing the full workflow");

/// <summary>
/// Determine routing for action (helper endpoint)
/// </summary>
executionGroup.MapPost("/route", async (
    RouteRequest request,
    IBlueprintStore blueprintStore,
    Sorcha.Blueprint.Engine.Interfaces.IExecutionEngine executionEngine) =>
{
    try
    {
        // Get blueprint
        var blueprint = await blueprintStore.GetAsync(request.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // Get action (parse ActionId string to int)
        if (!int.TryParse(request.ActionId, out var actionIdInt))
        {
            return Results.BadRequest(new { error = "Invalid action ID format" });
        }

        var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionIdInt);
        if (action == null)
        {
            return Results.BadRequest(new { error = "Action not found in blueprint" });
        }

        // Determine routing
        var result = await executionEngine.DetermineRoutingAsync(blueprint, action, request.Data);

        return Results.Ok(new
        {
            nextActionId = result.NextActionId,
            nextParticipantId = result.NextParticipantId,
            isWorkflowComplete = result.IsWorkflowComplete,
            rejectedToParticipantId = result.RejectedToParticipantId,
            matchedCondition = result.MatchedCondition
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("DetermineRouting")
.WithSummary("Determine routing")
.WithDescription("Determine the next action and participant based on routing conditions");

/// <summary>
/// Apply disclosure rules (helper endpoint)
/// </summary>
executionGroup.MapPost("/disclose", async (
    DiscloseRequest request,
    IBlueprintStore blueprintStore,
    Sorcha.Blueprint.Engine.Interfaces.IExecutionEngine executionEngine) =>
{
    try
    {
        // Get blueprint
        var blueprint = await blueprintStore.GetAsync(request.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // Get action (parse ActionId string to int)
        if (!int.TryParse(request.ActionId, out var actionIdInt))
        {
            return Results.BadRequest(new { error = "Invalid action ID format" });
        }

        var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionIdInt);
        if (action == null)
        {
            return Results.BadRequest(new { error = "Action not found in blueprint" });
        }

        // Apply disclosures
        var result = executionEngine.ApplyDisclosures(request.Data, action);

        return Results.Ok(new
        {
            disclosures = result.Select(d => new
            {
                participantId = d.ParticipantId,
                disclosedData = d.DisclosedData,
                disclosureId = d.DisclosureId,
                fieldCount = d.DisclosedData.Count
            })
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ApplyDisclosure")
.WithSummary("Apply disclosure rules")
.WithDescription("Apply selective disclosure rules to see what data each participant will receive");

// ===========================
// Notification Endpoint (Sprint 5)
// ===========================

var notificationGroup = app.MapGroup("/api/notifications")
    .WithTags("Notifications")
    .WithOpenApi()
    .RequireAuthorization("RequireService");

/// <summary>
/// Internal endpoint for Register Service to notify of transaction confirmations
/// </summary>
notificationGroup.MapPost("/transaction-confirmed", async (
    TransactionConfirmationNotification notification,
    Sorcha.Blueprint.Service.Services.Interfaces.INotificationService notificationService) =>
{
    try
    {
        // Broadcast notification via SignalR
        var actionNotification = new Sorcha.Blueprint.Service.Hubs.ActionNotification
        {
            TransactionHash = notification.TransactionHash,
            WalletAddress = notification.WalletAddress,
            RegisterAddress = notification.RegisterAddress,
            BlueprintId = notification.BlueprintId,
            ActionId = notification.ActionId,
            InstanceId = notification.InstanceId,
            Timestamp = notification.Timestamp,
            Message = "Transaction confirmed"
        };

        await notificationService.NotifyActionConfirmedAsync(actionNotification);

        return Results.Accepted();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("NotifyTransactionConfirmed")
.WithSummary("Notify transaction confirmed")
.WithDescription("Internal endpoint for Register Service to notify of transaction confirmations (requires service authentication)");

// ===========================
// Instance-Based Orchestration Endpoints (Sprint 6)
// ===========================

var instancesGroup = app.MapGroup("/api/instances")
    .WithTags("Instances")
    .WithOpenApi()
    .RequireAuthorization("CanExecuteBlueprints");

/// <summary>
/// Create a new workflow instance
/// </summary>
instancesGroup.MapPost("/", async (
    CreateInstanceRequest request,
    Sorcha.Blueprint.Service.Storage.IInstanceStore instanceStore,
    IBlueprintStore blueprintStore) =>
{
    try
    {
        // Validate blueprint exists
        var blueprint = await blueprintStore.GetAsync(request.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // Find starting actions
        var startingActions = blueprint.Actions
            .Where(a => a.IsStartingAction)
            .Select(a => a.Id)
            .ToList();

        if (startingActions.Count == 0)
        {
            // Default to first action if none marked as starting
            startingActions = [blueprint.Actions.First().Id];
        }

        // Create instance
        var instance = new Sorcha.Blueprint.Service.Models.Instance
        {
            Id = Guid.NewGuid().ToString(),
            BlueprintId = request.BlueprintId,
            BlueprintVersion = 1, // TODO: Get actual published version
            RegisterId = request.RegisterId,
            CurrentActionIds = startingActions,
            State = Sorcha.Blueprint.Service.Models.InstanceState.Active,
            TenantId = request.TenantId ?? "default",
            Metadata = request.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
                ?? new Dictionary<string, string>()
        };

        await instanceStore.CreateAsync(instance);

        return Results.Created($"/api/instances/{instance.Id}", instance);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateInstance")
.WithSummary("Create workflow instance")
.WithDescription("Create a new workflow instance for a published blueprint");

/// <summary>
/// Get workflow instance by ID
/// </summary>
instancesGroup.MapGet("/{instanceId}", async (
    string instanceId,
    Sorcha.Blueprint.Service.Storage.IInstanceStore instanceStore) =>
{
    var instance = await instanceStore.GetAsync(instanceId);
    if (instance == null)
    {
        return Results.NotFound(new { error = "Instance not found" });
    }

    return Results.Ok(instance);
})
.WithName("GetInstance")
.WithSummary("Get workflow instance")
.WithDescription("Retrieve a workflow instance by its ID");

/// <summary>
/// Execute an action in a workflow instance (with orchestration)
/// </summary>
instancesGroup.MapPost("/{instanceId}/actions/{actionId}/execute", async (
    HttpContext context,
    string instanceId,
    int actionId,
    Sorcha.Blueprint.Service.Models.Requests.ActionSubmissionRequest request,
    Sorcha.Blueprint.Service.Services.Interfaces.IActionExecutionService actionExecutionService) =>
{
    try
    {
        // Get delegation token from context (set by middleware)
        var delegationToken = context.Items["DelegationToken"] as string;
        if (string.IsNullOrEmpty(delegationToken))
        {
            return Results.BadRequest(new { error = "X-Delegation-Token header is required for action execution" });
        }

        var response = await actionExecutionService.ExecuteAsync(
            instanceId,
            actionId,
            request,
            delegationToken);

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ExecuteAction")
.WithSummary("Execute action with orchestration")
.WithDescription("Execute an action in a workflow instance with full orchestration: state reconstruction, validation, routing, transaction building, and notification. Requires X-Delegation-Token header.");

/// <summary>
/// Reject an action in a workflow instance
/// </summary>
instancesGroup.MapPost("/{instanceId}/actions/{actionId}/reject", async (
    HttpContext context,
    string instanceId,
    int actionId,
    Sorcha.Blueprint.Service.Models.Requests.ActionRejectionRequest request,
    Sorcha.Blueprint.Service.Services.Interfaces.IActionExecutionService actionExecutionService) =>
{
    try
    {
        // Get delegation token from context (set by middleware)
        var delegationToken = context.Items["DelegationToken"] as string;
        if (string.IsNullOrEmpty(delegationToken))
        {
            return Results.BadRequest(new { error = "X-Delegation-Token header is required for action rejection" });
        }

        var response = await actionExecutionService.RejectAsync(
            instanceId,
            actionId,
            request,
            delegationToken);

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("RejectActionInInstance")
.WithSummary("Reject action in workflow")
.WithDescription("Reject an action in a workflow instance, routing to the configured rejection target. Requires X-Delegation-Token header.");

/// <summary>
/// Get accumulated state for a workflow instance
/// </summary>
instancesGroup.MapGet("/{instanceId}/state", async (
    HttpContext context,
    string instanceId,
    Sorcha.Blueprint.Service.Services.Interfaces.IStateReconstructionService stateService,
    Sorcha.Blueprint.Service.Storage.IInstanceStore instanceStore,
    IBlueprintStore blueprintStore) =>
{
    try
    {
        // Get delegation token from context (set by middleware)
        var delegationToken = context.Items["DelegationToken"] as string;
        if (string.IsNullOrEmpty(delegationToken))
        {
            return Results.BadRequest(new { error = "X-Delegation-Token header is required to view state" });
        }

        var instance = await instanceStore.GetAsync(instanceId);
        if (instance == null)
        {
            return Results.NotFound(new { error = "Instance not found" });
        }

        var blueprint = await blueprintStore.GetAsync(instance.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        // Use the first current action for state reconstruction
        var currentActionId = instance.CurrentActionIds.FirstOrDefault();
        if (currentActionId == 0)
        {
            return Results.Ok(new
            {
                instanceId,
                actionCount = 0,
                previousTransactionId = (string?)null,
                data = new Dictionary<string, object?>(),
                branchStates = new Dictionary<string, object>()
            });
        }

        var state = await stateService.ReconstructAsync(
            blueprint,
            instanceId,
            currentActionId,
            instance.RegisterId,
            delegationToken,
            instance.ParticipantWallets);

        return Results.Ok(new
        {
            instanceId,
            actionCount = state.ActionCount,
            previousTransactionId = state.PreviousTransactionId,
            data = state.GetFlattenedData(),
            branchStates = state.BranchStates
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GetInstanceState")
.WithSummary("Get accumulated state")
.WithDescription("Get the accumulated state from all prior actions in the workflow. Requires X-Delegation-Token header.");

/// <summary>
/// Get next available actions for a workflow instance
/// </summary>
instancesGroup.MapGet("/{instanceId}/next-actions", async (
    string instanceId,
    Sorcha.Blueprint.Service.Storage.IInstanceStore instanceStore,
    IBlueprintStore blueprintStore) =>
{
    try
    {
        var instance = await instanceStore.GetAsync(instanceId);
        if (instance == null)
        {
            return Results.NotFound(new { error = "Instance not found" });
        }

        var blueprint = await blueprintStore.GetAsync(instance.BlueprintId);
        if (blueprint == null)
        {
            return Results.BadRequest(new { error = "Blueprint not found" });
        }

        var nextActions = new List<object>();
        foreach (var actionId in instance.CurrentActionIds)
        {
            var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionId);
            if (action != null)
            {
                // Get participant info
                var participant = action.Participants?.FirstOrDefault();
                nextActions.Add(new
                {
                    actionId = action.Id,
                    title = action.Title,
                    description = action.Description,
                    participantId = participant?.Principal,
                    branchId = instance.ActiveBranches
                        .FirstOrDefault(b => b.CurrentActionId == actionId)?.Id
                });
            }
        }

        return Results.Ok(new
        {
            instanceId,
            state = instance.State.ToString().ToLowerInvariant(),
            isComplete = instance.State == Sorcha.Blueprint.Service.Models.InstanceState.Completed,
            nextActions
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GetNextActions")
.WithSummary("Get next available actions")
.WithDescription("Get the next available actions that can be executed in the workflow instance");

// ===========================
// Health & Status Endpoints
// ===========================

app.MapGet("/api/health", async (IBlueprintStore blueprintStore, IPublishedBlueprintStore publishedStore) =>
{
    try
    {
        var blueprints = await blueprintStore.GetAllAsync();
        var blueprintCount = blueprints.Count();

        // Count published blueprints
        var publishedCount = 0;
        foreach (var blueprint in blueprints)
        {
            var versions = await publishedStore.GetVersionsAsync(blueprint.Id);
            publishedCount += versions.Count();
        }

        return Results.Ok(new
        {
            status = "healthy",
            service = "blueprint-service",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0",
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            metrics = new
            {
                totalBlueprints = blueprintCount,
                publishedVersions = publishedCount
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            service = "blueprint-service",
            timestamp = DateTimeOffset.UtcNow,
            error = ex.Message
        }, statusCode: 503);
    }
})
.WithName("HealthCheck")
.WithSummary("Service health check with metrics")
.WithTags("Health")
.WithOpenApi();

app.Run();

// ===========================
// Service Interfaces & Implementations
// ===========================

/// <summary>
/// Blueprint storage interface
/// </summary>
public interface IBlueprintStore
{
    Task<BlueprintModel?> GetAsync(string id);
    Task<IEnumerable<BlueprintModel>> GetAllAsync();
    Task<BlueprintModel> AddAsync(BlueprintModel blueprint);
    Task<BlueprintModel?> UpdateAsync(string id, BlueprintModel blueprint);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// Published blueprint storage interface
/// </summary>
public interface IPublishedBlueprintStore
{
    Task<PublishedBlueprint> AddAsync(PublishedBlueprint published);
    Task<PublishedBlueprint?> GetVersionAsync(string blueprintId, int version);
    Task<IEnumerable<PublishedBlueprint>> GetVersionsAsync(string blueprintId);
}

/// <summary>
/// Blueprint service interface
/// </summary>
public interface IBlueprintService
{
    Task<PagedResult<BlueprintSummary>> GetAllAsync(int page, int pageSize, string? search, string? status);
    Task<BlueprintModel?> GetByIdAsync(string id);
    Task<BlueprintModel> CreateAsync(BlueprintModel blueprint);
    Task<BlueprintModel?> UpdateAsync(string id, BlueprintModel blueprint);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// Publish service interface
/// </summary>
public interface IPublishService
{
    Task<PublishResult> PublishAsync(string blueprintId);
}

/// <summary>
/// In-memory blueprint store
/// </summary>
public class InMemoryBlueprintStore : IBlueprintStore
{
    private readonly ConcurrentDictionary<string, BlueprintModel> _blueprints = new();

    public Task<BlueprintModel?> GetAsync(string id)
    {
        _blueprints.TryGetValue(id, out var blueprint);
        return Task.FromResult(blueprint);
    }

    public Task<IEnumerable<BlueprintModel>> GetAllAsync()
    {
        return Task.FromResult(_blueprints.Values.AsEnumerable());
    }

    public Task<BlueprintModel> AddAsync(BlueprintModel blueprint)
    {
        blueprint.Id = Guid.NewGuid().ToString();
        blueprint.CreatedAt = DateTimeOffset.UtcNow;
        blueprint.UpdatedAt = DateTimeOffset.UtcNow;
        _blueprints[blueprint.Id] = blueprint;
        return Task.FromResult(blueprint);
    }

    public Task<BlueprintModel?> UpdateAsync(string id, BlueprintModel blueprint)
    {
        if (!_blueprints.ContainsKey(id)) return Task.FromResult<BlueprintModel?>(null);

        blueprint.Id = id;
        blueprint.UpdatedAt = DateTimeOffset.UtcNow;
        _blueprints[id] = blueprint;
        return Task.FromResult<BlueprintModel?>(blueprint);
    }

    public Task<bool> DeleteAsync(string id)
    {
        return Task.FromResult(_blueprints.TryRemove(id, out _));
    }
}

/// <summary>
/// In-memory published blueprint store
/// </summary>
public class InMemoryPublishedBlueprintStore : IPublishedBlueprintStore
{
    private readonly ConcurrentDictionary<string, List<PublishedBlueprint>> _published = new();

    public Task<PublishedBlueprint> AddAsync(PublishedBlueprint published)
    {
        var versions = _published.GetOrAdd(published.BlueprintId, _ => []);
        published.Version = versions.Count + 1;
        published.PublishedAt = DateTimeOffset.UtcNow;
        versions.Add(published);
        return Task.FromResult(published);
    }

    public Task<PublishedBlueprint?> GetVersionAsync(string blueprintId, int version)
    {
        if (_published.TryGetValue(blueprintId, out var versions))
        {
            return Task.FromResult(versions.FirstOrDefault(v => v.Version == version));
        }
        return Task.FromResult<PublishedBlueprint?>(null);
    }

    public Task<IEnumerable<PublishedBlueprint>> GetVersionsAsync(string blueprintId)
    {
        if (_published.TryGetValue(blueprintId, out var versions))
        {
            return Task.FromResult(versions.AsEnumerable());
        }
        return Task.FromResult(Enumerable.Empty<PublishedBlueprint>());
    }
}

/// <summary>
/// Blueprint service implementation
/// </summary>
public class BlueprintService(IBlueprintStore store) : IBlueprintService
{
    private readonly IBlueprintStore _store = store;

    public async Task<PagedResult<BlueprintSummary>> GetAllAsync(int page, int pageSize, string? search, string? status)
    {
        var allBlueprints = await _store.GetAllAsync();

        // Apply filtering
        var filtered = allBlueprints.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(b =>
                b.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (b.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var total = filtered.Count();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlueprintSummary
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                ParticipantCount = b.Participants.Count,
                ActionCount = b.Actions.Count
            })
            .ToList();

        return new PagedResult<BlueprintSummary>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public Task<BlueprintModel?> GetByIdAsync(string id) => _store.GetAsync(id);

    public Task<BlueprintModel> CreateAsync(BlueprintModel blueprint) => _store.AddAsync(blueprint);

    public Task<BlueprintModel?> UpdateAsync(string id, BlueprintModel blueprint) => _store.UpdateAsync(id, blueprint);

    public Task<bool> DeleteAsync(string id) => _store.DeleteAsync(id);
}

/// <summary>
/// Publish service implementation with validation
/// </summary>
public class PublishService(IBlueprintStore blueprintStore, IPublishedBlueprintStore publishedStore) : IPublishService
{
    private readonly IBlueprintStore _blueprintStore = blueprintStore;
    private readonly IPublishedBlueprintStore _publishedStore = publishedStore;

    public async Task<PublishResult> PublishAsync(string blueprintId)
    {
        var blueprint = await _blueprintStore.GetAsync(blueprintId);
        if (blueprint is null)
        {
            return PublishResult.Failed("Blueprint not found");
        }

        // Validate blueprint
        var errors = ValidateBlueprint(blueprint);
        if (errors.Count > 0)
        {
            return PublishResult.Failed(errors.ToArray());
        }

        // Create published version (immutable snapshot)
        var published = new PublishedBlueprint
        {
            BlueprintId = blueprint.Id,
            Blueprint = blueprint,
            PublishedAt = DateTimeOffset.UtcNow
        };

        await _publishedStore.AddAsync(published);

        return PublishResult.Success(published);
    }

    private List<string> ValidateBlueprint(BlueprintModel blueprint)
    {
        var errors = new List<string>();

        // Rule 1: Must have at least 2 participants
        if (blueprint.Participants.Count < 2)
        {
            errors.Add("Blueprint must have at least 2 participants");
        }

        // Rule 2: Must have at least 1 action
        if (blueprint.Actions.Count < 1)
        {
            errors.Add("Blueprint must have at least 1 action");
        }

        // Rule 3: All participant references in actions must exist
        var participantIds = blueprint.Participants.Select(p => p.Id).ToHashSet();
        foreach (var action in blueprint.Actions)
        {
            if (action.Participants != null)
            {
                foreach (var participant in action.Participants)
                {
                    // Validate participant principal references
                    if (!string.IsNullOrWhiteSpace(participant.Principal))
                    {
                        // TODO: More sophisticated validation of participant references
                    }
                }
            }
        }

        // Rule 4: No circular action dependencies
        // TODO: Implement graph cycle detection

        return errors;
    }
}

// ===========================
// DTOs & Models
// ===========================

/// <summary>
/// Blueprint summary for list views
/// </summary>
public record BlueprintSummary
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int ParticipantCount { get; init; }
    public int ActionCount { get; init; }
}

/// <summary>
/// Paged result wrapper
/// </summary>
public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Published blueprint with version
/// </summary>
public record PublishedBlueprint
{
    public string BlueprintId { get; init; } = string.Empty;
    public int Version { get; set; }
    public BlueprintModel Blueprint { get; init; } = null!;
    public DateTimeOffset PublishedAt { get; set; }
}

/// <summary>
/// Publish result
/// </summary>
public record PublishResult
{
    public bool IsSuccess { get; init; }
    public PublishedBlueprint? PublishedBlueprint { get; init; }
    public string[] Errors { get; init; } = [];

    public static PublishResult Success(PublishedBlueprint published) => new()
    {
        IsSuccess = true,
        PublishedBlueprint = published
    };

    public static PublishResult Failed(params string[] errors) => new()
    {
        IsSuccess = false,
        Errors = errors
    };
}

// ===========================
// Execution Endpoint Request DTOs (Sprint 5)
// ===========================

/// <summary>
/// Request for validating action data
/// </summary>
public record ValidateRequest
{
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

/// <summary>
/// Request for applying calculations
/// </summary>
public record CalculateRequest
{
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

/// <summary>
/// Request for determining routing
/// </summary>
public record RouteRequest
{
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

/// <summary>
/// Request for applying disclosure rules
/// </summary>
public record DiscloseRequest
{
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

// ===========================
// Notification DTOs (Sprint 5)
// ===========================

/// <summary>
/// Notification sent by Register Service when a transaction is confirmed
/// </summary>
public record TransactionConfirmationNotification
{
    public required string TransactionHash { get; init; }
    public required string WalletAddress { get; init; }
    public required string RegisterAddress { get; init; }
    public string? BlueprintId { get; init; }
    public string? ActionId { get; init; }
    public string? InstanceId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

// ===========================
// Instance DTOs (Sprint 6)
// ===========================

/// <summary>
/// Request to create a new workflow instance
/// </summary>
public record CreateInstanceRequest
{
    /// <summary>
    /// The ID of the blueprint to instantiate
    /// </summary>
    public required string BlueprintId { get; init; }

    /// <summary>
    /// The register ID where transactions will be stored
    /// </summary>
    public required string RegisterId { get; init; }

    /// <summary>
    /// Optional tenant ID for isolation (defaults to "default")
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Optional metadata to associate with the instance
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
