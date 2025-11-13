# SORCHA Project Constitution

**Version:** 1.0
**Last Updated:** 2025-11-11
**Status:** Active

## Purpose

This constitution establishes the foundational principles, standards, and guidelines for the SORCHA project. All development, architectural decisions, and implementations must align with these principles.

## Project Overview

SORCHA is a distributed ledger platform built on microservices architecture, providing secure, scalable blockchain capabilities through a suite of specialized services including Wallet, Tenant, Register, Peer, Blueprint, Action, and Validator services.

## Core Principles

### 1. Architecture Principles

**Microservices-First Architecture**
- Each service must be independently deployable and maintainable
- Services communicate via well-defined APIs and message buses
- Use .Net Aspire for microservices orchestration and service-to-service communication
- Maintain clear service boundaries with minimal coupling

**Cloud-Native Design**
- Design for containerization (Docker) from the start
- Support both Kubernetes and Docker Compose deployments
- Implement health checks and readiness probes for all services
- Use configuration management appropriate for cloud environments

**Scalability and Resilience**
- Services must be horizontally scalable
- Implement circuit breakers and retry policies
- Use distributed caching (Redis) for performance optimization
- Design for eventual consistency where appropriate

### 2. Security Principles

**Zero Trust Security Model**
- All service-to-service communication must be authenticated
- Implement API tokens for Dapr communication
- Never commit secrets or sensitive configuration to source control
- Use secret management (Azure Key Vault, Kubernetes secrets, local secret stores)

**Cryptographic Standards**
- Use industry-standard cryptographic libraries (Sorcha.Cryptography)
- Encrypt sensitive data at rest (wallet private keys using AES-256-GCM)
- Implement secure key management practices (Azure Key Vault, AWS KMS, or local DPAPI)
- Support multiple cryptographic algorithms (ED25519, SECP256K1, RSA)
- Implement BIP32/BIP39/BIP44 for HD wallet support
- Maintain audit trails for cryptographic operations
- Never store mnemonics - user responsibility to backup

**Identity and Access Management**
- Integrate with external identity providers (Azure AD, B2C)
- Implement proper authentication and authorization
- Use JWT tokens for API authentication
- Support multi-tenant security isolation

### 3. Development Standards

**Code Quality**
- Maintain minimum 80% unit test coverage for core libraries
- Write integration tests for all service APIs
- Follow C# coding conventions and .NET best practices
- Use async/await patterns for I/O operations

**.NET Framework Standards**
- Target .NET 10 or later for all projects
- Use modern C# language features appropriately
- Leverage dependency injection throughout
- Follow RESTful API design principles

**Version Control**
- Use Git with feature branch workflow
- Write clear, descriptive commit messages
- Reference work items in commits where applicable
- Maintain clean commit history

### 4. Testing Principles

**Comprehensive Testing Strategy**
- Unit tests for business logic and utilities
- Integration tests for service APIs and database interactions
- End-to-end tests for critical user workflows
- Performance testing for scalability validation

**Test Organization**
- Maintain separate test projects (UnitTests, IntegrationTests, EndToEndTests)
- Use xUnit as the primary testing framework
- Mock external dependencies appropriately
- Maintain test data isolation

### 5. Documentation Standards

**Code Documentation**
- Document all public APIs with XML comments
- Maintain README files for each service
- Document configuration requirements and dependencies
- Keep architectural decision records (ADRs)

**Operational Documentation**
- Document deployment procedures
- Maintain troubleshooting guides
- Document configuration management
- Keep dependency upgrade procedures current

### 6. Infrastructure as Code

**Declarative Infrastructure**
- Use Bicep for Azure infrastructure definitions
- Maintain Kubernetes manifests for service deployments
- Version control all infrastructure code
- Use Docker Compose for local development consistency

**Configuration Management**
- Separate configuration from code
- Use environment-specific configuration files
- Implement configuration validation on startup
- Document all configuration parameters

### 7. Data Management

**Data Storage Principles**
- Use appropriate storage for each service (PostgreSQL, MongoDB, Redis)
- Prefer PostgreSQL for relational data (Wallet Service)
- Use MongoDB for document-oriented data (Blueprint, Register Services)
- Use Redis for distributed caching and session state
- Implement database migrations for schema changes
- Maintain data backup and recovery procedures
- Design for data sovereignty and compliance
- Implement soft deletes for sensitive data (wallets)

**State Management**
- Use .NET Aspire for service orchestration and messaging
- Implement event-driven architecture for service communication
- Implement event sourcing where appropriate
- Maintain audit logs for critical operations (especially wallet operations)
- Design for data consistency across services
- Use optimistic concurrency control where appropriate

### 8. Observability

**Logging Standards**
- Use structured logging (Serilog)
- Log to centralized systems (Seq)
- Include correlation IDs for distributed tracing
- Log at appropriate levels (Debug, Info, Warning, Error)

**Monitoring and Tracing**
- Implement distributed tracing (Zipkin)
- Monitor service health and performance
- Set up alerting for critical failures
- Track key performance indicators (KPIs)

### 9. Dependency Management

**Dependency Strategy**
- Use private NuGet feed for internal packages
- Keep dependencies up to date with security patches
- Document dependency upgrade procedures
- Minimize external dependencies

**Package Management**
- Use NuGet for .NET package management
- Version internal packages semantically
- Test dependency upgrades thoroughly
- Maintain dependency compatibility matrix

### 10. License and Copyright

**Licensing Requirements**
- All code is proprietary under Siccar Proprietary Limited Use License
- Maintain copyright notices in all source files
- Document third-party licenses and attributions
- Ensure compliance with license terms

**Copyright Notice Format**
```
// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

```

## Constitution Enforcement

### Compliance Checking
- Constitutional compliance must be verified during code reviews
- Architectural decisions must reference relevant constitutional principles
- Violations must be documented and justified
- Regular audits to ensure ongoing compliance

### Amendment Process
- Constitutional amendments require team consensus
- Major version increments for breaking principle changes
- Minor version increments for principle additions
- Patch version increments for clarifications

### Conflict Resolution
- Technical disputes resolved by referencing constitutional principles
- Escalation path: Team Lead → Architecture Review → Project Stakeholders
- Document all significant architectural decisions

## Related Documents

- [Project Specification](.specify/spec.md)
- [Implementation Plan](.specify/plan.md)
- [Project README](../README.md)
- [License](../LICENCE.txt)
- [Troubleshooting Guide](../TROUBLESHOOTING.md)

---

**Document Control**
- **Created:** 2025-11-11
- **Authority:** SORCHA Architecture Team
- **Review Frequency:** Quarterly
- **Next Review:** 2026-02-11
