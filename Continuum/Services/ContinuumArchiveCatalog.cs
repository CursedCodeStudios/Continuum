namespace Continuum.Services;

/// <summary>
/// Defines the live GitHub archive sources that Continuum loads at runtime.
/// </summary>
internal static class ContinuumArchiveCatalog
{
    private const string RawArchiveBaseUrl = "https://raw.githubusercontent.com/CursedCodeStudios/Continuum/main/Playlist-Data/";

    private static readonly string[] ListFiles =
    [
        "chicago-universe.jsonc",
        "star-wars-test.jsonc",
        "marvel-cinematic-universe.jsonc",
    ];

    public static IReadOnlyList<Uri> GetListUrls()
    {
        return ListFiles
            .Select(fileName => new Uri($"{RawArchiveBaseUrl}{fileName}", UriKind.Absolute))
            .ToArray();
    }
}
