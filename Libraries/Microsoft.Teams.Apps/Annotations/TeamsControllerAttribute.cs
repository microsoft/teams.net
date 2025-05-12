namespace Microsoft.Teams.Apps.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class TeamsControllerAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}