using Microsoft.Extensions.Logging;

namespace Sorcha.Cli.Demo.Services;

/// <summary>
/// Builds and manages the transaction chain during blueprint execution
/// </summary>
public class TransactionChainBuilder
{
    private readonly ILogger<TransactionChainBuilder> _logger;

    public TransactionChainBuilder(ILogger<TransactionChainBuilder> logger)
    {
        _logger = logger;
    }

    // Implementation will be added in Phase 7
}
