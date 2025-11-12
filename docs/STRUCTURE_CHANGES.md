# Directory Structure Consolidation - January 12, 2025

## Summary

The Sorcha project structure has been consolidated to remove unnecessary directory nesting and provide a cleaner, more maintainable organization.

## Changes Made

### 1. Directory Structure Simplification

**Before:**
```
src/
├── Apps/
│   ├── Hosting/
│   │   ├── Sorcha.AppHost/
│   │   └── Sorcha.ServiceDefaults/
│   ├── Services/
│   │   ├── Sorcha.ApiGateway/
│   │   └── Sorcha.Blueprint.Service/
│   └── UI/
│       └── Sorcha.Blueprint.Designer.Client/
├── Common/
├── Core/
└── Services/
    └── Sorcha.Peer.Service/
```

**After:**
```
src/
├── Apps/                           # Application layer
│   ├── Sorcha.AppHost/            # .NET Aspire orchestration
│   └── UI/
│       └── Sorcha.Blueprint.Designer.Client/
├── Common/                         # Cross-cutting concerns
│   ├── Sorcha.Blueprint.Models/
│   ├── Sorcha.Cryptography/
│   └── Sorcha.ServiceDefaults/
├── Core/                           # Business logic
│   ├── Sorcha.Blueprint.Engine/
│   ├── Sorcha.Blueprint.Fluent/
│   └── Sorcha.Blueprint.Schemas/
└── Services/                       # Service layer
    ├── Sorcha.ApiGateway/
    ├── Sorcha.Blueprint.Service/
    └── Sorcha.Peer.Service/
```

### 2. Key Changes

1. **Removed `Apps/Hosting/` subdirectory**
   - Moved `Sorcha.AppHost` from `Apps/Hosting/` to `Apps/`
   - Moved `Sorcha.ServiceDefaults` from `Apps/Hosting/` to `Common/`

2. **Removed `Apps/Services/` subdirectory**
   - Moved `Sorcha.ApiGateway` from `Apps/Services/` to `Services/`
   - Moved `Sorcha.Blueprint.Service` from `Apps/Services/` to `Services/`

3. **Consolidated all services in `Services/` directory**
   - All REST APIs, gRPC services, and background services now in one location
   - No more duplicate `Services/` folder at root level

### 3. Project References Updated

All project references have been updated to reflect the new paths:

- **Sorcha.AppHost**: Updated references to Services projects
- **Sorcha.ApiGateway**: Updated reference to ServiceDefaults
- **Sorcha.Blueprint.Service**: Updated references to Common projects
- **Sorcha.Peer.Service**: Updated references to Common projects
- **Sorcha.Blueprint.Engine**: Updated reference to ServiceDefaults
- **Sorcha.Blueprint.Fluent**: Updated reference to Models
- **Test projects**: Updated references to Apps and Services

### 4. Documentation Updated

The following documentation files have been updated:

- **README.md**: Updated project structure and all command examples
- **docs/architecture.md**: Updated solution structure and component descriptions
- **docs/project-structure.md**: Completely revised to reflect new 4-layer architecture
- **docs/STRUCTURE_CHANGES.md**: This file

### 5. Solution File Updates

- Removed old project references with incorrect paths
- Added projects with correct paths
- Cleaned up duplicate entries

## Architecture Layers

The new structure follows a clear 4-layer architecture:

### Layer 1: Apps (Application Layer)
- **Purpose**: Orchestration and UI applications
- **Contents**: AppHost, UI projects
- **Dependencies**: Can depend on Services, Core, and Common

### Layer 2: Services (Service Layer)
- **Purpose**: REST APIs, gRPC services, API Gateways
- **Contents**: ApiGateway, Blueprint.Service, Peer.Service
- **Dependencies**: Can depend on Core and Common

### Layer 3: Core (Business Logic Layer)
- **Purpose**: Core business logic and domain services
- **Contents**: Blueprint.Engine, Blueprint.Fluent, Blueprint.Schemas
- **Dependencies**: Can only depend on Common

### Layer 4: Common (Cross-Cutting Layer)
- **Purpose**: Shared models, utilities, and infrastructure
- **Contents**: Blueprint.Models, Cryptography, ServiceDefaults
- **Dependencies**: No dependencies on other src/ projects

## Migration Guide

### For Developers

If you have local branches with uncommitted changes:

1. **Pull the latest changes**
   ```bash
   git pull origin master
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Rebuild the solution**
   ```bash
   dotnet clean
   dotnet build
   ```

4. **Update your IDE**
   - Close and reopen your solution in Visual Studio/Rider
   - Reload projects in VS Code

### For CI/CD

No changes required - all builds should work automatically with the new structure.

### Running the Application

Commands have been updated:

**Old:**
```bash
dotnet run --project src/Apps/Hosting/Sorcha.AppHost
dotnet run --project src/Apps/Services/Sorcha.ApiGateway
```

**New:**
```bash
dotnet run --project src/Apps/Sorcha.AppHost
dotnet run --project src/Services/Sorcha.ApiGateway
```

## Benefits

1. **Cleaner structure**: Removed unnecessary nested directories
2. **Logical grouping**: All services in one location
3. **Clear separation**: 4 distinct layers with well-defined purposes
4. **Easier navigation**: Shorter paths and fewer levels
5. **Better organization**: Services, Core, and Common are clearly distinguished
6. **Consistent naming**: All layers follow the same organizational pattern

## Breaking Changes

### Project References

Any project or tool that references projects by path will need to be updated:

- Docker files
- Build scripts
- Launch configurations
- Test runners
- IDE project files

### Solution File

The solution file has been updated. If you have local .sln modifications, they may need to be resolved.

## Rollback

If needed, rollback can be performed by:

1. Reverting to commit before structure changes
2. Running `dotnet restore` and `dotnet build`

However, we recommend moving forward with the new structure as it provides significant long-term benefits.

## Questions?

For questions or issues related to the structure changes:

1. Review the updated documentation in `docs/`
2. Check `docs/project-structure.md` for detailed placement rules
3. See `docs/architecture.md` for architecture overview
4. Open an issue on GitHub if you encounter problems
