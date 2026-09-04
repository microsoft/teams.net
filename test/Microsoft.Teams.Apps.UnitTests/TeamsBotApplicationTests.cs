// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsBotApplicationTests
{
    [Fact]
    public async Task Reply_Proactive_ThrowsOnInvalidMessageId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("19:abc@thread.skype", "not-a-number", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_ThrowsOnZeroMessageId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("19:abc@thread.skype", "0", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_ThrowsOnEmptyConversationId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("", "1680000000000", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_UsesReplyEndpointWithoutThreadedConversationId()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        string? capturedConversationId = null;
        string? capturedRootId = null;
        conversationClient
            .Setup(c => c.ReplyToActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (conversationId, rootId, _, _, _, _, _, _) =>
                {
                    capturedConversationId = conversationId;
                    capturedRootId = rootId;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });

        await app.ReplyAsync(
            "19:abc@thread.skype;messageid=old",
            "1680000000000",
            "hello",
            new Uri("https://test.service.url/"));

        Assert.Equal("19:abc@thread.skype", capturedConversationId);
        Assert.Equal("1680000000000", capturedRootId);
    }

    [Fact]
    public async Task Reply_Proactive_WithTargetedRecipient_UsesTargetedReplyEndpoint()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        bool? capturedIsTargeted = null;
        conversationClient
            .Setup(c => c.ReplyToActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, _, isTargeted, _, _, _) => capturedIsTargeted = isTargeted)
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });

        await app.ReplyAsync(
            "19:abc@thread.skype",
            "1680000000000",
            new MessageActivityInput()
                .WithText("hello")
                .WithRecipient(new TeamsChannelAccount { Id = "user-1" }, isTargeted: true),
            new Uri("https://test.service.url/"));

        Assert.True(capturedIsTargeted);
    }

    [Fact]
    public async Task Send_Proactive_WithLegacyThreadedId_UsesReplyEndpoint()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        string? capturedConversationId = null;
        string? capturedRootId = null;
        conversationClient
            .Setup(c => c.ReplyToActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (conversationId, rootId, _, _, _, _, _, _) =>
                {
                    capturedConversationId = conversationId;
                    capturedRootId = rootId;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });

        await app.SendAsync(
            "19:abc@thread.skype;messageid=1680000000000",
            new MessageActivityInput().WithText("hello"),
            new Uri("https://test.service.url/"));

        Assert.Equal("19:abc@thread.skype", capturedConversationId);
        Assert.Equal("1680000000000", capturedRootId);
        conversationClient.Verify(
            c => c.SendActivityAsync(
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void HasMatchingRoute_ReturnsTrueForRegisteredInvokeHandler()
    {
        TeamsBotApplication app = CreateApp();
        app.OnInvoke((_, _) => Task.FromResult(InvokeResponse.Ok()));

        Assert.True(app.HasMatchingRoute(new InvokeActivity(InvokeNames.TaskFetch)));
        Assert.False(app.HasMatchingRoute(new CoreActivity(ActivityType.Message)));
    }

    private static TeamsBotApplication CreateApp()
        => CreateAppWithConversationClient().App;

    private static (TeamsBotApplication App, Mock<ConversationClient> ConversationClient) CreateAppWithConversationClient()
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);

        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        TeamsBotApplication app = new(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        return (app, mockConversationClient);
    }
}
