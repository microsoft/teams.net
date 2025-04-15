namespace Microsoft.Teams.AI.Annotations;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class FunctionAttribute : Attribute
{
    /// <summary>
    /// the functions name
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// the functions description
    /// </summary>
    public string? Description { get; private set; }

    public FunctionAttribute(string? Name = null, string? Description = null)
    {
        this.Name = Name;
        this.Description = Description;
    }
}