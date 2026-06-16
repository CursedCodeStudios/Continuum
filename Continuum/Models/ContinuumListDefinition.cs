namespace Continuum.Models;

/// <summary>
/// A manual ordered Continuum list.
/// </summary>
public sealed class ContinuumListDefinition
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the list is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the ordered items.
    /// </summary>
    public List<ContinuumListEntry> Items { get; set; } = [];
}
