using System.Reflection;

namespace Microsoft.Teams.Apps.Plugins;

internal static class PluginService
{
    public static PluginAttribute GetAttribute(IPlugin plugin)
    {
        var assembly = Assembly.GetAssembly(plugin.GetType());
        var attribute = (PluginAttribute?)Attribute.GetCustomAttribute(plugin.GetType(), typeof(PluginAttribute));

        if (attribute is null)
        {
            throw new InvalidOperationException($"type '{plugin.GetType().Name}' is not a valid plugin");
        }

        attribute.Name = assembly?.GetName().Name ?? throw new InvalidOperationException("plugin is missing a name");
        attribute.Version = assembly?.GetName()?.Version?.ToString() ?? "0.0.0";
        return attribute;
    }
}