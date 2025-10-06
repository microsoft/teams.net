using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.Attachment ToTeamsEntity(this Types.Attachment attachment)
    {
        return new()
        {
            Name = attachment.Name,
            Content = attachment.Content,
            ContentType = new(attachment.ContentType),
            ContentUrl = attachment.ContentUrl,
            ThumbnailUrl = attachment.ThumbnailUrl
        };
    }
}

public static partial class AgentExtensions
{
    public static Types.Attachment ToAgentEntity(this Api.Attachment attachment)
    {
        return new()
        {
            Name = attachment.Name,
            Content = attachment.Content,
            ContentType = attachment.ContentType,
            ContentUrl = attachment.ContentUrl,
            ThumbnailUrl = attachment.ThumbnailUrl
        };
    }
}