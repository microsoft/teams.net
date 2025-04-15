using Microsoft.Teams.AI.Templates;

namespace Microsoft.Teams.AI.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public class PromptAttribute : Attribute
{
    /// <summary>
    /// the prompts name
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// the prompts description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// the prompts instructions
    /// </summary>
    public ITemplate? Instructions { get; private set; }

    public PromptAttribute(string? Name = null, string? Description = null)
    {
        this.Name = Name;
        this.Description = Description;
    }

    public PromptAttribute(string? Name = null, string? Description = null, params string[] Instructions)
    {
        this.Name = Name;
        this.Description = Description;
        this.Instructions = new StringTemplate(string.Join("\n", Instructions));
    }
}