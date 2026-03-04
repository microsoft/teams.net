// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests.Streaming;

public class ActivityStreamingWriterTests
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
        Type = TeamsActivityType.Message,
        ServiceUrl = new Uri("https://smba.trafficmanager.net/amer/"),
        ChannelId = "msteams",
        Conversation = TeamsConversation.FromConversation(new Conversation { Id = "conv-123" }),
        From = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "user-123", Name = "User" }),
        Recipient = TeamsConversationAccount.FromConversationAccount(new ConversationAccount { Id = "bot-123", Name = "Bot" })
    };

    private static (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) CreateWriter(TeamsActivity? reference = null)
    {
        FakeHttpMessageHandler handler = new();
        ConversationClient client = new(new HttpClient(handler), NullLogger<ConversationClient>.Instance);
        ActivityStreamingWriter writer = new(client, reference ?? CreateReferenceActivity());
        return (writer, handler);
    }

    // ── Guard conditions ──────────────────────────────────────────────────────

    [Fact]
    public async Task AppendAsync_AfterFinalizeAsync_ThrowsInvalidOperationException()
    {
        (ActivityStreamingWriter writer, _) = CreateWriter();

        await writer.AppendAsync("Hello");
        await writer.FinalizeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.AppendAsync("Too late"));
    }

    [Fact]
    public async Task FinalizeAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        (ActivityStreamingWriter writer, _) = CreateWriter();

        await writer.AppendAsync("Hello");
        await writer.FinalizeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.FinalizeAsync());
    }

    // ── Informative-first path ────────────────────────────────────────────────

    [Fact]
    public async Task SendInformativeAsync_SendsMessageActivityWithInformativeStreamType()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeAsync("Thinking…");

        Assert.Single(handler.RequestBodies);
        string body = handler.RequestBodies[0];
        Assert.Contains("\"type\": \"message\"", body);
        Assert.Contains("\"streamType\": \"informative\"", body);
        Assert.Contains("\"streamSequence\": 1", body);
        Assert.Contains("Thinking", body);
    }

    [Fact]
    public async Task AppendAsync_AfterSendInformativeAsync_SendsAccumulatedText()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeAsync("Hello");
        await writer.AppendAsync("World");

        Assert.Equal(2, handler.RequestBodies.Count);
        string body = handler.RequestBodies[1];
        Assert.Contains("\"type\": \"message\"", body);
        Assert.Contains("\"streamType\": \"streaming\"", body);
        Assert.Contains("World", body);
    }

    [Fact]
    public async Task FinalizeAsync_AfterInformative_SendsAccumulatedTextAsFinal()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeAsync("Hello");
        await writer.AppendAsync("Final");
        await writer.FinalizeAsync();

        string finalBody = handler.RequestBodies[2];
        Assert.Contains("\"type\": \"message\"", finalBody);
        Assert.Contains("\"streamType\": \"final\"", finalBody);
        Assert.Contains("Final", finalBody);
    }

    // ── Accumulation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AppendAsync_AccumulatesChunksAndSendsFullTextEachTime()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendAsync("Hello");
        await writer.AppendAsync(", world");

        Assert.Contains("Hello", handler.RequestBodies[0]);
        Assert.Contains("Hello, world", handler.RequestBodies[1]);   // full accumulated text
        Assert.DoesNotContain(", world\"", handler.RequestBodies[0]); // first send has no second chunk
    }

    [Fact]
    public async Task FinalizeAsync_SendsFullAccumulatedText()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendAsync("Hello");
        await writer.AppendAsync(", world");
        await writer.FinalizeAsync();

        string finalBody = handler.RequestBodies[2];
        Assert.Contains("Hello, world", finalBody);
        Assert.Contains("\"streamType\": \"final\"", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_WithNoAppendCalls_ThrowsInvalidOperationException()
    {
        (ActivityStreamingWriter writer, _) = CreateWriter();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.FinalizeAsync());
    }

    [Fact]
    public async Task FinalizeAsync_AfterOnlyInformative_ThrowsInvalidOperationException()
    {
        (ActivityStreamingWriter writer, _) = CreateWriter();

        await writer.SendInformativeAsync("Thinking…");

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.FinalizeAsync());
    }

    // ── Sequence numbering ────────────────────────────────────────────────────

    [Fact]
    public async Task AppendAsync_MultipleChunks_IncrementsSequenceCorrectly()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeAsync("Hello");   // sequence 1
        await writer.AppendAsync("chunk 1");           // sequence 2
        await writer.AppendAsync("chunk 2");           // sequence 3

        Assert.Contains("\"streamSequence\": 2", handler.RequestBodies[1]);
        Assert.Contains("\"streamSequence\": 3", handler.RequestBodies[2]);
    }

    [Fact]
    public async Task AppendAsync_WithoutInformative_SequenceStartsAtOne()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendAsync("chunk 1");   // sequence 1
        await writer.AppendAsync("chunk 2");   // sequence 2
        await writer.AppendAsync("chunk 3");   // sequence 3

        Assert.Contains("\"streamSequence\": 1", handler.RequestBodies[0]);
        Assert.Contains("\"streamSequence\": 2", handler.RequestBodies[1]);
        Assert.Contains("\"streamSequence\": 3", handler.RequestBodies[2]);
    }

    // ── Shared streamId ───────────────────────────────────────────────────────

    [Fact]
    public async Task AllChunks_ShareTheSameStreamId()
    {
        (ActivityStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeAsync("Hello");
        await writer.AppendAsync("chunk");
        await writer.FinalizeAsync();

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
        Assert.NotNull(streamIds[0]);
        Assert.All(streamIds, id => Assert.Equal(streamIds[0], id));
    }
}
