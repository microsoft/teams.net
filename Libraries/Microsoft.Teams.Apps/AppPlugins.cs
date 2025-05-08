using System.Reflection;

using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Apps;

public partial class App
{
    protected IList<IPlugin> Plugins { get; set; }

    public IPlugin? GetPlugin(string name)
    {
        return Plugins.SingleOrDefault(p => GetPluginAttribute(p).Name == name);
    }

    public IPlugin? GetPlugin(Type type)
    {
        return Plugins.SingleOrDefault(p => p.GetType() == type);
    }

    public TPlugin? GetPlugin<TPlugin>() where TPlugin : IPlugin
    {
        return (TPlugin?)Plugins.SingleOrDefault(p => p.GetType() == typeof(TPlugin));
    }

    public App AddPlugin(IPlugin plugin)
    {
        var attr = GetPluginAttribute(plugin);

        // broadcast plugin events
        plugin.Events += Events.Emit;
        Plugins.Add(plugin);
        Container.Register(attr.Name, new ValueProvider(plugin));
        Container.Register(plugin.GetType().Name, new ValueProvider(plugin));
        Logger.Debug($"plugin {attr.Name} registered");
        return this;
    }

    protected static PluginAttribute GetPluginAttribute(IPlugin plugin)
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

    protected void Inject(IPlugin plugin)
    {
        var assembly = Assembly.GetAssembly(plugin.GetType());
        var metadata = GetPluginAttribute(plugin);
        var properties = plugin
            .GetType()
            .GetProperties()
            .Where(property => property.IsDefined(typeof(DependencyAttribute), true));

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<DependencyAttribute>();

            if (attribute is null) continue;

            var dependency = Container.Resolve<object>(attribute.Name ?? property.PropertyType.Name);

            if (dependency is null)
            {
                dependency = Container.Resolve<object>(property.Name);
            }

            if (dependency is null)
            {
                if (attribute.Optional) continue;
                throw new InvalidOperationException($"dependency '{property.PropertyType.Name}' of property '{property.Name}' not found, but plugin '{metadata.Name}' depends on it");
            }

            if (dependency is ILogger logger)
            {
                dependency = logger.Child(metadata.Name);
            }

            property.SetValue(plugin, dependency);
        }
    }
}