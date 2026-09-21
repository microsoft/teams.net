// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for thread placement against the live service.
/// </summary>
/// <remarks>
/// These exercise <see cref="ConversationClient.ReplyToActivityAsync"/>, which posts to
/// <c>/v3/conversations/{conversationId}/threads/{rootId}</c>.
/// </remarks>
public class ThreadingTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public ThreadingTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    private static CoreActivityInput Message(string text) =>
        CoreActivityInput.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithProperty("text", text)
            .Build();

    private BotRequestContext? Context => BotRequestContext.FromAgenticIdentity(_f.AgenticIdentity);

    /// <summary>
    /// Posts a root message and returns its activity id, so each test threads under a root it owns
    /// rather than one left behind by an earlier run.
    /// </summary>
    private async Task<string> CreateRootAsync(string conversationId, string label)
    {
        SendActivityResponse? root = await _f.ConversationClient.SendActivityAsync(
            conversationId,
            Message($"[Threading] {label} root at `{DateTime.UtcNow:s}`"),
            _f.ServiceUrl,
            requestContext: Context);

        Assert.NotNull(root?.Id);
        return root.Id;
    }

    /// <summary>
    /// Replies beneath a root and asserts the service created a distinct, addressable activity.
    /// </summary>
    /// <remarks>
    /// A 2xx alone is weak evidence here: the service also accepts a threaded write whose root does
    /// not exist. Updating the reply afterwards is what proves a real activity came back.
    /// </remarks>
    private async Task ReplyAndVerifyAsync(string conversationId, string rootId, string label)
    {
        SendActivityResponse? reply = await _f.ConversationClient.ReplyToActivityAsync(
            conversationId,
            rootId,
            Message($"[Threading] {label} reply at `{DateTime.UtcNow:s}`"),
            _f.ServiceUrl,
            requestContext: Context);

        Assert.NotNull(reply?.Id);
        Assert.NotEqual(rootId, reply.Id);
        _output.WriteLine($"{label}: root {rootId} -> reply {reply.Id}");

        UpdateActivityResponse updated = await _f.ConversationClient.UpdateActivityAsync(
            conversationId,
            reply.Id,
            Message($"[Threading] {label} reply edited at `{DateTime.UtcNow:s}`"),
            _f.ServiceUrl,
            false,
            Context);

        Assert.Equal(reply.Id, updated.Id);
    }

    [SkippableFact(Timeout = 15000)]
    [Trait("Category", "Threading")]
    public async Task GroupChat_ReplyToActivity_PlacesInThread()
    {
        string rootId = await CreateRootAsync(_f.ConversationId, "group chat");
        await ReplyAndVerifyAsync(_f.ConversationId, rootId, "group chat");
    }

    /// <summary>
    /// Covers <c>ReplyToTargetedActivityAsync</c>, which posts to the same thread endpoint with
    /// <c>isTargetedActivity=true</c>. A targeted message is rejected without a recipient, so the
    /// recipient is taken from the conversation roster rather than left unset.
    /// </summary>
    [SkippableFact(Timeout = 15000)]
    [Trait("Category", "Threading")]
    public async Task GroupChat_ReplyToTargetedActivity_PlacesInThread()
    {
        TeamsChannelAccount? recipient = _f.CachedMembers?.FirstOrDefault(m => m is not null);
        Skip.If(recipient is null, "no non-bot member available in the test conversation");

        string rootId = await CreateRootAsync(_f.ConversationId, "group chat targeted");

        MessageActivityInput activity = new MessageActivityInput()
            .WithText($"[Threading] group chat targeted reply at `{DateTime.UtcNow:s}`")
            .WithRecipient(recipient!, isTargeted: true);

        SendActivityResponse? reply = await _f.ScopedApiClient.Conversations.ReplyToTargetedActivityAsync(
            _f.ConversationId,
            rootId,
            activity);

        Assert.NotNull(reply?.Id);
        Assert.NotEqual(rootId, reply.Id);
        _output.WriteLine($"group chat targeted: root {rootId} -> reply {reply.Id}");
    }

    [SkippableFact(Timeout = 15000)]
    [Trait("Category", "Threading")]
    public async Task Channel_ReplyToActivity_PlacesInThread()
    {
        string rootId = await CreateRootAsync(_f.ChannelId, "channel");
        await ReplyAndVerifyAsync(_f.ChannelId, rootId, "channel");
    }

    /// <summary>
    /// The legacy <c>;messageid=</c> conversation-id suffix is still honored by the service and is
    /// still parsed by the SDK, so it is covered here to catch the compatibility path breaking.
    /// </summary>
    [SkippableFact(Timeout = 15000)]
    [Trait("Category", "Threading")]
    public async Task LegacySuffix_PlacesInThread()
    {
        string rootId = await CreateRootAsync(_f.ConversationId, "legacy suffix");

#pragma warning disable CS0618 // the suffix is deprecated for new code and still exercised here
        string threadedConversationId = ConversationExtensions.ToThreadedConversationId(_f.ConversationId, rootId);
#pragma warning restore CS0618

        SendActivityResponse? reply = await _f.ConversationClient.SendActivityAsync(
            threadedConversationId,
            Message($"[Threading] legacy suffix reply at `{DateTime.UtcNow:s}`"),
            _f.ServiceUrl,
            requestContext: Context);

        Assert.NotNull(reply?.Id);
        Assert.NotEqual(rootId, reply.Id);
        _output.WriteLine($"legacy suffix: root {rootId} -> reply {reply.Id}");
    }

    /// <summary>
    /// A threaded reply must not disturb the unthreaded path, which posts to <c>/activities/</c> and
    /// is what a group chat root message uses.
    /// </summary>
    [SkippableFact(Timeout = 15000)]
    [Trait("Category", "Threading")]
    public async Task SendActivity_StillPostsToActivitiesEndpoint()
    {
        SendActivityResponse? sent = await _f.ConversationClient.SendActivityAsync(
            _f.ConversationId,
            Message($"[Threading] unthreaded send at `{DateTime.UtcNow:s}`"),
            _f.ServiceUrl,
            requestContext: Context);

        Assert.NotNull(sent?.Id);
        _output.WriteLine($"unthreaded send: {sent.Id}");
    }
}
