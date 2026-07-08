// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsStreamingWriterTests
{
    // Fake HttpMessageHandler that captures requests and returns pre-configured responses.
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<string> RequestBodies { get; } = [];
        public List<HttpRequestMessage> Requests { get; } = [];

        public void EnqueueResponse(string jsonBody, HttpStatusCode statusCode = HttpStatusCode.OK)
            => _responses.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(jsonBody)
            });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : string.Empty);

            return _responses.Count > 0
                ? _responses.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"default-id\"}") };
        }
    }

    private static TeamsActivity CreateReferenceActivity() => new()
    {
        Type = TeamsActivityTypes.Message,
        ServiceUrl = new Uri("https://smba.trafficmanager.net/amer/"),
        ChannelId = "msteams",
        Conversation = TeamsConversation.FromConversation(new Conversation { Id = "conv-123" }),
        From = TeamsChannelAccount.FromChannelAccount(new ChannelAccount { Id = "user-123", Name = "User" }),
        Recipient = TeamsChannelAccount.FromChannelAccount(new ChannelAccount { Id = "bot-123", Name = "Bot" })
    };

    private static (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) CreateWriter(TeamsActivity? reference = null)
    {
        FakeHttpMessageHandler handler = new();
        ConversationClient client = new(new HttpClient(handler), NullLogger<ConversationClient>.Instance);
        TeamsStreamingWriter writer = new(client, reference ?? CreateReferenceActivity());
        return (writer, handler);
    }

    // ── Guard conditions ──────────────────────────────────────────────────────

    [Fact]
    public async Task AppendAsync_AfterFinalizeAsync_ReopensStreamForNextMessage()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync("Hello");
        await writer.FinalizeResponseAsync();

        // Appending after finalize reopens the stream on the same instance instead of throwing.
        await writer.AppendResponseAsync("Second message");
        await writer.FinalizeResponseAsync();

        int finalCount = handler.RequestBodies.Count(b => b.Contains("\"streamType\": \"final\"", StringComparison.Ordinal));
        Assert.Equal(2, finalCount);
        // The reopened cycle only carries the second message's text (accumulated text was reset).
        Assert.Contains("Second message", handler.RequestBodies.Last());
    }

    [Fact]
    public async Task FinalizeAsync_CalledTwice_IsIdempotent()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync("Hello");
        await writer.FinalizeResponseAsync();

        int countAfterFirstFinalize = handler.RequestBodies.Count;

        // A second finalize is a no-op until the next append/informative reopens the stream.
        await writer.FinalizeResponseAsync();

        Assert.Equal(countAfterFirstFinalize, handler.RequestBodies.Count);
    }

    // ── Informative-first path ────────────────────────────────────────────────

    [Fact]
    public async Task SendInformativeAsync_SendsTypingActivityWithInformativeStreamType()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeUpdateAsync("Thinking…");

        Assert.Single(handler.RequestBodies);
        string body = handler.RequestBodies[0];
        Assert.Contains("\"type\": \"typing\"", body);
        Assert.Contains("\"streamType\": \"informative\"", body);
        Assert.Contains("\"streamSequence\": 1", body);
        Assert.Contains("Thinking", body);
    }

    [Fact]
    public async Task AppendAsync_AfterSendInformativeAsync_SendsAccumulatedText()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeUpdateAsync("Hello");
        await writer.AppendResponseAsync("World");

        Assert.Equal(2, handler.RequestBodies.Count);
        string body = handler.RequestBodies[1];
        Assert.Contains("\"type\": \"typing\"", body);
        Assert.Contains("\"streamType\": \"streaming\"", body);
        Assert.Contains("World", body);
    }

    [Fact]
    public async Task FinalizeAsync_SendsFullAccumulatedText()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeUpdateAsync("info");

        await writer.AppendResponseAsync("Hello");
        await writer.AppendResponseAsync(", world");
        await writer.FinalizeResponseAsync();

        string finalBody = handler.RequestBodies.Last();
        Assert.Contains("Hello, world", finalBody);
        Assert.Contains("\"streamType\": \"final\"", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_WithNoAppendCalls_ThrowsInvalidOperationException()
    {
        (TeamsStreamingWriter writer, _) = CreateWriter();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.FinalizeResponseAsync());
    }

    [Fact]
    public async Task FinalizeAsync_AfterOnlyInformative_ThrowsInvalidOperationException()
    {
        (TeamsStreamingWriter writer, _) = CreateWriter();

        await writer.SendInformativeUpdateAsync("Thinking…");

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.FinalizeResponseAsync());
    }

    // ── MessageActivity-based finalize ────────────────────────────────────────

    [Fact]
    public async Task FinalizeAsync_WithCustomMessageActivity_UsesCallerSuppliedContent()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync("streamed text");

        MessageActivity final = new("explicit text");
        final.AddFeedback(FeedbackTypes.Custom);

        await writer.FinalizeResponseAsync(final);

        string finalBody = handler.RequestBodies.Last();
        // Caller-supplied text wins over accumulated.
        Assert.Contains("explicit text", finalBody);
        Assert.DoesNotContain("streamed text", finalBody);
        // Writer still injects the streamType=final marker.
        Assert.Contains("\"streamType\": \"final\"", finalBody);
        // Custom feedback set on the caller's activity is preserved.
        Assert.Contains("\"feedbackLoop\"", finalBody);
        Assert.Contains("\"type\": \"custom\"", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_WithMessageActivityWithoutText_FallsBackToAccumulated()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync("Hello, ");
        await writer.AppendResponseAsync("world");

        // No Text set on the activity — writer should fill in the accumulated text.
        MessageActivity final = new();
        final.AddFeedback(FeedbackTypes.Default);

        await writer.FinalizeResponseAsync(final);

        string finalBody = handler.RequestBodies.Last();
        Assert.Contains("Hello, world", finalBody);
        Assert.Contains("\"type\": \"default\"", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_AttachmentOnlyReply_RequiresExplicitEmptyText()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        // Note: no AppendResponseAsync — the reply is the attachment only.
        TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
            .WithContentType("application/vnd.microsoft.card.adaptive")
            .WithContent(new JsonObject { ["type"] = "AdaptiveCard", ["version"] = "1.5" })
            .Build();

        MessageActivity final = new() { Text = "" };
        final.AddAttachment(attachment);

        await writer.FinalizeResponseAsync(final);

        string finalBody = handler.RequestBodies.Last();
        Assert.Contains("\"streamType\": \"final\"", finalBody);
        Assert.Contains("AdaptiveCard", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_EmptyActivityWithNoStreamedText_Throws()
    {
        (TeamsStreamingWriter writer, _) = CreateWriter();

        MessageActivity final = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.FinalizeResponseAsync(final));
    }

    // ── Shared streamId ───────────────────────────────────────────────────────

    [Fact]
    public async Task AllChunks_ShareTheSameStreamId()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeUpdateAsync("Hello");
        await writer.AppendResponseAsync("chunk");
        await writer.FinalizeResponseAsync();

        List<string?> streamIds = handler.RequestBodies
            .Select(b =>
            {
                int start = b.IndexOf("\"streamId\": \"", StringComparison.Ordinal);
                if (start < 0) return null;
                start += "\"streamId\": \"".Length;
                int end = b.IndexOf('"', start);
                return end > start ? b[start..end] : null;
            })
            .ToList();

        Assert.Equal(3, streamIds.Count);
        Assert.Null(streamIds[0]);
        Assert.NotNull(streamIds[1]);
        Assert.Equal(streamIds[1], streamIds[2]);
    }

    // ── Error handling (HTTP 403) ─────────────────────────────────────────────

    [Fact]
    public async Task AppendAsync_403NotAllowed_ThrowsStreamNotAllowedException()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("{\"error\":{\"message\":\"Content stream is not allowed\"}}", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<StreamNotAllowedException>(() => writer.AppendResponseAsync("hi"));
    }

    [Fact]
    public async Task AppendAsync_403UnknownMessage_ThrowsTerminalStreamException()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("{\"error\":{\"message\":\"Message size too large\"}}", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<TerminalStreamException>(() => writer.AppendResponseAsync("hi"));
    }

    [Fact]
    public async Task AppendAsync_403Cancelled_IsSwallowedAndFlagsCancelled()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("{\"error\":{\"message\":\"Content stream was canceled by user\"}}", HttpStatusCode.Forbidden);

        // Cancellation is swallowed, not thrown.
        await writer.AppendResponseAsync("hi");

        Assert.True(writer.Cancelled);

        // Finalize sends nothing once the stream is cancelled.
        int countBeforeFinalize = handler.Requests.Count;
        await writer.FinalizeResponseAsync();
        Assert.Equal(countBeforeFinalize, handler.Requests.Count);
    }

    [Fact]
    public async Task FinalizeAsync_AfterStreamingTimeout_UpdatesOriginalMessageInPlace()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        // Informative send assigns the streamId that the in-place update will target.
        handler.EnqueueResponse("{\"id\":\"stream-1\"}");
        await writer.SendInformativeUpdateAsync("Thinking…");

        // The next chunk trips the two-minute limit.
        handler.EnqueueResponse(
            "{\"error\":{\"message\":\"Content stream finished due to exceeded streaming time.\"}}",
            HttpStatusCode.Forbidden);
        await writer.AppendResponseAsync("Final answer");

        Assert.True(writer.TimedOut);

        await writer.FinalizeResponseAsync();

        // Finalize updates the original streamed message in place: a PUT to the streamId,
        // carrying the buffered text without any stream markers.
        HttpRequestMessage lastRequest = handler.Requests.Last();
        Assert.Equal(HttpMethod.Put, lastRequest.Method);
        Assert.EndsWith("/activities/stream-1", lastRequest.RequestUri!.AbsolutePath);

        string lastBody = handler.RequestBodies.Last();
        Assert.Contains("Final answer", lastBody);
        Assert.DoesNotContain("\"streamType\": \"final\"", lastBody);
        Assert.DoesNotContain("streaminfo", lastBody);
    }

    [Fact]
    public async Task FinalizeAsync_WhenFinalSendTimesOut_FallsBackToInPlaceUpdate()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        // First chunk streams fine and assigns the streamId.
        handler.EnqueueResponse("{\"id\":\"stream-1\"}");
        await writer.AppendResponseAsync("Buffered answer");

        // The final streamed send itself trips the two-minute limit.
        handler.EnqueueResponse(
            "{\"error\":{\"message\":\"Content stream finished due to exceeded streaming time.\"}}",
            HttpStatusCode.Forbidden);
        await writer.FinalizeResponseAsync();

        Assert.True(writer.TimedOut);

        // The final send fell back to updating the original message in place.
        HttpRequestMessage lastRequest = handler.Requests.Last();
        Assert.Equal(HttpMethod.Put, lastRequest.Method);
        Assert.EndsWith("/activities/stream-1", lastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("Buffered answer", handler.RequestBodies.Last());
    }
}
