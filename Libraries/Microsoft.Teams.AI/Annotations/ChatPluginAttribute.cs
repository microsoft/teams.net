namespace Microsoft.Teams.AI.Annotations;

[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public class ChatPluginAttribute : Attribute
{
    public ChatPluginAttribute()
    {
    }
}