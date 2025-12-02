using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Handlers;
using Microsoft.Teams.Schema;

using Microsoft.Bot.Core;

namespace Microsoft.Teams;

public class TeamsBotApplication : BotApplication
{
    public MessageHandler? OnMessage { get; set; }
    public MessageReactionHandler? OnMessageReaction { get; set; }
    public InstallationUpdateHandler? OnInstallationUpdate { get; set; }
    public ConversationUpdateHandler? OnConversationUpdate { get; set; }

    public TeamsBotApplication()
    {
    }

    public TeamsBotApplication(IConfiguration config, ILogger<BotApplication> logger, string serviceKey = "AzureAd")
        : base(config, logger, serviceKey)
    {
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("New activity received of type {type} from {from}", activity.Type, activity.From?.Id);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            if (teamsActivity.Type == TeamsActivityTypes.Message && OnMessage is not null)
            {
                await OnMessage.Invoke(teamsActivity, cancellationToken);
            }
            if (teamsActivity.Type == TeamsActivityTypes.InstallationUpdate && OnInstallationUpdate is not null)
            {
                await OnInstallationUpdate.Invoke(new InstallationUpdateArgs(teamsActivity), cancellationToken);
            }
            if (teamsActivity.Type == TeamsActivityTypes.MessageReaction && OnMessageReaction is not null)
            {
                await OnMessageReaction.Invoke(new MessageReactionArgs(teamsActivity), cancellationToken);
            }
            if (teamsActivity.Type == TeamsActivityTypes.ConversationUpdate && OnConversationUpdate is not null)
            {
                await OnConversationUpdate.Invoke(new ConversationUpdateArgs(teamsActivity), cancellationToken);
            }
        };
    }
}
