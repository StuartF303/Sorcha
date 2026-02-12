// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.IO.Compression;

namespace Sorcha.ApiGateway.Services;

/// <summary>
/// Service for packaging and serving the Blazor client application
/// </summary>
public class ClientDownloadService
{
    private readonly ILogger<ClientDownloadService> _logger;
    private readonly IConfiguration _configuration;

    public ClientDownloadService(
        ILogger<ClientDownloadService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the client installation instructions
    /// </summary>
    public string GetInstallationInstructions()
    {
        var gatewayUrl = _configuration["Gateway:PublicUrl"] ?? "https://localhost:7082";

        return $@"# Sorcha Client Installation Instructions

## Prerequisites
- .NET 10.0 SDK or later
- Modern web browser (Chrome, Firefox, Edge, Safari)

## Option 1: Run via dotnet (Development)
1. Download and extract the client package
2. Navigate to the extracted folder
3. Run: `dotnet run`
4. Open your browser to the displayed URL

## Option 2: Deploy to Web Server (Production)
1. Download and extract the client package
2. Publish the application: `dotnet publish -c Release`
3. Deploy the contents of `bin/Release/net10.0/publish/wwwroot` to your web server
4. Configure the web server to serve static files
5. Update the API Gateway URL in the configuration

## Configuration
Update the API Gateway base URL in `Services/ApiConfiguration.cs`:
```csharp
public static string GatewayBaseUrl {{ get; set; }} = ""{gatewayUrl}"";
```

## Support
For issues and documentation, visit: https://github.com/yourusername/sorcha
";
    }

    /// <summary>
    /// Creates a ZIP package of the client application source code
    /// </summary>
    public async Task<byte[]> CreateClientPackageAsync(string clientProjectPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(clientProjectPath))
        {
            _logger.LogWarning("Client project path not found: {Path}", clientProjectPath);
            throw new DirectoryNotFoundException($"Client project not found at: {clientProjectPath}");
        }

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            await AddDirectoryToArchiveAsync(archive, clientProjectPath, "", cancellationToken);
        }

        return memoryStream.ToArray();
    }

    private async Task AddDirectoryToArchiveAsync(
        ZipArchive archive,
        string sourcePath,
        string entryPrefix,
        CancellationToken cancellationToken)
    {
        var directory = new DirectoryInfo(sourcePath);

        // Skip bin, obj, and hidden directories
        if (directory.Name == "bin" || directory.Name == "obj" || directory.Name.StartsWith("."))
            return;

        // Add files
        foreach (var file in directory.GetFiles())
        {
            // Skip user-specific and build files
            if (file.Name.EndsWith(".user") || file.Name.EndsWith(".suo"))
                continue;

            var entryName = Path.Combine(entryPrefix, file.Name).Replace("\\", "/");
            var entry = archive.CreateEntry(entryName);
            entry.LastWriteTime = file.LastWriteTime;

            using var fileStream = file.OpenRead();
            using var entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }

        // Add subdirectories recursively
        foreach (var subDir in directory.GetDirectories())
        {
            var newPrefix = Path.Combine(entryPrefix, subDir.Name);
            await AddDirectoryToArchiveAsync(archive, subDir.FullName, newPrefix, cancellationToken);
        }
    }

    /// <summary>
    /// Gets information about the client application
    /// </summary>
    public ClientInfo GetClientInfo()
    {
        return new ClientInfo
        {
            Name = "Sorcha Blueprint Designer",
            Version = "1.0.0",
            Description = "Blazor WebAssembly client for designing and managing blockchain blueprints",
            Framework = ".NET 10.0",
            Type = "Blazor WebAssembly",
            License = "MIT"
        };
    }
}

/// <summary>
/// Information about the client application
/// </summary>
public class ClientInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
}
