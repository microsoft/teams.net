// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.UnitTests;

public class CoreActivityBuilderTests
{
    [Fact]
    public void Constructor_DefaultConstructor_CreatesNewActivity()
    {
        CoreActivityBuilder builder = new();
        CoreActivity activity = builder.Build();

        Assert.NotNull(activity);
        Assert.NotNull(activity.From);
        Assert.NotNull(activity.Recipient);
        Assert.NotNull(activity.Conversation);
    }

    [Fact]
    public void Constructor_WithExistingActivity_UsesProvidedActivity()
    {
        CoreActivity existingActivity = new()
        {
            Id = "test-id",
            Text = "existing text"
        };

        CoreActivityBuilder builder = new(existingActivity);
        CoreActivity activity = builder.Build();

        Assert.Equal("test-id", activity.Id);
        Assert.Equal("existing text", activity.Text);
    }

    [Fact]
    public void Constructor_WithNullActivity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CoreActivityBuilder(null!));
    }

    [Fact]
    public void WithId_SetsActivityId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void WithServiceUrl_SetsServiceUrl()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/teams/");

        CoreActivity activity = new CoreActivityBuilder()
            .WithServiceUrl(serviceUrl)
            .Build();

        Assert.Equal(serviceUrl, activity.ServiceUrl);
    }

    [Fact]
    public void WithChannelId_SetsChannelId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithChannelId("msteams")
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
    }

    [Fact]
    public void WithType_SetsActivityType()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithType(ActivityTypes.Message)
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithText("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Text);
    }

    [Fact]
    public void WithFrom_SetsSenderAccount()
    {
        ConversationAccount fromAccount = new()
        {
            Id = "sender-id",
            Name = "Sender Name"
        };

        CoreActivity activity = new CoreActivityBuilder()
            .WithFrom(fromAccount)
            .Build();

        Assert.Equal("sender-id", activity.From.Id);
        Assert.Equal("Sender Name", activity.From.Name);
    }

    [Fact]
    public void WithRecipient_SetsRecipientAccount()
    {
        ConversationAccount recipientAccount = new()
        {
            Id = "recipient-id",
            Name = "Recipient Name"
        };

        CoreActivity activity = new CoreActivityBuilder()
            .WithRecipient(recipientAccount)
            .Build();

        Assert.Equal("recipient-id", activity.Recipient.Id);
        Assert.Equal("Recipient Name", activity.Recipient.Name);
    }

    [Fact]
    public void WithConversation_SetsConversationInfo()
    {
        Conversation conversation = new()
        {
            Id = "conversation-id"
        };

        CoreActivity activity = new CoreActivityBuilder()
            .WithConversation(conversation)
            .Build();

        Assert.Equal("conversation-id", activity.Conversation.Id);
    }

    [Fact]
    public void WithChannelData_SetsChannelData()
    {
        ChannelData channelData = new();

        CoreActivity activity = new CoreActivityBuilder()
            .WithChannelData(channelData)
            .Build();

        Assert.NotNull(activity.ChannelData);
    }

    [Fact]
    public void FluentAPI_CompleteActivity_BuildsCorrectly()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithType(ActivityTypes.Message)
            .WithId("activity-123")
            .WithChannelId("msteams")
            .WithText("Test message")
            .WithServiceUrl(new Uri("https://smba.trafficmanager.net/teams/"))
            .WithFrom(new ConversationAccount
            {
                Id = "sender-id",
                Name = "Sender"
            })
            .WithRecipient(new ConversationAccount
            {
                Id = "recipient-id",
                Name = "Recipient"
            })
            .WithConversation(new Conversation
            {
                Id = "conv-id"
            })
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("activity-123", activity.Id);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("Test message", activity.Text);
        Assert.Equal("sender-id", activity.From.Id);
        Assert.Equal("recipient-id", activity.Recipient.Id);
        Assert.Equal("conv-id", activity.Conversation.Id);
    }

    [Fact]
    public void FluentAPI_MethodChaining_ReturnsBuilderInstance()
    {
        CoreActivityBuilder builder = new();

        CoreActivityBuilder result1 = builder.WithId("id");
        CoreActivityBuilder result2 = builder.WithText("text");
        CoreActivityBuilder result3 = builder.WithType(ActivityTypes.Message);

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        CoreActivityBuilder builder = new CoreActivityBuilder()
            .WithId("test-id");

        CoreActivity activity1 = builder.Build();
        CoreActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
    }

    [Fact]
    public void Builder_ModifyingExistingActivity_PreservesOriginalData()
    {
        CoreActivity original = new()
        {
            Id = "original-id",
            Text = "original text",
            Type = ActivityTypes.Message
        };

        CoreActivity modified = new CoreActivityBuilder(original)
            .WithText("modified text")
            .Build();

        Assert.Equal("original-id", modified.Id);
        Assert.Equal("modified text", modified.Text);
        Assert.Equal(ActivityTypes.Message, modified.Type);
    }

    [Fact]
    public void WithConversationReference_WithNullActivity_ThrowsArgumentNullException()
    {
        CoreActivityBuilder builder = new();

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(null!));
    }

    [Fact]
    public void WithConversationReference_WithNullChannelId_ThrowsArgumentNullException()
    {
        CoreActivityBuilder builder = new();
        CoreActivity sourceActivity = new()
        {
            ChannelId = null,
            ServiceUrl = new Uri("https://test.com"),
            Conversation = new Conversation(),
            From = new ConversationAccount(),
            Recipient = new ConversationAccount()
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        CoreActivityBuilder builder = new();
        CoreActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = null,
            Conversation = new Conversation(),
            From = new ConversationAccount(),
            Recipient = new ConversationAccount()
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithNullConversation_ThrowsArgumentNullException()
    {
        CoreActivityBuilder builder = new();
        CoreActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = null!,
            From = new ConversationAccount(),
            Recipient = new ConversationAccount()
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithNullFrom_ThrowsArgumentNullException()
    {
        CoreActivityBuilder builder = new();
        CoreActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = new Conversation(),
            From = null!,
            Recipient = new ConversationAccount()
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithNullRecipient_ThrowsArgumentNullException()
    {
        CoreActivityBuilder builder = new();
        CoreActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = new Conversation(),
            From = new ConversationAccount(),
            Recipient = null!
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_AppliesConversationReference()
    {
        CoreActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/"),
            Conversation = new Conversation { Id = "conv-123" },
            From = new ConversationAccount { Id = "user-1", Name = "User One" },
            Recipient = new ConversationAccount { Id = "bot-1", Name = "Bot" }
        };

        CoreActivity activity = new CoreActivityBuilder()
            .WithConversationReference(sourceActivity)
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal(new Uri("https://smba.trafficmanager.net/teams/"), activity.ServiceUrl);
        Assert.Equal("conv-123", activity.Conversation.Id);
        Assert.Equal("bot-1", activity.From.Id);
        Assert.Equal("Bot", activity.From.Name);
        Assert.Equal("user-1", activity.Recipient.Id);
        Assert.Equal("User One", activity.Recipient.Name);
    }

    [Fact]
    public void WithConversationReference_SwapsFromAndRecipient()
    {
        CoreActivity incomingActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = new Conversation { Id = "conv-123" },
            From = new ConversationAccount { Id = "user-id", Name = "User" },
            Recipient = new ConversationAccount { Id = "bot-id", Name = "Bot" }
        };

        CoreActivity replyActivity = new CoreActivityBuilder()
            .WithConversationReference(incomingActivity)
            .Build();

        Assert.Equal("bot-id", replyActivity.From.Id);
        Assert.Equal("Bot", replyActivity.From.Name);
        Assert.Equal("user-id", replyActivity.Recipient.Id);
        Assert.Equal("User", replyActivity.Recipient.Name);
    }

    [Fact]
    public void WithChannelData_WithNullValue_SetsToNull()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithChannelData(new ChannelData())
            .WithChannelData(null)
            .Build();

        Assert.Null(activity.ChannelData);
    }

    [Fact]
    public void WithId_WithEmptyString_SetsEmptyId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithId(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.Id);
    }

    [Fact]
    public void WithChannelId_WithEmptyString_SetsEmptyChannelId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithChannelId(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.ChannelId);
    }

    [Fact]
    public void WithType_WithEmptyString_SetsEmptyType()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithType(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.Type);
    }

    [Fact]
    public void WithText_WithEmptyString_SetsEmptyText()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithText(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.Text);
    }

    [Fact]
    public void WithText_WithNullString_SetsNullText()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithText("initial")
            .WithText(null!)
            .Build();

        Assert.Null(activity.Text);
    }

    [Fact]
    public void WithConversationReference_ChainedWithOtherMethods_MaintainsFluentInterface()
    {
        CoreActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = new Conversation { Id = "conv-123" },
            From = new ConversationAccount { Id = "user-1" },
            Recipient = new ConversationAccount { Id = "bot-1" }
        };

        CoreActivity activity = new CoreActivityBuilder()
            .WithType(ActivityTypes.Message)
            .WithConversationReference(sourceActivity)
            .WithText("Reply message")
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("Reply message", activity.Text);
        Assert.Equal("bot-1", activity.From.Id);
        Assert.Equal("user-1", activity.Recipient.Id);
    }

    [Fact]
    public void Build_AfterModificationThenBuild_ReflectsChanges()
    {
        CoreActivityBuilder builder = new CoreActivityBuilder()
            .WithId("id-1")
            .WithText("text-1");

        CoreActivity activity1 = builder.Build();
        Assert.Equal("id-1", activity1.Id);
        Assert.Equal("text-1", activity1.Text);

        builder.WithText("text-2");
        CoreActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
        Assert.Equal("text-2", activity2.Text);
    }

    [Fact]
    public void IntegrationTest_CreateComplexActivity()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/amer/test/");
        ChannelData channelData = new();

        CoreActivity activity = new CoreActivityBuilder()
            .WithType(ActivityTypes.Message)
            .WithId("msg-001")
            .WithServiceUrl(serviceUrl)
            .WithChannelId("msteams")
            .WithText("Please review this document")
            .WithFrom(new ConversationAccount
            {
                Id = "bot-id",
                Name = "Bot"
            })
            .WithRecipient(new ConversationAccount
            {
                Id = "user-id",
                Name = "User"
            })
            .WithConversation(new Conversation
            {
                Id = "conv-001"
            })
            .WithChannelData(channelData)
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("msg-001", activity.Id);
        Assert.Equal(serviceUrl, activity.ServiceUrl);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("Please review this document", activity.Text);
        Assert.Equal("bot-id", activity.From.Id);
        Assert.Equal("user-id", activity.Recipient.Id);
        Assert.Equal("conv-001", activity.Conversation.Id);
        Assert.NotNull(activity.ChannelData);
    }
}
