// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsBotApplicationTests
{
    private static readonly Uri ServiceUrl = new("https://test.service.url/");

    [Theory]
    [InlineData("", "1680000000000")]
    [InlineData("   ", "1680000000000")]
    [InlineData("19:abc@thread.v2", "")]
    [InlineData("19:abc@thread.v2", "   ")]
    public async Task ReplyAsync_BlankIds_ThrowBeforeAnyRequest(string conversationId, string messageId)
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();

        await Assert.ThrowsAsync<ArgumentException>(() => app.ReplyAsync(conversationId, messageId, "hello", ServiceUrl));
        await Assert.ThrowsAsync<ArgumentException>(() => app.ReplyAsync(conversationId, messageId, new MessageActivityInput().WithText("hello"), ServiceUrl));

        VerifyNoRequests(conversationClient);
    }

    [Theory]
    [InlineData(null, "1680000000000")]
    [InlineData("19:abc@thread.v2", null)]
    public async Task ReplyAsync_NullIds_ThrowBeforeAnyRequest(string? conversationId, string? messageId)
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => app.ReplyAsync(conversationId!, messageId!, "hello", ServiceUrl));

        VerifyNoRequests(conversationClient);
    }

    [Fact]
    public async Task ReplyAsync_PassesIdsToReplyChainsCallUnchanged()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);

        await app.ReplyAsync("19:abc@thread.v2", "encrypted-root-id", "hello", ServiceUrl);

        Assert.Equal("19:abc@thread.v2", capture.ConversationId);
        Assert.Equal("encrypted-root-id", capture.ActivityId);
        Assert.Equal(ServiceUrl, capture.ServiceUrl);
        Assert.False(capture.IsTargeted);
        Assert.Null(capture.Activity?.ReplyToId);
        VerifyNoSendRequests(conversationClient);
    }

    [Fact]
    public async Task ReplyAsync_DoesNotParseOrStripConversationIdSuffix()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);

        await app.ReplyAsync("19:abc@thread.tacv2;messageid=111", "222", "hello", ServiceUrl);

        Assert.Equal("19:abc@thread.tacv2;messageid=111", capture.ConversationId);
        Assert.Equal("222", capture.ActivityId);
    }

    [Fact]
    public async Task ReplyAsync_WithTargetedRecipient_UsesTargetedReply()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);

        await app.ReplyAsync(
            "19:abc@thread.v2",
            "1680000000000",
            new MessageActivityInput()
                .WithText("hello")
                .WithRecipient(new TeamsChannelAccount { Id = "user-1" }, isTargeted: true),
            ServiceUrl);

        Assert.True(capture.IsTargeted);
    }

    [Fact]
    public async Task ReplyAsync_PassesAgenticIdentityAndCancellationToken()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);
        AgenticIdentity identity = new() { AgenticAppId = "app-123", AgenticUserId = "user-456" };
        using CancellationTokenSource cts = new();

        await app.ReplyAsync("19:abc@thread.v2", "1680000000000", "hello", ServiceUrl, identity, cts.Token);

        Assert.Same(identity, capture.RequestContext?.AgenticIdentity);
        Assert.Equal(cts.Token, capture.CancellationToken);
    }

    [Fact]
    public async Task ReplyAsync_WithoutServiceUrl_ThrowsWhenNoActivityHasBeenSeen()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.ReplyAsync("19:abc@thread.v2", "1680000000000", "hello"));

        VerifyNoRequests(conversationClient);
    }

    [Fact]
    public async Task ReplyAsync_PrefersExplicitServiceUrlOverLastSeen()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);
        await app.OnActivity!(CreateSeedActivity(new Uri("https://seen.service.url/")), CancellationToken.None);

        await app.ReplyAsync("19:abc@thread.v2", "1680000000000", "hello", ServiceUrl);

        Assert.Equal(ServiceUrl, capture.ServiceUrl);
    }

    [Fact]
    public async Task ReplyAsync_UsesLastSeenServiceUrlWhenNoneIsGiven()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);
        Uri seen = new("https://seen.service.url/");
        await app.OnActivity!(CreateSeedActivity(seen), CancellationToken.None);

        await app.ReplyAsync("19:abc@thread.v2", "1680000000000", "hello");

        Assert.Equal(seen, capture.ServiceUrl);
    }

    private static CoreActivity CreateSeedActivity(Uri serviceUrl) => new(ActivityType.Message) { ServiceUrl = serviceUrl };

    [Fact]
    public async Task Reply_Obsolete_UsesReplyChainsCall()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        ReplyCapture capture = SetupReply(conversationClient);
        await app.OnActivity!(CreateSeedActivity(ServiceUrl), CancellationToken.None);

#pragma warning disable CS0618 // exercising the obsolete inbound-activity overload
        await app.Reply("19:abc@thread.v2", "1680000000000", new MessageActivity("hello"));
#pragma warning restore CS0618

        Assert.Equal("19:abc@thread.v2", capture.ConversationId);
        Assert.Equal("1680000000000", capture.ActivityId);
        Assert.False(capture.IsTargeted);
        VerifyNoSendRequests(conversationClient);
    }

    [Fact]
    public async Task SendAsync_SendsToTheExactConversationId()
    {
        (TeamsBotApplication app, Mock<ConversationClient> conversationClient) = CreateAppWithConversationClient();
        string? capturedConversationId = null;
        conversationClient
            .Setup(c => c.SendActivityAsync(
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (conversationId, _, _, _, _, _, _) => capturedConversationId = conversationId)
            .ReturnsAsync(new SendActivityResponse { Id = "sent-id" });

        await app.SendAsync("19:abc@thread.tacv2;messageid=1680000000000", new MessageActivityInput().WithText("hello"), ServiceUrl);

        Assert.Equal("19:abc@thread.tacv2;messageid=1680000000000", capturedConversationId);
        conversationClient.Verify(
            c => c.SendReplyChainActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void HasMatchingRoute_ReturnsTrueForRegisteredInvokeHandler()
    {
        TeamsBotApplication app = CreateApp();
        app.OnInvoke((_, _) => Task.FromResult(InvokeResponse.Ok()));

        Assert.True(app.HasMatchingRoute(new InvokeActivity(InvokeNames.TaskFetch)));
        Assert.False(app.HasMatchingRoute(new CoreActivity(ActivityType.Message)));
    }

    [Fact]
    public async Task ProcessAsync_HttpContext_InvokeWritesStatusAndJsonBody()
    {
        DefaultHttpContext httpContext = CreateHttpContext(new InvokeActivity(InvokeNames.TaskFetch));
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;
        // ASP.NET populates the accessor for real requests; the invoke response is written through it.
        TeamsBotApplication app = CreateApp(new HttpContextAccessor { HttpContext = httpContext });
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(200, new { hello = "world" })));

        await app.ProcessAsync(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.StartsWith("application/json", httpContext.Response.ContentType, StringComparison.Ordinal);
        Assert.Equal("{\"hello\":\"world\"}", Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    [Fact]
    public async Task ProcessWithInvokeResponseAsync_InvokeReturnsResponse()
    {
        TeamsBotApplication app = CreateApp();
        object body = new { hello = "world" };
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, body)));

        InvokeResponse? response = await app.ProcessWithInvokeResponseAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

        Assert.NotNull(response);
        Assert.Equal(202, response.Status);
        Assert.Same(body, response.Body);
    }

    [Fact]
    public async Task ProcessWithInvokeResponseAsync_UnhandledInvokeReturnsNotImplemented()
    {
        TeamsBotApplication app = CreateApp();
        app.OnMessage((_, _) => Task.CompletedTask);

        InvokeResponse? response = await app.ProcessWithInvokeResponseAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

        Assert.NotNull(response);
        Assert.Equal(501, response.Status);
    }

    [Fact]
    public async Task ProcessWithInvokeResponseAsync_MessageReturnsNull()
    {
        TeamsBotApplication app = CreateApp();
        bool handled = false;
        app.OnMessage((_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        InvokeResponse? response = await app.ProcessWithInvokeResponseAsync(new CoreActivity(ActivityType.Message), user: null, correlationVector: null);

        Assert.True(handled);
        Assert.Null(response);
    }

    [Fact]
    public async Task ProcessWithInvokeResponseAsync_ConcurrentTurnsReturnOwnResponses()
    {
        TeamsBotApplication app = CreateApp();
        TaskCompletionSource bothStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        app.OnInvoke(async (ctx, _) =>
        {
            if (Interlocked.Increment(ref started) == 2)
            {
                bothStarted.SetResult();
            }

            await bothStarted.Task;
            return new InvokeResponse(200, ctx.Activity.Id);
        });

        Task<InvokeResponse?> first = app.ProcessWithInvokeResponseAsync(new InvokeActivity(InvokeNames.TaskFetch) { Id = "first" }, user: null, correlationVector: null);
        Task<InvokeResponse?> second = app.ProcessWithInvokeResponseAsync(new InvokeActivity(InvokeNames.TaskFetch) { Id = "second" }, user: null, correlationVector: null);
        InvokeResponse?[] responses = await Task.WhenAll(first, second);

        Assert.Equal("first", responses[0]?.Body);
        Assert.Equal("second", responses[1]?.Body);
    }

    [Fact]
    public async Task ProcessWithInvokeResponseAsync_NestedAppDoesNotStealResponse()
    {
        DefaultHttpContext ambient = new();
        MemoryStream responseBody = new();
        ambient.Response.Body = responseBody;
        TeamsBotApplication inner = CreateApp(new HttpContextAccessor { HttpContext = ambient });
        inner.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(418)));
        TeamsBotApplication outer = CreateApp();
        outer.OnInvoke(async (_, ct) =>
        {
            // Another app's turn on the same async flow must not write into this turn's capture.
            await inner.OnActivity!(new InvokeActivity(InvokeNames.TaskFetch), ct);
            return new InvokeResponse(200);
        });

        InvokeResponse? response = await outer.ProcessWithInvokeResponseAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

        Assert.Equal(200, response?.Status);
        Assert.Equal(418, ambient.Response.StatusCode);
    }

    [Fact]
    public async Task ProcessWithInvokeResponseAsync_DoesNotWriteToAmbientHttpContext()
    {
        DefaultHttpContext ambient = new();
        MemoryStream responseBody = new();
        ambient.Response.Body = responseBody;
        TeamsBotApplication app = CreateApp(new HttpContextAccessor { HttpContext = ambient });
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, new { hello = "world" })));

        await app.ProcessWithInvokeResponseAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

        Assert.Equal(200, ambient.Response.StatusCode);
        Assert.Equal(0, responseBody.Length);
    }

    [Fact]
    public async Task OnActivity_InvokedDirectly_FallsBackToHttpContextWrite()
    {
        DefaultHttpContext ambient = new();
        MemoryStream responseBody = new();
        ambient.Response.Body = responseBody;
        TeamsBotApplication app = CreateApp(new HttpContextAccessor { HttpContext = ambient });
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, new { hello = "world" })));

        await app.OnActivity!(new InvokeActivity(InvokeNames.TaskFetch), CancellationToken.None);

        Assert.Equal(202, ambient.Response.StatusCode);
        Assert.Equal("{\"hello\":\"world\"}", Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    private static DefaultHttpContext CreateHttpContext(CoreActivity activity)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(activity.ToJson()));
        httpContext.Request.ContentType = "application/json";
        return httpContext;
    }

    private sealed class ReplyCapture
    {
        public string? ConversationId { get; set; }
        public string? ActivityId { get; set; }
        public CoreActivityInput? Activity { get; set; }
        public Uri? ServiceUrl { get; set; }
        public bool IsTargeted { get; set; }
        public BotRequestContext? RequestContext { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    private static ReplyCapture SetupReply(Mock<ConversationClient> conversationClient)
    {
        ReplyCapture capture = new();
        conversationClient
            .Setup(c => c.SendReplyChainActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (conversationId, activityId, activity, serviceUrl, isTargeted, requestContext, _, cancellationToken) =>
                {
                    capture.ConversationId = conversationId;
                    capture.ActivityId = activityId;
                    capture.Activity = activity;
                    capture.ServiceUrl = serviceUrl;
                    capture.IsTargeted = isTargeted;
                    capture.RequestContext = requestContext;
                    capture.CancellationToken = cancellationToken;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });
        return capture;
    }

    private static void VerifyNoRequests(Mock<ConversationClient> conversationClient)
    {
        conversationClient.Verify(
            c => c.SendReplyChainActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNoSendRequests(conversationClient);
    }

    private static void VerifyNoSendRequests(Mock<ConversationClient> conversationClient)
    {
        conversationClient.Verify(
            c => c.SendActivityAsync(
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static TeamsBotApplication CreateApp(IHttpContextAccessor? httpContextAccessor = null)
        => CreateAppWithConversationClient(httpContextAccessor).App;

    private static (TeamsBotApplication App, Mock<ConversationClient> ConversationClient) CreateAppWithConversationClient(IHttpContextAccessor? httpContextAccessor = null)
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);

        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        TeamsBotApplication app = new(
            apiClient,
            httpContextAccessor ?? new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        return (app, mockConversationClient);
    }

    /// <summary>
    /// The five-parameter constructor shipped in 2.1.0 still exists.
    /// <para>C# bakes optional arguments into the CALLER, so an assembly compiled against 2.1.0 emits a call to the
    /// five-parameter form. Removing this overload makes that call throw <c>MissingMethodException</c> at runtime,
    /// which recompiling the consuming app cannot fix when the offending IL is inside a third-party library. The
    /// reflection assertion below is what detects removal.</para>
    /// </summary>
    [Fact]
    public void TheConstructorShippedIn2_1_0_StillExists()
    {
        Mock<UserTokenClient> userTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);
        Mock<ConversationClient> conversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);
        ApiClient apiClient = new(new HttpClient(), conversationClient.Object, userTokenClient.Object);

        // Five positional arguments prove the overload is callable and forwards correctly. They do NOT pin its
        // existence: the six-parameter constructor's last three parameters are all optional, so with the overload
        // deleted this exact call silently re-binds to it. The reflection assertion below is what detects removal.
        TeamsBotApplication app = new(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" },
            null);

        Assert.NotNull(app);

        // The actual guard. Binary compatibility is about the emitted signature, so it has to be asserted against
        // the metadata rather than through a call the compiler is free to redirect to a different overload.
        ConstructorInfo? ctor = typeof(TeamsBotApplication).GetConstructor(
        [
            typeof(ApiClient),
            typeof(IHttpContextAccessor),
            typeof(ILogger<TeamsBotApplication>),
            typeof(TeamsBotApplicationOptions),
            typeof(TurnStateLoader),
        ]);

        Assert.NotNull(ctor);
    }
}
