# Quickstart: Action Form Renderer

**Feature**: 032-action-form-renderer
**Date**: 2026-02-12

## Overview

The Action Form Renderer replaces the basic `ActionForm.razor` stub with a full recursive form renderer that interprets blueprint `Control` trees, validates against JSON Schema, filters by disclosure rules, supports credentials, and signs submissions with the participant's wallet.

## Component Usage

### Basic: Render a form for a pending action

```razor
<SorchaFormRenderer
    Action="@_action"
    ParticipantAddress="@_participantAddress"
    SigningWalletAddress="@_walletAddress"
    OnSubmit="@HandleSubmit"
    OnReject="@HandleReject"
    OnCancel="@HandleCancel" />

@code {
    private Action _action;          // From blueprint
    private string _participantAddress = "did:sorcha:w:ws1abc...";
    private string _walletAddress = "ws1abc...";

    private async Task HandleSubmit(FormSubmission submission)
    {
        // submission.Data — user-entered values
        // submission.Signature — wallet signature
        // submission.CredentialPresentations — any presented credentials
        await WorkflowService.SubmitActionAsync(new ActionSubmissionViewModel
        {
            ActionId = _action.Id.ToString(),
            InstanceId = _instanceId,
            Data = submission.Data
        });
    }
}
```

### With previous data (approval step)

```razor
<SorchaFormRenderer
    Action="@_approvalAction"
    PreviousData="@_invoiceData"
    ParticipantAddress="@_approverAddress"
    SigningWalletAddress="@_walletAddress"
    OnSubmit="@HandleApproval" />
```

### Read-only view (audit/review)

```razor
<SorchaFormRenderer
    Action="@_action"
    PreviousData="@_submittedData"
    ParticipantAddress="@_viewerAddress"
    SigningWalletAddress=""
    IsReadOnly="true" />
```

## Blueprint Form Definition

Forms are defined in blueprint JSON on each action's `form` property:

```json
{
  "id": 0,
  "title": "Submit Invoice",
  "form": {
    "type": "Layout",
    "layout": "VerticalLayout",
    "title": "Invoice Details",
    "elements": [
      {
        "type": "Layout",
        "layout": "HorizontalLayout",
        "elements": [
          { "type": "TextLine", "scope": "/invoiceNumber", "title": "Invoice Number" },
          { "type": "DateTime", "scope": "/invoiceDate", "title": "Invoice Date" }
        ]
      },
      { "type": "TextArea", "scope": "/description", "title": "Description" },
      { "type": "Numeric", "scope": "/amount", "title": "Amount" },
      { "type": "Selection", "scope": "/currency", "title": "Currency" }
    ]
  },
  "dataSchemas": [{
    "type": "object",
    "properties": {
      "invoiceNumber": { "type": "string", "minLength": 3 },
      "invoiceDate": { "type": "string", "format": "date" },
      "description": { "type": "string" },
      "amount": { "type": "number", "minimum": 0.01 },
      "currency": { "type": "string", "enum": ["USD", "EUR", "GBP"] }
    },
    "required": ["invoiceNumber", "invoiceDate", "amount", "currency"]
  }]
}
```

## Conditional Display (JSON Forms Rules)

Controls can show/hide or enable/disable based on form data:

```json
{
  "type": "TextArea",
  "scope": "/rejectionReason",
  "title": "Reason for Rejection",
  "rule": {
    "effect": "SHOW",
    "condition": {
      "scope": "/approved",
      "schema": { "const": false }
    }
  }
}
```

When `approved` is `false`, the rejection reason field appears. When `true`, it's hidden.

## Disclosure Filtering

The renderer checks each control's `scope` against the participant's disclosure permissions:

```json
"disclosures": [
  { "participantAddress": "vendor", "dataPointers": ["/approved", "/paymentDate", "/notes"] },
  { "participantAddress": "auditor", "dataPointers": ["/approved", "/amount"] }
]
```

- **Vendor** sees: approved, paymentDate, notes
- **Auditor** sees: approved, amount
- Fields not in the participant's disclosure list are never rendered

## Auto-Generation Fallback

When an action has `dataSchemas` but no `form`, the renderer auto-generates:

1. Creates a VerticalLayout root control
2. For each property in the schema, infers control type:
   - `"type": "string"` → TextLine (or TextArea if maxLength > 500)
   - `"type": "number"` / `"integer"` → Numeric
   - `"type": "boolean"` → Checkbox
   - `"format": "date"` / `"date-time"` → DateTime
   - `"enum": [...]` → Selection
3. Sets scope to `/{propertyName}`
4. Sets title from schema `title` or humanized property name

## File Structure

```
src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Components/
│   └── Forms/                          # NEW — form renderer components
│       ├── SorchaFormRenderer.razor     # Root component
│       ├── ControlDispatcher.razor      # Recursive type dispatcher
│       ├── Controls/                    # Input control renderers
│       │   ├── TextLineRenderer.razor
│       │   ├── TextAreaRenderer.razor
│       │   ├── NumericRenderer.razor
│       │   ├── DateTimeRenderer.razor
│       │   ├── CheckboxRenderer.razor
│       │   ├── SelectionRenderer.razor
│       │   ├── ChoiceRenderer.razor
│       │   ├── FileRenderer.razor
│       │   ├── LabelRenderer.razor
│       │   └── UnsupportedControlRenderer.razor
│       ├── Layouts/                     # Layout renderers
│       │   ├── VerticalLayoutRenderer.razor
│       │   ├── HorizontalLayoutRenderer.razor
│       │   ├── GroupRenderer.razor
│       │   └── CategorizationRenderer.razor
│       └── Panels/                      # Supporting panels
│           ├── PreviousDataPanel.razor
│           ├── CredentialGatePanel.razor
│           └── CalculatedFieldsPanel.razor
├── Models/
│   └── Forms/                          # NEW — form models
│       ├── FormContext.cs
│       ├── FormSubmission.cs
│       ├── FileAttachmentInfo.cs
│       └── ProofAttachment.cs
└── Services/
    └── Forms/                          # NEW — form services
        ├── IFormSchemaService.cs
        ├── FormSchemaService.cs         # Schema parsing, scope resolution, validation
        ├── IFormSigningService.cs
        └── FormSigningService.cs        # SHA-256 hash + wallet sign integration

src/Common/Sorcha.Blueprint.Models/
├── Control.cs                          # MODIFY — add Rule, Options, deprecate Conditions
├── FormRule.cs                         # NEW
├── SchemaBasedCondition.cs             # NEW
└── RuleEffect.cs                       # NEW

tests/Sorcha.UI.Core.Tests/             # NEW test project (or extend existing)
└── Components/Forms/
    ├── ControlDispatcherTests.cs
    ├── FormContextTests.cs
    ├── DisclosureFilterTests.cs
    ├── SchemaValidationTests.cs
    ├── CalculatedFieldTests.cs
    └── AutoGenerationTests.cs
```

## Testing

```bash
# Unit tests for form logic (FormContext, schema parsing, disclosure filtering)
dotnet test tests/Sorcha.UI.Core.Tests/

# Build verification (0 warnings, 0 errors)
dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core/

# E2E tests with Docker (renders sample blueprints)
dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "FormRenderer"
```
