// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

/// <summary>
/// Verifies that stream-cancellation detection in TeamsStreamingWriter uses structured
/// signals (HTTP status code / additional text) rather than a single fragile message
/// string match (A-009).
/// </summary>
public class StreamCancellationDetectionTests
{
    private static TeamsActivity CreateRef() => new()
    {
        Type = TeamsActivityType.Message,
        ChannelId = "msteams",
        ServiceUrl = new Uri("https://smba.trafficmanager.net/amer/"),
        Conversation = TeamsConversation.FromConversation(new Conversation { Id = "conv-1" }),
        From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "user-1", Name = "User" }),
        Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "bot-1", Name = "Bot" })
    };

    private sealed class AlwaysThrowHandler(HttpStatusCode status, string message) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException(message, inner: null, statusCode: status);
    }

    private static TeamsStreamingWriter BuildWriter(HttpStatusCode status, string message)
    {
        ConversationClient client = new(new HttpClient(new AlwaysThrowHandler(status, message)),
            NullLogger<ConversationClient>.Instance);
        return new TeamsStreamingWriter(client, CreateRef());
    }

    [Theory]
    [InlineData("Content stream was cancelled by user")]
    [InlineData("stream was closed")]
    public async Task AppendAsync_WithCancellationMessage_SetsCancelledSilently(string msg)
    {
        // Use a status code that is NOT 499/408 to prove the text fallback works
        TeamsStreamingWriter writer = BuildWriter(HttpStatusCode.InternalServerError, msg);

        // Should not throw — cancellation must be swallowed
        await writer.AppendResponseAsync("chunk");

        // After being cancelled the writer is a no-op — FinalizeResponseAsync returns without sending
        // (nothing accumulated; the cancellation guard returns early)
    }

    [Fact]
    public async Task AppendAsync_WithStatusCode499_SetsCancelledSilently()
    {
        // 499 "Client Closed Request" — structured cancellation signal
        TeamsStreamingWriter writer = BuildWriter((HttpStatusCode)499, "any message");

        await writer.AppendResponseAsync("chunk");
        // No exception — cancelled silently
    }

    [Fact]
    public async Task AppendAsync_WithUnrelatedServerError_PropagatesException()
    {
        // A real server error (500 with a non-cancellation message) must NOT be swallowed —
        // it propagates immediately from AppendResponseAsync.
        TeamsStreamingWriter writer = BuildWriter(HttpStatusCode.InternalServerError, "generic database error");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => writer.AppendResponseAsync("chunk"));
    }
}
