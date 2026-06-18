// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
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
        _f.OutputHelper = output;
        _output = output;
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task SendActivity()
    {
        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithServiceUrl(_f.ServiceUrl)
            .WithConversation(new(_f.ConversationId))
            .WithProperty("text", $"[ConversationClient] SendActivity at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? res = await _f.ConversationClient.SendActivityAsync(activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Sent activity: {res.Id}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task UpdateActivity()
    {
        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithServiceUrl(_f.ServiceUrl)
            .WithConversation(new(_f.ConversationId))
            .WithProperty("text", $"[ConversationClient] Original at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);

        CoreActivity updated = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithServiceUrl(_f.ServiceUrl)
            .WithConversation(new(_f.ConversationId))
            .WithProperty("text", $"[ConversationClient] Updated at `{DateTime.UtcNow:s}`")
            .Build();

        UpdateActivityResponse res = await _f.ConversationClient.UpdateActivityAsync(
            _f.ConversationId, sent.Id, updated, false, _f.AgenticIdentity);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated activity: {res.Id}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task DeleteActivity()
    {
        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithServiceUrl(_f.ServiceUrl)
            .WithConversation(new(_f.ConversationId))
            .WithProperty("text", $"[ConversationClient] To delete at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _f.ConversationClient.DeleteActivityAsync(
            _f.ConversationId, sent.Id, _f.ServiceUrl, _f.AgenticIdentity);

        _output.WriteLine($"Deleted activity: {sent.Id}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task GetConversationMembers()
    {
        IList<ChannelAccount> members = await _f.ConversationClient.GetConversationMembersAsync(
            _f.ConversationId, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        foreach (ChannelAccount m in members)
        {
            _output.WriteLine($"Member: {m.Id} — {m.Name}");
        }
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task GetConversationMember()
    {
        string memberId = _f.MemberMri1!;

        ChannelAccount member = await _f.ConversationClient.GetConversationMemberAsync<ChannelAccount>(
            _f.ConversationId, memberId, _f.ServiceUrl, _f.AgenticIdentity);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}");
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task GetPagedMembers()
    {
        Skip.If(_f.AgenticIdentity is not null, "Paged members returns 500 with agentic identity — service limitation");
        Skip.If(_f.IsCanary, "Paged members returns empty on canary — service limitation");

        PagedMembersResult result = await _f.ConversationClient.GetConversationPagedMembersAsync(
            _f.ConversationId, _f.ServiceUrl, pageSize: 5, agenticIdentity: _f.AgenticIdentity);

        Assert.NotNull(result?.Members);
        Assert.NotEmpty(result.Members);

        foreach (ChannelAccount m in result.Members.Take(5))
        {
            _output.WriteLine($"Member: {m.Id} — {m.Name}");
        }
    }

    [SkippableFact]
    [Trait("Category", "Activities")]
    public async Task AddAndDeleteReaction()
    {
        Skip.If(_f.AgenticIdentity is not null, "Reactions API returns 404 with agentic identity — service limitation");
        Skip.If(_f.IsCanary, "Reactions API returns 404 on canary — service limitation");

        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithServiceUrl(_f.ServiceUrl)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithConversation(new(_f.ConversationId))
            .WithProperty("text", $"[ConversationClient] Reaction test at `{DateTime.UtcNow:s}`")
            .Build();

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
