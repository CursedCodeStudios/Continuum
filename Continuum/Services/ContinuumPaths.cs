using MediaBrowser.Common.Configuration;
namespace Continuum.Services;

/// <summary>
/// Resolves plugin-owned writable file locations.
/// </summary>
internal static class ContinuumPaths
{
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
