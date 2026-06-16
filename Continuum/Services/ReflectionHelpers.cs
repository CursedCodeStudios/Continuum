using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Library;
using System.Collections;
using System.Reflection;

namespace Continuum.Services;

/// <summary>
/// Small reflection helpers for API surfaces that vary across Jellyfin internals.
/// </summary>
internal static class ReflectionHelpers
{
    public static User[] GetUsers(IUserManager userManager)
    {
        object runtimeManager = userManager;

        PropertyInfo? usersProperty = runtimeManager.GetType().GetProperty("Users", BindingFlags.Public | BindingFlags.Instance);
        if (TryReadUsers(usersProperty?.GetValue(runtimeManager), out User[] propertyUsers))
        {
            return propertyUsers;
        }

        foreach (string methodName in new[] { "GetUserList", "GetUsers" })
        {
            MethodInfo? method = runtimeManager.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (method is null || method.GetParameters().Length != 0)
            {
                continue;
            }

            if (TryReadUsers(method.Invoke(runtimeManager, null), out User[] methodUsers))
            {
                return methodUsers;
            }
        }

        throw new MissingMethodException(
            $"Unable to discover a supported user enumeration API on {runtimeManager.GetType().FullName}.");
    }

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

    private static bool TryReadUsers(object? value, out User[] users)
    {
        if (value is IEnumerable enumerable)
        {
            users = enumerable.Cast<object>().OfType<User>().ToArray();
            return true;
        }

        users = [];
        return false;
    }
}
