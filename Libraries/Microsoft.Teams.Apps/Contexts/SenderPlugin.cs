using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;


namespace Microsoft.Teams.Apps.Contexts;

internal class SenderPlugin(ConversationClient conversationClient) : ISenderPlugin
{
    
    public event EventFunction? Events;

    public IStreamer CreateStream(ConversationReference reference, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Response> Do(ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Events?.Invoke(this, "temp");
        throw new NotImplementedException();
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IActivity> Send(IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Send<IActivity>(activity, reference, isTargeted: false, cancellationToken);
    }

    public Task<IActivity> Send(IActivity activity, ConversationReference reference, bool isTargeted, CancellationToken cancellationToken = default)
    {
        return Send<IActivity>(activity, reference, isTargeted, cancellationToken);
    }

    public Task<TActivity> Send<TActivity>(TActivity activity, ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return Send<TActivity>(activity, reference, isTargeted: false, cancellationToken);
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity, ConversationReference reference, bool isTargeted, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        //var client = new ApiClient(reference.ServiceUrl, Client, cancellationToken);
        
        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;
        activity.ChannelId = reference.ChannelId;
        activity.ServiceUrl = reference.ServiceUrl;

        CoreActivity resultActivity = new()
        {
            Conversation = new Bot.Core.Schema.Conversation() { Id = activity.Conversation.Id },
            From = new ConversationAccount() { Id = activity.From.Id , Name = activity.From.Name, Properties = activity.From.Properties },
            Recipient = new ConversationAccount() { Id = activity.Recipient!.Id, Name = activity.Recipient.Name, Properties = activity.Recipient.Properties },
            ChannelId = activity.ChannelId,
            Type = activity.Type,
            Id = activity.Id,
            ReplyToId = activity.ReplyToId,
            //Locale = activity.Locale,
            //Timestamp = activity.Timestamp,
            //LocalTimestamp = activity.LocalTimestamp,
            //Entities = activity.Entities,
            ChannelData = activity.ChannelData,
            Properties = activity.Properties,
            ServiceUrl = activity.ServiceUrl,
            Text = "need to cast"
        };

        if (activity.Id is not null && !activity.IsStreaming)
        {
            await conversationClient.SendActivityAsync(resultActivity, cancellationToken);
            //await client
            //    .Conversations
            //    .Activities
            //    .UpdateAsync(reference.Conversation.Id, activity.Id, activity, isTargeted);

            return activity;
        }

        //var res = await client
        //    .Conversations
        //    .Activities
        //    .CreateAsync(reference.Conversation.Id, activity, isTargeted);

        //activity.Id = res?.Id;
        return activity;
    }
}
