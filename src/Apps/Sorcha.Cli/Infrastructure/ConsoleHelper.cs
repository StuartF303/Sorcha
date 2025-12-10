using System.Text;

namespace Sorcha.Cli.Infrastructure;

/// <summary>
/// Helper class for console I/O operations.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Reads a password from the console, masking the input with asterisks.
    /// </summary>
    /// <param name="prompt">Prompt message to display (optional)</param>
    /// <returns>The password entered by the user</returns>
    public static string ReadPassword(string? prompt = null)
    {
        if (!string.IsNullOrEmpty(prompt))
        {
            Console.Write(prompt);
        }

        var password = new StringBuilder();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b"); // Erase the last asterisk
            }
            else if (key.Key != ConsoleKey.Enter && !char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine(); // Move to next line after Enter
        return password.ToString();
    }

    /// <summary>
    /// Prompts the user for input.
    /// </summary>
    /// <param name="prompt">Prompt message to display</param>
    /// <param name="defaultValue">Default value if user presses Enter without typing</param>
    /// <returns>User input or default value</returns>
    public static string ReadLine(string prompt, string? defaultValue = null)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
        }
        else
        {
            Console.Write($"{prompt}: ");
        }

        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(defaultValue))
        {
            return defaultValue;
        }

        return input ?? string.Empty;
    }

    /// <summary>
    /// Prompts the user for a yes/no confirmation.
    /// </summary>
    /// <param name="prompt">Prompt message to display</param>
    /// <param name="defaultYes">If true, default to Yes when user presses Enter</param>
    /// <returns>True if user confirmed, false otherwise</returns>
    public static bool Confirm(string prompt, bool defaultYes = false)
    {
        var suffix = defaultYes ? " [Y/n]" : " [y/N]";
        Console.Write(prompt + suffix + ": ");

        var response = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(response))
        {
            return defaultYes;
        }

        return response == "y" || response == "yes";
    }

    /// <summary>
    /// Displays a success message in green.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// Displays an error message in red.
    /// </summary>
    public static void WriteError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// Displays a warning message in yellow.
    /// </summary>
    public static void WriteWarning(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// Displays an info message in cyan.
    /// </summary>
    public static void WriteInfo(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ {message}");
        Console.ForegroundColor = originalColor;
    }
}
