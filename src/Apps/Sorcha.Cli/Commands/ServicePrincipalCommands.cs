using System.CommandLine;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Service principal management commands.
/// </summary>
public class ServicePrincipalCommand : Command
{
    public ServicePrincipalCommand()
        : base("principal", "Manage service principals within organizations")
    {
        AddCommand(new PrincipalListCommand());
        AddCommand(new PrincipalGetCommand());
        AddCommand(new PrincipalCreateCommand());
        AddCommand(new PrincipalDeleteCommand());
        AddCommand(new PrincipalRotateSecretCommand());
    }
}

/// <summary>
/// Lists all service principals in an organization.
/// </summary>
public class PrincipalListCommand : Command
{
    public PrincipalListCommand()
        : base("list", "List all service principals in an organization")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        AddOption(orgIdOption);

        this.SetHandler(async (orgId) =>
        {
            Console.WriteLine($"Note: Full implementation requires service integration.");
            Console.WriteLine($"This command will list all service principals in organization: {orgId}");
        }, orgIdOption);
    }
}

/// <summary>
/// Gets a service principal by client ID.
/// </summary>
public class PrincipalGetCommand : Command
{
    public PrincipalGetCommand()
        : base("get", "Get a service principal by client ID")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var clientIdOption = new Option<string>(
            aliases: new[] { "--client-id", "-c" },
            description: "Service principal client ID")
        {
            IsRequired = true
        };

        AddOption(orgIdOption);
        AddOption(clientIdOption);

        this.SetHandler(async (orgId, clientId) =>
        {
            Console.WriteLine($"Note: Full implementation requires service integration.");
            Console.WriteLine($"This command will get service principal {clientId} in organization: {orgId}");
        }, orgIdOption, clientIdOption);
    }
}

/// <summary>
/// Creates a new service principal in an organization.
/// </summary>
public class PrincipalCreateCommand : Command
{
    public PrincipalCreateCommand()
        : base("create", "Create a new service principal in an organization")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Service principal name")
        {
            IsRequired = true
        };

        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Service principal description");

        var rolesOption = new Option<string?>(
            aliases: new[] { "--roles", "-r" },
            description: "Comma-separated list of roles (e.g., Admin,Service)");

        var expiresInDaysOption = new Option<int?>(
            aliases: new[] { "--expires-in-days", "-e" },
            description: "Number of days until the client secret expires (default: 365)");

        AddOption(orgIdOption);
        AddOption(nameOption);
        AddOption(descriptionOption);
        AddOption(rolesOption);
        AddOption(expiresInDaysOption);

        this.SetHandler(async (orgId, name, description, roles, expiresInDays) =>
        {
            Console.WriteLine($"Note: Full implementation requires service integration.");
            Console.WriteLine($"This command will create service principal in organization: {orgId}");
            Console.WriteLine($"  Name: {name}");
            if (!string.IsNullOrEmpty(description))
                Console.WriteLine($"  Description: {description}");
            if (!string.IsNullOrEmpty(roles))
                Console.WriteLine($"  Roles: {roles}");
            if (expiresInDays.HasValue)
                Console.WriteLine($"  Expires in days: {expiresInDays.Value}");
            Console.WriteLine($"\n⚠️  IMPORTANT: Save the client ID and client secret securely!");
            Console.WriteLine($"The client secret will only be displayed once.");
        }, orgIdOption, nameOption, descriptionOption, rolesOption, expiresInDaysOption);
    }
}

/// <summary>
/// Deletes a service principal.
/// </summary>
public class PrincipalDeleteCommand : Command
{
    public PrincipalDeleteCommand()
        : base("delete", "Delete a service principal")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var clientIdOption = new Option<string>(
            aliases: new[] { "--client-id", "-c" },
            description: "Service principal client ID")
        {
            IsRequired = true
        };

        var confirmOption = new Option<bool>(
            aliases: new[] { "--yes", "-y" },
            description: "Skip confirmation prompt");

        AddOption(orgIdOption);
        AddOption(clientIdOption);
        AddOption(confirmOption);

        this.SetHandler(async (orgId, clientId, confirm) =>
        {
            Console.WriteLine($"Note: Full implementation requires service integration.");
            Console.WriteLine($"This command will delete service principal {clientId} in organization: {orgId}");
            if (!confirm)
                Console.WriteLine("Use --yes to skip confirmation prompt");
        }, orgIdOption, clientIdOption, confirmOption);
    }
}

/// <summary>
/// Rotates the client secret for a service principal.
/// </summary>
public class PrincipalRotateSecretCommand : Command
{
    public PrincipalRotateSecretCommand()
        : base("rotate-secret", "Rotate the client secret for a service principal")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var clientIdOption = new Option<string>(
            aliases: new[] { "--client-id", "-c" },
            description: "Service principal client ID")
        {
            IsRequired = true
        };

        var expiresInDaysOption = new Option<int?>(
            aliases: new[] { "--expires-in-days", "-e" },
            description: "Number of days until the new client secret expires (default: 365)");

        AddOption(orgIdOption);
        AddOption(clientIdOption);
        AddOption(expiresInDaysOption);

        this.SetHandler(async (orgId, clientId, expiresInDays) =>
        {
            Console.WriteLine($"Note: Full implementation requires service integration.");
            Console.WriteLine($"This command will rotate secret for service principal {clientId} in organization: {orgId}");
            if (expiresInDays.HasValue)
                Console.WriteLine($"  New secret expires in days: {expiresInDays.Value}");
            Console.WriteLine($"\n⚠️  IMPORTANT: The old client secret will be immediately invalidated!");
            Console.WriteLine($"Save the new client secret securely - it will only be displayed once.");
        }, orgIdOption, clientIdOption, expiresInDaysOption);
    }
}
