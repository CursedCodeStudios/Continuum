using Continuum.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Continuum;

/// <summary>
/// Registers Continuum services when the host opts into plugin DI registration.
/// </summary>
public static class PluginServiceRegistrator
{
    /// <summary>
    /// Registers Continuum services.
    /// </summary>
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IContinuumListLoader, ContinuumListLoader>();
        services.AddSingleton<IContinuumItemResolver, ContinuumItemResolver>();
        services.AddSingleton<IUserWatchStateFilter, UserWatchStateFilter>();
        services.AddSingleton<IContinuumPlaylistService, ContinuumPlaylistService>();
        services.AddSingleton<IContinuumStateStore, ContinuumStateStore>();
        services.AddSingleton<IContinuumRefreshService, ContinuumRefreshService>();
    }
}
