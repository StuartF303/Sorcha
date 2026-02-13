// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace TransactionTest;

public class PerformanceRunner
{
    public static async Task<int> RunPerformanceTestsAsync(string[] args)
    {
        var baseUrl = args.Length > 0 ? args[0] : "http://localhost";

        AnsiConsole.Write(new FigletText("Performance Tests").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[yellow]Testing transaction performance via {baseUrl}[/]");
        AnsiConsole.WriteLine();

        var httpClient = new HttpClient();
        string? token = null;
        string? walletAddress = null;
        string? registerId = null;

        try
        {
            // Setup: Authenticate, create wallet, create register
            await AnsiConsole.Status()
                .StartAsync("Setting up test environment...", async ctx =>
                {
                    ctx.Status("Authenticating...");
                    token = await AuthenticateAsync(httpClient, baseUrl);

                    ctx.Status("Creating wallet...");
                    walletAddress = await CreateWalletAsync(httpClient, baseUrl, token);

                    ctx.Status("Creating register...");
                    registerId = await CreateRegisterAsync(httpClient, baseUrl, token, walletAddress);
                });

            AnsiConsole.MarkupLine("[green]✓[/] Test environment ready");
            AnsiConsole.MarkupLine($"  Register: [cyan]{registerId}[/]");
            AnsiConsole.MarkupLine($"  Wallet: [cyan]{walletAddress}[/]");

            var perfTests = new PerformanceTests(httpClient, baseUrl, token!, registerId!, walletAddress!);

            // Run all performance tests
            var results = new PerformanceTestResults();

            // 1. Latency Test
            results.Latency = await perfTests.RunLatencyTestAsync(100);

            // 2. Throughput Test
            results.Throughput = await perfTests.RunThroughputTestAsync(30);

            // 3. Concurrency Test
            results.Concurrency = await perfTests.RunConcurrencyTestAsync(50);

            // 4. Payload Size Test
            results.PayloadSize = await perfTests.RunPayloadSizeTestAsync();

            // Generate report
            GenerateReport(results, baseUrl);

            // Save report to file
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var reportFile = $"performance-report-{timestamp}.json";
            await File.WriteAllTextAsync(reportFile, JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            AnsiConsole.MarkupLine($"\n[green]✓ Performance tests complete![/]");
            AnsiConsole.MarkupLine($"  Report saved to: [cyan]{reportFile}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static void GenerateReport(PerformanceTestResults results, string baseUrl)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Performance Test Results[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        // Latency Results
        var latencyTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("Metric")
            .AddColumn(new TableColumn("Value").RightAligned());

        latencyTable.AddRow("Total Transactions", results.Latency.TotalTransactions.ToString());
        latencyTable.AddRow("Success Rate", $"{(results.Latency.SuccessCount * 100.0 / results.Latency.TotalTransactions):F1}%");
        latencyTable.AddRow("Min Latency", $"{results.Latency.Min:F1}ms");
        latencyTable.AddRow("P50 Latency", $"{results.Latency.P50:F1}ms");
        latencyTable.AddRow("P75 Latency", $"{results.Latency.P75:F1}ms");
        latencyTable.AddRow("P90 Latency", $"{results.Latency.P90:F1}ms");
        latencyTable.AddRow("P95 Latency", $"{results.Latency.P95:F1}ms");
        latencyTable.AddRow("P99 Latency", $"{results.Latency.P99:F1}ms");
        latencyTable.AddRow("Max Latency", $"{results.Latency.Max:F1}ms");
        latencyTable.AddRow("Mean Latency", $"{results.Latency.Mean:F1}ms");
        latencyTable.AddRow("Std Dev", $"{results.Latency.StdDev:F1}ms");

        AnsiConsole.Write(new Panel(latencyTable)
            .Header("[yellow]Latency Distribution[/]")
            .BorderColor(Color.Yellow));

        // Throughput Results
        var throughputTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("Metric")
            .AddColumn(new TableColumn("Value").RightAligned());

        throughputTable.AddRow("Duration", $"{results.Throughput.DurationSeconds:F1}s");
        throughputTable.AddRow("Total Transactions", results.Throughput.TotalTransactions.ToString());
        throughputTable.AddRow("Success Count", results.Throughput.SuccessCount.ToString());
        throughputTable.AddRow("Failure Count", results.Throughput.FailureCount.ToString());
        throughputTable.AddRow("[green]Transactions/Sec[/]", $"[green]{results.Throughput.TransactionsPerSecond:F2} TPS[/]");
        throughputTable.AddRow("P50 Latency", $"{results.Throughput.LatencyP50:F1}ms");
        throughputTable.AddRow("P95 Latency", $"{results.Throughput.LatencyP95:F1}ms");
        throughputTable.AddRow("P99 Latency", $"{results.Throughput.LatencyP99:F1}ms");

        AnsiConsole.Write(new Panel(throughputTable)
            .Header("[yellow]Throughput (30s sustained)[/]")
            .BorderColor(Color.Yellow));

        // Concurrency Results
        var concurrencyChart = new BarChart()
            .Width(60)
            .Label("[yellow]Concurrency Scaling (TPS)[/]");

        foreach (var (concurrency, tps, _) in results.Concurrency.Results)
        {
            concurrencyChart.AddItem($"{concurrency} concurrent", tps, Color.Green);
        }

        AnsiConsole.Write(concurrencyChart);
        AnsiConsole.WriteLine();

        // Payload Size Results
        var payloadTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("Payload Size")
            .AddColumn(new TableColumn("Avg Latency").RightAligned())
            .AddColumn(new TableColumn("Throughput").RightAligned());

        foreach (var (size, latency, throughput) in results.PayloadSize.Results)
        {
            payloadTable.AddRow(
                FormatBytes(size),
                $"{latency:F1}ms",
                $"{throughput:F1} TPS"
            );
        }

        AnsiConsole.Write(new Panel(payloadTable)
            .Header("[yellow]Payload Size Impact[/]")
            .BorderColor(Color.Yellow));

        // Summary
        AnsiConsole.WriteLine();
        var summaryTable = new Table()
            .Border(TableBorder.Heavy)
            .BorderColor(Color.Green)
            .AddColumn("[bold]Summary[/]")
            .AddColumn(new TableColumn("[bold]Result[/]").RightAligned());

        summaryTable.AddRow("Peak Throughput", $"[green]{results.Throughput.TransactionsPerSecond:F2} TPS[/]");
        summaryTable.AddRow("P50 Latency", $"{results.Latency.P50:F1}ms");
        summaryTable.AddRow("P95 Latency", $"{results.Latency.P95:F1}ms");
        summaryTable.AddRow("P99 Latency", $"{results.Latency.P99:F1}ms");
        summaryTable.AddRow("Success Rate", $"{(results.Latency.SuccessCount * 100.0 / results.Latency.TotalTransactions):F1}%");

        AnsiConsole.Write(summaryTable);
    }

    private static async Task<string> AuthenticateAsync(HttpClient httpClient, string baseUrl)
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

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    private static async Task<string> CreateWalletAsync(HttpClient httpClient, string baseUrl, string token)
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
            Headers = { { "Authorization", $"Bearer {token}" } }
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("wallet").GetProperty("address").GetString()!;
    }

    private static async Task<string> CreateRegisterAsync(HttpClient httpClient, string baseUrl, string token, string walletAddress)
    {
        var initiateRequest = new
        {
            name = "Performance Test Register",
            description = "Testing transaction performance",
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
            Headers = { { "Authorization", $"Bearer {token}" } }
        };

        var response = await httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Register initiation failed: {responseJson}");
        }

        var doc = JsonDocument.Parse(responseJson);
        var registerId = doc.RootElement.GetProperty("registerId").GetString()!;
        string? nonce = doc.RootElement.TryGetProperty("nonce", out var nonceElement)
            ? nonceElement.GetString()
            : null;

        var attestationsToSign = doc.RootElement.GetProperty("attestationsToSign");

        var signedAttestations = new List<object>();

        foreach (var att in attestationsToSign.EnumerateArray())
        {
            var dataToSignHex = att.GetProperty("dataToSign").GetString()!;
            var hashBytes = Convert.FromHexString(dataToSignHex);
            var dataToSignBase64 = Convert.ToBase64String(hashBytes);

            var signRequest = new
            {
                transactionData = dataToSignBase64,
                isPreHashed = true
            };

            json = JsonSerializer.Serialize(signRequest);
            request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/wallets/{walletAddress}/sign")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            responseJson = await response.Content.ReadAsStringAsync();
            var signDoc = JsonDocument.Parse(responseJson);

            var attestationData = att.GetProperty("attestationData");
            signedAttestations.Add(new
            {
                attestationData = JsonSerializer.Deserialize<object>(attestationData.GetRawText()),
                publicKey = signDoc.RootElement.GetProperty("publicKey").GetString(),
                signature = signDoc.RootElement.GetProperty("signature").GetString(),
                algorithm = "ED25519"
            });
        }

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
            Headers = { { "Authorization", $"Bearer {token}" } }
        };

        response = await httpClient.SendAsync(request);
        responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Register finalization failed: {responseJson}");
        }

        return registerId;
    }

    private static string FormatBytes(int bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024}KB";
        return $"{bytes / (1024 * 1024)}MB";
    }
}

public record PerformanceTestResults
{
    public LatencyResult Latency { get; set; } = new() { TotalTransactions = 0, SuccessCount = 0 };
    public ThroughputResult Throughput { get; set; } = new() { TotalTransactions = 0, SuccessCount = 0 };
    public ConcurrencyResult Concurrency { get; set; } = new();
    public PayloadSizeResult PayloadSize { get; set; } = new();
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    public string TestEnvironment { get; set; } = Environment.MachineName;
}
