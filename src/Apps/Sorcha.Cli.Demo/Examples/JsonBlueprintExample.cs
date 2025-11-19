using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sorcha.Cli.Demo.Services;
using Microsoft.Extensions.Logging;

namespace Sorcha.Cli.Demo.Examples;

/// <summary>
/// Blueprint example loaded from JSON file with JSON-e templating for runtime values
/// </summary>
public class JsonBlueprintExample : IBlueprintExample
{
    private readonly string _fileName;
    private readonly string _name;
    private readonly string _description;
    private readonly string[] _participants;
    private readonly JsonBlueprintLoader _loader;
    private readonly JsonETemplateEngine _templateEngine;
    private readonly ILogger<JsonBlueprintExample> _logger;

    public JsonBlueprintExample(
        string fileName,
        string name,
        string description,
        string[] participants,
        JsonBlueprintLoader loader,
        JsonETemplateEngine templateEngine,
        ILogger<JsonBlueprintExample> logger)
    {
        _fileName = fileName;
        _name = name;
        _description = description;
        _participants = participants;
        _loader = loader;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public string Name => _name;
    public string Description => _description;
    public string FileName => _fileName;

    public string[] GetParticipants() => _participants;

    /// <summary>
    /// Loads and processes the blueprint JSON template with runtime wallet addresses
    /// </summary>
    /// <param name="participantWallets">Dictionary mapping participant IDs to wallet addresses</param>
    /// <returns>Blueprint object with wallet addresses injected</returns>
    public async Task<Sorcha.Blueprint.Models.Blueprint> GetBlueprintAsync(Dictionary<string, string> participantWallets)
    {
        _logger.LogInformation("Loading blueprint: {Name} from {FileName}", _name, _fileName);

        try
        {
            // Step 1: Load JSON-e template from file
            var templateJson = await _loader.LoadBlueprintTemplateAsync(_fileName);

            _logger.LogDebug("Blueprint template loaded, size: {Size} bytes", templateJson.Length);

            // Step 2: Create runtime context with wallet addresses
            var context = _templateEngine.CreateWalletContext(participantWallets);

            _logger.LogDebug("Created wallet context with {Count} participants",
                participantWallets.Count);

            // Step 3: Process template to inject runtime values using JSON-e
            var processedJson = _templateEngine.ProcessTemplate(templateJson, context);

            _logger.LogDebug("Template processed, output size: {Size} bytes", processedJson.Length);

            // Step 4: Parse processed JSON into Blueprint object
            var blueprint = _loader.ParseBlueprint(processedJson);

            _logger.LogInformation("Successfully loaded blueprint: {Title} (ID: {Id})",
                blueprint.Title, blueprint.Id);

            // Step 5: Validate that all participants have wallet addresses
            ValidateBlueprintWallets(blueprint, participantWallets);

            return blueprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load blueprint: {Name} from {FileName}",
                _name, _fileName);
            throw;
        }
    }

    /// <summary>
    /// Validates that all participants in the blueprint have wallet addresses assigned
    /// </summary>
    private void ValidateBlueprintWallets(Sorcha.Blueprint.Models.Blueprint blueprint, Dictionary<string, string> participantWallets)
    {
        if (blueprint.Participants == null || blueprint.Participants.Count == 0)
        {
            throw new InvalidOperationException("Blueprint has no participants");
        }

        var missingWallets = new List<string>();

        foreach (var participant in blueprint.Participants)
        {
            if (string.IsNullOrEmpty(participant.WalletAddress))
            {
                missingWallets.Add(participant.Id);
                _logger.LogWarning("Participant {ParticipantId} has no wallet address assigned",
                    participant.Id);
            }
            else
            {
                _logger.LogDebug("Participant {ParticipantId} has wallet: {WalletAddress}",
                    participant.Id, participant.WalletAddress);
            }
        }

        if (missingWallets.Any())
        {
            throw new InvalidOperationException(
                $"The following participants are missing wallet addresses: {string.Join(", ", missingWallets)}");
        }

        _logger.LogInformation("All {Count} participants have valid wallet addresses",
            blueprint.Participants.Count);
    }
}
