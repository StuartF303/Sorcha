# Blueprint Templates

This directory contains reusable blueprint templates using JSON-e templating.

## Overview

Blueprint templates enable you to create parameterized, reusable workflow definitions. Instead of manually creating similar blueprints, you can define a template once and generate multiple blueprint instances with different parameters.

## Available Templates

### 1. Loan Application Template (`loan-application-template.json`)

A flexible template for loan application workflows with configurable:
- Approval thresholds
- Loan amount limits
- Senior approval requirements
- Loan terms

**Examples:**
- `standard-loan` - Two-tier approval process
- `simple-loan` - Single-tier approval
- `high-value-loan` - Stricter requirements for large loans

## Using Templates

### Via API

1. **Upload a template:**
```bash
POST /api/templates
Content-Type: application/json

{
  "id": "my-template",
  "title": "My Template",
  ...
}
```

2. **Evaluate a template:**
```bash
POST /api/templates/evaluate
Content-Type: application/json

{
  "templateId": "loan-application-001",
  "parameters": {
    "blueprintId": "my-loan-app",
    "blueprintTitle": "My Custom Loan Application",
    "bankName": "My Bank",
    "requiresSeniorApproval": true,
    "minLoanAmount": 1000,
    "maxLoanAmount": 100000,
    "seniorApprovalThreshold": 25000,
    "loanTermMonths": 48
  },
  "validate": true
}
```

3. **Test a template example:**
```bash
GET /api/templates/loan-application-001/examples/standard-loan
```

### Via C# Code

```csharp
using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Service.Templates;

// Inject the service
public class MyService
{
    private readonly IBlueprintTemplateService _templateService;

    public MyService(IBlueprintTemplateService templateService)
    {
        _templateService = templateService;
    }

    public async Task<Blueprint> GenerateLoanBlueprint()
    {
        var request = new TemplateEvaluationRequest
        {
            TemplateId = "loan-application-001",
            Parameters = new Dictionary<string, object>
            {
                ["blueprintId"] = "custom-loan-001",
                ["blueprintTitle"] = "Custom Loan Workflow",
                ["bankName"] = "My Bank",
                ["requiresSeniorApproval"] = true,
                ["minLoanAmount"] = 1000,
                ["maxLoanAmount"] = 100000,
                ["seniorApprovalThreshold"] = 25000,
                ["loanTermMonths"] = 48
            },
            Validate = true
        };

        var result = await _templateService.EvaluateTemplateAsync(request);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Template evaluation failed: {result.Error}");
        }

        return result.Blueprint!;
    }
}
```

## Template Structure

A blueprint template consists of:

### Required Fields

- **`id`** - Unique identifier
- **`title`** - Template name
- **`description`** - Purpose and use cases
- **`template`** - JSON-e template definition

### Optional Fields

- **`parameterSchema`** - JSON Schema for validating parameters
- **`defaultParameters`** - Default values for parameters
- **`examples`** - Predefined parameter sets
- **`category`** - Template category (e.g., "finance", "supply-chain")
- **`tags`** - Search tags
- **`author`** - Template creator
- **`published`** - Whether template is available for use

## JSON-e Syntax

Templates use JSON-e for dynamic content generation. Key features:

### Variable Substitution

```json
{
  "$eval": "variableName"
}
```

### Conditional Inclusion

```json
{
  "$if": "condition",
  "then": { "value": "if true" },
  "else": { "value": "if false" }
}
```

### Iteration

```json
{
  "$map": { "$eval": "items" },
  "each(item)": {
    "name": { "$eval": "item.name" }
  }
}
```

### Nested Context

```json
{
  "$eval": "template",
  "context": {
    "template": { "id": { "$eval": "myId" } },
    "myId": "value-123"
  }
}
```

## Creating Your Own Templates

1. **Start with a working blueprint** - Create a blueprint manually first
2. **Identify variable parts** - Find values that should be parameterized
3. **Replace with JSON-e** - Use `$eval` for variables, `$if` for conditionals
4. **Define parameter schema** - Create JSON Schema for validation
5. **Add examples** - Provide at least 2-3 example parameter sets
6. **Test thoroughly** - Evaluate examples and verify output

## Best Practices

1. **Use meaningful parameter names** - `minLoanAmount` not `min`
2. **Provide defaults** - Make templates work out-of-the-box
3. **Validate parameters** - Always include parameter schema
4. **Document examples** - Show common use cases
5. **Keep it simple** - Complex templates are hard to maintain
6. **Version your templates** - Increment version for breaking changes

## Troubleshooting

### Template evaluation fails

- Check parameter schema validation
- Verify all required parameters are provided
- Review JSON-e syntax (commas, brackets, quotes)

### Generated blueprint is invalid

- Enable validation in evaluation request
- Check participant references
- Verify action routing logic

### Parameters not being applied

- Ensure parameter names match `$eval` expressions
- Check context nesting in template
- Verify default parameters are not overriding

## See Also

- [JSON-e Documentation](../../docs/json-e-templates.md)
- [JSON Logic Guide](../../docs/json-logic-guide.md)
- [Blueprint Architecture](../../docs/blueprint-architecture.md)
