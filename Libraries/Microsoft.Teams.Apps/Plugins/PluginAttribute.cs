namespace Microsoft.Teams.Apps.Plugins;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class PluginAttribute(params string[] description) : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "0.0.0";
    public string? Description { get; set; } = string.Join("\n", description);
}