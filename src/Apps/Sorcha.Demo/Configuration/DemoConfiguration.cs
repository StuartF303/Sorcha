// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Demo.Configuration;

/// <summary>
/// Configuration for Sorcha API endpoints
/// </summary>
public class SorchaApiConfiguration
{
    public string BaseUrl { get; set; } = "https://localhost:7082";
    public string? WalletServiceUrl { get; set; }
    public string? BlueprintServiceUrl { get; set; }
    public string? RegisterServiceUrl { get; set; }
    public string? TenantServiceUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the effective Wallet Service URL (specific or via gateway)
    /// </summary>
    public string GetWalletServiceUrl() =>
        !string.IsNullOrWhiteSpace(WalletServiceUrl)
            ? WalletServiceUrl
            : $"{BaseUrl.TrimEnd('/')}/api/wallet";

    /// <summary>
    /// Gets the effective Blueprint Service URL (specific or via gateway)
    /// </summary>
    public string GetBlueprintServiceUrl() =>
        !string.IsNullOrWhiteSpace(BlueprintServiceUrl)
            ? BlueprintServiceUrl
            : $"{BaseUrl.TrimEnd('/')}/api/blueprint";

    /// <summary>
    /// Gets the effective Register Service URL (specific or via gateway)
    /// </summary>
    public string GetRegisterServiceUrl() =>
        !string.IsNullOrWhiteSpace(RegisterServiceUrl)
            ? RegisterServiceUrl
            : $"{BaseUrl.TrimEnd('/')}/api/register";

    /// <summary>
    /// Gets the effective Tenant Service URL (specific or via gateway)
    /// </summary>
    public string GetTenantServiceUrl() =>
        !string.IsNullOrWhiteSpace(TenantServiceUrl)
            ? TenantServiceUrl
            : $"{BaseUrl.TrimEnd('/')}/api/tenant";
}

/// <summary>
/// Demo application configuration
/// </summary>
public class DemoAppConfiguration
{
    public string WalletStoragePath { get; set; } = "~/.sorcha/demo-wallets.json";
    public string DefaultMode { get; set; } = "Interactive";
    public string DefaultAlgorithm { get; set; } = "ED25519";

    /// <summary>
    /// Gets the expanded wallet storage path with home directory resolved
    /// </summary>
    public string GetExpandedWalletStoragePath()
    {
        var path = WalletStoragePath;
        if (path.StartsWith("~/"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[2..]);
        }
        return path;
    }
}
