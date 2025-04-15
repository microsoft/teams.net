namespace Microsoft.Teams.Apps.Plugins;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class PluginAttribute : Attribute
{
    public string Name { get; set; }
    public string Version { get; set; } = "0.0.0";
    public string? Description { get; set; }

    public PluginAttribute(string name) : base()
    {
        Name = name;
    }

    public PluginAttribute(string name, string version) : base()
    {
        Name = name;
        Version = version;
    }

    public PluginAttribute(string name, string version, string description) : base()
    {
        Name = name;
        Version = version;
        Description = description;
    }
}