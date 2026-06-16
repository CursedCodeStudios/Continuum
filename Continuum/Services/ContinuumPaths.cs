using MediaBrowser.Common.Configuration;
namespace Continuum.Services;

/// <summary>
/// Resolves plugin-owned file locations.
/// </summary>
internal static class ContinuumPaths
{
    public static string GetBundledListsDirectory()
    {
        string? pluginAssemblyDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(pluginAssemblyDirectory))
        {
            return GetBundledListsDirectory(pluginAssemblyDirectory);
        }

        return GetBundledListsDirectory(AppContext.BaseDirectory);
    }

    internal static string GetBundledListsDirectory(string baseDirectory)
    {
        return Path.Combine(baseDirectory, "Playlist-Data");
    }

    public static string GetPluginDataDirectory(IApplicationPaths applicationPaths)
    {
        return Plugin.Instance?.DataFolderPath
            ?? Path.Combine(applicationPaths.ProgramDataPath, "plugins", "continuum");
    }

    public static string GetStateFilePath(IApplicationPaths applicationPaths)
    {
        return Path.Combine(GetPluginDataDirectory(applicationPaths), "continuum-state.json");
    }
}
