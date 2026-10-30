// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class PlanTests
{
    private const string ConversationId = "chat;messageid=thread-root";
    private const string PromptId = "prompt-id";
    private static readonly Uri ServiceUrl = new("https://example.com/service/");

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<JsonNode> Bodies { get; } = [];
        public Queue<HttpResponseMessage> Responses { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            Bodies.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!);
            return Responses.Count > 0 ? Responses.Dequeue() : Response(HttpStatusCode.Created, """{"id":"answer-id"}""");
        }
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (ApiClient api, RecordingHandler handler) CreateClient()
    {
        RecordingHandler handler = new();
        HttpClient http = new(handler);
        ConversationClient conversation = new(http, NullLogger<ConversationClient>.Instance);
        UserTokenClient token = new(http, new ConfigurationBuilder().Build(), NullLogger<UserTokenClient>.Instance);
        return (new ApiClient(ServiceUrl, http, conversation, token), handler);
    }

    private static PlanStep Step(int version = 1) => new()
    {
        Id = "lookup",
        Version = version,
        Title = "Look up sources",
        Status = PlanStepStatuses.Active
    };

    [Fact]
    public async Task ConstructionAndRestoration_StoreOnlyIdsAndHaveNoLifecycleState()
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        Plan original = new(api, ConversationId, PromptId);
        int sent = 0;
        original.Sent += (_, _) => sent++;
        JsonObject saved = JsonNode.Parse(JsonSerializer.Serialize(original))!.AsObject();
        Assert.Equal(2, saved.Count);
        Assert.Equal(ConversationId, saved["conversationId"]!.GetValue<string>());
        Assert.Equal(PromptId, saved["replyToId"]!.GetValue<string>());
        Assert.Empty(handler.Requests);

        PlanOptions options = JsonSerializer.Deserialize<PlanOptions>(saved.ToJsonString())!;
        Assert.Equal(saved.ToJsonString(), JsonSerializer.SerializeToNode(options)!.ToJsonString());
        Plan restored = new(api, options);
        await restored.UpdateAsync([Step(0)]);
        await restored.StopAsync();
        await restored.StartAsync([]);
        Assert.Equal(0, sent);
        Assert.Equal(0, handler.Bodies[0]["entities"]![0]!["steps"]![0]!["version"]!.GetValue<int>());
        Assert.Empty(handler.Bodies[2]["entities"]![0]!["steps"]!.AsArray());
        Assert.All(handler.Bodies, body => Assert.Equal(PromptId, body["replyToId"]!.GetValue<string>()));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"conversationId":"conversation"}""")]
    [InlineData("""{"replyToId":"prompt"}""")]
    public void Options_RequireBothTargetingIds(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlanOptions>(json));

    [Theory]
    [InlineData("", "prompt")]
    [InlineData("  ", "prompt")]
    [InlineData("conversation", "")]
    [InlineData("conversation", "  ")]
    public void Options_RejectBlankTargetingIds(string conversationId, string replyToId)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        Assert.Throws<ArgumentException>(() =>
            new Plan(api, new PlanOptions { ConversationId = conversationId, ReplyToId = replyToId }));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void Options_RejectNullDependencies()
    {
        (ApiClient api, _) = CreateClient();
        PlanOptions options = new() { ConversationId = ConversationId, ReplyToId = PromptId };
        Assert.Throws<ArgumentNullException>(() => new Plan(null!, options));
        Assert.Throws<ArgumentNullException>(() => new Plan(api, null!));
    }

    [Fact]
    public async Task Factory_SnapshotsOwnPromptAndConversationAndScopesClientIdentity()
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        TeamsBotApplication app = new(api, new HttpContextAccessor(), NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "app-id" });
        TeamsActivity activity = new()
        {
            Id = PromptId,
            ReplyToId = "parent-prompt",
            Conversation = TeamsConversation.FromConversation(new Conversation(ConversationId)),
            ServiceUrl = ServiceUrl,
            Recipient = TeamsChannelAccount.FromChannelAccount(new ChannelAccount
            {
                Id = "bot",
                AgenticAppId = "agent-app",
                AgenticUserId = "agent-user",
                TenantId = "tenant"
            })
        };
        Context<TeamsActivity> context = new(app, activity);
        Plan plan = Plan.CreateFromContext(context);
        Assert.NotSame(plan, Plan.CreateFromContext(context));
        Assert.Null(typeof(Context<TeamsActivity>).GetProperty("Plan"));
        Assert.Null(typeof(Context<TeamsActivity>).GetMethod("Plan"));
        activity.Id = "later-prompt";
        activity.Conversation = TeamsConversation.FromConversation(new Conversation("later-conversation"));
        activity.ServiceUrl = new Uri("https://different.example.com/");

        await plan.StartAsync([Step()]);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal($"{ServiceUrl}v3/conversations/{Uri.EscapeDataString(ConversationId)}/activities/", request.RequestUri!.AbsoluteUri);
        Assert.Equal(PromptId, Assert.Single(handler.Bodies)["replyToId"]!.GetValue<string>());
        Assert.True(request.Options.TryGetValue(new HttpRequestOptionsKey<AgenticIdentity>(BotRequestContext.AgenticIdentityKey), out AgenticIdentity? identity));
        Assert.Equal("agent-user", identity!.AgenticUserId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FinalConflictRecovery_ExplicitCleanupDoesNotRepeatCompletion(bool parameterlessFirst)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        handler.Responses.Enqueue(Response(HttpStatusCode.Accepted, ""));
        handler.Responses.Enqueue(Response(HttpStatusCode.Created, """{"id":"stream-id"}"""));
        string conflictBody = """{"error":{"code":"PlanConflict","message":"Stopped"}}""";
        handler.Responses.Enqueue(Response(HttpStatusCode.Conflict, conflictBody));
        handler.Responses.Enqueue(Response(HttpStatusCode.Accepted, ""));
        TeamsBotApplication app = new(api, new HttpContextAccessor(), NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "app-id" });
        List<PlanSentEventArgs> events = [];
        bool recovered = false;
        app.OnMessage(async (context, cancellationToken) =>
        {
            Plan plan = Plan.CreateFromContext(context);
            plan.Sent += (_, args) => events.Add(args);
            await plan.StartAsync([Step()], cancellationToken);
            TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);
            await writer.AppendResponseAsync("Answer", cancellationToken);
            MessageActivityInput final = plan.Completion();
            Task closing = writer.FinalizeResponseAsync(final, cancellationToken);
            PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() => closing);
            Assert.Same(error, closing.Exception!.InnerException);
            Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
            Assert.Equal(conflictBody, error.ResponseBody);
            Assert.Single(events);

            await plan.StopAsync(cancellationToken);
            recovered = true;
            await writer.FinalizeResponseAsync(parameterlessFirst ? null : final, cancellationToken);
            await writer.FinalizeResponseAsync(parameterlessFirst ? final : null, cancellationToken);
            Assert.False(writer.TimedOut);
            Assert.False(writer.Cancelled);
        });

        await app.ProcessAsync(new CoreActivity(ActivityType.Message)
        {
            Id = PromptId,
            ServiceUrl = ServiceUrl,
            Conversation = new Conversation(ConversationId),
            From = new ChannelAccount { Id = "user" },
            Recipient = new ChannelAccount { Id = "bot" }
        }, user: null, correlationVector: null);

        Assert.True(recovered);
        Assert.Equal(4, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));
        Assert.All(handler.Bodies, body => Assert.Equal(PromptId, body["replyToId"]!.GetValue<string>()));
        Assert.Single(handler.Bodies, body => body["type"]!.GetValue<string>() == "message");
        Assert.Equal("stop", handler.Bodies.Last()["entities"]![0]!["planType"]!.GetValue<string>());
        Assert.Equal(2, events.Count);
        Assert.All(events, args => Assert.Null(args.Response));
    }

    [Theory]
    [InlineData("start")]
    [InlineData("continue")]
    [InlineData("stop")]
    [InlineData("error")]
    public async Task Controls_CreateTypingAndAcceptEmptyAcknowledgements(string operation)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        handler.Responses.Enqueue(Response(HttpStatusCode.Accepted, ""));
        Plan plan = new(api, ConversationId, PromptId);
        List<PlanSentEventArgs> events = [];
        plan.Sent += (_, args) => events.Add(args);
        switch (operation)
        {
            case "start": await plan.StartAsync([Step()]); break;
            case "continue": await plan.UpdateAsync([Step()]); break;
            case "stop": await plan.StopAsync(); break;
            case "error": await plan.ErrorAsync(); break;
        }
        Assert.Equal(HttpMethod.Post, Assert.Single(handler.Requests).Method);
        JsonNode body = Assert.Single(handler.Bodies);
        Assert.Equal("typing", body["type"]!.GetValue<string>());
        Assert.Equal(PromptId, body["replyToId"]!.GetValue<string>());
        JsonObject entity = Assert.Single(body["entities"]!.AsArray())!.AsObject();
        Assert.Equal("planInfo", entity["type"]!.GetValue<string>());
        Assert.Equal(operation, entity["planType"]!.GetValue<string>());
        Assert.Equal(operation is "start" or "continue", entity.ContainsKey("steps"));
        Assert.Null(Assert.Single(events).Response);
    }

    [Fact]
    public async Task ControlAcknowledgementId_IsNotExposedAsMessageId()
    {
        (ApiClient api, _) = CreateClient();
        Plan plan = new(api, ConversationId, PromptId);
        List<PlanSentEventArgs> events = [];
        plan.Sent += (_, args) => events.Add(args);
        await plan.StopAsync();
        Assert.Null(Assert.Single(events).Response);
    }

    [Fact]
    public async Task Complete_PreservesCallerDataAndCreatesDespiteExistingId()
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        Plan plan = new(api, ConversationId, PromptId);
        SendActivityResponse? observed = null;
        plan.Sent += (_, args) => observed = args.Response;
        Entity custom = new("custom");
        JsonObject data = new() { ["nested"] = new JsonArray(1, 2) };
        custom.Properties["data"] = data;
        PlanInfoEntity completion = new() { PlanType = PlanTypes.Complete, Steps = [Step(3)] };
        MessageActivityInput message = new MessageActivityInput()
            .WithText("Final answer", TextFormats.Markdown)
            .AddEntity(custom, completion)
            .AddFeedback();
        message.Id = "existing-id";
        message.ReplyToId = "caller-prompt";
        message.Attachments = [new TeamsAttachment { ContentType = AttachmentContentType.AdaptiveCard, Content = JsonSerializer.SerializeToElement(new { type = "AdaptiveCard" }) }];
        message.Properties["customField"] = new JsonObject { ["retained"] = true };
        string before = message.ToJson();

        SendActivityResponse? result = await plan.CompleteAsync(message);

        Assert.Same(result, observed);
        Assert.Equal("answer-id", result!.Id);
        Assert.Equal(before, message.ToJson());
        Assert.Null(data.Parent);
        Assert.Equal(HttpMethod.Post, Assert.Single(handler.Requests).Method);
        JsonNode body = Assert.Single(handler.Bodies);
        Assert.Equal(PromptId, body["replyToId"]!.GetValue<string>());
        Assert.Equal("existing-id", body["id"]!.GetValue<string>());
        Assert.Equal("markdown", body["textFormat"]!.GetValue<string>());
        Assert.True(body["customField"]!["retained"]!.GetValue<bool>());
        Assert.Single(body["attachments"]!.AsArray());
        Assert.Single(body["entities"]!.AsArray(), entity => entity!["type"]!.GetValue<string>() == "planInfo");
        Assert.Equal(3, body["entities"]!.AsArray().Single(entity => entity!["type"]!.GetValue<string>() == "planInfo")!["steps"]![0]!["version"]!.GetValue<int>());
    }

    [Fact]
    public async Task Complete_AllowsEmptyTextAndAddsCompletionOnlyToCopy()
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        Plan plan = new(api, ConversationId, PromptId);
        MessageActivityInput message = new() { Text = "" };
        await plan.CompleteAsync(message);
        await plan.CompleteAsync("Answer");
        Assert.Null(message.Entities);
        Assert.Null(message.ReplyToId);
        Assert.All(handler.Bodies, body =>
            Assert.Equal("complete", Assert.Single(body["entities"]!.AsArray())!["planType"]!.GetValue<string>()));
    }

    [Theory]
    [InlineData("start")]
    [InlineData("continue")]
    [InlineData("stop")]
    [InlineData("error")]
    public async Task Complete_RejectsNonCompletionBeforeSending(string operation)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        Plan plan = new(api, ConversationId, PromptId);
        int sent = 0;
        plan.Sent += (_, _) => sent++;
        MessageActivityInput message = new MessageActivityInput().AddEntity(
            new PlanInfoEntity { PlanType = PlanTypes.Complete },
            new PlanInfoEntity { PlanType = new PlanType(operation) });
        await Assert.ThrowsAsync<ArgumentException>(() => plan.CompleteAsync(message));
        Assert.Empty(handler.Requests);
        Assert.Equal(0, sent);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Unavailable")]
    public async Task FailedSend_PreservesServiceErrorAndDoesNotRetryOrNotify(HttpStatusCode status, string code)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        handler.Responses.Enqueue(Response(status, JsonSerializer.Serialize(new { error = new { code, message = "Stopped" } })));
        Plan plan = new(api, ConversationId, PromptId);
        int sent = 0;
        plan.Sent += (_, _) => sent++;
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => plan.CompleteAsync("Answer"));
        Assert.Equal(status, error.StatusCode);
        Assert.Contains(code, error.Message);
        Assert.Contains("Stopped", error.Message);
        Assert.Single(handler.Requests);
        Assert.Equal(0, sent);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("continue")]
    [InlineData("stop")]
    [InlineData("error")]
    [InlineData("complete")]
    public async Task PlanConflict_IsTypedAndPreservesDetailsWithoutRetryOrNotification(string operation)
    {
        const string body = """{"error":{"code":"PlanConflict","message":"Stopped","details":{"reason":"user"}}}""";
        (ApiClient api, RecordingHandler handler) = CreateClient();
        handler.Responses.Enqueue(Response(HttpStatusCode.Conflict, body));
        Plan plan = new(api, new PlanOptions { ConversationId = ConversationId, ReplyToId = PromptId });
        int sent = 0;
        plan.Sent += (_, _) => sent++;
        PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() => operation switch
        {
            "start" => plan.StartAsync([Step()]),
            "continue" => plan.UpdateAsync([Step()]),
            "stop" => plan.StopAsync(),
            "error" => plan.ErrorAsync(),
            _ => plan.CompleteAsync("Answer")
        });
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("PlanConflict", error.ErrorCode);
        Assert.Equal("Stopped", error.ErrorMessage);
        Assert.Equal(body, error.ResponseBody);
        HttpRequestException original = Assert.IsType<HttpRequestException>(error.InnerException);
        Assert.Equal(error.StatusCode, original.StatusCode);
        Assert.Equal(original.Message, error.Message);
        Assert.Single(handler.Requests);
        Assert.Equal(0, sent);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":"Other","message":"PlanConflict"}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":"planconflict"}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":null}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":409}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":null}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":[]}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":"PlanConflict"}""")]
    [InlineData(HttpStatusCode.Conflict, """{"code":"PlanConflict"}""")]
    [InlineData(HttpStatusCode.Conflict, "[]")]
    [InlineData(HttpStatusCode.Conflict, "null")]
    [InlineData(HttpStatusCode.Conflict, "not json")]
    [InlineData(HttpStatusCode.Conflict, "")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":{"code":"PlanConflict"}}""")]
    public async Task OtherServiceErrors_RetainOrdinaryHttpBehavior(HttpStatusCode status, string body)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        handler.Responses.Enqueue(Response(status, body));
        Plan plan = new(api, ConversationId, PromptId);
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => plan.CompleteAsync("Answer"));
        Assert.Equal(status, error.StatusCode);
        Assert.Contains(body, error.Message);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("""{"error":{"code":"PlanConflict"}}""")]
    [InlineData("""{"error":{"code":"PlanConflict","message":null}}""")]
    [InlineData("""{"error":{"code":"PlanConflict","message":{"detail":"Stopped"}}}""")]
    public async Task PlanConflict_DoesNotRequireAStringMessage(string body)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        handler.Responses.Enqueue(Response(HttpStatusCode.Conflict, body));
        Plan plan = new(api, ConversationId, PromptId);
        PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() => plan.StopAsync());
        Assert.Null(error.ErrorMessage);
        Assert.Equal(body, error.ResponseBody);
    }

    [Fact]
    public async Task CancelledSend_DoesNotNotify()
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        Plan plan = new(api, ConversationId, PromptId);
        int sent = 0;
        plan.Sent += (_, _) => sent++;
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plan.StartAsync([Step()], cancellation.Token));
        Assert.Empty(handler.Requests);
        Assert.Equal(0, sent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LowLevelReply_CreatesWithoutMutatingPlanPayload(bool legacy)
    {
        (ApiClient api, RecordingHandler handler) = CreateClient();
        MessageActivityInput activity = new MessageActivityInput { Id = "existing-id", ReplyToId = "caller-prompt" }
            .AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });
        if (legacy)
        {
#pragma warning disable CS0618
            await api.Conversations.Activities.ReplyAsync(ConversationId, PromptId, activity);
#pragma warning restore CS0618
        }
        else
        {
            await api.Conversations.ReplyToActivityAsync(ConversationId, PromptId, activity);
        }
        Assert.Equal("caller-prompt", activity.ReplyToId);
        Assert.Equal(HttpMethod.Post, Assert.Single(handler.Requests).Method);
        Assert.EndsWith("/activities/", handler.Requests[0].RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal(PromptId, Assert.Single(handler.Bodies)["replyToId"]!.GetValue<string>());
    }
}
