namespace Microsoft.Teams.AI.Annotations;

public static partial class Prompt
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class NameAttribute(string Name) : Attribute
    {
        /// <summary>
        /// the prompts name
        /// </summary>
        public string Name { get; private set; } = Name;
    }
}