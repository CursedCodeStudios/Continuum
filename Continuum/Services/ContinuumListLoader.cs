using Continuum.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Continuum.Services;

/// <summary>
/// Loads Continuum manual list files from the bundled community archive.
/// </summary>
public sealed class ContinuumListLoader : IContinuumListLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<ContinuumListLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContinuumListLoader"/> class.
    /// </summary>
    public ContinuumListLoader(IApplicationPaths applicationPaths, ILogger<ContinuumListLoader> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContinuumListDefinition>> LoadAllAsync(CancellationToken cancellationToken)
    {
        string listsDirectory = ContinuumPaths.GetBundledListsDirectory();
        return await LoadAllFromDirectoryAsync(listsDirectory, _logger, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<IReadOnlyList<ContinuumListDefinition>> LoadAllFromDirectoryAsync(
        string listsDirectory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(listsDirectory))
        {
            logger.LogWarning("Continuum bundled playlist archive was not found at {Path}.", listsDirectory);
            return [];
        }

        string[] files = Directory
            .EnumerateFiles(listsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            logger.LogWarning("Continuum bundled playlist archive at {Path} does not contain any JSON list files.", listsDirectory);
            return [];
        }

        List<ContinuumListDefinition> results = new List<ContinuumListDefinition>(files.Length);

        foreach (string filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                ContinuumListDefinition? definition = DeserializeDefinition(json);

                if (definition is null || string.IsNullOrWhiteSpace(definition.Name) || string.IsNullOrWhiteSpace(definition.Slug))
                {
                    logger.LogWarning("Skipping invalid Continuum list file {Path}.", filePath);
                    continue;
                }

                definition.Items = definition.Items
                    .OrderBy(item => item.Order)
                    .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                results.Add(definition);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse Continuum list file {Path}.", filePath);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to read Continuum list file {Path}.", filePath);
            }
        }

        logger.LogInformation("Loaded {Count} Continuum list definition(s) from the bundled archive.", results.Count);
        return results;
    }

    internal static ContinuumListDefinition? DeserializeDefinition(string json)
    {
        ContinuumListDefinition? definition = JsonSerializer.Deserialize<ContinuumListDefinition>(json, SerializerOptions);
        if (definition is null)
        {
            return null;
        }

        definition.Items = definition.Items
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return definition;
    }
}
