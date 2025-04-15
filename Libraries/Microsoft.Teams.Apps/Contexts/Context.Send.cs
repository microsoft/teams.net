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
    public Task<MessageActivity> Send(Cards.Card card);

    /// <summary>
    /// send a typing activity
    /// </summary>
    public Task<TypingActivity> Typing();
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public async Task<T> Send<T>(T activity) where T : IActivity
    {
        var res = await Sender.Send(activity, Ref, CancellationToken);
        await OnActivitySent(res, ToActivityType<IActivity>());
        return res;
    }

    public async Task<MessageActivity> Send(string text)
    {
        var activity = new MessageActivity(text)
        {
            From = Ref.Bot,
            Recipient = Ref.User,
            Conversation = Ref.Conversation
        };

        return await Send(activity);
    }

    public async Task<MessageActivity> Send(Cards.Card card)
    {
        var activity = new MessageActivity()
        {
            From = Ref.Bot,
            Recipient = Ref.User,
            Conversation = Ref.Conversation
        };

        activity = activity.AddAttachment(card);
        return await Send(activity);
    }

    public async Task<TypingActivity> Typing()
    {
        var activity = new TypingActivity()
        {
            From = Ref.Bot,
            Recipient = Ref.User,
            Conversation = Ref.Conversation
        };

        return await Send(activity);
    }
}