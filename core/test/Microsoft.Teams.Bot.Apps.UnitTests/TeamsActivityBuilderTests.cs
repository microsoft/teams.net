// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

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
        TeamsActivity activity = TeamsActivity.CreateBuilder().Build();

        Assert.NotNull(activity);
        Assert.Null(activity.From);
        Assert.Null(activity.Recipient);
        Assert.Null(activity.Conversation);
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
        TeamsActivity activity = builder
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void WithServiceUrl_SetsServiceUrl()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/teams/");

        TeamsActivity activity = builder
            .WithServiceUrl(serviceUrl)
            .Build();

        Assert.Equal(serviceUrl, activity.ServiceUrl);
    }

    [Fact]
    public void WithChannelId_SetsChannelId()
    {
        TeamsActivity activity = builder
            .WithChannelId("msteams")
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
    }

    [Fact]
    public void WithType_SetsActivityType()
    {
        TeamsActivity activity = builder
            .WithType(TeamsActivityType.Message)
            .Build();

        Assert.Equal(TeamsActivityType.Message, activity.Type);
    }

    [Fact]
    public void WithFrom_SetsSenderAccount()
    {
        TeamsConversationAccount? fromAccount = TeamsConversationAccount.FromConversationAccount(new ConversationAccount
        {
            Id = "sender-id",
            Name = "Sender Name"
        });

        TeamsActivity activity = builder
            .WithFrom(fromAccount)
            .Build();

        Assert.Equal("sender-id", activity.From?.Id);
        Assert.Equal("Sender Name", activity.From?.Name);
    }

    [Fact]
    public void WithRecipient_SetsRecipientAccount()
    {
        TeamsConversationAccount? recipientAccount = TeamsConversationAccount.FromConversationAccount(new ConversationAccount
        {
            Id = "recipient-id",
            Name = "Recipient Name"
        });
        Assert.NotNull(recipientAccount);
        TeamsActivity activity = builder
            .WithRecipient(recipientAccount)
            .Build();

        Assert.Equal("recipient-id", activity.Recipient?.Id);
        Assert.Equal("Recipient Name", activity.Recipient?.Name);
    }

    [Fact]
    public void WithConversation_SetsConversationInfo()
    {
        Conversation baseConversation = new Conversation("conversation-id");
        baseConversation.Properties.Add("tenantId", "tenant-123");
        baseConversation.Properties.Add("conversationType", "channel");
        TeamsConversation? conversation = TeamsConversation.FromConversation(baseConversation);

        TeamsActivity activity = builder
            .WithConversation(conversation)
            .Build();

        Assert.Equal("conversation-id", activity.Conversation?.Id);
        Assert.Equal("tenant-123", activity.Conversation?.TenantId);
        Assert.Equal("channel", activity.Conversation?.ConversationType);
    }

    [Fact]
    public void WithChannelData_SetsChannelData()
    {
        TeamsChannelData channelData = new()
        {
            TeamsChannelId = "19:channel-id@thread.tacv2",
            TeamsTeamId = "19:team-id@thread.tacv2"
        };

        TeamsActivity activity = builder
            .WithChannelData(channelData)
            .Build();

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

        TeamsActivity activity = builder
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

        TeamsActivity activity = builder
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

        TeamsActivity activity = builder
            .AddEntity(entity)
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<ClientInfoEntity>(activity.Entities[0]);
    }

    [Fact]
    public void AddEntity_MultipleEntities_AddsAllToCollection()
    {
        TeamsActivity activity = builder
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddEntity(new ProductInfoEntity { Id = "product-123" })
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count);
    }

    [Fact]
    public void AddAttachment_AddsAttachmentToCollection()
    {
        TeamsAttachment attachment = new()
        {
            ContentType = "text/html",
            Name = "test.html"
        };

        TeamsActivity activity = builder
            .AddAttachment(attachment)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("text/html", activity.Attachments[0].ContentType);
    }

    [Fact]
    public void AddAttachment_MultipleAttachments_AddsAllToCollection()
    {
        TeamsActivity activity = builder
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

        TeamsActivity activity = builder
            .AddAdaptiveCardAttachment(adaptiveCard)
            .Build();

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("application/vnd.microsoft.card.adaptive", activity.Attachments[0].ContentType);
        Assert.Same(adaptiveCard, activity.Attachments[0].Content);
    }

    [Fact]
    public void AddAdaptiveCardAttachment_WithConfigure_AppliesChanges()
    {
        var adaptiveCard = new { type = "AdaptiveCard" };

        TeamsActivity activity = builder
            .AddAdaptiveCardAttachment(adaptiveCard, b => b.WithName("feedback"))
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
    public void FluentAPI_MethodChaining_ReturnsBuilderInstance()
    {
        TeamsActivityBuilder result1 = builder.WithId("id");
        TeamsActivityBuilder result2 = builder.WithType(TeamsActivityType.Message);

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        builder.WithId("test-id");

        TeamsActivity activity1 = builder.Build();
        TeamsActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
    }

    [Fact]
    public void WithChannelData_NullValue_SetsToNull()
    {
        TeamsActivity activity = builder
            .WithChannelData(null!)
            .Build();

        Assert.Null(activity.ChannelData);
    }

    [Fact]
    public void AddEntity_NullEntitiesCollection_InitializesCollection()
    {
        TeamsActivity activity = builder.Build();
        Assert.Null(activity.Entities);

        ClientInfoEntity entity = new() { Locale = "en-US" };
        builder.AddEntity(entity);

        TeamsActivity result = builder.Build();
        Assert.NotNull(result.Entities);
        Assert.Single(result.Entities);
    }

    [Fact]
    public void AddAttachment_NullAttachmentsCollection_InitializesCollection()
    {
        TeamsActivity activity = builder.Build();
        Assert.Null(activity.Attachments);

        TeamsAttachment attachment = new() { ContentType = "text/html" };
        builder.AddAttachment(attachment);

        TeamsActivity result = builder.Build();
        Assert.NotNull(result.Attachments);
        Assert.Single(result.Attachments);
    }

    [Fact]
    public void WithConversationReference_WithNullActivity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(null!));
    }

    [Fact]
    public void WithConversationReference_WithNullChannelId_ThrowsArgumentNullException()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = null,
            ServiceUrl = new Uri("https://test.com"),
            Conversation = TeamsConversation.FromConversation(new Conversation()),
            From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount()),
            Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount())
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = null,
            Conversation = TeamsConversation.FromConversation(new Conversation()),
            From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount()),
            Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount())
        };

        Assert.Throws<ArgumentNullException>(() => builder.WithConversationReference(sourceActivity));
    }

    [Fact]
    public void WithConversationReference_WithEmptyConversationId_DoesNotThrow()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = TeamsConversation.FromConversation(new Conversation()),
            From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "user-1" }),
            Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "bot-1" })
        };

        TeamsActivity result = builder.WithConversationReference(sourceActivity).Build();
        Assert.NotNull(result.Conversation);
    }

    [Fact]
    public void WithConversationReference_WithEmptyFromId_DoesNotThrow()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = TeamsConversation.FromConversation(new Conversation { Id = "conv-1" }),
            From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount()),
            Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "bot-1" })
        };

        TeamsActivity result = builder.WithConversationReference(sourceActivity).Build();
        Assert.NotNull(result.From);
    }

    [Fact]
    public void WithConversationReference_WithEmptyRecipientId_DoesNotThrow()
    {
        TeamsActivity sourceActivity = new()
        {
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://test.com"),
            Conversation = TeamsConversation.FromConversation(new Conversation { Id = "conv-1" }),
            From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "user-1" }),
            Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount())
        };

        TeamsActivity result = builder.WithConversationReference(sourceActivity).Build();
        Assert.NotNull(result.From);
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

        Assert.IsType<TeamsConversationAccount>(activity.From);
        Assert.Equal("user-123", activity.From?.Id);
        Assert.Equal("User Name", activity.From?.Name);
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

        Assert.IsType<TeamsConversationAccount>(activity.Recipient);
        Assert.Equal("bot-123", activity.Recipient?.Id);
        Assert.Equal("Bot Name", activity.Recipient?.Name);
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
        Assert.Equal("conv-123", activity.Conversation?.Id);
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
    public void Build_MultipleCalls_ReturnsRebasedActivity()
    {
        builder.AddEntity(new ClientInfoEntity { Locale = "en-US" });

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
}
