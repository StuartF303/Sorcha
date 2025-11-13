# WALLET-001: Setup Siccar.WalletService Project

**Status:** Not Started
**Priority:** Critical
**Estimated Hours:** 6
**Dependencies:** None
**Related Spec:** [siccar-wallet-service.md](../specs/siccar-wallet-service.md)

## Objective

Create the foundational project structure for Siccar.WalletService library, including the main library project, test projects, and necessary configuration files.

## Requirements

### Project Structure

Create the following project structure:

```
src/
└── Libraries/
    └── Siccar.WalletService/
        ├── Siccar.WalletService/               # Main library
        │   ├── Domain/
        │   │   ├── Entities/
        │   │   ├── ValueObjects/
        │   │   └── Events/
        │   ├── Services/
        │   │   ├── Interfaces/
        │   │   └── Implementation/
        │   ├── Repositories/
        │   │   ├── Interfaces/
        │   │   └── Implementation/
        │   ├── Encryption/
        │   │   ├── Interfaces/
        │   │   └── Providers/
        │   ├── Events/
        │   │   ├── Interfaces/
        │   │   └── Publishers/
        │   └── Siccar.WalletService.csproj
        │
        ├── Siccar.WalletService.Tests/         # Unit tests
        │   ├── Services/
        │   ├── Repositories/
        │   ├── Encryption/
        │   ├── Events/
        │   ├── Fixtures/
        │   └── Siccar.WalletService.Tests.csproj
        │
        └── Siccar.WalletService.IntegrationTests/  # Integration tests
            ├── Database/
            ├── Encryption/
            ├── Events/
            ├── Fixtures/
            └── Siccar.WalletService.IntegrationTests.csproj
```

### Main Library Project (Siccar.WalletService.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageId>Siccar.WalletService</PackageId>
    <Version>1.0.0</Version>
    <Authors>Siccar Platform Team</Authors>
    <Company>Siccar</Company>
    <Description>Wallet management library for SICCAR distributed ledger platform</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/siccar-platform/SICCARV3</RepositoryUrl>
  </PropertyGroup>

  <ItemGroup>
    <!-- Internal Dependencies -->
    <ProjectReference Include="..\..\Siccar.Cryptography\Siccar.Cryptography.csproj" />
    <ProjectReference Include="..\..\Siccar.TransactionHandler\Siccar.TransactionHandler.csproj" />
    <ProjectReference Include="..\..\..\Common\SiccarCommon\SiccarCommon.csproj" />

    <!-- Database -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.0" />

    <!-- Encryption -->
    <PackageReference Include="Azure.Security.KeyVault.Keys" Version="4.5.0" />
    <PackageReference Include="Azure.Identity" Version="1.10.4" />
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.0" />

    <!-- HD Wallet Support -->
    <PackageReference Include="NBitcoin" Version="7.0.37" />

    <!-- Logging -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />

    <!-- Dependency Injection -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
  </ItemGroup>

</Project>
```

### Test Project (Siccar.WalletService.Tests.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Siccar.WalletService\Siccar.WalletService.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="AutoFixture" Version="4.18.0" />
    <PackageReference Include="AutoFixture.Xunit2" Version="4.18.0" />
  </ItemGroup>

</Project>
```

### Integration Test Project (Siccar.WalletService.IntegrationTests.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Siccar.WalletService\Siccar.WalletService.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Testcontainers" Version="3.6.0" />
    <PackageReference Include="Testcontainers.MySql" Version="3.6.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.6.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  </ItemGroup>

</Project>
```

### Configuration Files

**Directory.Build.props** (root of Siccar.WalletService/)
```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
</Project>
```

**.editorconfig** (root of Siccar.WalletService/)
```ini
root = true

[*.cs]
# Indentation
indent_style = tab
indent_size = 4

# Naming conventions
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

# Code style
csharp_prefer_braces = true:warning
csharp_style_var_elsewhere = true:suggestion
dotnet_sort_system_directives_first = true
```

**README.md** (root of Siccar.WalletService/)
```markdown
# Siccar.WalletService

Wallet management library for SICCAR distributed ledger platform.

## Features

- HD wallet creation and recovery (BIP32/BIP39/BIP44)
- Multi-algorithm support (ED25519, SECP256K1, RSA)
- Transaction signing and verification
- Encrypted key storage with multiple providers
- Access control and delegation
- Event-driven architecture

## Getting Started

See [Integration Guide](../../.specify/tasks/WALLET-036-integration-guide.md) for details.

## Documentation

- [Specification](../../.specify/specs/siccar-wallet-service.md)
- [Task Overview](../../.specify/tasks/WALLET-OVERVIEW.md)

## License

MIT
```

## Acceptance Criteria

- [ ] All project files created with correct structure
- [ ] Main library project compiles successfully
- [ ] Test projects compile successfully
- [ ] All NuGet dependencies resolve correctly
- [ ] Solution file created and all projects added
- [ ] README.md with basic documentation
- [ ] .editorconfig and Directory.Build.props configured
- [ ] CI/CD pipeline configured (GitHub Actions or Azure DevOps)
- [ ] Initial commit to repository

## Testing

1. Build solution: `dotnet build`
2. Verify all projects compile without warnings
3. Run empty test suite: `dotnet test`
4. Verify project references work correctly

## Notes

- Ensure Siccar.Cryptography v2.0 and Siccar.TransactionHandler v1.0 are available
- Use .NET 8.0 as the target framework
- Enable nullable reference types across all projects
- Configure XML documentation generation for the main library

## Next Steps

After completing this task:
1. Proceed to WALLET-002 (Domain Models & Enums)
2. Setup CI/CD pipeline for automated builds and tests
3. Configure code coverage reporting
