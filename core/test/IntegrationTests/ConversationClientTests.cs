// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for core <see cref="ConversationClient"/> making real API calls.
/// </summary>
public class ConversationClientTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public ConversationClientTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _output = output;
    }

    [Fact]
    public async Task SendActivity()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ConversationClient] SendActivity at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _f.ServiceUrl,
            Conversation = new(_f.ConversationId)
        };

        SendActivityResponse? res = await _f.ConversationClient.SendActivityAsync(activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Sent activity: {res.Id}");
    }

    [Fact]
    public async Task UpdateActivity()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ConversationClient] Original at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _f.ServiceUrl,
            Conversation = new(_f.ConversationId)
        };

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);

        CoreActivity updated = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ConversationClient] Updated at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _f.ServiceUrl,
            Conversation = new(_f.ConversationId)
        };

        UpdateActivityResponse res = await _f.ConversationClient.UpdateActivityAsync(
            _f.ConversationId, sent.Id, updated);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated activity: {res.Id}");
    }

    [Fact]
    public async Task DeleteActivity()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ConversationClient] To delete at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _f.ServiceUrl,
            Conversation = new(_f.ConversationId)
        };

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _f.ConversationClient.DeleteActivityAsync(
            _f.ConversationId, sent.Id, _f.ServiceUrl);

        _output.WriteLine($"Deleted activity: {sent.Id}");
    }

    [Fact]
    public async Task GetConversationMembers()
    {
        IList<ConversationAccount> members = await _f.ConversationClient.GetConversationMembersAsync(
            _f.ConversationId, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        foreach (ConversationAccount m in members)
        {
            _output.WriteLine($"Member: {m.Id} — {m.Name}");
        }
    }

    [Fact]
    public async Task GetConversationMember()
    {
        // Get MRI-format member ID from the members list first
        IList<ConversationAccount> members = await _f.ConversationClient.GetConversationMembersAsync(
            _f.ConversationId, _f.ServiceUrl, _f.AgenticIdentity);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        ConversationAccount member = await _f.ConversationClient.GetConversationMemberAsync<ConversationAccount>(
            _f.ConversationId, memberId, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}");
    }

    [Fact]
    public async Task GetPagedMembers()
    {
        PagedMembersResult result = await _f.ConversationClient.GetConversationPagedMembersAsync(
            _f.ConversationId, _f.ServiceUrl, pageSize: 5, agenticIdentity: _f.AgenticIdentity);

        Assert.NotNull(result?.Members);
        Assert.NotEmpty(result.Members);

        foreach (ConversationAccount m in result.Members)
        {
            _output.WriteLine($"Member: {m.Id} — {m.Name}");
        }
    }

    [Fact(Skip = "Reactions API returns NotFound — needs service-url scoped auth")]
    public async Task AddAndDeleteReaction()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ConversationClient] Reaction test at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _f.ServiceUrl,
            Conversation = new(_f.ConversationId)
        };

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);

        await _f.ConversationClient.AddReactionAsync(
            _f.ConversationId, sent.Id, "like", _f.ServiceUrl, _f.AgenticIdentity);
        _output.WriteLine("Added 'like' reaction");

        await Task.Delay(1000);

        await _f.ConversationClient.DeleteReactionAsync(
            _f.ConversationId, sent.Id, "like", _f.ServiceUrl, _f.AgenticIdentity);
        _output.WriteLine("Removed 'like' reaction");
    }
}
