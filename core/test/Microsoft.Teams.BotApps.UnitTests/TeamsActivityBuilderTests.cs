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
        var builder = new TeamsActivityBuilder();
        var activity = builder.Build();

        Assert.NotNull(activity);
        Assert.NotNull(activity.From);
        Assert.NotNull(activity.Recipient);
        Assert.NotNull(activity.Conversation);
    }

    [Fact]
    public void Constructor_WithExistingActivity_UsesProvidedActivity()
    {
        var existingActivity = new TeamsActivity
        {
            Id = "test-id",
            Text = "existing text"
        };

        var builder = new TeamsActivityBuilder(existingActivity);
        var activity = builder.Build();

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
        var activity = new TeamsActivityBuilder()
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void WithServiceUrl_SetsServiceUrl()
    {
        var serviceUrl = new Uri("https://smba.trafficmanager.net/teams/");

        var activity = new TeamsActivityBuilder()
            .WithServiceUrl(serviceUrl)
            .Build();

        Assert.Equal(serviceUrl, activity.ServiceUrl);
    }

    [Fact]
    public void WithChannelId_SetsChannelId()
    {
        var activity = new TeamsActivityBuilder()
            .WithChannelId("msteams")
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
    }

    [Fact]
    public void WithType_SetsActivityType()
    {
        var activity = new TeamsActivityBuilder()
            .WithType(ActivityTypes.Message)
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent()
    {
        var activity = new TeamsActivityBuilder()
            .WithText("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Text);
    }

    [Fact]
    public void WithFrom_SetsSenderAccount()
    {
        var fromAccount = new TeamsConversationAccount(new ConversationAccount
        {
            Id = "sender-id",
            Name = "Sender Name"
        });

        var activity = new TeamsActivityBuilder()
            .WithFrom(fromAccount)
            .Build();

        Assert.Equal("sender-id", activity.From.Id);
        Assert.Equal("Sender Name", activity.From.Name);
    }

    [Fact]
    public void WithRecipient_SetsRecipientAccount()
    {
        var recipientAccount = new TeamsConversationAccount(new ConversationAccount
        {
            Id = "recipient-id",
            Name = "Recipient Name"
        });

        var activity = new TeamsActivityBuilder()
            .WithRecipient(recipientAccount)
            .Build();

        Assert.Equal("recipient-id", activity.Recipient.Id);
        Assert.Equal("Recipient Name", activity.Recipient.Name);
    }

    [Fact]
    public void WithConversation_SetsConversationInfo()
    {
        var conversation = new TeamsConversation(new Conversation
        {
            Id = "conversation-id"
        })
        {
            TenantId = "tenant-123",
            ConversationType = "channel"
        };

        var activity = new TeamsActivityBuilder()
            .WithConversation(conversation)
            .Build();

        Assert.Equal("conversation-id", activity.Conversation.Id);
        Assert.Equal("tenant-123", activity.Conversation.TenantId);
        Assert.Equal("channel", activity.Conversation.ConversationType);
    }

    [Fact]
    public void WithChannelData_SetsChannelData()
    {
        var channelData = new TeamsChannelData
        {
            TeamsChannelId = "19:channel-id@thread.tacv2",
            TeamsTeamId = "19:team-id@thread.tacv2"
        };

        var activity = new TeamsActivityBuilder()
            .WithChannelData(channelData)
            .Build();

        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel-id@thread.tacv2", activity.ChannelData.TeamsChannelId);
        Assert.Equal("19:team-id@thread.tacv2", activity.ChannelData.TeamsTeamId);
    }

    [Fact]
    public void WithEntities_SetsEntitiesCollection()
    {
        var entities = new EntityList
        {
            new ClientInfoEntity
            {
                Locale = "en-US",
                Platform = "Web"
            }
        };

        var activity = new TeamsActivityBuilder()
            .WithEntities(entities)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void WithAttachments_SetsAttachmentsCollection()
    {
        var attachments = new List<TeamsAttachment>
        {
            new TeamsAttachment
            {
                ContentType = "application/json",
                Name = "test-attachment"
            }
        };

        var activity = new TeamsActivityBuilder()
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
        var entity = new ClientInfoEntity
        {
            Locale = "en-US",
            Country = "US"
        };

        var activity = new TeamsActivityBuilder()
            .AddEntity(entity)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void AddEntity_MultipleEntities_AddsAllToCollection()
    {
        var activity = new TeamsActivityBuilder()
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddEntity(new ProductInfoEntity { Id = "product-123" })
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);
    }

    [Fact]
    public void AddAttachment_AddsAttachmentToCollection()
    {
        var attachment = new TeamsAttachment
        {
            ContentType = "text/html",
            Name = "test.html"
        };

        var activity = new TeamsActivityBuilder()
            .AddAttachment(attachment)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("text/html", activity.Attachments[0].ContentType);
    }

    [Fact]
    public void AddAttachment_MultipleAttachments_AddsAllToCollection()
    {
        var activity = new TeamsActivityBuilder()
            .AddAttachment(new TeamsAttachment { ContentType = "text/html" })
            .AddAttachment(new TeamsAttachment { ContentType = "application/json" })
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Equal(2, activity.Attachments.Count);
    }

    [Fact]
    public void AddMention_WithNullAccount_ThrowsArgumentNullException()
    {
        var builder = new TeamsActivityBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddMention(null!));
    }

    [Fact]
    public void AddMention_WithAccountAndDefaultText_AddsMentionAndUpdatesText()
    {
        var account = new ConversationAccount
        {
            Id = "user-123",
            Name = "John Doe"
        };

        var activity = new TeamsActivityBuilder()
            .WithText("said hello")
            .AddMention(account)
            .Build();

        Assert.Equal("<at>John Doe</at> said hello", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        var mention = activity.Entities[0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("user-123", mention.Mentioned?.Id);
        Assert.Equal("John Doe", mention.Mentioned?.Name);
        Assert.Equal("<at>John Doe</at>", mention.Text);
    }

    [Fact]
    public void AddMention_WithCustomText_UsesCustomText()
    {
        var account = new ConversationAccount
        {
            Id = "user-123",
            Name = "John Doe"
        };

        var activity = new TeamsActivityBuilder()
            .WithText("replied")
            .AddMention(account, "CustomName")
            .Build();

        Assert.Equal("<at>CustomName</at> replied", activity.Text);

        var mention = activity.Entities![0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("<at>CustomName</at>", mention.Text);
    }

    [Fact]
    public void AddMention_WithAddTextFalse_DoesNotUpdateText()
    {
        var account = new ConversationAccount
        {
            Id = "user-123",
            Name = "John Doe"
        };

        var activity = new TeamsActivityBuilder()
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
        var account1 = new ConversationAccount { Id = "user-1", Name = "User One" };
        var account2 = new ConversationAccount { Id = "user-2", Name = "User Two" };

        var activity = new TeamsActivityBuilder()
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
        var activity = new TeamsActivityBuilder()
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
        var builder = new TeamsActivityBuilder();

        var result1 = builder.WithId("id");
        var result2 = builder.WithText("text");
        var result3 = builder.WithType(ActivityTypes.Message);

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        var builder = new TeamsActivityBuilder()
            .WithId("test-id");

        var activity1 = builder.Build();
        var activity2 = builder.Build();

        Assert.Same(activity1, activity2);
    }

    [Fact]
    public void Builder_ModifyingExistingActivity_PreservesOriginalData()
    {
        var original = new TeamsActivity
        {
            Id = "original-id",
            Text = "original text",
            Type = ActivityTypes.Message
        };

        var modified = new TeamsActivityBuilder(original)
            .WithText("modified text")
            .Build();

        Assert.Equal("original-id", modified.Id);
        Assert.Equal("modified text", modified.Text);
        Assert.Equal(ActivityTypes.Message, modified.Type);
    }

    [Fact]
    public void AddMention_UpdatesBaseEntityCollection()
    {
        var account = new ConversationAccount
        {
            Id = "user-123",
            Name = "Test User"
        };

        var activity = new TeamsActivityBuilder()
            .AddMention(account)
            .Build();

        var baseActivity = (CoreActivity)activity;
        Assert.NotNull(baseActivity.Entities);
        Assert.NotEmpty(baseActivity.Entities);
    }

    [Fact]
    public void WithChannelData_NullValue_SetsToNull()
    {
        var activity = new TeamsActivityBuilder()
            .WithChannelData(null!)
            .Build();

        Assert.Null(activity.ChannelData);
    }

    [Fact]
    public void AddEntity_NullEntitiesCollection_InitializesCollection()
    {
        var builder = new TeamsActivityBuilder();
        var activity = builder.Build();
        
        // Ensure entities is null initially
        Assert.Null(activity.Entities);

        // Add entity through builder
        var entity = new ClientInfoEntity { Locale = "en-US" };
        builder.AddEntity(entity);
        
        var result = builder.Build();
        Assert.NotNull(result.Entities);
        Assert.Single(result.Entities);
    }

    [Fact]
    public void AddAttachment_NullAttachmentsCollection_InitializesCollection()
    {
        var builder = new TeamsActivityBuilder();
        var activity = builder.Build();
        
        // Ensure attachments is null initially
        Assert.Null(activity.Attachments);

        // Add attachment through builder
        var attachment = new TeamsAttachment { ContentType = "text/html" };
        builder.AddAttachment(attachment);
        
        var result = builder.Build();
        Assert.NotNull(result.Attachments);
        Assert.Single(result.Attachments);
    }

    [Fact]
    public void Builder_EmptyText_AddMention_PrependsMention()
    {
        var account = new ConversationAccount
        {
            Id = "user-123",
            Name = "User"
        };

        var activity = new TeamsActivityBuilder()
            .AddMention(account)
            .Build();

        Assert.Equal("<at>User</at> ", activity.Text);
    }

    [Fact]
    public void IntegrationTest_CreateComplexActivity()
    {
        var serviceUrl = new Uri("https://smba.trafficmanager.net/amer/test/");
        var channelData = new TeamsChannelData
        {
            TeamsChannelId = "19:channel@thread.tacv2",
            TeamsTeamId = "19:team@thread.tacv2"
        };

        var activity = new TeamsActivityBuilder()
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
