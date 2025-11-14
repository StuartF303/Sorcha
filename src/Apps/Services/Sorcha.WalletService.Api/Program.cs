using Sorcha.Cryptography;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Interfaces;
using Sorcha.WalletService.Encryption.Interfaces;
using Sorcha.WalletService.Encryption.Providers;
using Sorcha.WalletService.Events.Interfaces;
using Sorcha.WalletService.Events.Publishers;
using Sorcha.WalletService.Repositories.Implementation;
using Sorcha.WalletService.Repositories.Interfaces;
using Sorcha.WalletService.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure OpenAPI (built-in .NET 10)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register Sorcha.Cryptography services
builder.Services.AddSingleton<ICryptoModule, CryptoModule>();
builder.Services.AddSingleton<IHashProvider, HashProvider>();
builder.Services.AddSingleton<IWalletUtilities, Sorcha.Cryptography.Utilities.WalletUtilities>();

// Register WalletService infrastructure
builder.Services.AddSingleton<IEncryptionProvider, LocalEncryptionProvider>();
builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
builder.Services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();

// Register WalletService domain services
builder.Services.AddScoped<KeyManagementService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<DelegationService>();
builder.Services.AddScoped<WalletManager>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
}

app.UseHttpsRedirection();

// TODO: Add authentication middleware
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();
