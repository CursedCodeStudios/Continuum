namespace Continuum.Configuration;

/// <summary>
/// XML-serializable per-list enablement setting.
/// </summary>
public sealed class ContinuumEnabledListSetting
{
    /// <summary>
    /// Gets or sets the list slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the list is enabled on this server.
    /// </summary>
    public bool Enabled { get; set; }
}
