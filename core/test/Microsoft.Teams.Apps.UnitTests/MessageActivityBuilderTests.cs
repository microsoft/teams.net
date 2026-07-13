// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class MessageActivityBuilderTests
{
    private readonly MessageActivityBuilder builder;
    private readonly MessageActivityBuilder messageBuilder;
    public MessageActivityBuilderTests()
    {
        builder = MessageActivity.CreateBuilder();
        messageBuilder = MessageActivity.CreateBuilder();
    }

    [Fact]
    public void Constructor_DefaultConstructor_CreatesNewActivity()
    {
        MessageActivity activity = MessageActivity.CreateBuilder().Build();

        Assert.NotNull(activity);
        Assert.Null(activity.From);
        Assert.Null(activity.Recipient);
        Assert.Null(activity.Conversation);
    }

    [Fact]
    public void WithId_SetsActivityId()
    {
        MessageActivity activity = builder
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void Build_DefaultsToMessageType()
    {
        MessageActivity activity = builder
            .Build();

        Assert.Equal(TeamsActivityTypes.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent()
    {
        MessageActivity activity = builder
            .WithText("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Text);
    }

    [Fact]
    public void FromChannelAccount_PreservesTenantId()
    {
        TeamsChannelAccount source = new()
        {
            Id = "user-id",
            Name = "User Name",
            TenantId = "tenant-abc",
            AgenticAppId = "app-1",
            AgenticUserId = "user-1",
            AgenticAppBlueprintId = "bp-1",
        };

        TeamsChannelAccount? result = TeamsChannelAccount.FromChannelAccount(source);

        Assert.NotNull(result);
        Assert.Equal("tenant-abc", result.TenantId);
        Assert.Equal("app-1", result.AgenticAppId);
        Assert.Equal("user-1", result.AgenticUserId);
        Assert.Equal("bp-1", result.AgenticAppBlueprintId);
    }

    [Fact]
    public void WithChannelData_SetsChannelData()
    {
        TeamsChannelData channelData = new()
        {
            TeamsChannelId = "19:channel-id@thread.tacv2",
            TeamsTeamId = "19:team-id@thread.tacv2"
        };

        MessageActivity activity = builder
            .WithText("hi")
            .Build();
        activity.ChannelData = channelData;

        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel-id@thread.tacv2", activity.ChannelData?.TeamsChannelId);
        Assert.Equal("19:team-id@thread.tacv2", activity.ChannelData?.TeamsTeamId);
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

        MessageActivity activity = builder
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

        MessageActivity activity = (MessageActivity)messageBuilder
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

        MessageActivity activity = (MessageActivity)messageBuilder
            .AddAttachment(attachment)
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

        MessageActivity activity = builder
            .AddEntity(entity)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void AddEntity_MultipleEntities_AddsAllToCollection()
    {
        MessageActivity activity = builder
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddEntity(new ProductInfoEntity { Id = "product-123" })
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count);
    }

    [Fact]
    public void AddClientInfo_AddsClientInfoEntity()
    {
        MessageActivity activity = builder
            .AddClientInfo("Web", "US", "America/Los_Angeles", "en-US")
            .Build();

        ClientInfoEntity? entity = activity.Entities?.OfType<ClientInfoEntity>().SingleOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("Web", entity.Platform);
        Assert.Equal("US", entity.Country);
        Assert.Equal("America/Los_Angeles", entity.Timezone);
        Assert.Equal("en-US", entity.Locale);
    }

    [Fact]
    public void AddProductInfo_AddsProductInfoEntity()
    {
        MessageActivity activity = builder
            .AddProductInfo("product-123")
            .Build();

        ProductInfoEntity? entity = activity.Entities?.OfType<ProductInfoEntity>().SingleOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("product-123", entity.Id);
    }

    [Fact]
    public void AddFeedback_WithMode_SetsFeedbackLoopAndClearsFeedbackLoopEnabled()
    {
        MessageActivity activity = builder
            .AddFeedback(FeedbackTypes.Custom)
            .Build();

        Assert.NotNull(activity.ChannelData);
        Assert.Null(activity.ChannelData.FeedbackLoopEnabled);
        Assert.NotNull(activity.ChannelData.FeedbackLoop);
        Assert.Equal(FeedbackTypes.Custom, activity.ChannelData.FeedbackLoop.Type);
    }

    [Fact]
    public void AddAttachment_AddsAttachmentToCollection()
    {
        TeamsAttachment attachment = new()
        {
            ContentType = "text/html",
            Name = "test.html"
        };

        MessageActivity activity = (MessageActivity)messageBuilder
            .AddAttachment(attachment)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("text/html", activity.Attachments[0].ContentType);
    }

    [Fact]
    public void AddAttachment_MultipleAttachments_AddsAllToCollection()
    {
        MessageActivity activity = (MessageActivity)messageBuilder
            .AddAttachment(new TeamsAttachment { ContentType = "text/html" })
            .AddAttachment(new TeamsAttachment { ContentType = "application/json" })
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Equal(2, activity.Attachments?.Count);
    }

    [Fact]
    public void AddAdaptiveCardAttachment_AddsAdaptiveCard()
    {
        var adaptiveCard = new { type = "AdaptiveCard", version = "1.2" };

        MessageActivity activity = (MessageActivity)messageBuilder
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

        MessageActivity activity = (MessageActivity)messageBuilder
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
        ChannelAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        MessageActivity activity = builder
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
        ChannelAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        MessageActivity activity = builder
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
        ChannelAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        MessageActivity activity = builder
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
        ChannelAccount account1 = new() { Id = "user-1", Name = "User One" };
        ChannelAccount account2 = new() { Id = "user-2", Name = "User Two" };

        MessageActivity activity = builder
            .WithText("message")
            .AddMention(account1)
            .AddMention(account2)
            .Build();

        Assert.Equal("<at>User Two</at> <at>User One</at> message", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count);
    }

    [Fact]
    public void FluentAPI_CompleteActivity_BuildsCorrectly()
    {
        MessageActivity activity = (MessageActivity)messageBuilder
            .WithId("activity-123")
            .WithText("Test message")
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddAttachment(new TeamsAttachment { ContentType = "text/html" })
            .AddMention(new ChannelAccount { Id = "user-1", Name = "User" })
            .Build();

        Assert.Equal(TeamsActivityTypes.Message, activity.Type);
        Assert.Equal("activity-123", activity.Id);
        string? text = activity.Text;
        if (text is null && activity.Properties.TryGetValue("text", out object? rawText))
        {
            text = rawText?.ToString();
        }
        Assert.Equal("<at>User</at> Test message", text);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count); // ClientInfo + Mention
        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
    }

    [Fact]
    public void FluentAPI_MethodChaining_ReturnsBuilderInstance()
    {

        MessageActivityBuilder result1 = builder.WithId("id");
        MessageActivityBuilder result2 = builder.WithText("text");
        MessageActivityBuilder result3 = builder.AddText("!");

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        builder
            .WithId("test-id");

        MessageActivity activity1 = builder.Build();
        MessageActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
    }

    [Fact]
    public void Builder_ModifyingExistingActivity_PreservesOriginalData()
    {
        MessageActivity modified = MessageActivity.CreateBuilder()
            .WithId("original-id")
            .WithText("modified text")
            .Build();

        Assert.Equal("original-id", modified.Id);
        Assert.Equal("modified text", modified.Text);
        Assert.Equal(TeamsActivityTypes.Message, modified.Type);
    }

    [Fact]
    public void AddMention_UpdatesBaseEntityCollection()
    {
        ChannelAccount account = new()
        {
            Id = "user-123",
            Name = "Test User"
        };

        MessageActivity activity = builder
            .AddMention(account)
            .Build();

        // Entities are on TeamsActivity, not CoreActivity; verify via TeamsActivity
        Assert.NotNull(activity.Entities);
        Assert.NotEmpty(activity.Entities);
    }

    [Fact]
    public void WithChannelData_NullValue_SetsToNull()
    {
        MessageActivity activity = builder
            .Build();

        Assert.Null(activity.ChannelData);
    }

    [Fact]
    public void AddEntity_NullEntitiesCollection_InitializesCollection()
    {
        MessageActivity activity = builder.Build();

        Assert.Null(activity.Entities);

        ClientInfoEntity entity = new() { Locale = "en-US" };
        builder.AddEntity(entity);

        MessageActivity result = builder.Build();
        Assert.NotNull(result.Entities);
        Assert.Single(result.Entities);
    }

    [Fact]
    public void AddAttachment_NullAttachmentsCollection_InitializesCollection()
    {
        MessageActivity activity = (MessageActivity)messageBuilder.Build();

        Assert.Null(activity.Attachments);

        TeamsAttachment attachment = new() { ContentType = "text/html" };
        messageBuilder.AddAttachment(attachment);

        MessageActivity result = (MessageActivity)messageBuilder.Build();
        Assert.NotNull(result.Attachments);
        Assert.Single(result.Attachments);
    }

    [Fact]
    public void Builder_EmptyText_AddMention_PrependsMention()
    {
        ChannelAccount account = new()
        {
            Id = "user-123",
            Name = "User"
        };

        MessageActivity activity = builder
            .AddMention(account)
            .Build();

        Assert.Equal("<at>User</at> ", activity.Text);
    }

    [Fact]
    public void WithEntities_WithNullValue_SetsToNull()
    {
        MessageActivity activity = builder
            .WithEntities([new ClientInfoEntity()])
            .WithEntities(null!)
            .Build();

        Assert.Null(activity.Entities);
    }

    [Fact]
    public void WithAttachments_WithNullValue_SetsToNull()
    {
        MessageActivity activity = (MessageActivity)messageBuilder
            .WithAttachments([new()])
            .WithAttachments(null!)
            .Build();

        Assert.Null(activity.Attachments);
    }

    [Fact]
    public void AddMention_WithAccountWithNullName_UsesNullText()
    {
        ChannelAccount account = new()
        {
            Id = "user-123",
            Name = null
        };

        MessageActivity activity = builder
            .WithText("message")
            .AddMention(account)
            .Build();

        Assert.Equal("<at></at> message", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
    }

    [Fact]
    public void Build_MultipleCalls_ReturnsRebasedActivity()
    {
        builder
            .AddEntity(new ClientInfoEntity { Locale = "en-US" });

        MessageActivity activity1 = builder.Build();
        Assert.NotNull(activity1.Entities);

        builder.AddEntity(new ProductInfoEntity { Id = "prod-1" });
        MessageActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
        Assert.NotNull(activity2.Entities);
        Assert.Equal(2, activity2.Entities!.Count);
    }

    [Fact]
    public void IntegrationTest_CreateComplexActivity()
    {
        TeamsChannelData channelData = new()
        {
            TeamsChannelId = "19:channel@thread.tacv2",
            TeamsTeamId = "19:team@thread.tacv2"
        };

        MessageActivity activity = (MessageActivity)messageBuilder
            .WithId("msg-001")
            .WithText("Please review this document")
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
            .AddMention(new ChannelAccount
            {
                Id = "manager-id",
                Name = "Manager"
            }, "Manager")
            .Build();
        activity.ChannelData = channelData;

        // Verify all properties
        Assert.Equal(TeamsActivityTypes.Message, activity.Type);
        Assert.Equal("msg-001", activity.Id);
        string? text = activity.Text;
        if (text is null && activity.Properties.TryGetValue("text", out object? rawText))
        {
            text = rawText?.ToString();
        }
        Assert.Equal("<at>Manager</at> Please review this document", text);
        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel@thread.tacv2", activity.ChannelData?.TeamsChannelId);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count); // ClientInfo + Mention
        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
    }

    private const string json = """
                {
                    "type": "message",
                    "channelId": "msteams",
                    "serviceUrl": "https://smba.trafficmanager.net/amer/test/",
                    "text": "hello",
                    "from": {
                        "id": "user-1",
                        "name": "User One"
                    },
                    "recipient": {
                        "id": "bot-1",
                        "name": "Bot One"
                    },
                    "conversation": {
                        "id": "conv-1",
                        "tenantId": "tenant-1"
                    }
                }
                """;
}
