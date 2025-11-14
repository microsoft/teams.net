using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Events;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

using Microsoft.Extensions.Logging.Abstractions;
using static Microsoft.Teams.Apps.Activities.Events.Event;

namespace Microsoft.Teams.Apps.Tests.Activities.Events;

public class ReadReceiptEventTests
{
    private readonly App _app;
    private readonly TestPlugin _plugin = new();
    private readonly ReadReceiptController _controller = new();
    private readonly IToken _token = Globals.Token;

    public ReadReceiptEventTests()
    {
        _app = new App(NullLogger<App>.Instance);
        _app.AddPlugin(_plugin);
        _app.AddController(_controller);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnReadReceiptEvent()
    {
        // Arrange
        var handlerCalled = false;
        var eventContext = default(IContext<ReadReceiptActivity>);

        _app.OnReadReceipt(context =>
        {
            handlerCalled = true;
            eventContext = context;
            return Task.FromResult<object?>(null);
        });

        // Create a ReadReceiptActivity
        var ReadReceiptActivity = new ReadReceiptActivity
        {
            Id = "readReceiptId",
        };

        // Act
        var res = await _plugin.Do(_token, ReadReceiptActivity);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.True(handlerCalled, "The ReadReceipt event handler should be called");
        Assert.NotNull(eventContext);
        Assert.IsType<ReadReceiptActivity>(eventContext.Activity);
    }

    [Fact]
    public async Task Should_NotCallHandler_ForOtherEventTypes()
    {
        // Arrange
        var handlerCalled = false;

        _app.OnReadReceipt(context =>
        {
            handlerCalled = true;
            return Task.FromResult<object?>(null);
        });

        // Act - Send a different activity type
        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.False(handlerCalled, "The ReadReceipt event handler should not be called for other activity types");
    }

    [Fact]
    public void ReadReceiptAttribute_Select_ReturnsTrueForReadReceiptActivity()
    {
        // Arrange
        var attribute = new ReadReceiptAttribute();
        var activity = new ReadReceiptActivity
        {
            Id = "readReceiptId",
            Conversation = new Api.Conversation()
            {
                Id = "conversationId",
                Name = "conversationName",
                Type = new ConversationType("group"),
            },
            ChannelId = new ChannelId("webchat"),
            Recipient = new Account()
            {
                Id = "recipientId",
                Name = "recipientName",
            },
            ReplyToId = "replyToId",
        };

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ReadReceiptAttribute_Select_ReturnsFalseForOtherActivities()
    {
        // Arrange
        var attribute = new ReadReceiptAttribute();
        var activity = new MessageActivity("hello world");

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.False(result);
    }


    [Fact]
    public async Task ReadReceiptAttribute_Controller_Call()
    {
        var activity = new ReadReceiptActivity
        {
            Id = "readReceiptId",
            ReplyToId = "replyToId",
        };

        var res = await _app.Process<TestPlugin>(_token, activity);

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal("readReceiptMethod", _controller.MethodCalled);

    }

    [TeamsController]
    public class ReadReceiptController
    {
        public string MethodCalled { get; set; } = string.Empty;

        [ReadReceipt]
        public async Task Method1(IContext<ReadReceiptActivity> context, [Context] IContext.Next next)
        {
            MethodCalled = "readReceiptMethod";
            await next();
        }
    }
}