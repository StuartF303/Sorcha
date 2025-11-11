# Sorcha Specification Kit

## 📋 Overview

This directory contains the **Specification Kit (spec-kit)** for the Sorcha project—a comprehensive set of architectural principles, coding standards, security requirements, and development guidelines that govern all development work.

## 🎯 Purpose

The spec-kit ensures:
- **Consistency** across the codebase
- **Quality** through enforced standards
- **Security** by design
- **Maintainability** over time
- **Onboarding** efficiency for new developers
- **AI Assistance** alignment with project standards

## 📚 Documents

| Document | Purpose | Priority |
|----------|---------|----------|
| **[spec-kit.md](./spec-kit.md)** | Main entry point and overview | **START HERE** |
| **[architecture-rules.md](./architecture-rules.md)** | System design, structure, and patterns | **CRITICAL** |
| **[coding-standards.md](./coding-standards.md)** | Code style, conventions, and best practices | **CRITICAL** |
| **[security-guidelines.md](./security-guidelines.md)** | Security requirements and practices | **CRITICAL** |
| **[testing-requirements.md](./testing-requirements.md)** | Test coverage, strategies, and standards | **HIGH** |

## 🚀 Quick Start

### For Human Developers

1. **Start here**: Read [spec-kit.md](./spec-kit.md) for the overview
2. **Before coding**: Review [architecture-rules.md](./architecture-rules.md) and [coding-standards.md](./coding-standards.md)
3. **Before committing**: Check the compliance checklists in each document
4. **Security-critical work**: Review [security-guidelines.md](./security-guidelines.md)
5. **Writing tests**: Follow [testing-requirements.md](./testing-requirements.md)

### For AI Assistants

When assisting with the Sorcha project:

1. ✅ **Read the spec-kit first** before making any code changes
2. ✅ **Follow all mandatory rules** marked with ✅ REQUIRED or CRITICAL
3. ✅ **Maintain consistency** with existing patterns
4. ✅ **Explain decisions** by referencing relevant spec-kit sections
5. ✅ **Flag violations** if you encounter code that doesn't comply
6. ✅ **Suggest improvements** aligned with spec-kit principles

## 🔒 Compliance

### Mandatory Rules

All code MUST comply with:
- ✅ Architecture rules (no violations of layer boundaries)
- ✅ Coding standards (nullable reference types, naming conventions)
- ✅ Security guidelines (input validation, OWASP Top 10 protection)
- ✅ Testing requirements (minimum coverage thresholds)

### Before Committing Code

Run through the checklists in each document:
- [ ] Architecture compliance ✓
- [ ] Coding standards followed ✓
- [ ] Security requirements met ✓
- [ ] Tests written and passing ✓
- [ ] Code coverage above minimum ✓

## 📖 Using This Spec-Kit

### When to Reference

- **Planning new features**: Check architecture rules for proper design
- **Writing code**: Follow coding standards for consistency
- **Adding APIs**: Review security guidelines for protection
- **Creating tests**: Follow testing requirements
- **Code reviews**: Use as reference for evaluation
- **Resolving disputes**: Spec-kit is the source of truth

### When to Update

Update the spec-kit when:
- Introducing new architectural patterns
- Changing technology stack
- Adding new security requirements
- Establishing new conventions
- Discovering better practices

**Process**:
1. Discuss proposed changes in an issue or PR
2. Update relevant spec-kit documents
3. Update version number and history
4. Commit with clear description
5. Announce to team

## 🤖 AI Assistant Integration

This spec-kit is designed to be machine-readable and AI-friendly:

- **Clear structure**: Hierarchical organization for easy parsing
- **Explicit rules**: ✅ CORRECT and ❌ WRONG examples
- **Searchable**: Rich keywords and cross-references
- **Actionable**: Specific, implementable guidance
- **Versioned**: Track changes over time

### AI Reading Guidelines

1. **Priority**: Read documents in order of priority (Critical → High → Normal)
2. **Context**: Understand the "why" behind rules, not just "what"
3. **Examples**: Learn from code examples (✅ correct, ❌ wrong)
4. **Checklist**: Use compliance checklists before completing tasks
5. **Reference**: Cite spec-kit sections when making decisions

## 🏗️ Project Context

**Sorcha** is a modern .NET 10 blueprint execution engine and designer for data flow orchestration:

- **Technology**: .NET 10, C# 13, ASP.NET Core, Blazor, .NET Aspire
- **Architecture**: Microservices, cloud-native, API-first
- **Deployment**: Docker, Azure Container Apps
- **Focus**: Security, observability, maintainability

For more context, see:
- [Project README](../../README.md)
- [Architecture Documentation](../../docs/architecture.md)
- [Getting Started Guide](../../docs/getting-started.md)

## 📞 Questions and Feedback

- **Questions**: Open an issue with the `question` label
- **Suggestions**: Open an issue with the `enhancement` label
- **Violations**: Report with the `spec-violation` label
- **Updates**: Submit a PR with proposed changes

## 📜 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-11-11 | Initial spec-kit creation with comprehensive guidelines |

## 🎯 Success Criteria

The spec-kit is successful when:
- ✅ All developers reference it regularly
- ✅ Code reviews use it as evaluation criteria
- ✅ New contributors can onboard quickly
- ✅ AI assistants produce compliant code
- ✅ Technical debt is minimized
- ✅ Security vulnerabilities are prevented
- ✅ Code quality remains consistently high

---

**Remember**: This spec-kit exists to help, not hinder. It's a living document that evolves with the project. When in doubt, refer to the spec-kit—and if the spec-kit is unclear, help improve it! 🚀
