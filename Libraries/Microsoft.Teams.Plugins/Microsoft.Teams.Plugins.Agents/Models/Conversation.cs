using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.Conversation ToTeamsEntity(this Types.ConversationAccount account)
    {
        return new()
        {
            Id = account.Id,
            AadObjectId = account.AadObjectId,
            TenantId = account.TenantId,
            Type = new(account.ConversationType),
            Name = account.Name,
            IsGroup = account.IsGroup
        };
    }

    public static Api.ConversationReference ToTeamsEntity(this Types.ConversationReference reference)
    {
        return new()
        {
            Bot = reference.Agent?.ToTeamsEntity(),
            ChannelId = new(reference.ChannelId),
            Conversation = reference.Conversation?.ToTeamsEntity(),
            ServiceUrl = reference.ServiceUrl,
            ActivityId = reference.ActivityId,
            Locale = reference.Locale,
            User = reference.User?.ToTeamsEntity(),
        };
    }
}

public static partial class AgentExtensions
{
    public static Types.ConversationAccount ToAgentEntity(this Api.Conversation account)
    {
        return new()
        {
            Id = account.Id,
            AadObjectId = account.AadObjectId,
            TenantId = account.TenantId,
            ConversationType = account.Type,
            Name = account.Name,
            IsGroup = account.IsGroup
        };
    }

    public static Types.ConversationReference ToAgentEntity(this Api.ConversationReference reference)
    {
        return new()
        {
            Agent = reference.Bot.ToAgentEntity(),
            ChannelId = reference.ChannelId,
            Conversation = reference.Conversation.ToAgentEntity(),
            ServiceUrl = reference.ServiceUrl,
            ActivityId = reference.ActivityId,
            Locale = reference.Locale,
            User = reference.User?.ToAgentEntity(),
        };
    }
}