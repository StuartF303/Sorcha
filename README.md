# Sorcha

A modern .NET 10 blueprint execution engine and designer for data flow orchestration.

## Overview

Sorcha is a modernized, cloud-native platform for defining, designing, and executing data flow blueprints. Built on .NET 10 and leveraging .NET Aspire for cloud-native orchestration, Sorcha provides a flexible and scalable solution for workflow automation and data processing pipelines.

## Features

- **Blueprint Engine**: Execute data flow blueprints with high performance and reliability
- **Blueprint Designer**: Visual designer for creating and managing workflows
- **.NET 10**: Built on the latest .NET platform for maximum performance
- **.NET Aspire**: Cloud-native orchestration and service discovery
- **Minimal APIs**: Modern, lightweight API design
- **Observability**: Built-in OpenTelemetry support for monitoring and tracing

## Project Structure

```
Sorcha/
├── src/
│   ├── Sorcha.AppHost/              # Aspire orchestration host
│   ├── Sorcha.ServiceDefaults/      # Shared service configurations
│   ├── Sorcha.Blueprint.Engine/     # Blueprint execution engine (API)
│   └── Sorcha.Blueprint.Designer/   # Blueprint visual designer (Web)
├── tests/                           # Test projects
├── docs/                            # Documentation
└── .github/                         # GitHub workflows
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Docker](https://www.docker.com/) (optional, for containerization)
- [Visual Studio 2025](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

### Building

```bash
dotnet restore
dotnet build
```

### Running

Using .NET Aspire AppHost:

```bash
dotnet run --project src/Sorcha.AppHost
```

This will start the Aspire dashboard and orchestrate all services.

### Running Individual Services

Blueprint Engine:
```bash
dotnet run --project src/Sorcha.Blueprint.Engine
```

Blueprint Designer:
```bash
dotnet run --project src/Sorcha.Blueprint.Designer
```

## Development

### Solution Structure

- **Sorcha.AppHost**: The .NET Aspire orchestration project that manages all services
- **Sorcha.ServiceDefaults**: Shared configurations including OpenTelemetry, health checks, and service discovery
- **Sorcha.Blueprint.Engine**: The core execution engine for running blueprints via minimal APIs
- **Sorcha.Blueprint.Designer**: Blazor-based web application for designing and managing blueprints

### Architecture

Sorcha follows a microservices architecture with:

- **Service-oriented design**: Each component is independently deployable
- **Cloud-native patterns**: Built-in support for service discovery, health checks, and distributed tracing
- **Modern APIs**: RESTful APIs using minimal API patterns
- **Reactive UI**: Blazor Server for responsive, real-time user interfaces

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:

- Code of conduct
- Development workflow
- Submitting pull requests
- Reporting issues

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [ ] Core blueprint execution engine
- [ ] Visual blueprint designer
- [ ] Blueprint validation and testing framework
- [ ] Plugin system for custom actions
- [ ] Multi-tenant support
- [ ] Cloud deployment templates (Azure, AWS, GCP)
- [ ] Distributed execution support
- [ ] Real-time monitoring dashboard

## Documentation

Full documentation is available in the [docs](docs/) directory:

- [Architecture Overview](docs/architecture.md)
- [Getting Started Guide](docs/getting-started.md)
- [Blueprint Schema](docs/blueprint-schema.md)
- [API Reference](docs/api-reference.md)
- [Deployment Guide](docs/deployment.md)

## Support

- Documentation: [docs/](docs/)
- Issues: [GitHub Issues](https://github.com/yourusername/sorcha/issues)
- Discussions: [GitHub Discussions](https://github.com/yourusername/sorcha/discussions)

## Acknowledgments

This project is inspired by and modernizes concepts from the [SiccarV3](https://github.com/stuartf303/siccarv3) project.

---

Built with ❤️ using .NET 10 and .NET Aspire
