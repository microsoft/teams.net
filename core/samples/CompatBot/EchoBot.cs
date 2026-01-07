// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CompatBot;

public class ConversationData
{
    public int MessageCount { get; set; } = 0;

}

internal class EchoBot(ConversationState conversationState, ILogger<EchoBot> logger) : TeamsActivityHandler
{
    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        await base.OnTurnAsync(turnContext, cancellationToken);

        await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
    }
    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("OnMessage");
        IStatePropertyAccessor<ConversationData> conversationStateAccessors = conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
        ConversationData conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData(), cancellationToken);

        string replyText = $"Echo from BF Compat [{conversationData.MessageCount++}]: {turnContext.Activity.Text}";
        await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        // await turnContext.SendActivityAsync(MessageFactory.Text($"Send a proactive message `/api/notify/{turnContext.Activity.Conversation.Id}`"), cancellationToken);

        var conversationClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.IConnectorClient>().Conversations;

        var cr = turnContext.Activity.GetConversationReference();
        var reply = Activity.CreateMessageActivity();
        reply.ApplyConversationReference(cr, isIncoming: false);
        reply.Text = "This is a proactive message sent using the Conversations API.";

        var res = await conversationClient.SendToConversationAsync(cr.Conversation.Id, (Activity)reply, cancellationToken);

        await Task.Delay(2000, cancellationToken);

        await conversationClient.UpdateActivityAsync(cr.Conversation.Id, res.Id!, new Activity
        {
            Id = res.Id,
            ServiceUrl = turnContext.Activity.ServiceUrl,
            Type = ActivityTypes.Message,
            Text = "This message has been updated.",
        }, cancellationToken);

        await Task.Delay(2000, cancellationToken);

        await conversationClient.DeleteActivityAsync(cr.Conversation.Id, res.Id!, cancellationToken);

        await turnContext.SendActivityAsync(MessageFactory.Text("Proactive message sent and deleted."), cancellationToken);

        var attachment = new Attachment
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = Cards.FeedbackCardObj
        };
        var attachmentReply = MessageFactory.Attachment(attachment);
        await turnContext.SendActivityAsync(attachmentReply, cancellationToken);

    }

    protected override async Task OnMessageReactionActivityAsync(ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Message reaction received."), cancellationToken);
    }

    protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Installation update received."), cancellationToken);
        await turnContext.SendActivityAsync(MessageFactory.Text($"Send a proactive messages to  `/api/notify/{turnContext.Activity.Conversation.Id}`"), cancellationToken);
    }

    protected override async Task OnInstallationUpdateAddAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Installation update Add received."), cancellationToken);
        await turnContext.SendActivityAsync(MessageFactory.Text($"Send a proactive messages to  `/api/notify/{turnContext.Activity.Conversation.Id}`"), cancellationToken);
    }

    protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Invoke Activity received: {Name}", turnContext.Activity.Name);
        var actionValue = JObject.FromObject(turnContext.Activity.Value);
        var action = actionValue["action"] as JObject;
        var actionData = action?["data"] as JObject;
        var userInput = actionData?["feedback"]?.ToString();
        //var userInput = actionValue["userInput"]?.ToString();

        logger.LogInformation("Action: {Action}, User Input: {UserInput}", action, userInput);



        var attachment = new Attachment
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = Cards.ResponseCard(userInput)
        };

        var card = MessageFactory.Attachment(attachment);
        await turnContext.SendActivityAsync(card, cancellationToken);

        return new InvokeResponse
        {
            Status = 200,
            Body = "invokes from compat bot"
        };
    }

    //protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    //{
    //    await turnContext.SendActivityAsync(MessageFactory.Text("Welcome."), cancellationToken);
    //    await turnContext.SendActivityAsync(MessageFactory.Text($"Send a proactive messages to  `/api/notify/{turnContext.Activity.Conversation.Id}`"), cancellationToken);
    //}

    //protected override async Task OnTeamsMeetingStartAsync(MeetingStartEventDetails meeting, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
    //{
    //    await turnContext.SendActivityAsync(MessageFactory.Text("Welcome to meeting: "), cancellationToken);
    //    await turnContext.SendActivityAsync(MessageFactory.Text($"{meeting.Title} {meeting.MeetingType}"), cancellationToken);
    //}
}
