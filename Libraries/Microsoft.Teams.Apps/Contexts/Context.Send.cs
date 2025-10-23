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

    /// <summary>
    /// Send a targeted activity to a specific user in the conversation
    /// </summary>
    /// <param name="userId">The user MRI of the targeted message recipient</param>
    /// <param name="activity">The activity to send as a targeted message</param>
    public Task<T> SendTargeted<T>(string userId, T activity) where T : IActivity;

    /// <summary>
    /// Send a targeted message to a specific user in the conversation
    /// </summary>
    /// <param name="userId">The user MRI of the targeted message recipient</param>
    /// <param name="text">The text to send</param>
    public Task<MessageActivity> SendTargeted(string userId, string text);

    /// <summary>
    /// Send a targeted message with a card attachment to a specific user
    /// </summary>
    /// <param name="userId">The user MRI of the targeted message recipient</param>
    /// <param name="card">The card to send as an attachment</param>
    public Task<MessageActivity> SendTargeted(string userId, Cards.AdaptiveCard card);

    /// <summary>
    /// Update a previously sent targeted message
    /// </summary>
    /// <param name="userId">The user MRI of the targeted message recipient</param>
    /// <param name="activityId">The targeted message ID to update</param>
    /// <param name="activity">The updated activity</param>
    public Task<T> UpdateTargeted<T>(string userId, string activityId, T activity) where T : IActivity;
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public async Task<T> Send<T>(T activity) where T : IActivity
    {
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

    public async Task<T> SendTargeted<T>(string userId, T activity) where T : IActivity
    {   
        var res = await Api.Conversations.Activities.SendTargetedAsync(userId, Ref.Conversation.Id, activity);
        
        activity.Id = res?.Id;
        
        await OnActivitySent(activity, ToActivityType<IActivity>());
        return activity;
    }

    public Task<MessageActivity> SendTargeted(string userId, string text)
    {
        return SendTargeted(userId, new MessageActivity(text));
    }

    public Task<MessageActivity> SendTargeted(string userId, Cards.AdaptiveCard card)
    {
        return SendTargeted(userId, new MessageActivity().AddAttachment(card));
    }

    public async Task<T> UpdateTargeted<T>(string userId, string activityId, T activity) where T : IActivity
    {        
        var res = await Api.Conversations.Activities.UpdateTargetedAsync(userId, Ref.Conversation.Id, activityId, activity);
        
        activity.Id = res?.Id;
        
        await OnActivitySent(activity, ToActivityType<IActivity>());
        return activity;
    }
}