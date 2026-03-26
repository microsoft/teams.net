// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps;

public partial interface IContext<TActivity>
{
    /// <summary>
    /// send an activity to the conversation
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<T> Send<T>(T activity, CancellationToken cancellationToken = default) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Send(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Send(Cards.AdaptiveCard card, CancellationToken cancellationToken = default);

    /// <summary>
    /// send an activity to the conversation as a reply, automatically quoting the inbound message
    /// </summary>
    /// <param name="activity">activity to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<T> Reply<T>(T activity, CancellationToken cancellationToken = default) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation as a reply, automatically quoting the inbound message
    /// </summary>
    /// <param name="text">the text to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Reply(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message activity with a card attachment as a reply, automatically quoting the inbound message
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Reply(Cards.AdaptiveCard card, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">the ID of the message to quote</param>
    /// <param name="activity">the activity to send — a quote placeholder for messageId will be prepended to its text</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<T> Quote<T>(string messageId, T activity, CancellationToken cancellationToken = default) where T : IActivity;

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">the ID of the message to quote</param>
    /// <param name="text">the response text, appended to the quoted message placeholder</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<MessageActivity> Quote(string messageId, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a typing activity
    /// </summary>
    /// <param name="text">optional text to include</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<TypingActivity> Typing(string? text = null, CancellationToken cancellationToken = default);
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public async Task<T> Send<T>(T activity, CancellationToken cancellationToken = default) where T : IActivity
    {
        var res = await Sender.Send(activity, Ref, CancellationToken);
        await OnActivitySent(res, ToActivityType<IActivity>());
        return res;
    }

    public Task<MessageActivity> Send(string text, CancellationToken cancellationToken = default)
    {
        return Send(new MessageActivity(text), cancellationToken);
    }

    public Task<MessageActivity> Send(Cards.AdaptiveCard card, CancellationToken cancellationToken = default)
    {
        return Send(new MessageActivity().AddAttachment(card), cancellationToken);
    }

#pragma warning disable ExperimentalTeamsQuotedReplies
    public Task<T> Reply<T>(T activity, CancellationToken cancellationToken = default) where T : IActivity
    {
        if (Activity.Id != null)
        {
            return Quote(Activity.Id, activity, cancellationToken);
        }

        return Send(activity, cancellationToken);
    }
#pragma warning restore ExperimentalTeamsQuotedReplies

    public Task<MessageActivity> Reply(string text, CancellationToken cancellationToken = default)
    {
        return Reply(new MessageActivity(text), cancellationToken);
    }

    public Task<MessageActivity> Reply(Cards.AdaptiveCard card, CancellationToken cancellationToken = default)
    {
        return Reply(new MessageActivity().AddAttachment(card), cancellationToken);
    }

    [Experimental("ExperimentalTeamsQuotedReplies")]
#pragma warning disable ExperimentalTeamsQuotedReplies
    public Task<T> Quote<T>(string messageId, T activity, CancellationToken cancellationToken = default) where T : IActivity
    {
        if (activity is MessageActivity message)
        {
            message.PrependQuote(messageId);
        }

        return Send(activity, cancellationToken);
    }
#pragma warning restore ExperimentalTeamsQuotedReplies

    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<MessageActivity> Quote(string messageId, string text, CancellationToken cancellationToken = default)
    {
        return Quote(messageId, new MessageActivity(text), cancellationToken);
    }

    public Task<TypingActivity> Typing(string? text = null, CancellationToken cancellationToken = default)
    {
        var activity = new TypingActivity();

        if (text is not null)
        {
            activity.Text = text;
        }

        return Send(activity, cancellationToken);
    }
}