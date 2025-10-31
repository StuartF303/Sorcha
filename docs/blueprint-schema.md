# Blueprint Schema

This document defines the schema for Sorcha blueprints. Blueprints are JSON documents that describe workflows and data flow pipelines.

## Overview

A blueprint is a declarative definition of a workflow consisting of:
- Metadata (name, version, description)
- Input parameters
- Actions to execute
- Output values
- Error handling rules

## Blueprint Structure

```json
{
  "$schema": "https://sorcha.dev/schemas/blueprint/v1.json",
  "name": "string",
  "version": "string",
  "description": "string",
  "metadata": {},
  "inputs": [],
  "actions": [],
  "outputs": [],
  "errorHandling": {}
}
```

## Schema Definition

### Root Object

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `$schema` | string | No | URI of the JSON schema for validation |
| `name` | string | Yes | Blueprint name |
| `version` | string | Yes | Semantic version (e.g., "1.0.0") |
| `description` | string | No | Human-readable description |
| `metadata` | object | No | Additional metadata (tags, author, etc.) |
| `inputs` | array | No | Input parameter definitions |
| `actions` | array | Yes | Actions to execute |
| `outputs` | array | No | Output value definitions |
| `errorHandling` | object | No | Error handling configuration |

### Input Parameter

```json
{
  "name": "string",
  "type": "string|number|boolean|object|array",
  "required": boolean,
  "default": any,
  "description": "string",
  "validation": {}
}
```

### Action

```json
{
  "id": "string",
  "type": "string",
  "name": "string",
  "description": "string",
  "config": {},
  "inputs": {},
  "outputs": {},
  "retry": {},
  "condition": "string"
}
```

#### Action Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string | Yes | Unique action identifier |
| `type` | string | Yes | Action type (e.g., "http", "transform", "log") |
| `name` | string | No | Display name |
| `description` | string | No | Action description |
| `config` | object | Yes | Action-specific configuration |
| `inputs` | object | No | Input mappings |
| `outputs` | object | No | Output mappings |
| `retry` | object | No | Retry configuration |
| `condition` | string | No | Conditional execution expression |

### Output Definition

```json
{
  "name": "string",
  "type": "string",
  "source": "string",
  "description": "string"
}
```

## Action Types

### Built-in Actions

#### 1. HTTP Request

Make HTTP requests to external APIs.

```json
{
  "id": "fetch-data",
  "type": "http",
  "config": {
    "url": "https://api.example.com/data",
    "method": "GET",
    "headers": {
      "Authorization": "Bearer ${inputs.token}"
    },
    "timeout": 30
  },
  "outputs": {
    "data": "$.response.body"
  }
}
```

#### 2. Transform

Transform data using JSONPath or custom logic.

```json
{
  "id": "transform-data",
  "type": "transform",
  "config": {
    "expression": "$.items[*].name",
    "input": "${actions.fetch-data.outputs.data}"
  },
  "outputs": {
    "names": "$"
  }
}
```

#### 3. Log

Write log messages.

```json
{
  "id": "log-result",
  "type": "log",
  "config": {
    "level": "Information",
    "message": "Processing complete: ${actions.transform-data.outputs.names}"
  }
}
```

#### 4. Condition

Conditional branching.

```json
{
  "id": "check-result",
  "type": "condition",
  "config": {
    "condition": "${actions.fetch-data.outputs.data.count} > 0",
    "then": ["process-data"],
    "else": ["log-empty"]
  }
}
```

#### 5. Loop

Iterate over collections.

```json
{
  "id": "process-items",
  "type": "loop",
  "config": {
    "items": "${actions.fetch-data.outputs.data.items}",
    "actions": [
      {
        "id": "process-item",
        "type": "transform",
        "config": {
          "expression": "$.name",
          "input": "${loop.current}"
        }
      }
    ]
  }
}
```

## Expression Language

Sorcha uses a simple expression language for dynamic values:

### Variable References

```
${inputs.paramName}          - Input parameter
${actions.actionId.outputs.outputName}  - Action output
${env.VARIABLE_NAME}         - Environment variable
${loop.current}              - Current loop item
${loop.index}                - Current loop index
```

### Functions

```
${concat(value1, value2)}    - String concatenation
${upper(string)}             - Uppercase
${lower(string)}             - Lowercase
${json(string)}              - Parse JSON
${now()}                     - Current timestamp
${uuid()}                    - Generate UUID
```

### Operators

```
==  Equal
!=  Not equal
>   Greater than
<   Less than
>=  Greater or equal
<=  Less or equal
&&  Logical AND
||  Logical OR
!   Logical NOT
```

## Error Handling

```json
{
  "errorHandling": {
    "strategy": "continue|stop|retry",
    "retry": {
      "maxAttempts": 3,
      "delaySeconds": 5,
      "backoffMultiplier": 2
    },
    "onError": [
      {
        "id": "log-error",
        "type": "log",
        "config": {
          "level": "Error",
          "message": "Blueprint failed: ${error.message}"
        }
      }
    ]
  }
}
```

## Complete Example

```json
{
  "$schema": "https://sorcha.dev/schemas/blueprint/v1.json",
  "name": "Data Processing Pipeline",
  "version": "1.0.0",
  "description": "Fetches data from API, transforms it, and stores results",
  "metadata": {
    "author": "John Doe",
    "tags": ["data", "processing", "api"],
    "created": "2025-01-01T00:00:00Z"
  },
  "inputs": [
    {
      "name": "apiKey",
      "type": "string",
      "required": true,
      "description": "API authentication key"
    },
    {
      "name": "limit",
      "type": "number",
      "required": false,
      "default": 100,
      "description": "Maximum number of items to process"
    }
  ],
  "actions": [
    {
      "id": "fetch-data",
      "type": "http",
      "name": "Fetch Data from API",
      "config": {
        "url": "https://api.example.com/data",
        "method": "GET",
        "headers": {
          "Authorization": "Bearer ${inputs.apiKey}",
          "Accept": "application/json"
        },
        "queryParams": {
          "limit": "${inputs.limit}"
        },
        "timeout": 30
      },
      "retry": {
        "maxAttempts": 3,
        "delaySeconds": 2
      },
      "outputs": {
        "items": "$.data.items",
        "count": "$.data.count"
      }
    },
    {
      "id": "validate-data",
      "type": "condition",
      "name": "Check if data exists",
      "config": {
        "condition": "${actions.fetch-data.outputs.count} > 0"
      }
    },
    {
      "id": "process-items",
      "type": "loop",
      "name": "Process each item",
      "condition": "${actions.validate-data.result} == true",
      "config": {
        "items": "${actions.fetch-data.outputs.items}",
        "actions": [
          {
            "id": "transform-item",
            "type": "transform",
            "config": {
              "input": "${loop.current}",
              "expression": {
                "id": "$.id",
                "name": "${upper($.name)}",
                "processedAt": "${now()}"
              }
            }
          }
        ]
      }
    },
    {
      "id": "log-completion",
      "type": "log",
      "name": "Log completion",
      "config": {
        "level": "Information",
        "message": "Processed ${actions.fetch-data.outputs.count} items"
      }
    }
  ],
  "outputs": [
    {
      "name": "processedCount",
      "type": "number",
      "source": "${actions.fetch-data.outputs.count}",
      "description": "Number of items processed"
    }
  ],
  "errorHandling": {
    "strategy": "stop",
    "onError": [
      {
        "id": "log-error",
        "type": "log",
        "config": {
          "level": "Error",
          "message": "Blueprint execution failed: ${error.message}"
        }
      }
    ]
  }
}
```

## Validation

Blueprints are validated against the JSON schema before execution. Common validation errors:

- Missing required fields
- Invalid action types
- Circular dependencies
- Invalid expressions
- Type mismatches

## Best Practices

1. **Use meaningful IDs**: Action IDs should be descriptive
2. **Add descriptions**: Document your blueprints
3. **Version properly**: Use semantic versioning
4. **Handle errors**: Always include error handling
5. **Test expressions**: Validate expressions before deployment
6. **Minimize dependencies**: Keep actions loosely coupled
7. **Use variables**: Don't hardcode values

## Schema Versioning

The blueprint schema follows semantic versioning:
- Major version: Breaking changes
- Minor version: New features
- Patch version: Bug fixes

## Related Documentation

- [Getting Started](getting-started.md)
- [API Reference](api-reference.md)
- [Architecture](architecture.md)
