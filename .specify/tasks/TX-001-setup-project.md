# Task: Setup Siccar.TransactionHandler Project

**ID:** TX-001
**Status:** Not Started
**Priority:** Critical
**Estimate:** 4 hours
**Created:** 2025-11-12

## Objective

Create the new `Siccar.TransactionHandler` library project with proper configuration, dependencies on Siccar.Cryptography v2.0, and organized folder structure.

## Implementation Details

### Project Configuration (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0;netstandard2.1</TargetFrameworks>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>

    <!-- Package info -->
    <PackageId>Siccar.TransactionHandler</PackageId>
    <Version>2.0.0</Version>
    <Authors>Siccar Development Team</Authors>
    <Company>Wallet.Services (Scotland) Ltd</Company>
    <Description>Transaction and payload management library for the Siccar platform</Description>
    <PackageTags>siccar;blockchain;transaction;payload;distributed-ledger</PackageTags>
    <RepositoryUrl>https://github.com/StuartF303/SICCARV3</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>

    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>

    <!-- Build -->
    <Deterministic>true</Deterministic>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core dependency -->
    <PackageReference Include="Siccar.Cryptography" Version="2.0.0" />
    <PackageReference Include="System.Text.Json" Version="9.0.0" />
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

### Folder Structure
```
src/Common/Siccar.TransactionHandler/
├── Siccar.TransactionHandler.csproj
├── Enums/
├── Interfaces/
├── Core/
├── Payload/
├── Serialization/
├── Models/
└── Versioning/
```

## Acceptance Criteria

- [ ] New project created in correct location
- [ ] Depends on Siccar.Cryptography v2.0
- [ ] All folders created
- [ ] Project builds without errors
- [ ] XML documentation enabled
- [ ] Added to SICCARV3.sln

---

**Dependencies:** Siccar.Cryptography v2.0 must be available
