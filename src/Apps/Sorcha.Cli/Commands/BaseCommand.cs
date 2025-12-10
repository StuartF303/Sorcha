using System.CommandLine;
using System.CommandLine.Invocation;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Base class for all CLI commands.
/// Provides common functionality for output formatting, error handling, and authentication.
/// </summary>
public abstract class BaseCommand : Command
{
    // Global options - these will be set by Program.cs
    public static Option<string>? ProfileOption { get; set; }
    public static Option<string>? OutputOption { get; set; }
    public static Option<bool>? QuietOption { get; set; }
    public static Option<bool>? VerboseOption { get; set; }

    protected BaseCommand(string name, string? description = null)
        : base(name, description)
    {
        this.SetHandler(HandleCommandAsync);
    }

    /// <summary>
    /// Handles command execution with error handling and output formatting.
    /// </summary>
    private async Task<int> HandleCommandAsync(InvocationContext context)
    {
        try
        {
            var commandContext = BuildCommandContext(context);
            return await ExecuteAsync(commandContext);
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteError(context, "Authentication required. Please login first.", ex);
            return ExitCodes.AuthenticationError;
        }
        catch (InvalidOperationException ex)
        {
            WriteError(context, ex.Message, ex);
            return ExitCodes.GeneralError;
        }
        catch (ArgumentException ex)
        {
            WriteError(context, $"Invalid argument: {ex.Message}", ex);
            return ExitCodes.ValidationError;
        }
        catch (HttpRequestException ex)
        {
            WriteError(context, $"Network error: {ex.Message}", ex);
            return ExitCodes.NetworkError;
        }
        catch (Exception ex)
        {
            WriteError(context, $"Unexpected error: {ex.Message}", ex);
            return ExitCodes.GeneralError;
        }
    }

    /// <summary>
    /// Executes the command logic. Must be implemented by derived classes.
    /// </summary>
    /// <param name="context">Command context with global options and services</param>
    /// <returns>Exit code</returns>
    protected abstract Task<int> ExecuteAsync(CommandContext context);

    /// <summary>
    /// Builds the command context from invocation context.
    /// Override this method to extract command-specific options.
    /// </summary>
    protected virtual CommandContext BuildCommandContext(InvocationContext invocationContext)
    {
        // This will be populated with actual services via DI in a future task
        var parseResult = invocationContext.ParseResult;

        return new CommandContext
        {
            ProfileName = (ProfileOption != null ? parseResult.GetValueForOption(ProfileOption) : null) ?? "dev",
            OutputFormat = (OutputOption != null ? parseResult.GetValueForOption(OutputOption) : null) ?? "table",
            Quiet = QuietOption != null && parseResult.GetValueForOption(QuietOption),
            Verbose = VerboseOption != null && parseResult.GetValueForOption(VerboseOption)
        };
    }

    /// <summary>
    /// Gets the appropriate output formatter based on context.
    /// </summary>
    protected IOutputFormatter GetFormatter(CommandContext context)
    {
        return context.OutputFormat.ToLowerInvariant() switch
        {
            "json" => new JsonOutputFormatter(),
            "csv" => new CsvOutputFormatter(),
            "table" => new TableOutputFormatter(),
            _ => new TableOutputFormatter()
        };
    }

    /// <summary>
    /// Writes output to console based on format.
    /// </summary>
    protected void WriteOutput<T>(CommandContext context, T data) where T : class
    {
        if (context.Quiet)
        {
            return;
        }

        var formatter = GetFormatter(context);
        var output = formatter.FormatSingle(data);
        Console.WriteLine(output);
    }

    /// <summary>
    /// Writes a collection to console based on format.
    /// </summary>
    protected void WriteCollection<T>(CommandContext context, IEnumerable<T> data) where T : class
    {
        if (context.Quiet)
        {
            return;
        }

        var formatter = GetFormatter(context);
        var output = formatter.FormatCollection(data);
        Console.WriteLine(output);
    }

    /// <summary>
    /// Writes a simple message to console.
    /// </summary>
    protected void WriteMessage(CommandContext context, string message)
    {
        if (context.Quiet)
        {
            return;
        }

        var formatter = GetFormatter(context);
        var output = formatter.FormatMessage(message);
        Console.WriteLine(output);
    }

    /// <summary>
    /// Writes an error message to stderr.
    /// </summary>
    protected void WriteError(InvocationContext context, string message, Exception? exception = null)
    {
        var verbose = VerboseOption != null && context.ParseResult.GetValueForOption(VerboseOption);

        Console.Error.WriteLine($"Error: {message}");

        if (verbose && exception != null)
        {
            Console.Error.WriteLine($"Details: {exception}");
        }
    }

    /// <summary>
    /// Ensures the user is authenticated for the current profile.
    /// Throws UnauthorizedAccessException if not authenticated.
    /// </summary>
    protected async Task EnsureAuthenticatedAsync(CommandContext context)
    {
        var isAuthenticated = await context.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            throw new UnauthorizedAccessException(
                $"Not authenticated for profile '{context.ProfileName}'. Please run 'sorcha auth login' first.");
        }
    }
}
