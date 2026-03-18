using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Contexts;

public class ContextQuotedReplyTests
{
    private readonly IToken _token = Globals.Token;

    private static MessageActivity CreateInbound(string text, string? id = "msg-123")
    {
        return new MessageActivity(text)
        {
            Id = id,
            From = new Account { Id = "user1" },
            Recipient = new Account { Id = "bot1" },
            Conversation = new Api.Conversation { Id = "conv1" }
        };
    }

    [Fact]
    public async Task Reply_Should_Stamp_QuotedReplyEntity_With_ActivityId()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.Reply(new MessageActivity("reply text"));
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-123"));

        Assert.NotNull(sent);
        var quotedEntity = Assert.Single(sent!.Entities!.OfType<QuotedReplyEntity>());
        Assert.Equal("msg-123", quotedEntity.QuotedReply.MessageId);
    }

    [Fact]
    public async Task Reply_Should_Prepend_Placeholder_To_Text()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.Reply(new MessageActivity("reply text"));
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-123"));

        Assert.NotNull(sent);
        Assert.StartsWith("<quoted messageId=\"msg-123\"/>", sent!.Text);
        Assert.Contains("reply text", sent.Text);
    }

    [Fact]
    public async Task Reply_Should_Handle_Empty_Text()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.Reply(new MessageActivity());
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-456"));

        Assert.NotNull(sent);
        Assert.Equal("<quoted messageId=\"msg-456\"/>", sent!.Text);
    }

    [Fact]
    public async Task Reply_Should_Not_Stamp_Entity_When_ActivityId_Is_Null()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.Reply(new MessageActivity("reply text"));
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", null));

        Assert.NotNull(sent);
        var quotedEntities = (sent!.Entities ?? new List<IEntity>()).OfType<QuotedReplyEntity>().ToList();
        Assert.Empty(quotedEntities);
    }

    [Fact]
    public async Task Reply_Should_Preserve_Existing_Entities()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            var activity = new MessageActivity("reply text");
            activity.Entities = new List<IEntity>
            {
                new MentionEntity { Mentioned = new Account { Id = "user2", Name = "User Two" }, Text = "<at>User Two</at>" }
            };
            sent = await context.Reply(activity);
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-789"));

        Assert.NotNull(sent);
        Assert.Equal(2, sent!.Entities!.Count);
        Assert.Single(sent.Entities.OfType<MentionEntity>());
        Assert.Single(sent.Entities.OfType<QuotedReplyEntity>());
    }

    [Fact]
    public async Task QuoteReply_Should_Stamp_Entity_With_Provided_MessageId()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.QuoteReply("custom-msg-id", new MessageActivity("quote reply text"));
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-000"));

        Assert.NotNull(sent);
        var quotedEntity = Assert.Single(sent!.Entities!.OfType<QuotedReplyEntity>());
        Assert.Equal("custom-msg-id", quotedEntity.QuotedReply.MessageId);
    }

    [Fact]
    public async Task QuoteReply_Should_Prepend_Placeholder_To_Text()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.QuoteReply("custom-msg-id", new MessageActivity("quote reply text"));
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-000"));

        Assert.NotNull(sent);
        Assert.StartsWith("<quoted messageId=\"custom-msg-id\"/>", sent!.Text);
        Assert.Contains("quote reply text", sent.Text);
    }

    [Fact]
    public async Task QuoteReply_Should_Handle_Empty_Text()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.QuoteReply("custom-msg-id", new MessageActivity());
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-000"));

        Assert.NotNull(sent);
        Assert.Equal("<quoted messageId=\"custom-msg-id\"/>", sent!.Text);
    }

    [Fact]
    public async Task QuoteReply_String_Overload_Should_Stamp_Entity_And_Prepend_Placeholder()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.QuoteReply("custom-msg-id", "quote reply text");
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-000"));

        Assert.NotNull(sent);
        var quotedEntity = Assert.Single(sent!.Entities!.OfType<QuotedReplyEntity>());
        Assert.Equal("custom-msg-id", quotedEntity.QuotedReply.MessageId);
        Assert.StartsWith("<quoted messageId=\"custom-msg-id\"/>", sent.Text);
        Assert.Contains("quote reply text", sent.Text);
    }

    [Fact]
    public async Task Reply_String_Overload_Should_Stamp_Entity_And_Prepend_Placeholder()
    {
        var app = new App();
        app.AddPlugin(new TestPlugin());

        MessageActivity? sent = null;

        app.OnMessage(async context =>
        {
            sent = await context.Reply("reply text");
        });

        await app.Process<TestPlugin>(_token, CreateInbound("hello", "msg-123"));

        Assert.NotNull(sent);
        var quotedEntity = Assert.Single(sent!.Entities!.OfType<QuotedReplyEntity>());
        Assert.Equal("msg-123", quotedEntity.QuotedReply.MessageId);
        Assert.StartsWith("<quoted messageId=\"msg-123\"/>", sent.Text);
        Assert.Contains("reply text", sent.Text);
    }
}