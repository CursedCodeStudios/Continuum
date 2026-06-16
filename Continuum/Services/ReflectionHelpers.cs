using Jellyfin.Database.Implementations.Entities;
using System.Reflection;

namespace Continuum.Services;

/// <summary>
/// Small reflection helpers for API surfaces that vary across Jellyfin internals.
/// </summary>
internal static class ReflectionHelpers
{
    public static bool IsDisabled(User user)
    {
        object? policy = user.GetType().GetProperty("Policy", BindingFlags.Public | BindingFlags.Instance)?.GetValue(user);
        if (policy is not null)
        {
            object? isDisabled = policy.GetType().GetProperty("IsDisabled", BindingFlags.Public | BindingFlags.Instance)?.GetValue(policy);
            if (isDisabled is bool policyDisabled)
            {
                return policyDisabled;
            }
        }

        object? disabled = user.GetType().GetProperty("Disabled", BindingFlags.Public | BindingFlags.Instance)?.GetValue(user);
        return disabled as bool? == true;
    }

    public static string GetUserName(User user)
    {
        return user.GetType().GetProperty("Username", BindingFlags.Public | BindingFlags.Instance)?.GetValue(user) as string
            ?? user.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(user) as string
            ?? user.Id.ToString("D");
    }
}
