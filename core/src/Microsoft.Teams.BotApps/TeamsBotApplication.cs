// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.BotApps.Handlers;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class TeamsBotApplication : BotApplication
{
    /// <summary>
    /// Handler for message activities.
    /// </summary>
    public MessageHandler? OnMessage { get; set; }

    /// <summary>
    /// Handler for message reaction activities.
    /// </summary>
    public MessageReactionHandler? OnMessageReaction { get; set; }

    /// <summary>
    /// Handler for installation update activities.
    /// </summary>
    public InstallationUpdateHandler? OnInstallationUpdate { get; set; }

    /// <summary>
    /// Handler for conversation update activities.
    /// </summary>
    public ConversationUpdateHandler? OnConversationUpdate { get; set; }
    /// <param name="conversationClient"></param>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    /// <param name="sectionName"></param>
    public TeamsBotApplication(ConversationClient conversationClient, IConfiguration config, ILogger<BotApplication> logger, string sectionName = "Teams") : base(conversationClient, config, logger, sectionName)
    {
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("New activity received of type {Type} from {From}", activity.Type, activity.From?.Id);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            Context context = new(this, teamsActivity);
            if (teamsActivity.Type == TeamsActivityTypes.Message && OnMessage is not null)
            {
                await OnMessage.Invoke(context, cancellationToken).ConfigureAwait(false);
            }
            if (teamsActivity.Type == TeamsActivityTypes.InstallationUpdate && OnInstallationUpdate is not null)
            {
                await OnInstallationUpdate.Invoke(new InstallationUpdateArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);
            }
            if (teamsActivity.Type == TeamsActivityTypes.MessageReaction && OnMessageReaction is not null)
            {
                await OnMessageReaction.Invoke(new MessageReactionArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);
            }
            if (teamsActivity.Type == TeamsActivityTypes.ConversationUpdate && OnConversationUpdate is not null)
            {
                await OnConversationUpdate.Invoke(new ConversationUpdateArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
