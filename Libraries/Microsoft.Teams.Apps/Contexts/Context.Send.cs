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
    /// <param name="isTargeted">whether the activity is targeted</param>
    /// <remarks>
    /// <para>The <paramref name="isTargeted"/> parameter is in preview.</para>
    /// <para>Targeted messages are delivered privately to the recipient specified in the activity's Recipient property.</para>
    /// </remarks>
    public Task<T> Send<T>(T activity, bool isTargeted = false) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    /// <param name="isTargeted">whether the activity is targeted</param>
    /// <remarks>
    /// <para>The <paramref name="isTargeted"/> parameter is in preview.</para>
    /// <para>Targeted messages are delivered privately to the recipient specified in the activity's Recipient property.</para>
    /// </remarks>
    public Task<MessageActivity> Send(string text, bool isTargeted = false);

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    /// <param name="isTargeted">whether the activity is targeted</param>
    /// <remarks>
    /// <para>The <paramref name="isTargeted"/> parameter is in preview.</para>
    /// <para>Targeted messages are delivered privately to the recipient specified in the activity's Recipient property.</para>
    /// </remarks>
    public Task<MessageActivity> Send(Cards.AdaptiveCard card, bool isTargeted = false);

    /// <summary>
    /// send an activity to the conversation as a reply
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    /// <param name="isTargeted">whether the activity is targeted</param>
    /// <remarks>
    /// <para>The <paramref name="isTargeted"/> parameter is in preview.</para>
    /// <para>Targeted messages are delivered privately to the recipient specified in the activity's Recipient property.</para>
    /// </remarks>
    public Task<T> Reply<T>(T activity, bool isTargeted = false) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation as a reply
    /// </summary>
    /// <param name="text">the text to send</param>
    /// <param name="isTargeted">whether the activity is targeted</param>
    /// <remarks>
    /// <para>The <paramref name="isTargeted"/> parameter is in preview.</para>
    /// <para>Targeted messages are delivered privately to the recipient specified in the activity's Recipient property.</para>
    /// </remarks>
    public Task<MessageActivity> Reply(string text, bool isTargeted = false);

    /// <summary>
    /// send a message activity with a card attachment as a reply
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    /// <param name="isTargeted">whether the activity is targeted</param>
    /// <remarks>
    /// <para>The <paramref name="isTargeted"/> parameter is in preview.</para>
    /// <para>Targeted messages are delivered privately to the recipient specified in the activity's Recipient property.</para>
    /// </remarks>
    public Task<MessageActivity> Reply(Cards.AdaptiveCard card, bool isTargeted = false);

    /// <summary>
    /// send a typing activity
    /// </summary>
    public Task<TypingActivity> Typing(string? text = null);
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public async Task<T> Send<T>(T activity, bool isTargeted = false) where T : IActivity
    {
        var res = await Sender.Send(activity, Ref, isTargeted, CancellationToken);
        await OnActivitySent(res, ToActivityType<IActivity>());
        return res;
    }

    public Task<MessageActivity> Send(string text, bool isTargeted = false)
    {
        return Send(new MessageActivity(text), isTargeted);
    }

    public Task<MessageActivity> Send(Cards.AdaptiveCard card, bool isTargeted = false)
    {
        return Send(new MessageActivity().AddAttachment(card), isTargeted);
    }

    public Task<T> Reply<T>(T activity, bool isTargeted = false) where T : IActivity
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

        return Send(activity, isTargeted);
    }

    public Task<MessageActivity> Reply(string text, bool isTargeted = false)
    {
        return Reply(new MessageActivity(text), isTargeted);
    }

    public Task<MessageActivity> Reply(Cards.AdaptiveCard card, bool isTargeted = false)
    {
        return Reply(new MessageActivity().AddAttachment(card), isTargeted);
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