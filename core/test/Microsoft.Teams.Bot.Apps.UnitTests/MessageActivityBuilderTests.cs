// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class MessageActivityBuilderTests
{
    [Fact]
    public void WithText_SetsTextAndFormat()
    {
        MessageActivity activity = MessageActivity.CreateBuilder()
            .WithText("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Text);
        Assert.Equal("plain", activity.TextFormat);
    }

    [Fact]
    public void WithText_CustomFormat_SetsFormat()
    {
        MessageActivity activity = MessageActivity.CreateBuilder()
            .WithText("**bold**", "markdown")
            .Build();

        Assert.Equal("**bold**", activity.Text);
        Assert.Equal("markdown", activity.TextFormat);
    }

    [Fact]
    public void WithSuggestedActions_SetsSuggestedActions()
    {
        SuggestedActions suggestedActions = new()
        {
            Actions = [new() { Title = "Yes", Type = "imBack", Value = "yes" }]
        };

        MessageActivity activity = MessageActivity.CreateBuilder()
            .WithSuggestedActions(suggestedActions)
            .Build();

        Assert.NotNull(activity.SuggestedActions);
        Assert.Single(activity.SuggestedActions.Actions!);
        Assert.Equal("Yes", activity.SuggestedActions.Actions![0].Title);
    }

    [Fact]
    public void AddMention_WithNullAccount_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MessageActivity.CreateBuilder().AddMention(null!));
    }

    [Fact]
    public void AddMention_WithAccountAndDefaultText_AddsMentionAndUpdatesText()
    {
        ConversationAccount account = new()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        MessageActivity activity = MessageActivity.CreateBuilder()
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

        MessageActivity activity = MessageActivity.CreateBuilder()
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

        MessageActivity activity = MessageActivity.CreateBuilder()
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

        MessageActivity activity = MessageActivity.CreateBuilder()
            .WithText("message")
            .AddMention(account1)
            .AddMention(account2)
            .Build();

        Assert.Equal("<at>User Two</at> <at>User One</at> message", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count);
    }

    [Fact]
    public void AddMention_EmptyText_PrependsMention()
    {
        ConversationAccount account = new() { Id = "user-123", Name = "User" };

        MessageActivity activity = MessageActivity.CreateBuilder()
            .AddMention(account)
            .Build();

        Assert.Equal("<at>User</at> ", activity.Text);
    }

    [Fact]
    public void AddMention_WithAccountWithNullName_UsesNullText()
    {
        ConversationAccount account = new() { Id = "user-123", Name = null };

        MessageActivity activity = MessageActivity.CreateBuilder()
            .WithText("message")
            .AddMention(account)
            .Build();

        Assert.Equal("<at></at> message", activity.Text);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
    }

    [Fact]
    public void AddMention_UpdatesBaseEntityCollection()
    {
        ConversationAccount account = new() { Id = "user-123", Name = "Test User" };

        MessageActivity activity = MessageActivity.CreateBuilder()
            .AddMention(account)
            .Build();

        CoreActivity baseActivity = activity;
        Assert.NotNull(baseActivity.Entities);
        Assert.NotEmpty(baseActivity.Entities);
    }

    [Fact]
    public void MethodChaining_ReturnsBuilderInstance()
    {
        MessageActivityBuilder msgBuilder = MessageActivity.CreateBuilder();

        MessageActivityBuilder result1 = msgBuilder.WithId("id");
        MessageActivityBuilder result2 = msgBuilder.WithText("text");
        MessageActivityBuilder result3 = msgBuilder.WithType(TeamsActivityType.Message);

        Assert.Same(msgBuilder, result1);
        Assert.Same(msgBuilder, result2);
        Assert.Same(msgBuilder, result3);
    }

    [Fact]
    public void CreateBuilder_WithExistingActivity_PreservesData()
    {
        MessageActivity original = new() { Id = "original-id", Text = "original text" };

        MessageActivity modified = MessageActivity.CreateBuilder(original)
            .WithText("modified text")
            .Build();

        Assert.Equal("original-id", modified.Id);
        Assert.Equal("modified text", modified.Text);
    }

    [Fact]
    public void IntegrationTest_CreateComplexMessageActivity()
    {
        Uri serviceUrl = new("https://smba.trafficmanager.net/amer/test/");
        TeamsChannelData channelData = new()
        {
            TeamsChannelId = "19:channel@thread.tacv2",
            TeamsTeamId = "19:team@thread.tacv2"
        };

        Conversation conv = new()
        {
            Id = "conv-001",
            Properties =
            {
                { "tenantId", "tenant-001" },
                { "conversationType", "channel" }
            }
        };

        TeamsConversation? tc = TeamsConversation.FromConversation(conv);
        Assert.NotNull(tc);

        MessageActivity activity = MessageActivity.CreateBuilder()
            .WithId("msg-001")
            .WithServiceUrl(serviceUrl)
            .WithChannelId("msteams")
            .WithText("Please review this document")
            .WithFrom(TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "bot-id", Name = "Bot" }))
            .WithRecipient(TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "user-id", Name = "User" }))
            .WithConversation(tc)
            .WithChannelData(channelData)
            .AddEntity(new ClientInfoEntity { Locale = "en-US", Country = "US", Platform = "Web" })
            .AddAttachment(new TeamsAttachment { ContentType = "application/vnd.microsoft.card.adaptive", Name = "card.json" })
            .AddMention(new ConversationAccount { Id = "manager-id", Name = "Manager" }, "Manager")
            .Build();

        Assert.Equal(TeamsActivityType.Message, activity.Type);
        Assert.Equal("msg-001", activity.Id);
        Assert.Equal(serviceUrl, activity.ServiceUrl);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("<at>Manager</at> Please review this document", activity.Text);
        Assert.Equal("bot-id", activity.From?.Id);
        Assert.Equal("user-id", activity.Recipient?.Id);
        Assert.Equal("conv-001", activity.Conversation?.Id);
        Assert.Equal("tenant-001", activity.Conversation?.TenantId);
        Assert.Equal("channel", activity.Conversation?.ConversationType);
        Assert.NotNull(activity.ChannelData);
        Assert.Equal("19:channel@thread.tacv2", activity.ChannelData?.TeamsChannelId);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities?.Count); // ClientInfo + Mention
        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
    }
}
