using Continuum.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Continuum.Services;

/// <summary>
/// Reads and writes persisted Continuum state.
/// </summary>
public sealed class ContinuumStateStore : IContinuumStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<ContinuumStateStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContinuumStateStore"/> class.
    /// </summary>
    public ContinuumStateStore(IApplicationPaths applicationPaths, ILogger<ContinuumStateStore> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContinuumState> LoadAsync(CancellationToken cancellationToken)
    {
        string statePath = ContinuumPaths.GetStateFilePath(_applicationPaths);
        if (!File.Exists(statePath))
        {
            return new ContinuumState();
        }

        try
        {
            await using FileStream stream = File.OpenRead(statePath);
            ContinuumState? state = await JsonSerializer.DeserializeAsync<ContinuumState>(
                stream,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);
            return state ?? new ContinuumState();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Continuum state file {Path}. Starting from an empty state.", statePath);
            return new ContinuumState();
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read Continuum state file {Path}. Starting from an empty state.", statePath);
            return new ContinuumState();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(ContinuumState state, CancellationToken cancellationToken)
    {
        string statePath = ContinuumPaths.GetStateFilePath(_applicationPaths);
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);

        string temporaryPath = $"{statePath}.tmp";
        await using (FileStream stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporaryPath, statePath, true);
    }
}
