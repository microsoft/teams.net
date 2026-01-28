// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class TeamsActivityBuilderTests
{
    private readonly TeamsActivityBuilder builder;
    public TeamsActivityBuilderTests()
    {
        builder = TeamsActivity.CreateBuilder();
    }

    [Fact]
    public void Constructor_DefaultConstructor_CreatesNewActivity()
    {
        TeamsActivity activity = builder.Build();

        Assert.NotNull(activity);
        Assert.NotNull(activity.Conversation);
    }

    [Fact]
    public void Constructor_WithExistingActivity_UsesProvidedActivity()
    {
        TeamsActivity existingActivity = new()
        {
            Id = "test-id"
        };
        existingActivity.Properties["text"] = "existing text";

        TeamsActivityBuilder taBuilder = TeamsActivity.CreateBuilder(existingActivity);
        TeamsActivity activity = taBuilder.Build();

        Assert.Equal("test-id", activity.Id);
        Assert.Equal("existing text", activity.Properties["text"]);
    }

    [Fact]
    public void Constructor_WithNullActivity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TeamsActivity.CreateBuilder(null!));
    }

    [Fact]
    public void WithId_SetsActivityId()
    {
        var activity = builder
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void WithServiceUrl_SetsServiceUrl()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/teams/");

        var activity = builder
            .WithServiceUrl(serviceUrl)
            .Build();

        Assert.Equal(serviceUrl, activity.ServiceUrl);
    }

    [Fact]
    public void WithChannelId_SetsChannelId()
    {
        var activity = builder
            .WithChannelId("msteams")
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
    }

    [Fact]
    public void WithType_SetsActivityType()
    {
        var activity = builder
            .WithType(ActivityType.Message)
            .Build();

        Assert.Equal(ActivityType.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent()
    {
        var activity = builder
            .WithText("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Properties["text"]);
    }

    [Fact]
    public void WithFrom_SetsSenderAccount()
    {
        TeamsConversationAccount fromAccount = new(new ConversationAccount
        {
            Id = "sender-id",
            Name = "Sender Name"
        });

        var activity = builder
            .WithFrom(fromAccount)
            .Build();

        Assert.NotNull(activity.From);
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

        var activity = builder
            .WithRecipient(recipientAccount)
            .Build();

        Assert.NotNull(activity.Recipient);
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

        var activity = builder
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

        var activity = builder
            .WithChannelData(channelData)
            .Build();

        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel-id@thread.tacv2", activity.ChannelData.TeamsChannelId);
        Assert.Equal("19:team-id@thread.tacv2", activity.ChannelData.TeamsTeamId);
    }

    [Fact]
    public void WithEntities_SetsEntitiesCollection()
    {
        EntityList entities =
        [
            new ClientInfoEntity
            {
                Locale = "en-US",
                Platform = "Web"
            }
        ];

        var activity = builder
            .WithEntities(entities)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void WithAttachments_SetsAttachmentsCollection()
    {
        List<TeamsAttachment> attachments =
        [
            new() {
                ContentType = "application/json",
                Name = "test-attachment"
            }
        ];

        var activity = builder
            .WithAttachments(attachments)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("application/json", activity.Attachments[0].ContentType);
        Assert.Equal("test-attachment", activity.Attachments[0].Name);
    }

    [Fact]
    public void WithAttachment_SetsSingleAttachment()
    {
        TeamsAttachment attachment = new()
        {
            ContentType = "application/json",
            Name = "single"
        };

        var activity = builder
            .WithAttachment(attachment)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("single", activity.Attachments[0].Name);
    }

    [Fact]
    public void AddEntity_AddsEntityToCollection()
    {
        ClientInfoEntity entity = new()
        {
            Locale = "en-US",
            Country = "US"
        };

        var activity = builder
            .AddEntity(entity)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void AddEntity_MultipleEntities_AddsAllToCollection()
    {
        var activity = builder
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

        var activity = builder
            .AddAttachment(attachment)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("text/html", activity.Attachments[0].ContentType);
    }

    [Fact]
    public void AddAttachment_MultipleAttachments_AddsAllToCollection()
    {
        var activity = builder
            .AddAttachment(new TeamsAttachment { ContentType = "text/html" })
            .AddAttachment(new TeamsAttachment { ContentType = "application/json" })
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Equal(2, activity.Attachments.Count);
    }

    [Fact]
    public void AddAdaptiveCardAttachment_AddsAdaptiveCard()
    {
        var adaptiveCard = new { type = "AdaptiveCard", version = "1.2" };

        var activity = builder
            .AddAdaptiveCardAttachment(adaptiveCard)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("application/vnd.microsoft.card.adaptive", activity.Attachments[0].ContentType);
        Assert.Same(adaptiveCard, activity.Attachments[0].Content);
    }

    [Fact]
    public void WithAdaptiveCardAttachment_ConfigureActionAppliesChanges()
    {
        var adaptiveCard = new { type = "AdaptiveCard" };

        var activity = builder
            .WithAdaptiveCardAttachment(adaptiveCard, b => b.WithName("feedback"))
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("feedback", activity.Attachments[0].Name);
    }

    [Fact]
    public void AddAdaptiveCardAttachment_WithNullPayload_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => builder.AddAdaptiveCardAttachment(null!));
    }

    [Fact]
    public void AddMention_WithNullAccount_ThrowsArgumentNullException()
    {
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

        var activity = builder
            .WithText("said hello")
            .AddMention(account)
            .Build();

        Assert.Equal("<at>John Doe</at> said hello", activity.Properties["text"]);
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

        var activity = builder
            .WithText("replied")
            .AddMention(account, "CustomName")
            .Build();

        Assert.Equal("<at>CustomName</at> replied", activity.Properties["text"]);

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

        TeamsActivity activity = builder
            .WithText("original text")
            .AddMention(account, addText: false)
            .Build();

        Assert.Equal("original text", activity.Properties["text"]);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
    }

    [Fact]
    public void AddMention_MultipleMentions_AddsAllMentions()
    {
        ConversationAccount account1 = new() { Id = "user-1", Name = "User One" };
        ConversationAccount account2 = new() { Id = "user-2", Name = "User Two" };

        TeamsActivity activity = builder
            .WithText("message")
            .AddMention(account1)
            .AddMention(account2)
            .Build();

        Assert.Equal("<at>User Two</at> <at>User One</at> message", activity.Properties["text"]);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);
    }

    [Fact]
    public void FluentAPI_CompleteActivity_BuildsCorrectly()
    {
        TeamsActivity activity = builder
            .WithType(ActivityType.Message)
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

        Assert.Equal(ActivityType.Message, activity.Type);
        Assert.Equal("activity-123", activity.Id);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("<at>User</at> Test message", activity.Properties["text"]);
        Assert.NotNull(activity.From);
        Assert.Equal("sender-id", activity.From.Id);
        Assert.NotNull(activity.Recipient);
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

        TeamsActivityBuilder result1 = builder.WithId("id");
        TeamsActivityBuilder result2 = builder.WithText("text");
        TeamsActivityBuilder result3 = builder.WithType(ActivityType.Message);

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        builder
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
            Type = ActivityType.Message
        };
        original.Properties["text"] = "original text";

        TeamsActivity modified = TeamsActivity.CreateBuilder(original)
            .WithText("modified text")
            .Build();

        Assert.Equal("original-id", modified.Id);
        Assert.Equal("modified text", modified.Properties["text"]);
        Assert.Equal(ActivityType.Message, modified.Type);
    }

    [Fact]
    public void AddMention_UpdatesBaseEntityCollection()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "Test User"
        };

        TeamsActivity activity = builder
            .AddMention(account)
            .Build();

        CoreActivity baseActivity = activity;
        Assert.NotNull(baseActivity.Entities);
        Assert.NotEmpty(baseActivity.Entities);
    }

    [Fact]
    public void WithChannelData_NullValue_SetsToNull()
    {
        TeamsChannelData channelData = new() { TeamsChannelId = "channel-1" };
        TeamsActivityBuilder testBuilder = TeamsActivity.CreateBuilder();
        testBuilder.WithChannelData(channelData);
        testBuilder.WithChannelData(null!);
        TeamsActivity activity = testBuilder.Build();

        Assert.Null(activity.ChannelData);
    }

    [Fact]
    public void AddEntity_InitializesCollectionIfNeeded()
    {
        ClientInfoEntity entity = new() { Locale = "en-US" };
        builder.AddEntity(entity);

        TeamsActivity result = builder.Build();
        Assert.NotNull(result.Entities);
        Assert.Single(result.Entities);
    }

    [Fact]
    public void AddAttachment_InitializesCollectionIfNeeded()
    {
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

        TeamsActivity activity = builder
            .AddMention(account)
            .Build();

        Assert.Equal("<at>User</at> ", activity.Properties["text"]);
    }

    [Fact]
    public void WithConversationReference_WithNullActivity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(null!));
    }

    [Fact]
    public void WithConversationReference_WithNullChannelId_DoesNotThrow()
    {

        TeamsActivity sourceActivity = TeamsActivity.CreateBuilder()
            .WithServiceUrl(new Uri("https://test.com"))
            .WithConversation(new TeamsConversation(new Conversation()))
            .WithFrom(new TeamsConversationAccount(new ConversationAccount()))
            .WithRecipient(new TeamsConversationAccount(new ConversationAccount { Id = "bot-1" }))
            .Build();

        TeamsActivity result = builder.WithConversationReference(sourceActivity).Build();
        // ChannelId is not set by WithConversationReference when null
        Assert.Equal(ActivityType.Message, result.Type);
        Assert.NotNull(result.From);
    }

    [Fact]
    public void WithConversationReference_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = null,
            Conversation = new TeamsConversation(new Conversation()),
            From = new TeamsConversationAccount(new ConversationAccount()),
            Recipient = new TeamsConversationAccount(new ConversationAccount())
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithEmptyConversationId_SetsFromRecipient()
    {
        TeamsActivity sourceActivity = TeamsActivity.CreateBuilder()
            .WithChannelId("msteams")
            .WithServiceUrl(new Uri("https://test.com"))
            .WithConversation(new TeamsConversation(new Conversation()))
            .WithFrom(new TeamsConversationAccount(new ConversationAccount { Id = "user-1" }))
            .WithRecipient(new TeamsConversationAccount(new ConversationAccount { Id = "bot-1" }))
            .Build();

        TeamsActivity result = builder.WithConversationReference(sourceActivity).Build();

        Assert.NotNull(result.Conversation);
        Assert.NotNull(result.From);
        Assert.Equal("bot-1", result.From.Id);
    }

    [Fact]
    public void WithConversationReference_WithEmptyFromId_SetsFromRecipient()
    {
        TeamsActivity sourceActivity = TeamsActivity.CreateBuilder()
            .WithChannelId("msteams")
            .WithServiceUrl(new Uri("https://test.com"))
            .WithConversation(new TeamsConversation(new Conversation { Id = "conv-1" }))
            .WithFrom(new TeamsConversationAccount(new ConversationAccount()))
            .WithRecipient(new TeamsConversationAccount(new ConversationAccount { Id = "bot-1" }))
            .Build();

        TeamsActivity result = builder.WithConversationReference(sourceActivity).Build();

        Assert.NotNull(result.From);
        Assert.Equal("bot-1", result.From.Id);
    }

    [Fact]
    public void WithConversationReference_WithEmptyRecipientId_ThrowsArgumentNullException()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = new TeamsConversation(new Conversation { Id = "conv-1" }),
            From = new TeamsConversationAccount(new ConversationAccount { Id = "user-1" }),
            Recipient = null!
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithFrom_WithBaseConversationAccount_ConvertsToTeamsConversationAccount()
    {
        ConversationAccount baseAccount = new()
        {
            Id = "user-123",
            Name = "User Name"
        };

        TeamsActivity activity = builder
            .WithFrom(baseAccount)
            .Build();

        Assert.NotNull(activity.From);
        Assert.IsType<TeamsConversationAccount>(activity.From);
        Assert.Equal("user-123", activity.From.Id);
        Assert.Equal("User Name", activity.From.Name);
    }

    [Fact]
    public void WithRecipient_WithBaseConversationAccount_ConvertsToTeamsConversationAccount()
    {
        ConversationAccount baseAccount = new()
        {
            Id = "bot-123",
            Name = "Bot Name"
        };

        TeamsActivity activity = builder
            .WithRecipient(baseAccount)
            .Build();

        Assert.NotNull(activity.Recipient);
        Assert.IsType<TeamsConversationAccount>(activity.Recipient);
        Assert.Equal("bot-123", activity.Recipient.Id);
        Assert.Equal("Bot Name", activity.Recipient.Name);
    }

    [Fact]
    public void WithConversation_WithBaseConversation_ConvertsToTeamsConversation()
    {
        Conversation baseConversation = new()
        {
            Id = "conv-123"
        };

        TeamsActivity activity = builder
            .WithConversation(baseConversation)
            .Build();

        Assert.IsType<TeamsConversation>(activity.Conversation);
        Assert.Equal("conv-123", activity.Conversation.Id);
    }

    [Fact]
    public void WithEntities_WithNullValue_SetsToNull()
    {
        TeamsActivity activity = builder
            .WithEntities([new ClientInfoEntity()])
            .WithEntities(null!)
            .Build();

        Assert.Null(activity.Entities);
    }

    [Fact]
    public void WithAttachments_WithNullValue_SetsToNull()
    {
        TeamsActivity activity = builder
            .WithAttachments([new()])
            .WithAttachments(null!)
            .Build();

        Assert.Null(activity.Attachments);
    }

    [Fact]
    public void AddMention_WithAccountWithNullName_UsesNullText()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = null
        };

        TeamsActivity activity = builder
            .WithText("message")
            .AddMention(account)
            .Build();

        Assert.Equal("<at></at> message", activity.Properties["text"]);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
    }

    [Fact]
    public void Build_MultipleCalls_ReturnsRebasedActivity()
    {
        builder
            .AddEntity(new ClientInfoEntity { Locale = "en-US" });

        TeamsActivity activity1 = builder.Build();
        CoreActivity baseActivity1 = activity1;
        Assert.NotNull(baseActivity1.Entities);

        builder.AddEntity(new ProductInfoEntity { Id = "prod-1" });
        TeamsActivity activity2 = builder.Build();
        CoreActivity baseActivity2 = activity2;

        Assert.Same(activity1, activity2);
        Assert.NotNull(baseActivity2.Entities);
        Assert.Equal(2, activity2.Entities!.Count);
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

        TeamsActivity activity = builder
            .WithType(ActivityType.Message)
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
        Assert.Equal(ActivityType.Message, activity.Type);
        Assert.Equal("msg-001", activity.Id);
        Assert.Equal(serviceUrl, activity.ServiceUrl);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("<at>Manager</at> Please review this document", activity.Properties["text"]);
        Assert.NotNull(activity.From);
        Assert.Equal("bot-id", activity.From.Id);
        Assert.NotNull(activity.Recipient);
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
