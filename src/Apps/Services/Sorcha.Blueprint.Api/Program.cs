// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.OutputCaching;
using Scalar.AspNetCore;
using System.Collections.Concurrent;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Redis output caching
builder.AddRedisOutputCache("redis");

// Add OpenAPI services
builder.Services.AddOpenApi();

// Add in-memory storage (later: replace with EF Core + PostgreSQL)
builder.Services.AddSingleton<IBlueprintStore, InMemoryBlueprintStore>();
builder.Services.AddSingleton<IPublishedBlueprintStore, InMemoryPublishedBlueprintStore>();

// Add Blueprint services
builder.Services.AddScoped<IBlueprintService, BlueprintService>();
builder.Services.AddScoped<IPublishService, PublishService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

// Configure Scalar API documentation (better than Swagger)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Blueprint API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseOutputCache();

// ===========================
// Blueprint CRUD Endpoints
// ===========================

var blueprintGroup = app.MapGroup("/api/blueprints")
    .WithTags("Blueprints")
    .WithOpenApi();

/// <summary>
/// Get all blueprints with pagination
/// </summary>
blueprintGroup.MapGet("/", async (
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
.WithDescription("Retrieve a paginated list of blueprints with optional search and status filtering")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

/// <summary>
/// Get blueprint by ID
/// </summary>
blueprintGroup.MapGet("/{id}", async (string id, IBlueprintService service) =>
{
    var blueprint = await service.GetByIdAsync(id);
    return blueprint is not null ? Results.Ok(blueprint) : Results.NotFound();
})
.WithName("GetBlueprintById")
.WithSummary("Get blueprint by ID")
.WithDescription("Retrieve a specific blueprint by its unique identifier")
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

/// <summary>
/// Create new blueprint
/// </summary>
blueprintGroup.MapPost("/", async (BlueprintModel blueprint, IBlueprintService service, IOutputCacheStore cache) =>
{
    var created = await service.CreateAsync(blueprint);
    await cache.EvictByTagAsync("blueprints", default);
    return Results.Created($"/api/blueprints/{created.Id}", created);
})
.WithName("CreateBlueprint")
.WithSummary("Create new blueprint")
.WithDescription("Create a new blueprint with the provided details");

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
.WithDescription("Validate and publish a blueprint to make it available for use");

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
    .WithOpenApi();

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
            service = "blueprint-api",
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
            service = "blueprint-api",
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
