// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

namespace Microsoft.Teams.BotApps.UnitTests;

public class TeamsActivityBuilderTests
{
    [Fact]
    public void Constructor_DefaultConstructor_CreatesNewActivity()
    {
        TeamsActivityBuilder builder = new();
        TeamsActivity activity = builder.Build();

        Assert.NotNull(activity);
        Assert.NotNull(activity.From);
        Assert.NotNull(activity.Recipient);
        Assert.NotNull(activity.Conversation);
    }

    [Fact]
    public void Constructor_WithExistingActivity_UsesProvidedActivity()
    {
        TeamsActivity existingActivity = new()
        {
            Id = "test-id",
            Text = "existing text"
        };

        TeamsActivityBuilder builder = new(existingActivity);
        TeamsActivity activity = builder.Build();

        Assert.Equal("test-id", activity.Id);
        Assert.Equal("existing text", activity.Text);
    }

    [Fact]
    public void Constructor_WithNullActivity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TeamsActivityBuilder(null!));
    }

    [Fact]
    public void WithId_SetsActivityId()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void WithServiceUrl_SetsServiceUrl()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/teams/");

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithServiceUrl(serviceUrl)
            .Build();

        Assert.Equal(serviceUrl, activity.ServiceUrl);
    }

    [Fact]
    public void WithChannelId_SetsChannelId()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .WithChannelId("msteams")
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
    }

    [Fact]
    public void WithType_SetsActivityType()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .WithType(ActivityTypes.Message)
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .WithText("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Text);
    }

    [Fact]
    public void WithFrom_SetsSenderAccount()
    {
        TeamsConversationAccount fromAccount = new(new ConversationAccount
        {
            Id = "sender-id",
            Name = "Sender Name"
        });

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithFrom(fromAccount)
            .Build();

        Assert.Equal("sender-id", activity.From.Id);
        Assert.Equal("Sender Name", activity.From.Name);
    }

    [Fact]
    public void WithRecipient_SetsRecipientAccount()
    {
        TeamsConversationAccount recipientAccount = new(new ConversationAccount
        {
            Id = "recipient-id",
            Name = "Recipient Name"
        });

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithRecipient(recipientAccount)
            .Build();

        Assert.Equal("recipient-id", activity.Recipient.Id);
        Assert.Equal("Recipient Name", activity.Recipient.Name);
    }

    [Fact]
    public void WithConversation_SetsConversationInfo()
    {
        TeamsConversation conversation = new(new Conversation
        {
            Id = "conversation-id"
        })
        {
            TenantId = "tenant-123",
            ConversationType = "channel"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithConversation(conversation)
            .Build();

        Assert.Equal("conversation-id", activity.Conversation.Id);
        Assert.Equal("tenant-123", activity.Conversation.TenantId);
        Assert.Equal("channel", activity.Conversation.ConversationType);
    }

    [Fact]
    public void WithChannelData_SetsChannelData()
    {
        TeamsChannelData channelData = new()
        {
            TeamsChannelId = "19:channel-id@thread.tacv2",
            TeamsTeamId = "19:team-id@thread.tacv2"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithChannelData(channelData)
            .Build();

        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel-id@thread.tacv2", activity.ChannelData.TeamsChannelId);
        Assert.Equal("19:team-id@thread.tacv2", activity.ChannelData.TeamsTeamId);
    }

    [Fact]
    public void WithEntities_SetsEntitiesCollection()
    {
        EntityList entities = new()
        {
            new ClientInfoEntity
            {
                Locale = "en-US",
                Platform = "Web"
            }
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithEntities(entities)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void WithAttachments_SetsAttachmentsCollection()
    {
        List<TeamsAttachment> attachments = new()
        {
            new() {
                ContentType = "application/json",
                Name = "test-attachment"
            }
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithAttachments(attachments)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("application/json", activity.Attachments[0].ContentType);
        Assert.Equal("test-attachment", activity.Attachments[0].Name);
    }

    [Fact]
    public void AddEntity_AddsEntityToCollection()
    {
        ClientInfoEntity entity = new()
        {
            Locale = "en-US",
            Country = "US"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .AddEntity(entity)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void AddEntity_MultipleEntities_AddsAllToCollection()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddEntity(new ProductInfoEntity { Id = "product-123" })
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);
    }

    [Fact]
    public void AddAttachment_AddsAttachmentToCollection()
    {
        TeamsAttachment attachment = new()
        {
            ContentType = "text/html",
            Name = "test.html"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .AddAttachment(attachment)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("text/html", activity.Attachments[0].ContentType);
    }

    [Fact]
    public void AddAttachment_MultipleAttachments_AddsAllToCollection()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .AddAttachment(new TeamsAttachment { ContentType = "text/html" })
            .AddAttachment(new TeamsAttachment { ContentType = "application/json" })
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Equal(2, activity.Attachments.Count);
    }

    [Fact]
    public void AddMention_WithNullAccount_ThrowsArgumentNullException()
    {
        TeamsActivityBuilder builder = new();

        Assert.Throws<ArgumentNullException>(() => builder.AddMention(null!));
    }

    [Fact]
    public void AddMention_WithAccountAndDefaultText_AddsMentionAndUpdatesText()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithText("said hello")
            .AddMention(account)
            .Build();

        Assert.Equal("<at>John Doe</at> said hello", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        MentionEntity? mention = activity.Entities[0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("user-123", mention.Mentioned?.Id);
        Assert.Equal("John Doe", mention.Mentioned?.Name);
        Assert.Equal("<at>John Doe</at>", mention.Text);
    }

    [Fact]
    public void AddMention_WithCustomText_UsesCustomText()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithText("replied")
            .AddMention(account, "CustomName")
            .Build();

        Assert.Equal("<at>CustomName</at> replied", activity.Text);

        MentionEntity? mention = activity.Entities![0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("<at>CustomName</at>", mention.Text);
    }

    [Fact]
    public void AddMention_WithAddTextFalse_DoesNotUpdateText()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithText("original text")
            .AddMention(account, addText: false)
            .Build();

        Assert.Equal("original text", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
    }

    [Fact]
    public void AddMention_MultipleMentions_AddsAllMentions()
    {
        ConversationAccount account1 = new() { Id = "user-1", Name = "User One" };
        ConversationAccount account2 = new() { Id = "user-2", Name = "User Two" };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithText("message")
            .AddMention(account1)
            .AddMention(account2)
            .Build();

        Assert.Equal("<at>User Two</at> <at>User One</at> message", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);
    }

    [Fact]
    public void FluentAPI_CompleteActivity_BuildsCorrectly()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .WithType(ActivityTypes.Message)
            .WithId("activity-123")
            .WithChannelId("msteams")
            .WithText("Test message")
            .WithServiceUrl(new Uri("https://smba.trafficmanager.net/teams/"))
            .WithFrom(new TeamsConversationAccount(new ConversationAccount
            {
                Id = "sender-id",
                Name = "Sender"
            }))
            .WithRecipient(new TeamsConversationAccount(new ConversationAccount
            {
                Id = "recipient-id",
                Name = "Recipient"
            }))
            .WithConversation(new TeamsConversation(new Conversation
            {
                Id = "conv-id"
            }))
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddAttachment(new TeamsAttachment { ContentType = "text/html" })
            .AddMention(new ConversationAccount { Id = "user-1", Name = "User" })
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("activity-123", activity.Id);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("<at>User</at> Test message", activity.Text);
        Assert.Equal("sender-id", activity.From.Id);
        Assert.Equal("recipient-id", activity.Recipient.Id);
        Assert.Equal("conv-id", activity.Conversation.Id);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count); // ClientInfo + Mention
        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
    }

    [Fact]
    public void FluentAPI_MethodChaining_ReturnsBuilderInstance()
    {
        TeamsActivityBuilder builder = new();

        TeamsActivityBuilder result1 = builder.WithId("id");
        TeamsActivityBuilder result2 = builder.WithText("text");
        TeamsActivityBuilder result3 = builder.WithType(ActivityTypes.Message);

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        TeamsActivityBuilder builder = new TeamsActivityBuilder()
            .WithId("test-id");

        TeamsActivity activity1 = builder.Build();
        TeamsActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
    }

    [Fact]
    public void Builder_ModifyingExistingActivity_PreservesOriginalData()
    {
        TeamsActivity original = new()
        {
            Id = "original-id",
            Text = "original text",
            Type = ActivityTypes.Message
        };

        TeamsActivity modified = new TeamsActivityBuilder(original)
            .WithText("modified text")
            .Build();

        Assert.Equal("original-id", modified.Id);
        Assert.Equal("modified text", modified.Text);
        Assert.Equal(ActivityTypes.Message, modified.Type);
    }

    [Fact]
    public void AddMention_UpdatesBaseEntityCollection()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "Test User"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .AddMention(account)
            .Build();

        CoreActivity baseActivity = activity;
        Assert.NotNull(baseActivity.Entities);
        Assert.NotEmpty(baseActivity.Entities);
    }

    [Fact]
    public void WithChannelData_NullValue_SetsToNull()
    {
        TeamsActivity activity = new TeamsActivityBuilder()
            .WithChannelData(null!)
            .Build();

        Assert.Null(activity.ChannelData);
    }

    [Fact]
    public void AddEntity_NullEntitiesCollection_InitializesCollection()
    {
        TeamsActivityBuilder builder = new();
        TeamsActivity activity = builder.Build();

        Assert.NotNull(activity.Entities);

        ClientInfoEntity entity = new() { Locale = "en-US" };
        builder.AddEntity(entity);

        TeamsActivity result = builder.Build();
        Assert.NotNull(result.Entities);
        Assert.Single(result.Entities);
    }

    [Fact]
    public void AddAttachment_NullAttachmentsCollection_InitializesCollection()
    {
        TeamsActivityBuilder builder = new();
        TeamsActivity activity = builder.Build();

        Assert.NotNull(activity.Attachments);

        TeamsAttachment attachment = new() { ContentType = "text/html" };
        builder.AddAttachment(attachment);

        TeamsActivity result = builder.Build();
        Assert.NotNull(result.Attachments);
        Assert.Single(result.Attachments);
    }

    [Fact]
    public void Builder_EmptyText_AddMention_PrependsMention()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "User"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .AddMention(account)
            .Build();

        Assert.Equal("<at>User</at> ", activity.Text);
    }

    [Fact]
    public void IntegrationTest_CreateComplexActivity()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/amer/test/");
        TeamsChannelData channelData = new()
        {
            TeamsChannelId = "19:channel@thread.tacv2",
            TeamsTeamId = "19:team@thread.tacv2"
        };

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithType(ActivityTypes.Message)
            .WithId("msg-001")
            .WithServiceUrl(serviceUrl)
            .WithChannelId("msteams")
            .WithText("Please review this document")
            .WithFrom(new TeamsConversationAccount(new ConversationAccount
            {
                Id = "bot-id",
                Name = "Bot"
            }))
            .WithRecipient(new TeamsConversationAccount(new ConversationAccount
            {
                Id = "user-id",
                Name = "User"
            }))
            .WithConversation(new TeamsConversation(new Conversation
            {
                Id = "conv-001"
            })
            {
                TenantId = "tenant-001",
                ConversationType = "channel"
            })
            .WithChannelData(channelData)
            .AddEntity(new ClientInfoEntity
            {
                Locale = "en-US",
                Country = "US",
                Platform = "Web"
            })
            .AddAttachment(new TeamsAttachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Name = "adaptive-card.json"
            })
            .AddMention(new ConversationAccount
            {
                Id = "manager-id",
                Name = "Manager"
            }, "Manager")
            .Build();

        // Verify all properties
        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("msg-001", activity.Id);
        Assert.Equal(serviceUrl, activity.ServiceUrl);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("<at>Manager</at> Please review this document", activity.Text);
        Assert.Equal("bot-id", activity.From.Id);
        Assert.Equal("user-id", activity.Recipient.Id);
        Assert.Equal("conv-001", activity.Conversation.Id);
        Assert.Equal("tenant-001", activity.Conversation.TenantId);
        Assert.Equal("channel", activity.Conversation.ConversationType);
        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel@thread.tacv2", activity.ChannelData.TeamsChannelId);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count); // ClientInfo + Mention
        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
    }
}
