using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

#pragma warning disable ExperimentalTeamsTargeted
public class PromptPreviewTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;

    public PromptPreviewTests()
    {
        _app.AddPlugin(new TestPlugin());
    }

    [Fact]
    public async Task Send_AutoPopulates_TargetedMessageInfoEntity_WhenIncomingIsTargeted()
    {
        IActivity? sentActivity = null;

        _app.OnMessage(async (context, cancellationToken) =>
        {
            sentActivity = await context.Send("Here is the result!", cancellationToken);
        });

        // Simulate an incoming targeted message (bot's Recipient.IsTargeted = true)
        var incomingActivity = new MessageActivity("summarize")
            .WithId("1772129782775")
            .WithFrom(new Account() { Id = "user1", Name = "User" })
            .WithRecipient(new Account() { Id = "bot1", Name = "Bot" }, true);

        await _app.Process<TestPlugin>(_token, incomingActivity);

        Assert.NotNull(sentActivity);
        Assert.NotNull(sentActivity!.Entities);

        var targetedEntity = sentActivity.Entities!.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.NotNull(targetedEntity);
        Assert.Equal("1772129782775", targetedEntity!.MessageId);
    }

    [Fact]
    public async Task Reply_AutoPopulates_TargetedMessageInfoEntity_WhenIncomingIsTargeted()
    {
        IActivity? sentActivity = null;

        _app.OnMessage(async (context, cancellationToken) =>
        {
            sentActivity = await context.Reply("Here is the result!", cancellationToken);
        });

        var incomingActivity = new MessageActivity("summarize")
            .WithId("1772129782775")
            .WithFrom(new Account() { Id = "user1", Name = "User" })
            .WithConversation(new Api.Conversation() { Id = "conv1" })
            .WithRecipient(new Account() { Id = "bot1", Name = "Bot" }, true);

        await _app.Process<TestPlugin>(_token, incomingActivity);

        Assert.NotNull(sentActivity);
        Assert.NotNull(sentActivity!.Entities);

        // Reply calls Send, which auto-populates the entity
        var targetedEntity = sentActivity.Entities!.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.NotNull(targetedEntity);
        Assert.Equal("1772129782775", targetedEntity!.MessageId);

        // quotedReply entities should be stripped by AddTargetedMessageInfo
        Assert.DoesNotContain(sentActivity.Entities!, e => e.Type == "quotedReply");
    }

    [Fact]
    public async Task Send_DoesNotAdd_TargetedMessageInfoEntity_WhenNotTargeted()
    {
        IActivity? sentActivity = null;

        _app.OnMessage(async (context, cancellationToken) =>
        {
            sentActivity = await context.Send("Hello!", cancellationToken);
        });

        // Normal (non-targeted) incoming message
        var incomingActivity = new MessageActivity("hello")
            .WithId("123456")
            .WithRecipient(new Account() { Id = "bot1", Name = "Bot" });

        await _app.Process<TestPlugin>(_token, incomingActivity);

        Assert.NotNull(sentActivity);
        var targetedEntity = sentActivity!.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.Null(targetedEntity);
    }

    [Fact]
    public async Task Send_DoesNotDuplicate_TargetedMessageInfoEntity_WhenAlreadyPresent()
    {
        IActivity? sentActivity = null;

        _app.OnMessage(async (context, cancellationToken) =>
        {
            // Developer manually adds the entity (proactive-like scenario)
            var activity = new MessageActivity("Result")
                .AddEntity(new TargetedMessageInfoEntity { MessageId = "9999" });

            sentActivity = await context.Send(activity, cancellationToken);
        });

        // Incoming activity is targeted
        var incomingActivity = new MessageActivity("summarize")
            .WithId("1772129782775")
            .WithFrom(new Account() { Id = "user1", Name = "User" })
            .WithRecipient(new Account() { Id = "bot1", Name = "Bot" }, true);

        await _app.Process<TestPlugin>(_token, incomingActivity);

        Assert.NotNull(sentActivity);
        Assert.NotNull(sentActivity!.Entities);

        var targetedEntities = sentActivity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(targetedEntities);
        // The developer-provided entity should be preserved, not overwritten
        Assert.Equal("9999", targetedEntities[0].MessageId);
    }
}
