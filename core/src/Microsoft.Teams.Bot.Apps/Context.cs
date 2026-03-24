// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
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
    /// Sends a message activity as a reply, automatically quoting the inbound message.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<SendActivityResponse?> ReplyAsync(string text, CancellationToken cancellationToken = default)
    {
        var reply = new MessageActivity(text);
        return ReplyAsync(reply, cancellationToken);
    }

    /// <summary>
    /// Sends an activity as a reply, automatically quoting the inbound message.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<SendActivityResponse?> ReplyAsync(TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (Activity.Id != null)
        {
            StampQuotedReply(activity, Activity.Id);
        }

        return SendActivityAsync(activity, cancellationToken);
    }

    /// <summary>
    /// Sends a message activity as a reply quoting a specific message by ID.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<SendActivityResponse?> QuoteReplyAsync(string messageId, string text, CancellationToken cancellationToken = default)
    {
        var reply = new MessageActivity(text);
        return QuoteReplyAsync(messageId, reply, cancellationToken);
    }

    /// <summary>
    /// Sends an activity as a reply quoting a specific message by ID.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<SendActivityResponse?> QuoteReplyAsync(string messageId, TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        StampQuotedReply(activity, messageId);
        return SendActivityAsync(activity, cancellationToken);
    }

    private static void StampQuotedReply(TeamsActivity activity, string messageId)
    {
        var placeholder = $"<quoted messageId=\"{messageId}\"/>";
        activity.Entities ??= [];
        activity.Entities.Add(new QuotedReplyEntity
        {
            QuotedReply = new QuotedReplyData { MessageId = messageId }
        });

        if (activity is MessageActivity message)
        {
            var text = message.Text?.Trim() ?? "";
            message.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";
        }

        activity.Rebase();
    }

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
