// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for replying inside a reply chain against the live service.
/// </summary>
/// <remarks>
/// These exercise <see cref="ConversationClient.SendReplyChainActivityAsync"/>, which posts to
/// <c>/v3/conversations/{conversationId}/replyChains/{rootId}</c>.
/// The conversation id is the id of the chat or channel, never the inbound conversation id.
/// The service returns a distinct, editable activity whether or not the reply lands in the chain, so these tests do not observe reply-chain membership.
/// A bot has no operation that reads a conversation back, so membership is observable only in the Teams client.
/// What the service does enforce is checked directly: it rejects a conversation id that carries a suffix and a root that is itself a reply.
/// Run them with the canary service url, because the route is not deployed on every ring.
/// With an agentic user configured, every test runs as that user, so the user must be a member of the test channel's team as well as in a personal chat with the test user.
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
    /// Posts a root message and returns its activity id, so each test replies under a root it owns
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
    /// A 2xx alone is weak evidence here, because a reply that is posted flat also returns 2xx.
    /// Updating the reply afterwards proves a real activity came back.
    /// </remarks>
    private async Task<string> ReplyAndVerifyAsync(string conversationId, string rootId, string label)
    {
        SendActivityResponse? reply = await _f.ConversationClient.SendReplyChainActivityAsync(
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
        return reply.Id;
    }

    /// <summary>
    /// Resolves the conversation a scope names, skipping the test when the environment cannot supply it.
    /// </summary>
    private string ConversationFor(string scope)
    {
        if (scope == "personal")
        {
            Skip.If(string.IsNullOrWhiteSpace(_f.PersonalConversationId), "TEST_PERSONALCONVERSATIONID is not set");
            return _f.PersonalConversationId!;
        }

        return _f.ChannelId;
    }

    private async Task<HttpRequestException> ReplyExpectingFailureAsync(string conversationId, string rootId, string label)
    {
        return await Assert.ThrowsAsync<HttpRequestException>(() => _f.ConversationClient.SendReplyChainActivityAsync(
            conversationId,
            rootId,
            Message($"[Threading] {label} at `{DateTime.UtcNow:s}`"),
            _f.ServiceUrl,
            requestContext: Context));
    }

    [SkippableTheory(Timeout = 15000)]
    [Trait("Category", "Threading")]
    [InlineData("personal")]
    [InlineData("channel")]
    public async Task SendReplyChainActivity_CreatesEditableReply(string scope)
    {
        string conversationId = ConversationFor(scope);

        string rootId = await CreateRootAsync(conversationId, scope);
        await ReplyAndVerifyAsync(conversationId, rootId, scope);
    }

    [SkippableTheory(Timeout = 15000)]
    [Trait("Category", "Threading")]
    [InlineData("personal")]
    [InlineData("channel")]
    public async Task SendReplyChainActivity_WithAConversationIdThatCarriesASuffix_IsRejected(string scope)
    {
        string conversationId = ConversationFor(scope);
        string rootId = await CreateRootAsync(conversationId, scope);

        HttpRequestException error = await ReplyExpectingFailureAsync($"{conversationId};messageid={rootId}", rootId, $"{scope} suffixed id");

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [SkippableTheory(Timeout = 20000)]
    [Trait("Category", "Threading")]
    [InlineData("personal")]
    [InlineData("channel")]
    public async Task SendReplyChainActivity_WithAReplyAsTheRoot_IsRejected(string scope)
    {
        string conversationId = ConversationFor(scope);
        string rootId = await CreateRootAsync(conversationId, scope);
        string replyId = await ReplyAndVerifyAsync(conversationId, rootId, scope);

        HttpRequestException error = await ReplyExpectingFailureAsync(conversationId, replyId, $"{scope} reply as root");

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }
}
