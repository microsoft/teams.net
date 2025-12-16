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
    public async Task<ResourceResponse?> SendActivityAsync(string text, CancellationToken cancellationToken = default)
    {
        TeamsActivity reply = new TeamsActivityBuilder()
            .WithText(text)
            .WithConversationReference(Activity)
            .Build();

        return await TeamsBotApplication.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
    }
}
