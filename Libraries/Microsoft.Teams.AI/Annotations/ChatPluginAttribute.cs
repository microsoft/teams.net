namespace Microsoft.Teams.AI.Annotations;

[AttributeUsage(AttributeTargets.Field, Inherited = true)]
[Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
public class ChatPluginAttribute : Attribute
{
    public ChatPluginAttribute()
    {
    }
}