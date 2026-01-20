# Sample Blueprints

This directory contains sample blueprint files for testing the Blueprint Designer's import/export functionality.

## Available Samples

### 1. simple-approval.json
A basic two-step approval workflow demonstrating:
- Two participants (Requester and Approver)
- Simple request submission and approval flow
- Inline JSON schema for data validation

**Best for:** Getting started, testing basic import functionality

### 2. loan-application.json
A comprehensive loan application workflow featuring:
- Four participants with different roles (Initiator, Approver, Observer)
- Conditional routing based on loan amount and document scores
- Calculated fields (risk score computation)
- Branching workflow paths

**Best for:** Testing complex conditions and calculations

### 3. supply-chain.yaml
A supply chain tracking workflow in YAML format showcasing:
- Six participants across multiple organizations
- Sequential tracking from manufacturer to retailer
- Quality control conditions
- Customs clearance requirements
- Delivery calculations

**Best for:** Testing YAML import, multi-organization workflows

## Usage

### Import via Designer
1. Open the Blueprint Designer
2. Click the **Import** button in the toolbar
3. Select one of these sample files
4. The blueprint will be loaded into the designer

### Import via File Upload
1. Navigate to the Blueprints page
2. Click **Import Blueprint**
3. Choose JSON or YAML format
4. Select the sample file

## File Formats

- **JSON (.json)**: Standard JSON format, widely compatible
- **YAML (.yaml)**: Human-readable format, supports comments

Both formats support the same blueprint schema and can be imported interchangeably.

## Schema Reference

All blueprints follow the Sorcha Blueprint Schema v1.0:
- `$schema`: Schema identifier
- `$id`: Unique blueprint identifier
- `title`: Blueprint name
- `description`: Brief description
- `version`: Semantic version
- `participants`: Array of workflow participants
- `actions`: Array of workflow actions
- `metadata`: Optional metadata (category, tags, author)
