// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps;

public partial interface IContext<TActivity>
{
    /// <summary>
    /// send an activity to the conversation
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    public Task<T> Send<T>(T activity) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    public Task<MessageActivity> Send(string text);

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    public Task<MessageActivity> Send(Cards.AdaptiveCard card);

    /// <summary>
    /// send an activity to the conversation as a reply
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    public Task<T> Reply<T>(T activity) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation as a reply
    /// </summary>
    /// <param name="text">the text to send</param>
    public Task<MessageActivity> Reply(string text);

    /// <summary>
    /// send a message activity with a card attachment as a reply
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    public Task<MessageActivity> Reply(Cards.AdaptiveCard card);

    /// <summary>
    /// send a typing activity
    /// </summary>
    public Task<TypingActivity> Typing(string? text = null);
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public async Task<T> Send<T>(T activity) where T : IActivity
    {
        // For targeted send, set the recipient if not already set.
        // For targeted update (activity.Id exists), we don't update recipient since recipient cannot be changed.
        if (activity is MessageActivity messageActivity && messageActivity.IsTargeted == true && activity.Id is null && messageActivity.Recipient is null)
        {
            messageActivity.Recipient = Activity.From;
        }

        var res = await Sender.Send(activity, Ref, CancellationToken);
        await OnActivitySent(res, ToActivityType<IActivity>());
        return res;
    }

    public Task<MessageActivity> Send(string text)
    {
        return Send(new MessageActivity(text));
    }

    public Task<MessageActivity> Send(Cards.AdaptiveCard card)
    {
        return Send(new MessageActivity().AddAttachment(card));
    }

    public Task<T> Reply<T>(T activity) where T : IActivity
    {
        activity.Conversation = Ref.Conversation.Copy();
        activity.Conversation.Id = Ref.Conversation.ThreadId;

        if (activity is MessageActivity message)
        {
            message.Text = string.Join("\n", [
                Activity.ToQuoteReply(),
                message.Text != string.Empty ? $"<p>{message.Text}</p>" : string.Empty
            ]);
        }

        return Send(activity);
    }

    public Task<MessageActivity> Reply(string text)
    {
        return Reply(new MessageActivity(text));
    }

    public Task<MessageActivity> Reply(Cards.AdaptiveCard card)
    {
        return Reply(new MessageActivity().AddAttachment(card));
    }

    public Task<TypingActivity> Typing(string? text = null)
    {
        var activity = new TypingActivity();

        if (text is not null)
        {
            activity.Text = text;
        }

        return Send(activity);
    }
}