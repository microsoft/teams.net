// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Context for a bot turn.
/// </summary>
/// <param name="botApplication"></param>
/// <param name="activity"></param>
public class Context(TeamsBotApplication botApplication, TeamsActivity activity)
{
    /// <summary>
    /// Base bot application.
    /// </summary>
    public TeamsBotApplication TeamsBotApplication { get; } = botApplication;

    /// <summary>
    /// Current activity.
    /// </summary>
    public TeamsActivity Activity { get; } = activity;

    /// <summary>
    /// Sends a message activity as a reply.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendActivityAsync(string text, CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            new TeamsActivityBuilder()
                .WithConversationReference(Activity)
                .WithText(text)
                .Build(), cancellationToken);

    /// <summary>
    /// Sends Activity
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendActivityAsync(TeamsActivity activity, CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            new TeamsActivityBuilder(activity)
                .WithConversationReference(Activity)
                .Build(), cancellationToken);


    /// <summary>
    /// Sends a typing activity to the conversation asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendTypingActivityAsync(CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            TeamsActivity.CreateBuilder()
                .WithType(TeamsActivityTypes.Typing)
                .WithConversationReference(Activity)
                .Build(), cancellationToken);
}
