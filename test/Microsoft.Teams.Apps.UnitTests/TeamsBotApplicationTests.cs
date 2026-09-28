// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsBotApplicationTests
{
    [Fact]
    public async Task Reply_Proactive_ThrowsOnInvalidMessageId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("19:abc@thread.skype", "not-a-number", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_ThrowsOnZeroMessageId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("19:abc@thread.skype", "0", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_ThrowsOnEmptyConversationId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("", "1680000000000", "hello"));
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
        TeamsBotApplication app = CreateApp();
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(200, new { hello = "world" })));
        DefaultHttpContext httpContext = CreateHttpContext(new InvokeActivity(InvokeNames.TaskFetch));
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await app.ProcessAsync(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.StartsWith("application/json", httpContext.Response.ContentType, StringComparison.Ordinal);
        Assert.Equal("{\"hello\":\"world\"}", Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_InvokeReturnsResponse()
    {
        TeamsBotApplication app = CreateApp();
        object body = new { hello = "world" };
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, body)));

        CoreInvokeResponse? response = await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

        Assert.NotNull(response);
        Assert.Equal(202, response.Status);
        Assert.Same(body, response.Body);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_UnhandledInvokeReturnsNotImplemented()
    {
        TeamsBotApplication app = CreateApp();
        app.OnMessage((_, _) => Task.CompletedTask);

        CoreInvokeResponse? response = await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

        Assert.NotNull(response);
        Assert.Equal(501, response.Status);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_MessageReturnsNull()
    {
        TeamsBotApplication app = CreateApp();
        bool handled = false;
        app.OnMessage((_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        CoreInvokeResponse? response = await app.ProcessAsync(new CoreActivity(ActivityType.Message), user: null, correlationVector: null);

        Assert.True(handled);
        Assert.Null(response);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_DoesNotWriteToAmbientHttpContext()
    {
        DefaultHttpContext ambient = new();
        MemoryStream responseBody = new();
        ambient.Response.Body = responseBody;
        TeamsBotApplication app = CreateApp(new HttpContextAccessor { HttpContext = ambient });
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, new { hello = "world" })));

        await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch), user: null, correlationVector: null);

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

    private static TeamsBotApplication CreateApp(IHttpContextAccessor? httpContextAccessor = null)
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

        return new TeamsBotApplication(
            apiClient,
            httpContextAccessor ?? new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
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
