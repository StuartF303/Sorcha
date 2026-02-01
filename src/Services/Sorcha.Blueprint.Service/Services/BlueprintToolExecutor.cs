// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Service.Models.Chat;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Services;

/// <summary>
/// Executes AI tool calls against a blueprint builder using the Fluent API.
/// </summary>
public class BlueprintToolExecutor : IBlueprintToolExecutor
{
    private readonly ILogger<BlueprintToolExecutor> _logger;
    private readonly IReadOnlyList<ToolDefinition> _toolDefinitions;

    public BlueprintToolExecutor(ILogger<BlueprintToolExecutor> logger)
    {
        _logger = logger;
        _toolDefinitions = CreateToolDefinitions();
    }

    /// <inheritdoc />
    public Task<ToolResult> ExecuteAsync(
        string toolName,
        JsonDocument arguments,
        BlueprintBuilder builder,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing tool {ToolName} with arguments: {Arguments}",
            toolName, arguments.RootElement.GetRawText());

        try
        {
            var result = toolName switch
            {
                "create_blueprint" => ExecuteCreateBlueprint(arguments, builder),
                "add_participant" => ExecuteAddParticipant(arguments, builder),
                "remove_participant" => ExecuteRemoveParticipant(arguments, builder),
                "add_action" => ExecuteAddAction(arguments, builder),
                "update_action" => ExecuteUpdateAction(arguments, builder),
                "set_disclosure" => ExecuteSetDisclosure(arguments, builder),
                "add_routing" => ExecuteAddRouting(arguments, builder),
                "validate_blueprint" => ExecuteValidateBlueprint(arguments, builder),
                _ => ToolResult.Failed(Guid.NewGuid().ToString(), $"Unknown tool: {toolName}")
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return Task.FromResult(ToolResult.Failed(Guid.NewGuid().ToString(), ex.Message));
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> GetToolDefinitions() => _toolDefinitions;

    private ToolResult ExecuteCreateBlueprint(JsonDocument arguments, BlueprintBuilder builder)
    {
        var root = arguments.RootElement;
        var title = root.GetProperty("title").GetString() ?? "Untitled Blueprint";
        var description = root.GetProperty("description").GetString() ?? "No description provided";

        builder.WithTitle(title).WithDescription(description);

        var draft = builder.BuildDraft();

        return ToolResult.Succeeded(
            Guid.NewGuid().ToString(),
            new { blueprintId = draft.Id, message = "Blueprint created successfully" },
            blueprintChanged: true);
    }

    private ToolResult ExecuteAddParticipant(JsonDocument arguments, BlueprintBuilder builder)
    {
        var root = arguments.RootElement;
        var id = root.GetProperty("id").GetString()!;
        var name = root.GetProperty("name").GetString()!;
        var organisation = root.TryGetProperty("organisation", out var orgProp) ? orgProp.GetString() : null;
        var role = root.TryGetProperty("role", out var roleProp) ? roleProp.GetString() : "person";

        builder.AddParticipant(id, p =>
        {
            p.Named(name);
            if (!string.IsNullOrEmpty(organisation))
            {
                p.FromOrganisation(organisation);
            }
            if (role == "organization")
            {
                p.AsOrganization();
            }
            else
            {
                p.AsPerson();
            }
        });

        var draft = builder.BuildDraft();

        return ToolResult.Succeeded(
            Guid.NewGuid().ToString(),
            new
            {
                participantId = id,
                participantCount = draft.Participants.Count,
                message = $"Participant '{name}' added"
            },
            blueprintChanged: true);
    }

    private ToolResult ExecuteRemoveParticipant(JsonDocument arguments, BlueprintBuilder builder)
    {
        var root = arguments.RootElement;
        var id = root.GetProperty("id").GetString()!;

        // Note: BlueprintBuilder doesn't have a RemoveParticipant method
        // This would need to rebuild the blueprint without this participant
        // For MVP, return a message explaining the limitation
        return ToolResult.Failed(
            Guid.NewGuid().ToString(),
            "Removing participants is not yet supported. Please recreate the blueprint without this participant.");
    }

    private ToolResult ExecuteAddAction(JsonDocument arguments, BlueprintBuilder builder)
    {
        var root = arguments.RootElement;
        var id = root.GetProperty("id").GetInt32();
        var title = root.GetProperty("title").GetString()!;
        var sender = root.GetProperty("sender").GetString()!;
        var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
        var isStartingAction = root.TryGetProperty("isStartingAction", out var startProp) && startProp.GetBoolean();
        var routeToNext = root.TryGetProperty("routeToNext", out var routeProp) ? routeProp.GetString() : null;

        builder.AddAction(id, a =>
        {
            a.WithTitle(title);
            a.SentBy(sender);

            if (!string.IsNullOrEmpty(description))
            {
                a.WithDescription(description);
            }

            if (!string.IsNullOrEmpty(routeToNext))
            {
                a.RouteToNext(routeToNext);
            }

            // Handle data fields if provided
            if (root.TryGetProperty("dataFields", out var fieldsArray))
            {
                a.RequiresData(d =>
                {
                    foreach (var field in fieldsArray.EnumerateArray())
                    {
                        var fieldName = field.GetProperty("name").GetString()!;
                        var fieldType = field.GetProperty("type").GetString()!;
                        var fieldTitle = field.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : fieldName;
                        var fieldDescription = field.TryGetProperty("description", out var descriptionProp) ? descriptionProp.GetString() : null;
                        var isRequired = !field.TryGetProperty("required", out var reqProp) || reqProp.GetBoolean();

                        // String constraints
                        var format = field.TryGetProperty("format", out var formatProp) ? formatProp.GetString() : null;
                        var minLength = field.TryGetProperty("minLength", out var minLenProp) ? minLenProp.GetInt32() : (int?)null;
                        var maxLength = field.TryGetProperty("maxLength", out var maxLenProp) ? maxLenProp.GetInt32() : (int?)null;
                        var pattern = field.TryGetProperty("pattern", out var patternProp) ? patternProp.GetString() : null;

                        // Numeric constraints
                        var minimum = field.TryGetProperty("minimum", out var minProp) ? minProp.GetDouble() : (double?)null;
                        var maximum = field.TryGetProperty("maximum", out var maxProp) ? maxProp.GetDouble() : (double?)null;

                        // Enum values
                        var enumValues = field.TryGetProperty("enumValues", out var enumProp)
                            ? enumProp.EnumerateArray().Select(e => e.GetString()!).ToArray()
                            : null;

                        switch (fieldType.ToLowerInvariant())
                        {
                            case "string":
                                d.AddString(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (fieldDescription != null) f.WithDescription(fieldDescription);
                                    if (isRequired) f.IsRequired();
                                    if (format != null) f.WithFormat(format);
                                    if (minLength.HasValue) f.WithMinLength(minLength.Value);
                                    if (maxLength.HasValue) f.WithMaxLength(maxLength.Value);
                                    if (pattern != null) f.WithPattern(pattern);
                                    if (enumValues != null) f.WithEnum(enumValues);
                                });
                                break;
                            case "number":
                                d.AddNumber(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (fieldDescription != null) f.WithDescription(fieldDescription);
                                    if (isRequired) f.IsRequired();
                                    if (minimum.HasValue) f.WithMinimum(minimum.Value);
                                    if (maximum.HasValue) f.WithMaximum(maximum.Value);
                                });
                                break;
                            case "integer":
                                d.AddInteger(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (fieldDescription != null) f.WithDescription(fieldDescription);
                                    if (isRequired) f.IsRequired();
                                    if (minimum.HasValue) f.WithMinimum((int)minimum.Value);
                                    if (maximum.HasValue) f.WithMaximum((int)maximum.Value);
                                });
                                break;
                            case "boolean":
                                d.AddBoolean(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (fieldDescription != null) f.WithDescription(fieldDescription);
                                    if (isRequired) f.IsRequired();
                                });
                                break;
                            case "date":
                                d.AddDate(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (fieldDescription != null) f.WithDescription(fieldDescription);
                                    if (isRequired) f.IsRequired();
                                });
                                break;
                            case "file":
                                d.AddFile(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (fieldDescription != null) f.WithDescription(fieldDescription);
                                    if (isRequired) f.IsRequired();
                                });
                                break;
                            default:
                                d.AddString(fieldName, f =>
                                {
                                    if (fieldTitle != null) f.WithTitle(fieldTitle);
                                    if (isRequired) f.IsRequired();
                                });
                                break;
                        }
                    }
                });
            }
        });

        // Note: IsStartingAction needs to be set after Build - update the action directly
        var draft = builder.BuildDraft();
        var action = draft.Actions.FirstOrDefault(a => a.Id == id);
        if (action != null && isStartingAction)
        {
            action.IsStartingAction = true;
        }

        return ToolResult.Succeeded(
            Guid.NewGuid().ToString(),
            new
            {
                actionId = id,
                message = $"Action '{title}' added",
                actionCount = draft.Actions.Count
            },
            blueprintChanged: true);
    }

    private ToolResult ExecuteUpdateAction(JsonDocument arguments, BlueprintBuilder builder)
    {
        // Note: BlueprintBuilder doesn't have an UpdateAction method
        // This would need to rebuild the action with new properties
        return ToolResult.Failed(
            Guid.NewGuid().ToString(),
            "Updating actions is not yet fully supported. Please use add_action with the same ID to replace the action.");
    }

    private ToolResult ExecuteSetDisclosure(JsonDocument arguments, BlueprintBuilder builder)
    {
        var root = arguments.RootElement;
        var actionId = root.GetProperty("actionId").GetInt32();
        var participantId = root.GetProperty("participantId").GetString()!;
        var fields = root.GetProperty("fields").EnumerateArray()
            .Select(f => f.GetString()!)
            .ToList();

        // Build the draft to access actions
        var draft = builder.BuildDraft();
        var action = draft.Actions.FirstOrDefault(a => a.Id == actionId);

        if (action == null)
        {
            return ToolResult.Failed(
                Guid.NewGuid().ToString(),
                $"Action with ID {actionId} not found");
        }

        // Create or update disclosure for this participant
        var existingDisclosure = action.Disclosures
            .FirstOrDefault(d => d.ParticipantAddress == participantId);

        if (existingDisclosure != null)
        {
            // Update existing disclosure
            existingDisclosure.DataPointers = fields;
        }
        else
        {
            // Add new disclosure
            var disclosures = action.Disclosures.ToList();
            disclosures.Add(new Sorcha.Blueprint.Models.Disclosure(participantId, fields));
            action.Disclosures = disclosures;
        }

        return ToolResult.Succeeded(
            Guid.NewGuid().ToString(),
            new
            {
                message = $"Disclosure configured for participant '{participantId}' on action '{action.Title}'",
                actionId,
                participantId,
                fieldsDisclosed = fields.Count,
                fields
            },
            blueprintChanged: true);
    }

    private ToolResult ExecuteAddRouting(JsonDocument arguments, BlueprintBuilder builder)
    {
        var root = arguments.RootElement;
        var actionId = root.GetProperty("actionId").GetInt32();
        var defaultRoute = root.TryGetProperty("defaultRoute", out var defaultProp) ? defaultProp.GetString() : null;

        // Build the draft to access actions
        var draft = builder.BuildDraft();
        var action = draft.Actions.FirstOrDefault(a => a.Id == actionId);

        if (action == null)
        {
            return ToolResult.Failed(
                Guid.NewGuid().ToString(),
                $"Action with ID {actionId} not found");
        }

        var routes = new List<Sorcha.Blueprint.Models.Route>();
        var routeCount = 0;

        // Process conditional routes
        if (root.TryGetProperty("conditions", out var conditionsArray))
        {
            foreach (var condition in conditionsArray.EnumerateArray())
            {
                var field = condition.GetProperty("field").GetString()!;
                var op = condition.GetProperty("operator").GetString()!;
                var value = condition.GetProperty("value");
                var routeTo = condition.GetProperty("routeTo").GetString()!;

                // Find target participant to get their action
                var targetParticipant = draft.Participants.FirstOrDefault(p => p.Id == routeTo || p.Name == routeTo);
                var targetAction = draft.Actions.FirstOrDefault(a =>
                    a.Sender == routeTo ||
                    (targetParticipant != null && a.Sender == targetParticipant.Id));

                var nextActionId = targetAction?.Id ?? actionId + 1;

                // Convert operator to JSON Logic
                var jsonLogicCondition = ConvertToJsonLogic(field, op, value);

                routes.Add(new Sorcha.Blueprint.Models.Route
                {
                    Id = $"route_{routeCount++}",
                    NextActionIds = [nextActionId],
                    Condition = jsonLogicCondition,
                    Description = $"Route to {routeTo} when {field} {op} {value}"
                });
            }
        }

        // Add default route if specified
        if (!string.IsNullOrEmpty(defaultRoute))
        {
            var defaultParticipant = draft.Participants.FirstOrDefault(p => p.Id == defaultRoute || p.Name == defaultRoute);
            var defaultAction = draft.Actions.FirstOrDefault(a =>
                a.Sender == defaultRoute ||
                (defaultParticipant != null && a.Sender == defaultParticipant.Id));

            routes.Add(new Sorcha.Blueprint.Models.Route
            {
                Id = $"route_default",
                NextActionIds = [defaultAction?.Id ?? actionId + 1],
                IsDefault = true,
                Description = $"Default route to {defaultRoute}"
            });
        }

        action.Routes = routes;

        return ToolResult.Succeeded(
            Guid.NewGuid().ToString(),
            new
            {
                message = $"Routing configured for action '{action.Title}'",
                actionId,
                routeCount = routes.Count,
                hasDefaultRoute = !string.IsNullOrEmpty(defaultRoute)
            },
            blueprintChanged: true);
    }

    private static System.Text.Json.Nodes.JsonNode? ConvertToJsonLogic(string field, string op, JsonElement value)
    {
        var fieldRef = new { @var = field };
        var valueObj = value.ValueKind switch
        {
            JsonValueKind.String => (object)value.GetString()!,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.GetRawText()
        };

        var jsonLogic = op.ToLowerInvariant() switch
        {
            "equals" or "==" => new Dictionary<string, object> { ["=="] = new object[] { fieldRef, valueObj } },
            "notequals" or "!=" => new Dictionary<string, object> { ["!="] = new object[] { fieldRef, valueObj } },
            "greaterthan" or ">" => new Dictionary<string, object> { [">"] = new object[] { fieldRef, valueObj } },
            "lessthan" or "<" => new Dictionary<string, object> { ["<"] = new object[] { fieldRef, valueObj } },
            "greaterorequal" or ">=" => new Dictionary<string, object> { [">="] = new object[] { fieldRef, valueObj } },
            "lessorequal" or "<=" => new Dictionary<string, object> { ["<="] = new object[] { fieldRef, valueObj } },
            "contains" => new Dictionary<string, object> { ["in"] = new object[] { valueObj, fieldRef } },
            _ => new Dictionary<string, object> { ["=="] = new object[] { fieldRef, valueObj } }
        };

        return System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(jsonLogic));
    }

    private ToolResult ExecuteValidateBlueprint(JsonDocument arguments, BlueprintBuilder builder)
    {
        var errors = new List<object>();
        var warnings = new List<object>();

        try
        {
            var draft = builder.BuildDraft();

            // Check minimum participants
            if (draft.Participants.Count < 2)
            {
                errors.Add(new
                {
                    code = "MIN_PARTICIPANTS",
                    message = "Blueprint requires at least 2 participants",
                    location = "participants"
                });
            }

            // Check minimum actions
            if (draft.Actions.Count < 1)
            {
                errors.Add(new
                {
                    code = "MIN_ACTIONS",
                    message = "Blueprint requires at least 1 action",
                    location = "actions"
                });
            }

            // Check title
            if (string.IsNullOrWhiteSpace(draft.Title) || draft.Title.Length < 3)
            {
                errors.Add(new
                {
                    code = "INVALID_TITLE",
                    message = "Blueprint title must be at least 3 characters",
                    location = "title"
                });
            }

            // Check description
            if (string.IsNullOrWhiteSpace(draft.Description) || draft.Description.Length < 5)
            {
                errors.Add(new
                {
                    code = "INVALID_DESCRIPTION",
                    message = "Blueprint description must be at least 5 characters",
                    location = "description"
                });
            }

            // Check for starting action
            var hasStartingAction = draft.Actions.Any(a => a.IsStartingAction);
            if (!hasStartingAction && draft.Actions.Count > 0)
            {
                warnings.Add(new
                {
                    code = "NO_STARTING_ACTION",
                    message = "No action is marked as a starting action",
                    location = "actions"
                });
            }

            // Validate action participant references
            var participantIds = draft.Participants.Select(p => p.Id).ToHashSet();
            foreach (var action in draft.Actions)
            {
                if (action.Participants != null)
                {
                    foreach (var participant in action.Participants)
                    {
                        if (!string.IsNullOrEmpty(participant.Principal) &&
                            !participantIds.Contains(participant.Principal))
                        {
                            errors.Add(new
                            {
                                code = "INVALID_PARTICIPANT_REF",
                                message = $"Action '{action.Title}' references non-existent participant '{participant.Principal}'",
                                location = $"actions[{action.Id}]"
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(new
            {
                code = "VALIDATION_ERROR",
                message = ex.Message,
                location = "blueprint"
            });
        }

        return ToolResult.Succeeded(
            Guid.NewGuid().ToString(),
            new
            {
                isValid = errors.Count == 0,
                errors,
                warnings
            },
            blueprintChanged: false);
    }

    private static IReadOnlyList<ToolDefinition> CreateToolDefinitions()
    {
        return new List<ToolDefinition>
        {
            ToolDefinition.Create(
                "create_blueprint",
                "Creates a new blueprint with basic metadata. This must be called first before adding participants or actions.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string", description = "Blueprint title (3-200 characters)", minLength = 3, maxLength = 200 },
                        description = new { type = "string", description = "Blueprint description (5-2000 characters)", minLength = 5, maxLength = 2000 }
                    },
                    required = new[] { "title", "description" }
                }),

            ToolDefinition.Create(
                "add_participant",
                "Adds a participant (actor) to the blueprint. Every blueprint needs at least 2 participants.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Unique participant identifier (e.g., 'applicant', 'reviewer')" },
                        name = new { type = "string", description = "Display name for the participant" },
                        organisation = new { type = "string", description = "Organization the participant belongs to" },
                        role = new { type = "string", @enum = new[] { "person", "organization" }, description = "Whether this is an individual or an organization" }
                    },
                    required = new[] { "id", "name" }
                }),

            ToolDefinition.Create(
                "remove_participant",
                "Removes a participant from the blueprint. Cannot reduce below 2 participants.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Participant ID to remove" }
                    },
                    required = new[] { "id" }
                }),

            ToolDefinition.Create(
                "add_action",
                "Adds a workflow action (step) to the blueprint. Every action needs a sender participant.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "integer", description = "Action sequence number (0-based)" },
                        title = new { type = "string", description = "Action title (e.g., 'Submit Application', 'Review', 'Approve')" },
                        description = new { type = "string", description = "Optional action description" },
                        sender = new { type = "string", description = "Participant ID who performs this action" },
                        isStartingAction = new { type = "boolean", description = "Whether this action can initiate the workflow" },
                        routeToNext = new { type = "string", description = "Participant ID for simple linear routing" },
                        dataFields = new
                        {
                            type = "array",
                            description = "Data fields to collect with optional constraints",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string", description = "Field name (camelCase)" },
                                    type = new { type = "string", @enum = new[] { "string", "number", "integer", "boolean", "date", "file" }, description = "Data type" },
                                    title = new { type = "string", description = "Display label" },
                                    description = new { type = "string", description = "Field description" },
                                    required = new { type = "boolean", description = "Whether field is required (default: true)" },
                                    format = new { type = "string", @enum = new[] { "email", "uri", "date-time", "uuid" }, description = "String format validation" },
                                    minLength = new { type = "integer", description = "Minimum string length" },
                                    maxLength = new { type = "integer", description = "Maximum string length" },
                                    pattern = new { type = "string", description = "Regex pattern for string validation" },
                                    minimum = new { type = "number", description = "Minimum value for numbers" },
                                    maximum = new { type = "number", description = "Maximum value for numbers" },
                                    enumValues = new { type = "array", items = new { type = "string" }, description = "Allowed values (dropdown)" }
                                },
                                required = new[] { "name", "type" }
                            }
                        }
                    },
                    required = new[] { "id", "title", "sender" }
                }),

            ToolDefinition.Create(
                "update_action",
                "Modifies an existing action. Only provided fields are updated.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "integer", description = "Action ID to update" },
                        title = new { type = "string" },
                        description = new { type = "string" },
                        sender = new { type = "string" },
                        isStartingAction = new { type = "boolean" }
                    },
                    required = new[] { "id" }
                }),

            ToolDefinition.Create(
                "set_disclosure",
                "Configures which data fields a participant can see at a specific action.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        actionId = new { type = "integer", description = "Action ID where disclosure applies" },
                        participantId = new { type = "string", description = "Participant who receives the disclosure" },
                        fields = new
                        {
                            type = "array",
                            description = "JSON Pointer paths to disclosed fields (e.g., '/applicantName', '/*' for all)",
                            items = new { type = "string" }
                        }
                    },
                    required = new[] { "actionId", "participantId", "fields" }
                }),

            ToolDefinition.Create(
                "add_routing",
                "Adds conditional routing to an action based on data values.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        actionId = new { type = "integer", description = "Action ID to add routing to" },
                        conditions = new
                        {
                            type = "array",
                            description = "Routing conditions",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    field = new { type = "string", description = "Field to evaluate" },
                                    @operator = new { type = "string", @enum = new[] { "equals", "notEquals", "greaterThan", "lessThan", "contains" } },
                                    value = new { description = "Value to compare against" },
                                    routeTo = new { type = "string", description = "Participant ID if condition matches" }
                                },
                                required = new[] { "field", "operator", "value", "routeTo" }
                            }
                        },
                        defaultRoute = new { type = "string", description = "Participant ID for default/else case" }
                    },
                    required = new[] { "actionId", "conditions" }
                }),

            ToolDefinition.Create(
                "validate_blueprint",
                "Validates the current blueprint and returns any errors or warnings. Call this after making changes to ensure the blueprint is valid.",
                new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                })
        };
    }
}
