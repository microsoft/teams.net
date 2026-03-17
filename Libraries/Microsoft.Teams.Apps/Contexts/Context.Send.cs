// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;

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
    /// send an activity to the conversation as a reply
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<T> Reply<T>(T activity, CancellationToken cancellationToken = default) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation as a reply
    /// </summary>
    /// <param name="text">the text to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Reply(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message activity with a card attachment as a reply
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Reply(Cards.AdaptiveCard card, CancellationToken cancellationToken = default);

    /// <summary>
    /// send an activity to the conversation as a quoted reply to an arbitrary message
    /// </summary>
    /// <param name="messageId">the id of the message to quote</param>
    /// <param name="activity">activity to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<T> QuoteReply<T>(string messageId, T activity, CancellationToken cancellationToken = default) where T : IActivity;

    /// <summary>
    /// send a message activity as a quoted reply to an arbitrary message
    /// </summary>
    /// <param name="messageId">the id of the message to quote</param>
    /// <param name="text">the text to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> QuoteReply(string messageId, string text, CancellationToken cancellationToken = default);

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

    public Task<T> Reply<T>(T activity, CancellationToken cancellationToken = default) where T : IActivity
    {
        activity.Conversation = Ref.Conversation.Copy();

        if (Activity.Id != null)
        {
            var placeholder = $"<quoted messageId=\"{Activity.Id}\"/>";
            activity.Entities ??= new List<IEntity>();
            activity.Entities.Add(new QuotedReplyEntity
            {
                QuotedReply = new QuotedReplyData { MessageId = Activity.Id }
            });

            if (activity is MessageActivity message)
            {
                var text = message.Text?.Trim() ?? "";
                message.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";
            }
        }

        return Send(activity, cancellationToken);
    }

    public Task<MessageActivity> Reply(string text, CancellationToken cancellationToken = default)
    {
        return Reply(new MessageActivity(text), cancellationToken);
    }

    public Task<MessageActivity> Reply(Cards.AdaptiveCard card, CancellationToken cancellationToken = default)
    {
        return Reply(new MessageActivity().AddAttachment(card), cancellationToken);
    }

    public Task<T> QuoteReply<T>(string messageId, T activity, CancellationToken cancellationToken = default) where T : IActivity
    {
        var placeholder = $"<quoted messageId=\"{messageId}\"/>";
        activity.Entities ??= new List<IEntity>();
        activity.Entities.Add(new QuotedReplyEntity
        {
            QuotedReply = new QuotedReplyData { MessageId = messageId }
        });

        if (activity is MessageActivity message)
        {
            var text = message.Text?.Trim() ?? "";
            message.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";
        }

        return Send(activity, cancellationToken);
    }

    public Task<MessageActivity> QuoteReply(string messageId, string text, CancellationToken cancellationToken = default)
    {
        return QuoteReply(messageId, new MessageActivity(text), cancellationToken);
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