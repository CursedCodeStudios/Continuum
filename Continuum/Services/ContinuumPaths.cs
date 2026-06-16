using MediaBrowser.Common.Configuration;

namespace Continuum.Services;

/// <summary>
/// Resolves plugin-owned file locations.
/// </summary>
internal static class ContinuumPaths
{
    public static string GetPluginDataDirectory(IApplicationPaths applicationPaths)
    {
        return Plugin.Instance?.DataFolderPath
            ?? Path.Combine(applicationPaths.ProgramDataPath, "plugins", "continuum");
    }

    public static string GetListsDirectory(IApplicationPaths applicationPaths)
    {
        return Path.Combine(GetPluginDataDirectory(applicationPaths), "lists");
    }

    public static string GetStateFilePath(IApplicationPaths applicationPaths)
    {
        return Path.Combine(GetPluginDataDirectory(applicationPaths), "continuum-state.json");
    }
}
