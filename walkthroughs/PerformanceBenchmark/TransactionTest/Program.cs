// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Spectre.Console;

namespace TransactionTest;

class Program
{
    private static readonly HttpClient _httpClient = new();
    private static string? _token;
    private static string? _registerId;
    private static string? _walletAddress;

    static async Task<int> Main(string[] args)
    {
        // Check if running performance tests
        if (args.Length > 0 && args[0] == "--performance")
        {
            return await PerformanceRunner.RunPerformanceTestsAsync(args.Skip(1).ToArray());
        }

        var gatewayUrl = args.Length > 0 ? args[0] : "http://localhost";

        AnsiConsole.Write(new FigletText("Transaction Test").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[yellow]Testing transaction submission via {gatewayUrl}[/]");
        AnsiConsole.MarkupLine($"[dim]Tip: Use --performance for full performance suite[/]");
        AnsiConsole.WriteLine();

        try
        {
            // Step 1: Authenticate
            await AnsiConsole.Status()
                .StartAsync("Authenticating...", async ctx =>
                {
                    _token = await AuthenticateAsync(gatewayUrl);
                });
            AnsiConsole.MarkupLine("[green]✓[/] Authenticated");

            // Step 2: Create wallet
            await AnsiConsole.Status()
                .StartAsync("Creating wallet...", async ctx =>
                {
                    _walletAddress = await CreateWalletAsync(gatewayUrl);
                });
            AnsiConsole.MarkupLine($"[green]✓[/] Wallet created: [cyan]{_walletAddress}[/]");

            // Step 3: Create register
            await AnsiConsole.Status()
                .StartAsync("Creating register...", async ctx =>
                {
                    _registerId = await CreateRegisterAsync(gatewayUrl, _walletAddress!);
                });
            AnsiConsole.MarkupLine($"[green]✓[/] Register created: [cyan]{_registerId}[/]");

            // Step 4: Submit test transaction
            var success = await SubmitTestTransactionAsync(gatewayUrl, _registerId!, _walletAddress!);

            if (success)
            {
                AnsiConsole.MarkupLine("\n[green bold]✓ All tests passed![/]");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine("\n[red bold]✗ Transaction submission failed[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]Error:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    static async Task<string> AuthenticateAsync(string baseUrl)
    {
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "admin@perf.local",
            ["password"] = "PerfTest2026!",
            ["client_id"] = "sorcha-cli"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/service-auth/token")
        {
            Content = new FormUrlEncodedContent(formData)
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    static async Task<string> CreateWalletAsync(string baseUrl)
    {
        var createRequest = new
        {
            name = "Performance Test Wallet",
            algorithm = "ED25519",
            wordCount = 12
        };

        var json = JsonSerializer.Serialize(createRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/wallets")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        var response = await _httpClient.SendAsync(request);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Wallet creation failed ({response.StatusCode}): {responseJson}");
        }

        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("wallet").GetProperty("address").GetString()!;
    }

    static async Task<string> CreateRegisterAsync(string baseUrl, string walletAddress)
    {
        // Step 1: Initiate register creation
        var initiateRequest = new
        {
            name = "Performance Test Register",
            description = "Testing transaction submission",
            tenantId = "00000000-0000-0000-0000-000000000000",
            advertise = false,
            owners = new[]
            {
                new
                {
                    userId = "perf-admin",
                    walletId = walletAddress
                }
            }
        };

        var json = JsonSerializer.Serialize(initiateRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/registers/initiate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        var response = await _httpClient.SendAsync(request);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Register initiation failed ({response.StatusCode}): {responseJson}");
        }

        Console.WriteLine($"Initiate response: {responseJson}");

        var doc = JsonDocument.Parse(responseJson);
        var registerId = doc.RootElement.GetProperty("registerId").GetString()!;

        // Try to get nonce if it exists
        string? nonce = doc.RootElement.TryGetProperty("nonce", out var nonceElement)
            ? nonceElement.GetString()
            : null;

        var attestationsToSign = doc.RootElement.GetProperty("attestationsToSign");

        // Step 2: Sign attestations (if any)
        var signedAttestations = new List<object>();

        foreach (var att in attestationsToSign.EnumerateArray())
        {
            // Get the dataToSign (hex-encoded hash)
            var dataToSignHex = att.GetProperty("dataToSign").GetString()!;

            // Convert hex to bytes then to base64
            var hashBytes = Convert.FromHexString(dataToSignHex);
            var dataToSignBase64 = Convert.ToBase64String(hashBytes);

            // Sign the attestation
            var signRequest = new
            {
                transactionData = dataToSignBase64,
                isPreHashed = true
            };

            json = JsonSerializer.Serialize(signRequest);
            request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/wallets/{walletAddress}/sign")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                Headers = { { "Authorization", $"Bearer {_token}" } }
            };

            response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            responseJson = await response.Content.ReadAsStringAsync();
            var signDoc = JsonDocument.Parse(responseJson);

            // Build signed attestation with the original attestationData object
            var attestationData = att.GetProperty("attestationData");
            signedAttestations.Add(new
            {
                attestationData = JsonSerializer.Deserialize<object>(attestationData.GetRawText()),
                publicKey = signDoc.RootElement.GetProperty("publicKey").GetString(),
                signature = signDoc.RootElement.GetProperty("signature").GetString(),
                algorithm = "ED25519"
            });
        }

        Console.WriteLine($"Signed {signedAttestations.Count} attestations");

        // Step 3: Finalize register creation
        var finalizeData = new Dictionary<string, object>
        {
            ["registerId"] = registerId,
            ["signedAttestations"] = signedAttestations
        };

        if (nonce != null)
        {
            finalizeData["nonce"] = nonce;
        }

        json = JsonSerializer.Serialize(finalizeData);
        request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/registers/finalize")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        Console.WriteLine($"Finalize request body: {json}");

        response = await _httpClient.SendAsync(request);
        responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Finalize failed ({response.StatusCode}): {responseJson}");
            throw new Exception($"Register finalization failed ({response.StatusCode}): {responseJson}");
        }

        Console.WriteLine($"Finalize response: {responseJson}");

        return registerId;
    }

    static async Task<bool> SubmitTestTransactionAsync(string baseUrl, string registerId, string walletAddress)
    {
        var table = new Table()
            .BorderColor(Color.Grey)
            .AddColumn("Step")
            .AddColumn("Status");

        // Create test payload with explicit timestamp string
        // CRITICAL: Must use exact same JSON string for hashing and sending
        var timestampString = DateTimeOffset.UtcNow.ToString("o");
        var payload = new Dictionary<string, object>
        {
            ["testData"] = "HELLO WORLD",
            ["timestamp"] = timestampString,  // Use string, not DateTimeOffset
            ["sequence"] = 1
        };

        // Serialize payload using System.Text.Json (same as the service)
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        Console.WriteLine($"DEBUG: Payload JSON for hashing: {payloadJson}");

        table.AddRow("Payload created", $"[green]{payloadJson.Length} bytes[/]");
        AnsiConsole.Write(table);

        // Compute payload hash
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var hashBytes = SHA256.HashData(payloadBytes);
        var payloadHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        table.AddRow("Payload hash", $"[cyan]{payloadHash}[/]");
        AnsiConsole.Write(table);

        // Generate transaction ID
        var txIdSource = $"{registerId}-{DateTimeOffset.UtcNow:o}-{Guid.NewGuid()}";
        var txIdBytes = SHA256.HashData(Encoding.UTF8.GetBytes(txIdSource));
        var transactionId = Convert.ToHexString(txIdBytes).ToLowerInvariant();

        table.AddRow("Transaction ID", $"[cyan]{transactionId}[/]");
        AnsiConsole.Write(table);

        // Sign transaction ID
        var signRequest = new
        {
            transactionData = Convert.ToBase64String(txIdBytes),
            isPreHashed = true
        };

        var json = JsonSerializer.Serialize(signRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/wallets/{walletAddress}/sign")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var signDoc = JsonDocument.Parse(responseJson);

        table.AddRow("Signature", $"[green]✓ Generated[/]");
        AnsiConsole.Write(table);

        // Build validator service request
        var payloadElement = JsonSerializer.Deserialize<JsonElement>(payloadJson);

        var validateRequest = new
        {
            transactionId = transactionId,
            registerId = registerId,
            blueprintId = "performance-test-v1",
            actionId = "1",
            payload = payloadElement,
            payloadHash = payloadHash,
            signatures = new[]
            {
                new
                {
                    publicKey = signDoc.RootElement.GetProperty("publicKey").GetString(),
                    signatureValue = signDoc.RootElement.GetProperty("signature").GetString(),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow,
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            priority = 1,
            metadata = new Dictionary<string, string>
            {
                ["source"] = "transaction-test"
            }
        };

        json = JsonSerializer.Serialize(validateRequest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Submitting to validator service...[/]");

        // Submit to validator
        request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/validator/transactions/validate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        try
        {
            response = await _httpClient.SendAsync(request);

            var statusCode = (int)response.StatusCode;
            responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                table.AddRow("Validation", $"[green]✓ Success (HTTP {statusCode})[/]");
                AnsiConsole.Write(table);

                AnsiConsole.WriteLine();
                var panel = new Panel(responseJson)
                    .Header("Response")
                    .BorderColor(Color.Green);
                AnsiConsole.Write(panel);

                return true;
            }
            else
            {
                table.AddRow("Validation", $"[red]✗ Failed (HTTP {statusCode})[/]");
                AnsiConsole.Write(table);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]Error Response:[/]");
                Console.WriteLine(responseJson);

                return false;
            }
        }
        catch (Exception ex)
        {
            table.AddRow("Validation", $"[red]✗ Exception: {ex.Message}[/]");
            AnsiConsole.Write(table);
            throw;
        }
    }
}
