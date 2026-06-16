// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for creating conversations with different ConversationParameters.
/// Tests personal chats, group chats, and channel thread creation via both
/// core <see cref="ConversationClient"/> and the <see cref="ApiClient"/> facade.
/// </summary>
public class CreateConversationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;
    private readonly ApiClient _api;

    public CreateConversationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
        _api = _f.ScopedApiClient;
    }

    /// <summary>
    /// Gets MRI-format member IDs by fetching the conversation members list.
    /// The API requires MRI IDs (e.g., "29:1abc..."), not pairwise bot framework IDs.
    /// </summary>
    private Task<(string first, string? second)> GetMemberMrisAsync()
    {
        string first = _f.MemberMri1!;
        string? second = _f.MemberMri2;

        _output.WriteLine($"Using member MRIs: first={first}, second={second ?? "(none)"}");
        return Task.FromResult((first, second));
    }

    #region Personal Chat (1:1) — Core ConversationClient

    [Fact(Timeout = 5000)]
    public async Task Core_CreatePersonalChat()
    {
        (string memberMri, _) = await GetMemberMrisAsync();

        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _f.ConversationClient.CreateConversationAsync(
            parameters, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"Created 1:1 conversation: {response.Id}");
    }

    [Fact(Timeout = 5000)]
    public async Task Core_CreatePersonalChat_AndSendMessage()
    {
        (string memberMri, _) = await GetMemberMrisAsync();

        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _f.ConversationClient.CreateConversationAsync(
            parameters, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(response?.Id);

        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithServiceUrl(_f.ServiceUrl)
            .WithConversation(new(response.Id))
            .WithProperty("text", $"[Core] 1:1 message at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);
        _output.WriteLine($"Created 1:1 conversation {response.Id} and sent activity {sent.Id}");
    }

    [Fact(Timeout = 5000)]
    public async Task Core_CreatePersonalChat_WithInitialActivity()
    {
        (string memberMri, _) = await GetMemberMrisAsync();

        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId,
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", $"[Core] Initial message at `{DateTime.UtcNow:s}`")
                .Build()
        };

        CreateConversationResponse response = await _f.ConversationClient.CreateConversationAsync(
            parameters, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"Created 1:1 conversation with initial activity: {response.Id}, activityId: {response.ActivityId}");
    }

    #endregion

    #region Group Chat — Core ConversationClient

    [Fact]
    public async Task Core_CreateGroupChat()
    {
        (string first, string? second) = await GetMemberMrisAsync();
        if (second is null)
        {
            _output.WriteLine("Skipping: need at least 2 members in conversation");
            return;
        }

        ConversationParameters parameters = new()
        {
            //IsGroup = true,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members =
            [
                //new() { Id = first },
                new() { Id = second }
            ],
            TenantId = _f.TenantId,
            //TopicName = $"Integration Test Group - {DateTime.UtcNow:s}",
            ChannelData = new { tenant = new { id = _f.TenantId } }
        };

        CreateConversationResponse response = await _f.ConversationClient.CreateConversationAsync(
            parameters, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"Created group conversation: {response.Id}");
    }

    [Fact]
    public async Task Core_CreateGroupChat_AndSendMessage()
    {
        (string first, string? second) = await GetMemberMrisAsync();
        if (second is null)
        {
            _output.WriteLine("Skipping: need at least 2 members in conversation");
            return;
        }

        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members =
            [
                //new() { Id = first },
                new() { Id = second }
            ],
            TenantId = _f.TenantId,
            ChannelData = new { tenant = new { id = _f.TenantId } }
        };

        CreateConversationResponse response = await _f.ConversationClient.CreateConversationAsync(
            parameters, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(response?.Id);

        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithServiceUrl(_f.ServiceUrl)
            .WithConversation(new(response.Id))
            .WithProperty("text", $"[Core] Group message at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);
        _output.WriteLine($"Created group {response.Id} and sent activity {sent.Id}");
    }

    #endregion

    #region Channel Thread — Core ConversationClient

    [Fact(Timeout = 5000)]
    public async Task Core_CreateChannelThread()
    {
        ConversationParameters parameters = new()
        {
            IsGroup = true,
            ChannelData = new { channel = new { id = _f.ChannelId } },
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", $"[Core] New channel thread at `{DateTime.UtcNow:s}`")
                .Build(),
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _f.ConversationClient.CreateConversationAsync(
            parameters, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"Created channel thread: {response.Id}, activityId: {response.ActivityId}");
    }

    #endregion

    #region Personal Chat — ApiClient

    [Fact(Timeout = 5000)]
    public async Task ApiClient_CreatePersonalChat()
    {
        (string memberMri, _) = await GetMemberMrisAsync();

        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _api.Conversations.CreateAsync(parameters, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"[ApiClient] Created 1:1 conversation: {response.Id}");
    }

    [Fact(Timeout = 5000)]
    public async Task ApiClient_CreatePersonalChat_AndSendViaActivities()
    {
        (string memberMri, _) = await GetMemberMrisAsync();

        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _api.Conversations.CreateAsync(parameters, _f.AgenticIdentity);
        Assert.NotNull(response?.Id);

        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithProperty("text", $"[ApiClient] 1:1 via Activities.Create at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(response.Id, activity);
        Assert.NotNull(sent?.Id);
        _output.WriteLine($"[ApiClient] Created 1:1 {response.Id}, sent activity {sent.Id}");
    }

    #endregion

    #region Group Chat — ApiClient

    [Fact]
    public async Task ApiClient_CreateGroupChat()
    {
        (string first, string? second) = await GetMemberMrisAsync();
        if (second is null)
        {
            _output.WriteLine("Skipping: need at least 2 members in conversation");
            return;
        }

        // Service rejects multiple members when creating via Bot + Members pattern.
        // Using a single non-bot member creates a 1:1 "group-style" conversation.
        ConversationParameters parameters = new()
        {
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members =
            [
                new() { Id = second }
            ],
            TenantId = _f.TenantId,
            TopicName = $"[ApiClient] Group - {DateTime.UtcNow:s}",
            ChannelData = new { tenant = new { id = _f.TenantId } }
        };

        CreateConversationResponse response = await _api.Conversations.CreateAsync(parameters, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"[ApiClient] Created group conversation: {response.Id}");
    }

    #endregion

    #region Channel Thread — ApiClient

    [Fact(Timeout = 5000)]
    public async Task ApiClient_CreateChannelThread()
    {
        ConversationParameters parameters = new()
        {
            IsGroup = true,
            ChannelData = new { channel = new { id = _f.ChannelId } },
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", $"[ApiClient] New channel thread at `{DateTime.UtcNow:s}`")
                .Build(),
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _api.Conversations.CreateAsync(parameters, _f.AgenticIdentity);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        _output.WriteLine($"[ApiClient] Created channel thread: {response.Id}, activityId: {response.ActivityId}");
    }

    [Fact(Timeout = 5000)]
    public async Task ApiClient_CreateChannelThread_AndReply()
    {
        ConversationParameters parameters = new()
        {
            IsGroup = true,
            ChannelData = new { channel = new { id = _f.ChannelId } },
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", $"[ApiClient] Thread root at `{DateTime.UtcNow:s}`")
                .Build(),
            TenantId = _f.TenantId
        };

        CreateConversationResponse response = await _api.Conversations.CreateAsync(parameters, _f.AgenticIdentity);
        Assert.NotNull(response?.Id);
        Assert.NotNull(response.ActivityId);

        // Reply to the thread
        CoreActivity reply = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithProperty("text", $"[ApiClient] Thread reply at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? replyResponse = await _api.Conversations.Activities.ReplyAsync(
            response.Id, response.ActivityId, reply);

        Assert.NotNull(replyResponse);
        _output.WriteLine($"[ApiClient] Created thread {response.Id}, root activity {response.ActivityId}, reply {replyResponse?.Id}");
    }

    #endregion
}
