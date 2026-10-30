// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
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

    // ── TextFormat propagation ─────────────────────────────────────────────────

    [Fact]
    public async Task PerChunkTextFormat_AppliesToInformativeStreamingAndFinalMessage()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.SendInformativeUpdateAsync("Thinking…", TextFormats.ExtendedMarkdown);
        await writer.AppendResponseAsync(new MessageActivityInput().WithText("Hello, world", TextFormats.ExtendedMarkdown));
        await writer.FinalizeResponseAsync();

        Assert.Equal(3, handler.RequestBodies.Count);
        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies[0]);
        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies[1]);
        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies[2]);
    }

    [Fact]
    public async Task WithoutTextFormat_NoTextFormatSentOnChunksOrFinalMessage()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync("Hello, world");
        await writer.FinalizeResponseAsync();

        Assert.All(handler.RequestBodies, body => Assert.DoesNotContain("\"textFormat\"", body, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AppendWithMessageActivityInput_TextFormatCarriesToFinalMessage()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        // The chunk's format becomes the last-streamed format and is applied to the final message.
        await writer.AppendResponseAsync(new MessageActivityInput().WithText("Hello, world", TextFormats.ExtendedMarkdown));
        await writer.FinalizeResponseAsync();

        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies[0]);
        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies.Last());
    }

    [Fact]
    public async Task AppendWithMessageActivityInput_LastFormatWins()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync(new MessageActivityInput().WithText("a", TextFormats.Markdown));
        // Wait out the rate limit so the second chunk actually sends.
        await Task.Delay(600);
        await writer.AppendResponseAsync(new MessageActivityInput().WithText("b", TextFormats.ExtendedMarkdown));
        await writer.FinalizeResponseAsync();

        // The most recent chunk's format wins for the final message.
        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies.Last());
    }

    [Fact]
    public async Task InformativeTextFormat_DoesNotAffectFinalMessageFormat()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        // An informative-only format must not leak into the streamed/final message.
        await writer.SendInformativeUpdateAsync("Thinking…", TextFormats.ExtendedMarkdown);
        await writer.AppendResponseAsync("Hello, world");
        await writer.FinalizeResponseAsync();

        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies[0]);
        Assert.DoesNotContain("\"textFormat\"", handler.RequestBodies[1], StringComparison.Ordinal);
        Assert.DoesNotContain("\"textFormat\"", handler.RequestBodies.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FinalizeAsync_ExplicitTextFormatOnFinalActivity_OverridesStreamedFormat()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync(new MessageActivityInput().WithText("Hello, world", TextFormats.ExtendedMarkdown));

        MessageActivityInput final = new MessageActivityInput().WithTextFormat(TextFormats.Markdown);
        await writer.FinalizeResponseAsync(final);

        // Intermediate chunk carried the streamed format...
        Assert.Contains("\"textFormat\": \"extendedmarkdown\"", handler.RequestBodies[0]);
        // ...but the caller's explicit format on the final activity wins there.
        string finalBody = handler.RequestBodies.Last();
        Assert.Contains("\"textFormat\": \"markdown\"", finalBody);
        Assert.DoesNotContain("extendedmarkdown", finalBody);
    }

    [Fact]
    public async Task StreamedTextFormat_IsResetWhenStreamIsReusedAfterFinalize()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();

        await writer.AppendResponseAsync(new MessageActivityInput().WithText("First message", TextFormats.ExtendedMarkdown));
        await writer.FinalizeResponseAsync();

        // Reopening the stream with a plain append should not carry the previous stream's format forward.
        await writer.AppendResponseAsync("Second message");
        await writer.FinalizeResponseAsync();

        string secondStreamFinalBody = handler.RequestBodies.Last();
        Assert.Contains("Second message", secondStreamFinalBody);
        Assert.DoesNotContain("\"textFormat\"", secondStreamFinalBody);
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

        MessageActivityInput final = new MessageActivityInput()
            .WithText("explicit text")
            .AddFeedback(FeedbackTypes.Custom)
            ;

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
        MessageActivityInput final = new MessageActivityInput()
            .AddFeedback(FeedbackTypes.Default)
            ;

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
            .WithContentType(AttachmentContentType.AdaptiveCard)
            .WithContent(new JsonObject { ["type"] = "AdaptiveCard", ["version"] = "1.5" })
            .Build();

        MessageActivityInput final = new MessageActivityInput()
            .WithText("")
            .AddAttachment(attachment)
            ;

        await writer.FinalizeResponseAsync(final);

        string finalBody = handler.RequestBodies.Last();
        Assert.Contains("\"streamType\": \"final\"", finalBody);
        Assert.Contains("AdaptiveCard", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_EmptyActivityWithNoStreamedText_Throws()
    {
        (TeamsStreamingWriter writer, _) = CreateWriter();

        MessageActivityInput final = new MessageActivityInput();

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

    // ── replyToId threading ───────────────────────────────────────────────────

    [Fact]
    public async Task AllSends_ThreadUnderInboundActivity_WhenReferenceHasId()
    {
        TeamsActivity reference = CreateReferenceActivity();
        reference.Id = "inbound-activity-id";
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter(reference);

        await writer.SendInformativeUpdateAsync("Thinking…"); // informative update
        await writer.AppendResponseAsync("hello ");           // streaming chunk
        await writer.AppendResponseAsync("world");            // accumulated (rate-limited)
        await writer.FinalizeResponseAsync();                 // final message

        Assert.NotEmpty(handler.RequestBodies);

        // Informative, streaming, and final sends are all threaded to the inbound message.
        Assert.All(
            handler.RequestBodies,
            body => Assert.Contains("\"replyToId\": \"inbound-activity-id\"", body, StringComparison.Ordinal));

        // And specifically the final message.
        string finalBody = handler.RequestBodies.Last();
        Assert.Contains("\"streamType\": \"final\"", finalBody);
        Assert.Contains("\"replyToId\": \"inbound-activity-id\"", finalBody);
    }

    [Fact]
    public async Task FinalizeAsync_AfterTimeout_ThreadsInPlaceUpdateUnderInboundActivity()
    {
        TeamsActivity reference = CreateReferenceActivity();
        reference.Id = "inbound-activity-id";
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter(reference);

        // Informative send assigns the streamId that the in-place update will target.
        handler.EnqueueResponse("{\"id\":\"stream-1\"}");
        await writer.SendInformativeUpdateAsync("Thinking…");

        // The next chunk trips the two-minute limit, forcing the in-place update path.
        handler.EnqueueResponse(
            "{\"error\":{\"message\":\"Content stream finished due to exceeded streaming time.\"}}",
            HttpStatusCode.Forbidden);
        await writer.AppendResponseAsync("Final answer");

        Assert.True(writer.TimedOut);

        await writer.FinalizeResponseAsync();

        // The in-place update (PUT) is still threaded under the inbound message.
        HttpRequestMessage lastRequest = handler.Requests.Last();
        Assert.Equal(HttpMethod.Put, lastRequest.Method);
        Assert.Contains("\"replyToId\": \"inbound-activity-id\"", handler.RequestBodies.Last());
    }

    // ── Error handling (HTTP 403) ─────────────────────────────────────────────

    [Fact]
    public async Task AppendAsync_403NotAllowed_ThrowsHttpRequestException()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("{\"error\":{\"message\":\"Content stream is not allowed\"}}", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<HttpRequestException>(() => writer.AppendResponseAsync("hi"));
    }

    [Fact]
    public async Task AppendAsync_403UnknownMessage_ThrowsHttpRequestException()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("{\"error\":{\"message\":\"Message size too large\"}}", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<HttpRequestException>(() => writer.AppendResponseAsync("hi"));
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

    [Theory]
    [InlineData(null, "bound-prompt")]
    [InlineData("selected-prompt", "selected-prompt")]
    public async Task PlanCompletion_IsFinalOnlyPostAndPreservesCallerAndReplyTarget(string? replyToId, string expectedPrompt)
    {
        TeamsActivity reference = CreateReferenceActivity();
        reference.Id = "bound-prompt";
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter(reference);
        MessageActivityInput chunk = new MessageActivityInput { Text = "Answer", TextFormat = TextFormats.Markdown }
            .AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });
        handler.EnqueueResponse("""{"id":"stream-id"}""");
        await writer.AppendResponseAsync(chunk);
        Assert.DoesNotContain("planInfo", handler.RequestBodies[0]);

        MessageActivityInput final = new MessageActivityInput { Id = "stream-id", ReplyToId = replyToId }
            .AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete })
            .AddFeedback();
        string before = final.ToJson();
        await writer.FinalizeResponseAsync(final);
        await writer.FinalizeResponseAsync(final);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));
        JsonNode body = JsonNode.Parse(handler.RequestBodies[1])!;
        Assert.Equal("message", body["type"]!.GetValue<string>());
        Assert.Equal("Answer", body["text"]!.GetValue<string>());
        Assert.Equal("markdown", body["textFormat"]!.GetValue<string>());
        Assert.Equal(expectedPrompt, body["replyToId"]!.GetValue<string>());
        Assert.Equal("complete", body["entities"]!.AsArray().Single(e => e!["type"]!.GetValue<string>() == "planInfo")!["planType"]!.GetValue<string>());
        Assert.Equal("final", body["entities"]!.AsArray().Single(e => e!["type"]!.GetValue<string>() == "streaminfo")!["streamType"]!.GetValue<string>());
        Assert.Equal(before, final.ToJson());
        Assert.Equal("bound-prompt", reference.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlanCompletion_TimeoutNeverUsesPutOrStripsCallerMetadata(bool timeoutOnFinal)
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("""{"id":"stream-id"}""");
        if (timeoutOnFinal)
            await writer.AppendResponseAsync("Answer");
        else
            await writer.SendInformativeUpdateAsync("Thinking");

        handler.EnqueueResponse("""{"error":{"message":"Content stream finished due to exceeded streaming time."}}""", HttpStatusCode.Forbidden);
        if (!timeoutOnFinal)
            await writer.AppendResponseAsync("Answer");

        MessageActivityInput final = new MessageActivityInput()
            .AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete })
            .AddFeedback();
        string before = final.ToJson();
        TimeoutException error = await Assert.ThrowsAsync<TimeoutException>(() => writer.FinalizeResponseAsync(final));
        Assert.Contains("requires a POST", error.Message);
        Assert.True(writer.TimedOut);
        Assert.False(writer.Cancelled);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));
        Assert.Equal(before, final.ToJson());
        await writer.FinalizeResponseAsync(final);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":"Other","message":"Conflict"}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":null}""")]
    [InlineData(HttpStatusCode.Conflict, "not json")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":{"code":"PlanConflict"}}""")]
    public async Task PlanCompletion_ServiceFailurePreservesHttpErrorWithoutRetryOrTimeout(HttpStatusCode status, string errorBody)
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("""{"id":"stream-id"}""");
        await writer.AppendResponseAsync("Answer");
        handler.EnqueueResponse(errorBody, status);
        MessageActivityInput final = new MessageActivityInput().AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => writer.FinalizeResponseAsync(final));
        Assert.Equal(status, error.StatusCode);
        Assert.Contains(errorBody, error.Message);
        Assert.False(writer.TimedOut);
        Assert.False(writer.Cancelled);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));

        await writer.FinalizeResponseAsync(final);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task PlanConflictDuringChunk_PropagatesImmediately()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        handler.EnqueueResponse("""{"error":{"code":"PlanConflict","message":"Stopped"}}""", HttpStatusCode.Conflict);
        PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() => writer.AppendResponseAsync("Answer"));
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("PlanConflict", error.ErrorCode);
        Assert.Equal("Stopped", error.ErrorMessage);
        Assert.Single(handler.Requests);
        Assert.False(writer.TimedOut);
    }

    [Theory]
    [InlineData("Stopped")]
    [InlineData("exceeded streaming time")]
    public async Task PlanConflictOnFinal_IsNotATimeoutAndNeverFallsBack(string message)
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        await writer.AppendResponseAsync("Answer");
        string body = System.Text.Json.JsonSerializer.Serialize(new { error = new { code = "PlanConflict", message } });
        handler.EnqueueResponse(body, HttpStatusCode.Conflict);
        MessageActivityInput final = new MessageActivityInput().AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });
        string before = final.ToJson();

        PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() => writer.FinalizeResponseAsync(final));
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("PlanConflict", error.ErrorCode);
        Assert.Equal(message, error.ErrorMessage);
        Assert.Equal(body, error.ResponseBody);
        Assert.Equal(error.Message, Assert.IsType<HttpRequestException>(error.InnerException).Message);
        Assert.False(writer.TimedOut);
        Assert.False(writer.Cancelled);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));
        Assert.Equal(before, final.ToJson());

        await writer.FinalizeResponseAsync(final);
        await writer.FinalizeResponseAsync();
        Assert.Equal(2, handler.Requests.Count);
        Assert.False(writer.TimedOut);
        Assert.False(writer.Cancelled);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("formatted")]
    [InlineData("informative")]
    public async Task PlanConflictOnFinal_NextWriteStartsFreshStream(string nextWrite)
    {
        TeamsActivity reference = CreateReferenceActivity();
        reference.Id = "bound-prompt";
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter(reference);
        handler.EnqueueResponse("""{"id":"old-stream"}""");
        await writer.AppendResponseAsync(
            new MessageActivityInput().WithText("Old answer", TextFormats.ExtendedMarkdown));
        handler.EnqueueResponse("""{"error":{"code":"PlanConflict","message":"Stopped"}}""", HttpStatusCode.Conflict);
        MessageActivityInput rejected = new MessageActivityInput()
            .AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete })
            .AddFeedback();
        await Assert.ThrowsAsync<PlanConflictException>(() => writer.FinalizeResponseAsync(rejected));

        handler.EnqueueResponse("""{"id":"new-stream"}""");
        switch (nextWrite)
        {
            case "text":
                await writer.AppendResponseAsync("Fresh answer");
                break;
            case "formatted":
                await writer.AppendResponseAsync(
                    new MessageActivityInput().WithText("Fresh answer", TextFormats.Markdown));
                break;
            case "informative":
                await writer.SendInformativeUpdateAsync("Fresh thinking");
                await writer.AppendResponseAsync("Fresh answer");
                break;
        }

        Assert.Equal(nextWrite == "informative" ? 4 : 3, handler.Requests.Count);
        JsonNode first = JsonNode.Parse(handler.RequestBodies[2])!;
        JsonNode firstInfo = Assert.Single(first["entities"]!.AsArray(),
            entity => entity!["type"]!.GetValue<string>() == "streaminfo")!;
        Assert.Equal(1, firstInfo["streamSequence"]!.GetValue<int>());
        Assert.Null(firstInfo["streamId"]);
        Assert.Equal("bound-prompt", first["replyToId"]!.GetValue<string>());
        Assert.Equal(nextWrite == "informative" ? "informative" : "streaming",
            firstInfo["streamType"]!.GetValue<string>());

        await writer.FinalizeResponseAsync();
        JsonNode final = JsonNode.Parse(handler.RequestBodies.Last())!;
        Assert.Equal("Fresh answer", final["text"]!.GetValue<string>());
        Assert.Equal(nextWrite == "formatted" ? "markdown" : null, final["textFormat"]?.GetValue<string>());
        JsonNode finalInfo = Assert.Single(final["entities"]!.AsArray())!;
        Assert.Equal("streaminfo", finalInfo["type"]!.GetValue<string>());
        Assert.Equal("final", finalInfo["streamType"]!.GetValue<string>());
        Assert.Equal("new-stream", finalInfo["streamId"]!.GetValue<string>());
        Assert.Equal("bound-prompt", final["replyToId"]!.GetValue<string>());
        Assert.DoesNotContain("planInfo", final.ToJsonString());
        Assert.DoesNotContain("feedbackLoopEnabled", final.ToJsonString());
        Assert.False(writer.TimedOut);
        Assert.False(writer.Cancelled);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));

        int sent = handler.Requests.Count;
        await writer.FinalizeResponseAsync();
        Assert.Equal(sent, handler.Requests.Count);
    }

    [Fact]
    public async Task PlanConflictWithoutPlanMetadata_DoesNotRetireOrdinaryFinal()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        await writer.AppendResponseAsync("Answer");
        handler.EnqueueResponse("""{"error":{"code":"PlanConflict","message":"Stopped"}}""", HttpStatusCode.Conflict);
        await Assert.ThrowsAsync<PlanConflictException>(() => writer.FinalizeResponseAsync());
        Assert.False(writer.TimedOut);
        Assert.False(writer.Cancelled);

        await writer.FinalizeResponseAsync();
        Assert.Equal(3, handler.Requests.Count);
        JsonNode final = JsonNode.Parse(handler.RequestBodies.Last())!;
        Assert.Equal("Answer", final["text"]!.GetValue<string>());
        Assert.DoesNotContain("planInfo", final.ToJsonString());
    }

    [Fact]
    public async Task PlanCompletion_CancellationIsNotSilentlySuccessful()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        await writer.AppendResponseAsync("Answer");
        handler.EnqueueResponse("""{"error":{"message":"Content stream was canceled by user"}}""", HttpStatusCode.Forbidden);
        MessageActivityInput final = new MessageActivityInput().AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });
        await Assert.ThrowsAsync<HttpRequestException>(() => writer.FinalizeResponseAsync(final));
        Assert.True(writer.Cancelled);
        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.FinalizeResponseAsync(final));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ReopeningAfterPlanCompletion_DoesNotCarryPlanMetadata()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        await writer.AppendResponseAsync("First");
        await writer.FinalizeResponseAsync(new MessageActivityInput().AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete }));
        await writer.AppendResponseAsync("Second");
        await writer.FinalizeResponseAsync();
        Assert.DoesNotContain("planInfo", handler.RequestBodies.Last());
    }

    [Fact]
    public async Task OrdinaryTimeoutFallback_DoesNotMutateCallerFinal()
    {
        (TeamsStreamingWriter writer, FakeHttpMessageHandler handler) = CreateWriter();
        await writer.AppendResponseAsync("Answer");
        handler.EnqueueResponse("""{"error":{"message":"exceeded streaming time"}}""", HttpStatusCode.Forbidden);
        MessageActivityInput final = new MessageActivityInput().AddFeedback().AddStreamFinal();
        string before = final.ToJson();
        await writer.FinalizeResponseAsync(final);
        Assert.Equal(HttpMethod.Put, handler.Requests.Last().Method);
        Assert.Equal(before, final.ToJson());
    }
}
