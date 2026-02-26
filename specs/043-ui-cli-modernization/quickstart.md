# Quickstart Guide: UI & CLI Modernization

**Feature**: 043-ui-cli-modernization | **Date**: 2026-02-26

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running
- Sorcha services running (`docker-compose up -d`)
- Node.js (for qrcode.js dependency, optional — can use CDN)

## Development Setup

### 1. Switch to Feature Branch

```bash
git checkout 043-ui-cli-modernization
```

### 2. Start Backend Services

```bash
docker-compose up -d
```

All services needed: Blueprint Service (5000), Tenant Service (5110), Wallet Service, API Gateway (80).

### 3. Backend: Events API (Blueprint Service)

**New files to create**:
```
src/Services/Sorcha.Blueprint.Service/
├── Data/BlueprintEventsDbContext.cs    # PostgreSQL context for events
├── Endpoints/EventEndpoints.cs         # REST API
├── Hubs/EventsHub.cs                   # SignalR hub
├── Models/ActivityEvent.cs             # Entity
├── Services/Interfaces/IEventService.cs
├── Services/Implementation/EventService.cs
└── Services/Implementation/EventCleanupService.cs  # Background job
```

**Register in Program.cs**:
```csharp
// Add events PostgreSQL context
builder.Services.AddDbContext<BlueprintEventsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventsDb")));

// Add event services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddHostedService<EventCleanupService>();

// Map endpoints and hub
app.MapEventEndpoints();
app.MapHub<EventsHub>("/hubs/events");
```

**Run migrations**:
```bash
cd src/Services/Sorcha.Blueprint.Service
dotnet ef migrations add AddActivityEvents --context BlueprintEventsDbContext
dotnet ef database update --context BlueprintEventsDbContext
```

### 4. Backend: User Preferences (Tenant Service)

**New files to create**:
```
src/Services/Sorcha.Tenant.Service/
├── Endpoints/UserPreferenceEndpoints.cs
├── Endpoints/TotpEndpoints.cs
├── Models/UserPreferences.cs
├── Models/TotpConfiguration.cs
├── Services/Interfaces/ITotpService.cs
└── Services/Implementation/TotpService.cs
```

**Add to TenantDbContext**:
```csharp
public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
public DbSet<TotpConfiguration> TotpConfigurations => Set<TotpConfiguration>();
```

**NuGet dependency**:
```bash
cd src/Services/Sorcha.Tenant.Service
dotnet add package OtpNet
```

**Run migrations**:
```bash
dotnet ef migrations add AddUserPreferencesAndTotp
dotnet ef database update
```

### 5. Frontend: UI Services (Sorcha.UI.Core)

**New files to create**:
```
src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Services/ActivityLogService.cs
├── Services/UserPreferencesService.cs
├── Services/ThemeService.cs
├── Services/LocalizationService.cs
├── Services/TotpService.cs
└── Models/
    ├── ActivityEventDto.cs
    └── UserPreferencesDto.cs
```

**Register services in DI** (ServiceCollectionExtensions.cs):
```csharp
services.AddScoped<IActivityLogService, ActivityLogService>();
services.AddScoped<IUserPreferencesService, UserPreferencesService>();
services.AddScoped<ThemeService>();
services.AddSingleton<LocalizationService>();
services.AddScoped<ITotpService, TotpService>();
```

### 6. Frontend: UI Components (Sorcha.UI.Web.Client)

**New components**:
```
src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/
├── Components/Layout/
│   ├── ActivityLogPanel.razor          # Side overlay panel
│   └── StatusFooter.razor              # Bottom bar
├── Components/Shared/
│   └── WalletQrDialog.razor            # QR code dialog
└── wwwroot/i18n/
    ├── en.json
    ├── fr.json
    ├── de.json
    └── es.json
```

**Modified components**:
- `MainLayout.razor` — sidebar consolidation, theme binding, footer, activity log
- `Home.razor` — wizard conditional on default wallet
- `Settings.razor` — new tabs for theme, language, 2FA
- `Admin/Validator.razor` — realtime stats via SignalR
- `Wallets/WalletList.razor` — list/card toggle, default wallet, QR, share
- `Wallets/CreateWallet.razor` — PQC algorithm options

### 7. CLI Commands (Sorcha.Cli)

**New command files**:
```
src/Apps/Sorcha.Cli/Commands/
├── BlueprintCommand.cs
├── ParticipantCommand.cs
├── CredentialCommand.cs
├── ValidatorCommand.cs
└── AdminCommand.cs
```

**Register in Program.cs**:
```csharp
rootCommand.Subcommands.Add(new BlueprintCommand(serviceProvider));
rootCommand.Subcommands.Add(new ParticipantCommand(serviceProvider));
rootCommand.Subcommands.Add(new CredentialCommand(serviceProvider));
rootCommand.Subcommands.Add(new ValidatorCommand(serviceProvider));
rootCommand.Subcommands.Add(new AdminCommand(serviceProvider));
```

## Build & Test

```bash
# Build everything
dotnet build

# Run backend tests
dotnet test tests/Sorcha.Blueprint.Service.Tests
dotnet test tests/Sorcha.Tenant.Service.Tests

# Run CLI tests
dotnet test tests/Sorcha.Cli.Tests

# Run UI tests
dotnet test tests/Sorcha.UI.Core.Tests

# Run all tests
dotnet test

# Rebuild Docker
docker-compose build --no-cache
docker-compose up -d
```

## Verification Checklist

- [ ] Events API: `curl http://localhost/api/events` returns events list
- [ ] Events Hub: SignalR connection to `/hubs/events` succeeds
- [ ] Preferences API: `curl http://localhost/api/preferences` returns defaults
- [ ] TOTP API: `curl -X POST http://localhost/api/totp/setup` returns secret + QR URI
- [ ] UI: Activity log bell icon with unread count visible in app bar
- [ ] UI: Sidebar shows consolidated "Administration" section
- [ ] UI: Footer bar shows version + health + pending count
- [ ] UI: Wallet list has card/list toggle and QR button
- [ ] UI: Settings page has Theme, Language, 2FA tabs
- [ ] UI: Dark mode toggles instantly
- [ ] CLI: `sorcha blueprint list` returns blueprints
- [ ] CLI: `sorcha participant list` returns participants
- [ ] CLI: `sorcha credential list` returns credentials
- [ ] CLI: `sorcha validator status` returns validator info
- [ ] CLI: `sorcha admin health` returns service health

## Key References

| Document | Purpose |
|----------|---------|
| [spec.md](spec.md) | Feature specification |
| [plan.md](plan.md) | Implementation plan |
| [research.md](research.md) | Technology decisions |
| [data-model.md](data-model.md) | Entity definitions |
| [contracts/events-api.md](contracts/events-api.md) | Events API contract |
| [contracts/user-preferences-api.md](contracts/user-preferences-api.md) | Preferences API contract |
| [contracts/totp-api.md](contracts/totp-api.md) | 2FA API contract |
