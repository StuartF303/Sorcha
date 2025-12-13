// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Scalar.AspNetCore;
using Sorcha.Wallet.Service.Extensions;
using Sorcha.Wallet.Service.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add Wallet Service infrastructure and domain services
builder.Services.AddWalletService(builder.Configuration);

// Add OpenAPI services (built-in .NET 10)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Sorcha Wallet Service API";
        document.Info.Version = "1.0.0";
        document.Info.Description = """
            # Wallet Service API

            ## Overview

            The Wallet Service provides **cryptographic wallet management** and **transaction signing** capabilities for the Sorcha distributed ledger platform. It manages HD (Hierarchical Deterministic) wallets with support for multiple cryptographic algorithms and secure key storage.

            ## Primary Use Cases

            - **Wallet Creation**: Generate new HD wallets with BIP39 mnemonic phrases
            - **Wallet Recovery**: Restore wallets from 12-24 word mnemonic phrases
            - **Transaction Signing**: Sign transactions and data payloads with wallet private keys
            - **Key Management**: Secure storage and retrieval of cryptographic keys
            - **Multi-Algorithm Support**: ED25519, NIST P-256, and RSA-4096 support

            ## Key Concepts

            ### HD Wallets (Hierarchical Deterministic)
            HD wallets follow BIP32/BIP39/BIP44 standards:
            - **BIP39**: Mnemonic phrase generation (12 or 24 words)
            - **BIP32**: Hierarchical key derivation
            - **BIP44**: Multi-account hierarchy (m/44'/coin'/account'/change/index)
            - **Deterministic**: Same mnemonic always generates same keys

            ### Supported Cryptographic Algorithms
            - **ED25519**: Edwards-curve Digital Signature Algorithm (default, fastest)
            - **NIST P-256**: FIPS 186-4 compliant elliptic curve
            - **RSA-4096**: Traditional RSA with 4096-bit keys

            ### Wallet Properties
            Each wallet has:
            - **Wallet ID**: Unique identifier
            - **Public Address**: Used to identify wallet in transactions
            - **Algorithm**: Cryptographic algorithm (ED25519, NISTP256, RSA4096)
            - **Creation Date**: When the wallet was created
            - **Key Derivation Path**: BIP44 path for HD key generation

            ### Security Model

            #### Key Storage
            - **Private keys NEVER exposed** via API
            - **Encrypted at rest** using AES-256-GCM
            - **Support for HSM integration** (Azure Key Vault, AWS KMS)
            - **Local storage** uses OS-level encryption (DPAPI on Windows, Keychain on macOS)

            #### Mnemonic Phrases
            - ⚠️ **User Responsibility**: Mnemonics are shown ONLY ONCE during wallet creation
            - ⚠️ **No Recovery**: Service does NOT store mnemonic phrases
            - ⚠️ **Backup Critical**: Users must securely backup their 12-24 word phrases
            - ✅ **Industry Standard**: Compatible with BIP39 wallets (MetaMask, Ledger, etc.)

            ## Getting Started

            ### 1. Create a New Wallet
            ```http
            POST /api/wallets
            Authorization: Bearer {token}
            Content-Type: application/json

            {
              "name": "My Primary Wallet",
              "algorithm": "ED25519",
              "wordCount": 12,
              "passphrase": ""
            }
            ```

            **Response:**
            ```json
            {
              "walletId": "wallet-abc123",
              "publicAddress": "0x1234567890abcdef...",
              "algorithm": "ED25519",
              "mnemonicWords": ["word1", "word2", ..., "word12"],
              "createdAt": "2025-12-11T10:30:00Z"
            }
            ```

            ⚠️ **CRITICAL**: Save the `mnemonicWords` immediately - they will never be shown again!

            ### 2. Recover a Wallet
            ```http
            POST /api/wallets/recover
            Authorization: Bearer {token}
            Content-Type: application/json

            {
              "name": "Recovered Wallet",
              "mnemonicWords": ["word1", "word2", ..., "word12"],
              "algorithm": "ED25519"
            }
            ```

            ### 3. Sign a Transaction
            ```http
            POST /api/wallets/{walletId}/sign
            Authorization: Bearer {token}
            Content-Type: application/json

            {
              "data": "base64-encoded-transaction-data"
            }
            ```

            **Response:**
            ```json
            {
              "signature": "base64-encoded-signature",
              "publicKey": "base64-encoded-public-key",
              "algorithm": "ED25519"
            }
            ```

            ## Wallet Lifecycle

            1. **Create** → Generate new wallet with mnemonic
            2. **Backup** → User saves mnemonic phrase securely
            3. **Sign** → Use wallet to sign transactions
            4. **Recover** → Restore from mnemonic if needed
            5. **Delete** → Remove wallet (cannot be recovered without mnemonic)

            ## Security Best Practices

            - ✅ Always backup mnemonic phrases in a secure location (offline preferred)
            - ✅ Use strong passphrases for additional security
            - ✅ Never share private keys or mnemonics
            - ✅ Verify signatures before submitting to ledger
            - ✅ Use HSM/Key Vault for production environments
            - ✅ Rotate API tokens regularly
            - ✅ Enable audit logging for all wallet operations

            ## Integration with Sorcha Platform

            ### Transaction Flow
            1. **Blueprint Service** creates transaction payload
            2. **Wallet Service** signs payload with user's wallet
            3. **Register Service** verifies signature and stores transaction

            ### Key Management
            - Each organization can have multiple wallets
            - Wallets can be associated with specific blueprints
            - Support for multi-signature workflows (future)

            ## Target Audience

            - **Application Developers**: Integrating wallet functionality
            - **CLI Users**: Managing wallets via command-line tools
            - **System Administrators**: Configuring key storage backends
            - **Security Teams**: Auditing cryptographic operations

            ## Related Services

            - **Tenant Service**: Wallet ownership and access control
            - **Blueprint Service**: Uses wallets to sign workflow transactions
            - **Register Service**: Verifies wallet signatures on transactions
            - **Cryptography Library**: Underlying crypto operations (ED25519, P-256, RSA)
            """;

        if (document.Info.Contact == null)
        {
            document.Info.Contact = new() { };
        }
        document.Info.Contact.Name = "Sorcha Platform Team";
        document.Info.Contact.Url = new Uri("https://github.com/siccar-platform/sorcha");

        if (document.Info.License == null)
        {
            document.Info.License = new() { };
        }
        document.Info.License.Name = "MIT License";
        document.Info.License.Url = new Uri("https://opensource.org/licenses/MIT");

        return Task.CompletedTask;
    });
});

// Add Wallet Service health checks
builder.Services.AddHealthChecks()
    .AddWalletServiceHealthChecks(builder.Configuration);

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

// Add JWT authentication and authorization (AUTH-002)
// JWT authentication is now configured via shared ServiceDefaults with auto-key generation
builder.AddJwtAuthentication();
builder.Services.AddWalletAuthorization();

var app = builder.Build();

// Apply database migrations automatically (only if PostgreSQL is configured)
await app.Services.ApplyWalletDatabaseMigrationsAsync();

// Map default Aspire endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Add OWASP security headers (SEC-004)
app.UseApiSecurityHeaders();

// Configure OpenAPI (available in all environments for API consumers)
app.MapOpenApi();

// Configure Scalar API documentation UI (development only)
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Wallet Service")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseCors("DevelopmentPolicy");
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware (AUTH-002)
app.UseAuthentication();
app.UseAuthorization();

// Map Wallet API endpoints
app.MapWalletEndpoints();
app.MapDelegationEndpoints();

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
