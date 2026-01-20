# Data Model: Blueprint Designer Completion

**Feature**: 001-designer-completion
**Date**: 2026-01-20

## Existing Models (from Sorcha.Blueprint.Models)

### Participant (existing)
```csharp
public class Participant
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }  // Wallet address
    public string? Role { get; set; }
    public JsonDocument? Metadata { get; set; }
}
```

### Blueprint (existing - key fields)
```csharp
public class Blueprint
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public List<Participant> Participants { get; set; }
    public List<Action> Actions { get; set; }
}
```

## New Models (Sorcha.Admin/Models/)

### ParticipantModel
UI-friendly model for participant editing.

```csharp
public class ParticipantModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;
    public string? WalletAddress { get; set; }
    public ParticipantRole Role { get; set; } = ParticipantRole.Member;
    public string? Description { get; set; }
    public bool IsNew { get; set; } = true;

    // Validation
    public bool IsValid => !string.IsNullOrWhiteSpace(DisplayName);

    // Convert to domain model
    public Participant ToParticipant() => new()
    {
        Id = Id,
        Name = DisplayName,
        Address = WalletAddress,
        Role = Role.ToString()
    };

    // Create from domain model
    public static ParticipantModel FromParticipant(Participant p) => new()
    {
        Id = p.Id,
        DisplayName = p.Name ?? string.Empty,
        WalletAddress = p.Address,
        Role = Enum.TryParse<ParticipantRole>(p.Role, out var r) ? r : ParticipantRole.Member,
        IsNew = false
    };
}

public enum ParticipantRole
{
    Initiator,
    Approver,
    Observer,
    Member,
    Administrator
}
```

### ConditionModel
UI model for building routing conditions visually.

```csharp
public class ConditionModel
{
    public List<ConditionClause> Clauses { get; set; } = new();
    public LogicalOperator Operator { get; set; } = LogicalOperator.And;
    public string? TargetParticipantId { get; set; }

    // Convert to JSON Logic
    public JsonNode ToJsonLogic()
    {
        if (Clauses.Count == 0) return null;
        if (Clauses.Count == 1) return Clauses[0].ToJsonLogic();

        var op = Operator == LogicalOperator.And ? "and" : "or";
        var clauseNodes = Clauses.Select(c => c.ToJsonLogic()).ToArray();
        return JsonNode.Parse($"{{\"{op}\":{JsonSerializer.Serialize(clauseNodes)}}}");
    }

    // Parse from JSON Logic
    public static ConditionModel FromJsonLogic(JsonNode? node);
}

public class ConditionClause
{
    public string FieldPath { get; set; } = string.Empty;  // JSON Pointer
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equals;
    public string Value { get; set; } = string.Empty;
    public FieldType ValueType { get; set; } = FieldType.String;

    public JsonNode ToJsonLogic()
    {
        var op = Operator switch
        {
            ComparisonOperator.Equals => "==",
            ComparisonOperator.NotEquals => "!=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.GreaterOrEqual => ">=",
            ComparisonOperator.LessOrEqual => "<=",
            ComparisonOperator.Contains => "in",
            _ => "=="
        };

        var fieldRef = new { var = FieldPath };
        var value = ValueType == FieldType.Number ? decimal.Parse(Value) : (object)Value;

        return JsonNode.Parse($"{{\"{op}\":[{JsonSerializer.Serialize(fieldRef)},{JsonSerializer.Serialize(value)}]}}");
    }
}

public enum LogicalOperator { And, Or }

public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    Contains,
    StartsWith,
    EndsWith
}

public enum FieldType { String, Number, Boolean, Date }
```

### CalculationModel
UI model for calculated field expressions.

```csharp
public class CalculationModel
{
    public string TargetFieldPath { get; set; } = string.Empty;
    public List<CalculationElement> Elements { get; set; } = new();

    public JsonNode ToJsonLogic();
    public static CalculationModel FromJsonLogic(JsonNode? node);

    // Test the calculation with sample values
    public decimal? Evaluate(Dictionary<string, object> testValues);
}

public class CalculationElement
{
    public CalculationElementType Type { get; set; }
    public string? FieldPath { get; set; }      // For field references
    public string? ConstantValue { get; set; }  // For constants
    public ArithmeticOperator? Operator { get; set; }
}

public enum CalculationElementType { Field, Constant, Operator, OpenParen, CloseParen }
public enum ArithmeticOperator { Add, Subtract, Multiply, Divide }
```

### SyncQueueItem
Offline sync queue entry.

```csharp
public class SyncQueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SyncOperation Operation { get; set; }
    public string BlueprintId { get; set; } = string.Empty;
    public string? BlueprintJson { get; set; }  // For create/update
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
}

public enum SyncOperation { Create, Update, Delete }
```

### BlueprintExportModel
Model for exported blueprint file.

```csharp
public class BlueprintExportModel
{
    public string FormatVersion { get; set; } = "1.0";
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ExportedBy { get; set; }
    public Blueprint Blueprint { get; set; } = null!;
}
```

### ImportValidationResult
Result of validating an imported blueprint.

```csharp
public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public Blueprint? Blueprint { get; set; }
    public List<ImportValidationError> Errors { get; set; } = new();
    public List<ImportValidationWarning> Warnings { get; set; } = new();
}

public class ImportValidationError
{
    public string Path { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ImportErrorType Type { get; set; }
}

public enum ImportErrorType
{
    MissingRequiredField,
    InvalidFormat,
    InvalidReference,
    CircularDependency,
    SchemaNotFound
}

public class ImportValidationWarning
{
    public string Path { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

## Entity Relationships

```
Blueprint (1) ──── contains ────▶ (*) Participant
     │
     └──── contains ────▶ (*) Action
                               │
                               ├──── has ────▶ (0..1) ConditionModel (routing)
                               │
                               └──── has ────▶ (*) CalculationModel (calculated fields)

SyncQueueItem (*) ──── references ────▶ (1) Blueprint (by ID)
```

## State Management

### Designer State
```
New Blueprint ──▶ Editing ──▶ Saved (LocalStorage)
                     │              │
                     │              ▼
                     └────────▶ Saved (Server)
                                    │
                                    ▼
                              Synced (both)
```

### Sync Queue State
```
Pending ──▶ Processing ──▶ Completed
    │            │
    │            ▼
    └──────▶ Failed (retry later)
                 │
                 ▼ (after max retries)
            Abandoned (user notified)
```
