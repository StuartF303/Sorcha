using System.CommandLine;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Authentication management commands.
/// </summary>
public class AuthCommand : Command
{
    public AuthCommand(
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("auth", "Manage authentication and login sessions")
    {
        AddCommand(new AuthLoginCommand(authService, configService));
        AddCommand(new AuthLogoutCommand(authService, configService));
        AddCommand(new AuthStatusCommand(authService, configService));
    }
}

/// <summary>
/// Login command for user and service principal authentication.
/// </summary>
public class AuthLoginCommand : Command
{
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;

    public AuthLoginCommand(
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("login", "Authenticate as a user or service principal")
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        var usernameOption = new Option<string?>(
            aliases: new[] { "--username", "-u" },
            description: "Username for user authentication");

        var passwordOption = new Option<string?>(
            aliases: new[] { "--password", "-p" },
            description: "Password for user authentication (INSECURE - use interactive mode instead)");

        var clientIdOption = new Option<string?>(
            aliases: new[] { "--client-id", "-c" },
            description: "Client ID for service principal authentication");

        var clientSecretOption = new Option<string?>(
            aliases: new[] { "--client-secret", "-s" },
            description: "Client secret for service principal authentication (INSECURE - use interactive mode instead)");

        var interactiveOption = new Option<bool>(
            aliases: new[] { "--interactive", "-i" },
            getDefaultValue: () => true,
            description: "Use interactive login (prompts for credentials securely)");

        var profileOption = new Option<string?>(
            aliases: new[] { "--profile" },
            description: "Profile to authenticate with (defaults to active profile)");

        AddOption(usernameOption);
        AddOption(passwordOption);
        AddOption(clientIdOption);
        AddOption(clientSecretOption);
        AddOption(interactiveOption);
        AddOption(profileOption);

        this.SetHandler(async (username, password, clientId, clientSecret, interactive, profileName) =>
        {
            try
            {
                // Determine profile
                if (string.IsNullOrEmpty(profileName))
                {
                    var activeProfile = await _configService.GetActiveProfileAsync();
                    profileName = activeProfile?.Name ?? "dev";
                }

                ConsoleHelper.WriteInfo($"Authenticating with profile: {profileName}");

                // Determine authentication type
                bool isServicePrincipal = !string.IsNullOrEmpty(clientId) || !string.IsNullOrEmpty(clientSecret);

                if (isServicePrincipal)
                {
                    await LoginServicePrincipalAsync(clientId, clientSecret, interactive, profileName);
                }
                else
                {
                    await LoginUserAsync(username, password, interactive, profileName);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Authentication failed: {ex.Message}");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
        }, usernameOption, passwordOption, clientIdOption, clientSecretOption, interactiveOption, profileOption);
    }

    private async Task LoginUserAsync(string? username, string? password, bool interactive, string profileName)
    {
        // Get credentials
        if (interactive || string.IsNullOrEmpty(username))
        {
            ConsoleHelper.WriteWarning("Interactive User Login");
            username = ConsoleHelper.ReadLine("Username", username);
        }

        if (string.IsNullOrEmpty(username))
        {
            throw new InvalidOperationException("Username is required.");
        }

        if (interactive || string.IsNullOrEmpty(password))
        {
            if (!string.IsNullOrEmpty(password))
            {
                ConsoleHelper.WriteWarning("⚠ Password provided as command-line argument is INSECURE!");
                ConsoleHelper.WriteWarning("  Use interactive mode to avoid exposing credentials in process list.");
            }
            password = ConsoleHelper.ReadPassword("Password: ");
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Password is required.");
        }

        // Authenticate
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        ConsoleHelper.WriteInfo($"Authenticating user: {username}");
        var response = await _authService.LoginAsync(request, profileName);

        ConsoleHelper.WriteSuccess("Authentication successful!");
        ConsoleHelper.WriteInfo($"Access token expires in {response.ExpiresIn} seconds");

        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            ConsoleHelper.WriteInfo("Refresh token received (will auto-refresh)");
        }
    }

    private async Task LoginServicePrincipalAsync(string? clientId, string? clientSecret, bool interactive, string profileName)
    {
        // Get credentials
        if (interactive || string.IsNullOrEmpty(clientId))
        {
            ConsoleHelper.WriteWarning("Interactive Service Principal Login");
            clientId = ConsoleHelper.ReadLine("Client ID", clientId);
        }

        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("Client ID is required.");
        }

        if (interactive || string.IsNullOrEmpty(clientSecret))
        {
            if (!string.IsNullOrEmpty(clientSecret))
            {
                ConsoleHelper.WriteWarning("⚠ Client secret provided as command-line argument is INSECURE!");
                ConsoleHelper.WriteWarning("  Use interactive mode to avoid exposing credentials in process list.");
            }
            clientSecret = ConsoleHelper.ReadPassword("Client Secret: ");
        }

        if (string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Client secret is required.");
        }

        // Authenticate
        var request = new ServicePrincipalLoginRequest
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        ConsoleHelper.WriteInfo($"Authenticating service principal: {clientId}");
        var response = await _authService.LoginServicePrincipalAsync(request, profileName);

        ConsoleHelper.WriteSuccess("Service principal authentication successful!");
        ConsoleHelper.WriteInfo($"Access token expires in {response.ExpiresIn} seconds ({response.ExpiresIn / 3600.0:F1} hours)");
    }
}

/// <summary>
/// Logout command to clear cached authentication tokens.
/// </summary>
public class AuthLogoutCommand : Command
{
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;

    public AuthLogoutCommand(
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("logout", "Clear cached authentication tokens")
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        var allOption = new Option<bool>(
            aliases: new[] { "--all", "-a" },
            description: "Clear tokens for all profiles (default: current profile only)");

        var profileOption = new Option<string?>(
            aliases: new[] { "--profile" },
            description: "Profile to logout from (defaults to active profile)");

        AddOption(allOption);
        AddOption(profileOption);

        this.SetHandler(async (all, profileName) =>
        {
            try
            {
                if (all)
                {
                    if (ConsoleHelper.Confirm("Clear authentication tokens for ALL profiles?", defaultYes: false))
                    {
                        await _authService.LogoutAllAsync();
                        ConsoleHelper.WriteSuccess("Logged out from all profiles");
                    }
                    else
                    {
                        ConsoleHelper.WriteInfo("Logout cancelled");
                    }
                }
                else
                {
                    // Determine profile
                    if (string.IsNullOrEmpty(profileName))
                    {
                        var activeProfile = await _configService.GetActiveProfileAsync();
                        profileName = activeProfile?.Name ?? "dev";
                    }

                    await _authService.LogoutAsync(profileName);
                    ConsoleHelper.WriteSuccess($"Logged out from profile: {profileName}");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Logout failed: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, allOption, profileOption);
    }
}

/// <summary>
/// Status command to check authentication status.
/// </summary>
public class AuthStatusCommand : Command
{
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;

    public AuthStatusCommand(
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("status", "Check authentication status for the current profile")
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        var profileOption = new Option<string?>(
            aliases: new[] { "--profile" },
            description: "Profile to check status for (defaults to active profile)");

        AddOption(profileOption);

        this.SetHandler(async (profileName) =>
        {
            try
            {
                // Determine profile
                if (string.IsNullOrEmpty(profileName))
                {
                    var activeProfile = await _configService.GetActiveProfileAsync();
                    profileName = activeProfile?.Name ?? "dev";
                }

                var isAuthenticated = await _authService.IsAuthenticatedAsync(profileName);
                var token = await _authService.GetAccessTokenAsync(profileName);

                Console.WriteLine();
                Console.WriteLine($"Profile: {profileName}");
                Console.WriteLine($"Status: {(isAuthenticated ? "Authenticated ✓" : "Not authenticated ✗")}");

                if (isAuthenticated && !string.IsNullOrEmpty(token))
                {
                    // Try to decode token expiration (JWT)
                    try
                    {
                        var parts = token.Split('.');
                        if (parts.Length == 3)
                        {
                            var payload = System.Text.Json.JsonDocument.Parse(
                                Convert.FromBase64String(PadBase64(parts[1])));

                            if (payload.RootElement.TryGetProperty("exp", out var expElement))
                            {
                                var exp = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
                                var remaining = exp - DateTimeOffset.UtcNow;

                                if (remaining.TotalSeconds > 0)
                                {
                                    Console.WriteLine($"Token expires: {exp:u} ({FormatTimeRemaining(remaining)} remaining)");
                                }
                                else
                                {
                                    ConsoleHelper.WriteWarning("Token has expired! Re-authentication recommended.");
                                }
                            }

                            if (payload.RootElement.TryGetProperty("sub", out var subElement))
                            {
                                Console.WriteLine($"Subject: {subElement.GetString()}");
                            }

                            if (payload.RootElement.TryGetProperty("token_type", out var typeElement))
                            {
                                Console.WriteLine($"Type: {typeElement.GetString()}");
                            }
                        }
                    }
                    catch
                    {
                        // Not a valid JWT or can't decode - that's okay
                    }
                }
                else
                {
                    ConsoleHelper.WriteInfo($"Run 'sorcha auth login --profile {profileName}' to authenticate");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to check status: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, profileOption);
    }

    private static string PadBase64(string base64)
    {
        // JWT uses base64url encoding without padding
        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 += new string('=', 4 - padding);
        }
        return base64.Replace('-', '+').Replace('_', '/');
    }

    private static string FormatTimeRemaining(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
        {
            return $"{remaining.TotalDays:F1} days";
        }
        if (remaining.TotalHours >= 1)
        {
            return $"{remaining.TotalHours:F1} hours";
        }
        if (remaining.TotalMinutes >= 1)
        {
            return $"{remaining.TotalMinutes:F0} minutes";
        }
        return $"{remaining.TotalSeconds:F0} seconds";
    }
}
