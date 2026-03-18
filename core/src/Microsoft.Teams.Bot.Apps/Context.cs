// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps;


/// <summary>
/// Context for a bot turn.
/// </summary>
/// <param name="botApplication"></param>
/// <param name="activity"></param>
public class Context<TActivity>(TeamsBotApplication botApplication, TActivity activity) where TActivity : TeamsActivity
{
    /// <summary>
    /// Base bot application.
    /// </summary>
    public TeamsBotApplication TeamsBotApplication { get; } = botApplication;

    /// <summary>
    /// Current activity.
    /// </summary>
    public TActivity Activity { get; } = activity;

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
    /// Creates a new <see cref="TeamsStreamingWriter"/> bound to the current activity's conversation.
    /// </summary>
    /// <returns>An <see cref="TeamsStreamingWriter"/> ready to stream message updates.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Creates a new instance on each call.")]
    public TeamsStreamingWriter GetStreamingWriter()
        => new(TeamsBotApplication.ConversationClient, Activity);

    /// <summary>
    /// Sends a typing activity to the conversation asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendTypingActivityAsync(CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            new TeamsActivityBuilder()
                .WithType(TeamsActivityType.Typing)
                .WithConversationReference(Activity)
                .Build(), cancellationToken);
}
