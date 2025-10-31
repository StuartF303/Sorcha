# Changelog

All notable changes to the Sorcha project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure with .NET 10
- .NET Aspire orchestration for cloud-native development
- Blueprint Engine service with minimal APIs
- Blueprint Designer web UI with Blazor Server
- Service Defaults for shared configurations
- OpenTelemetry integration for observability
- Health check endpoints
- Service discovery support
- GitHub Actions CI/CD workflows
  - Build and test workflow
  - Release workflow with NuGet and Docker support
  - CodeQL security analysis
- Comprehensive documentation
  - Architecture overview
  - Getting started guide
  - Blueprint schema specification
  - Contributing guidelines
- MIT License
- .gitignore and .gitattributes for repository hygiene

### Changed
- Migrated from SiccarV3 architecture
- Modernized to .NET 10 from .NET 8/9
- Simplified microservices to focus on core blueprint execution
- Adopted minimal API pattern for REST endpoints
- Replaced custom orchestration with .NET Aspire

### Removed
- Legacy dependencies (IdentityServer4, Dapr)
- Domain-specific services (to be re-added as needed)

## [0.1.0] - TBD

### Planned
- [ ] Core blueprint schema implementation
- [ ] Blueprint validation engine
- [ ] Basic execution engine
- [ ] Visual designer prototype
- [ ] Unit test coverage
- [ ] Integration tests
- [ ] API documentation
- [ ] Docker support
- [ ] Kubernetes manifests

---

## Version History

### Versioning Strategy

- **Major version (X.0.0)**: Breaking changes
- **Minor version (0.X.0)**: New features, backwards compatible
- **Patch version (0.0.X)**: Bug fixes

### Release Cycle

- **Alpha**: Internal testing, frequent changes
- **Beta**: Public preview, feature complete
- **RC**: Release candidate, production ready
- **Stable**: General availability

---

[Unreleased]: https://github.com/yourusername/sorcha/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/yourusername/sorcha/releases/tag/v0.1.0
