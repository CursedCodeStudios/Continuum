namespace Continuum.Services;

/// <summary>
/// Defines the live GitHub archive sources that Continuum loads at runtime.
/// </summary>
internal static class ContinuumArchiveCatalog
{
    private const string RawArchiveBaseUrl = "https://raw.githubusercontent.com/CursedCodeStudios/Continuum/main/Playlist-Data/";
    private const string ManifestFileName = "playlist-manifest.json";

    public static Uri GetManifestUrl()
    {
        return new Uri($"{RawArchiveBaseUrl}{ManifestFileName}", UriKind.Absolute);
    }

    public static Uri GetListUrl(string fileName)
    {
        return new Uri($"{RawArchiveBaseUrl}{fileName}", UriKind.Absolute);
    }
}
