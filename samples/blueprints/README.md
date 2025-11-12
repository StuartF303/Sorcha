# Sorcha Sample Blueprints

This directory contains sample blueprints demonstrating the Sorcha workflow system across various sectors and complexity levels. These examples can be used for testing, demonstration, and as templates for creating your own blueprints.

## Directory Structure

```
samples/blueprints/
├── finance/           # Financial sector workflows
├── healthcare/        # Healthcare sector workflows
├── benefits/          # Benefits & entitlement workflows
├── supply-chain/      # Supply chain & logistics workflows
└── README.md          # This file
```

## Blueprints by Complexity

### Simple Blueprints (2-3 Actions)

These blueprints demonstrate basic workflows with minimal participants and straightforward routing.

#### Finance
- **`simple-invoice-approval.json`** - Two-step invoice submission and approval workflow
  - **Participants:** Vendor, Accounts Payable
  - **Use Case:** Vendor submits invoice, AP approves payment
  - **Features:** Basic data schemas, simple disclosure

#### Healthcare
- **`simple-patient-referral.json`** - Three-step patient referral from PCP to specialist
  - **Participants:** Primary Care Physician, Specialist, Patient
  - **Use Case:** PCP creates referral, specialist accepts, appointment scheduled
  - **Features:** HIPAA compliance, patient consent, selective disclosure

#### Benefits
- **`simple-unemployment-claim.json`** - Three-step unemployment benefits application
  - **Participants:** Claimant, Unemployment Office, Claims Supervisor
  - **Use Case:** Application, review, approval with conditional routing to supervisor
  - **Features:** Conditional routing based on review requirements

---

### Moderate Blueprints (4-5 Actions)

These blueprints demonstrate intermediate complexity with multi-level approvals and conditional routing.

#### Finance
- **`moderate-purchase-order-approval.json`** - Five-step purchase order with multi-level conditional approval
  - **Participants:** Requester, Dept Manager, Finance Director, CFO, Vendor, Procurement
  - **Use Case:** Purchase requisition with amount-based routing, multi-level approval, PO creation
  - **Features:**
    - Conditional routing based on purchase amount ($5K/$25K thresholds)
    - Dynamic approval routing (Manager → Finance Director → CFO)
    - Calculations (line item totals, approval levels)
    - Secondary approval flag
    - Vendor acknowledgment

#### Healthcare
- **`moderate-medical-records-sharing.json`** - Five-step medical records sharing with consent
  - **Participants:** Patient, Primary Provider, Specialist, Records Custodian, Compliance Officer
  - **Use Case:** Patient authorization, compliance review, records preparation, transmission, acknowledgment
  - **Features:**
    - Explicit patient consent and authorization
    - HIPAA compliance review
    - Selective disclosure (privacy-preserving)
    - Record integrity verification (SHA-256 hashing)
    - Multi-provider coordination

#### Benefits
- **`moderate-disability-assessment.json`** - Four-step disability benefits assessment
  - **Participants:** Applicant, Claims Examiner, Medical Consultant, Vocational Specialist, Adjudicator
  - **Use Case:** Application, medical review with functional assessment, vocational evaluation, determination
  - **Features:**
    - Multi-stage assessment process
    - Severity scoring and calculations
    - Conditional routing based on medical severity
    - RFC (Residual Functional Capacity) evaluation
    - Approval/denial with appeal rights

---

### Complex Blueprints (6+ Actions)

These blueprints demonstrate advanced workflows with multiple organizations, dynamic routing, and regulatory compliance.

#### Finance
- **`complex-multi-bank-trade-settlement.json`** - Seven-step cross-border securities trade settlement
  - **Participants:** Buyer Trader, Buyer Bank, Seller Trader, Seller Bank, Clearinghouse, Regulator, FX Provider
  - **Use Case:** Trade initiation, confirmation, central clearing, FX conversion, pre-settlement, final settlement, regulatory reporting
  - **Features:**
    - Multi-party coordination (7 participants across organizations)
    - Cross-border trade handling
    - Dynamic routing based on trade size, currency, jurisdiction
    - Central counterparty clearing (CCP/novation)
    - FX conversion for cross-border trades
    - Delivery vs Payment (DVP) methods
    - Regulatory reporting (MiFID II, Dodd-Frank, EMIR)
    - Risk assessment and margin requirements
    - SWIFT message integration

---

## Key Features Demonstrated

### 1. Conditional Routing
Many blueprints use JSON Logic for dynamic routing based on data values:
```json
"condition": {
  "if": [
    { ">=": [{ "var": "totalAmount" }, 25000] },
    "cfo",
    "dept-manager"
  ]
}
```

### 2. Calculations
Blueprints can perform calculations on submitted data:
```json
"calculations": {
  "totalAmount": {
    "+": [{ "var": "amount" }, { "var": "taxAmount" }]
  }
}
```

### 3. Selective Disclosure
Control what each participant can see:
```json
"disclosures": [
  {
    "participantAddress": "approver",
    "dataPointers": ["/approved", "/comments"]
  }
]
```

### 4. Form Controls
Rich UI form definitions for data entry:
```json
"form": {
  "type": "Layout",
  "layout": "VerticalLayout",
  "elements": [
    {
      "type": "TextLine",
      "scope": "/invoiceNumber",
      "title": "Invoice Number"
    }
  ]
}
```

### 5. Multi-Organization Coordination
Complex blueprints involve multiple organizations working together with selective data sharing.

---

## Using These Blueprints

### Loading into Designer
1. Open the Sorcha Blueprint Designer
2. Use the "Import Blueprint" function
3. Select any `.json` file from this directory
4. The blueprint will be loaded and ready for visualization/editing

### Testing
These blueprints can be used with the Sorcha test harness:
```bash
dotnet test --filter "Category=Integration"
```

### As Templates
Copy any blueprint and modify it for your use case:
1. Change participant names and organizations
2. Modify data schemas to match your requirements
3. Adjust conditional routing logic
4. Add or remove actions as needed

---

## Blueprint Structure Reference

Each blueprint contains:
- **`id`**: Unique identifier
- **`title`**: Human-readable title
- **`description`**: Detailed description
- **`version`**: Version number
- **`metadata`**: Key-value pairs for categorization
- **`participants`**: Array of workflow participants
- **`actions`**: Array of workflow steps

### Participant Structure
```json
{
  "id": "unique-id",
  "name": "Display Name",
  "organisation": "Organization Name",
  "walletAddress": "0x...",
  "didUri": "did:example:...",
  "useStealthAddress": false
}
```

### Action Structure
```json
{
  "id": 0,
  "title": "Action Title",
  "description": "Action description",
  "sender": "participant-id",
  "target": "participant-id",
  "dataSchemas": [...],
  "disclosures": [...],
  "calculations": {...},
  "condition": {...},
  "form": {...}
}
```

---

## Sectors Covered

### Finance
- Invoice approval
- Purchase order procurement
- Securities trading and settlement
- Cross-border transactions
- Multi-bank coordination

### Healthcare
- Patient referrals
- Medical records sharing
- Multi-provider care coordination
- HIPAA-compliant workflows
- Health information exchange

### Benefits & Entitlements
- Unemployment insurance claims
- Disability benefits assessment
- Cross-agency entitlement determination
- Multi-stage eligibility assessment

### Supply Chain
- Multi-party logistics coordination
- Cross-border shipping
- Regulatory compliance (customs, import/export)

---

## Compliance & Privacy

Many blueprints demonstrate compliance features:
- **HIPAA** (Healthcare): Patient consent, selective disclosure, audit trails
- **GDPR** (Healthcare): Privacy-preserving data sharing, right to access
- **MiFID II** (Finance): Trade reporting, transparency
- **Dodd-Frank** (Finance): Clearing requirements, regulatory reporting
- **EMIR** (Finance): Trade repository reporting

---

## Advanced Features

### JSON Logic
Used for conditions and calculations. See examples in:
- `moderate-purchase-order-approval.json` (conditional routing)
- `moderate-disability-assessment.json` (severity scoring)

### JSON Pointers
Used for selective disclosure:
- `/*` - All fields
- `/fieldName` - Specific field
- `/parent/child` - Nested field

### Privacy Features
- **Stealth Addresses**: Participants can use privacy-preserving addresses
- **Selective Disclosure**: Control field-level visibility
- **Encryption**: Support for encrypted data transmission

---

## Contributing

When creating new sample blueprints:
1. Follow the existing naming convention: `{complexity}-{description}.json`
2. Include comprehensive metadata
3. Add realistic data schemas and validation
4. Document any special features in this README
5. Ensure JSON is properly formatted and validated

---

## Support & Documentation

- **Full Documentation**: [https://docs.sorcha.io](https://docs.sorcha.io)
- **Blueprint Schema**: `src/Common/blueprint.schema.json`
- **API Reference**: See project documentation
- **Issues**: Report bugs or request features via GitHub Issues

---

## License

These sample blueprints are provided as examples and may be freely used and modified for your projects.
