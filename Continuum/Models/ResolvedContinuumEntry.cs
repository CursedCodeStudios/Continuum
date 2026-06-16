using MediaBrowser.Controller.Entities;

namespace Continuum.Models;

/// <summary>
/// Resolution result for a manual list entry.
/// </summary>
public sealed class ResolvedContinuumEntry
{
    /// <summary>
    /// Gets or sets the owning list.
    /// </summary>
    public required ContinuumListDefinition List { get; init; }

    /// <summary>
    /// Gets or sets the source entry.
    /// </summary>
    public required ContinuumListEntry Source { get; init; }

    /// <summary>
    /// Gets or sets the resolved Jellyfin item.
    /// </summary>
    public BaseItem? Item { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry resolved successfully.
    /// </summary>
    public bool IsResolved => Item is not null;

    /// <summary>
    /// Gets or sets a value indicating whether the entry is missing.
    /// </summary>
    public bool IsMissing { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the entry is ambiguous.
    /// </summary>
    public bool IsAmbiguous { get; init; }

    /// <summary>
    /// Gets or sets an optional resolution message.
    /// </summary>
    public string? Message { get; init; }
}
