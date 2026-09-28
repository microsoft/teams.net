// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Core.Hosting;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeHostingTests
{
    private const string ClientId = "socket-client-id";

    private static ServiceCollection CreateServices(Dictionary<string, string?>? extraConfig = null)
    {
        Dictionary<string, string?> config = new()
        {
            ["AzureAd:ClientId"] = ClientId,
            ["AzureAd:TenantId"] = "socket-tenant-id",
        };
        foreach (KeyValuePair<string, string?> entry in extraConfig ?? [])
        {
            config[entry.Key] = entry.Value;
        }

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(config).Build());
        services.AddLogging();
        return services;
    }

    [Fact]
    public void WithoutSocketMode_RegistersNoSocketServices()
    {
        ServiceCollection services = CreateServices();
        services.AddTeamsBotApplication();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IHostedService>().OfType<SocketModeHostedService>());
        Assert.Null(provider.GetService<SocketModeTransport>());
    }

    [Fact]
    public async Task WithSocketMode_RegistersTransportAndHostedService()
    {
        ServiceCollection services = CreateServices();
        services.AddTeamsBotApplication(options => options.UseSocketMode());

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<IHostedService>().OfType<SocketModeHostedService>());
        SocketModeTransport transport = provider.GetRequiredService<SocketModeTransport>();
        Assert.Equal(SocketModeStatus.Idle, transport.Status);
        Assert.IsType<SignalRSocketConnectionFactory>(provider.GetRequiredService<ISocketConnectionFactory>());
    }

    [Theory]
    [InlineData("https://api.botframework.us")]
    [InlineData("https://api.botframework.azure.cn")]
    public void WithSocketMode_RejectsNonPublicCloudAtRegistration(string issuer)
    {
        ServiceCollection services = CreateServices(new() { ["BotFramework:BotTokenIssuer"] = issuer });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddTeamsBotApplication(options => options.UseSocketMode()));

        Assert.Contains("Socket Mode is not supported in this cloud environment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(issuer, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithSocketMode_AcceptsPublicIssuerWithTrailingSlash()
    {
        ServiceCollection services = CreateServices(new() { ["BotFramework:BotTokenIssuer"] = "https://api.botframework.com/" });

        services.AddTeamsBotApplication(options => options.UseSocketMode());
    }

    public static TheoryData<Action<SocketModeOptions>> InvalidOptions => new()
    {
        o => o.Geos = [],
        o => o.Geos = ["amer", null!],
        o => o.Geos = ["amer", " AMER/"],
        o => o.StartupTimeout = TimeSpan.FromSeconds(-1),
        o => o.ReadinessTimeout = TimeSpan.Zero,
        o => o.KeepAliveInterval = TimeSpan.Zero,
        o => o.ServerTimeout = TimeSpan.Zero,
    };

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public async Task InvalidOptions_AreAcceptedAtRegistrationAndRejectedWhenTheHostStarts(Action<SocketModeOptions> configure)
    {
        ServiceCollection services = CreateServices();
        services.AddTeamsBotApplication(options => options.UseSocketMode(configure));

        await using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService hosted = Assert.Single(provider.GetServices<IHostedService>().OfType<SocketModeHostedService>());

        await Assert.ThrowsAnyAsync<ArgumentException>(() => hosted.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Negotiator_UsesTheBotTokenForTheBotFrameworkScopeOnAPlainClient()
    {
        List<string> scopes = [];
        Mock<IAuthorizationHeaderProvider> header = new();
        header
            .Setup(h => h.CreateAuthorizationHeaderForAppAsync(It.IsAny<string>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<CancellationToken>()))
            .Returns((string scope, AuthorizationHeaderProviderOptions _, CancellationToken __) =>
            {
                scopes.Add(scope);
                return Task.FromResult("Bearer bot-token");
            });
        RecordingHandler handler = new();

        ServiceCollection services = CreateServices();
        services.AddTeamsBotApplication(options => options.UseSocketMode());
        services.AddKeyedSingleton("AzureAd", new BotTokenProvider(header.Object));
        services.AddHttpClient(SocketModeServiceRegistration.NegotiatorHttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        SocketModeNegotiateResponse response = await provider.GetRequiredService<ISocketModeNegotiator>()
            .NegotiateAsync(new Uri("https://botapi.skype.com/amer/v3/websockets/connect"));

        Assert.Equal("signalr-token", response.AccessToken);
        Assert.Equal(["https://api.botframework.com/.default"], scopes);
        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("bot-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task HostedService_StartsEveryGeoDispatchesToTheAppAndStops()
    {
        FakeConnectionFactory factory = new();
        ServiceCollection services = CreateServices();
        services.AddSingleton<ISocketConnectionFactory>(factory);
        services.AddTeamsBotApplication(options => options.UseSocketMode(socket => socket.Geos = ["amer", "emea"]));

        await using ServiceProvider provider = services.BuildServiceProvider();
        object body = new { hello = "world" };
        provider.GetRequiredService<TeamsBotApplication>()
            .OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, body)));
        IHostedService hosted = Assert.Single(provider.GetServices<IHostedService>().OfType<SocketModeHostedService>());
        SocketModeTransport transport = provider.GetRequiredService<SocketModeTransport>();

        await hosted.StartAsync(CancellationToken.None);

        Assert.Equal(SocketModeStatus.Ready, transport.Status);
        Assert.Equal(2, factory.Connections.Count);

        SocketReplyFrame? reply = await factory.Connections[0].Handlers.OnActivity(new SocketActivityEnvelope
        {
            EnvelopeId = "envelope-1",
            Type = TeamsActivityTypes.Invoke,
            Payload = JsonSerializer.SerializeToElement(new { type = TeamsActivityTypes.Invoke, name = InvokeNames.TaskFetch }),
        });

        Assert.NotNull(reply);
        Assert.Equal(202, reply.Status);
        Assert.Same(body, reply.Body);
        Assert.Equal(ClientId, reply.BotKey);

        await hosted.StopAsync(CancellationToken.None);

        Assert.Equal(SocketModeStatus.Stopped, transport.Status);
        Assert.All(factory.Connections, connection => Assert.True(connection.Disposed));
    }

    [Fact]
    public async Task HostedService_PropagatesStartupFailure()
    {
        IOException failure = new("negotiate failed");
        FakeConnectionFactory factory = new() { StartError = failure };
        ServiceCollection services = CreateServices();
        services.AddSingleton<ISocketConnectionFactory>(factory);
        services.AddTeamsBotApplication(options => options.UseSocketMode(socket => socket.StartupTimeout = TimeSpan.Zero));

        await using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService hosted = Assert.Single(provider.GetServices<IHostedService>().OfType<SocketModeHostedService>());

        IOException thrown = await Assert.ThrowsAsync<IOException>(() => hosted.StartAsync(CancellationToken.None));

        Assert.Same(failure, thrown);
        Assert.Equal(SocketModeStatus.Stopped, provider.GetRequiredService<SocketModeTransport>().Status);
    }

    [Fact]
    public async Task DispatchAsync_NonInvokeReturnsOkWithoutBody()
    {
        ServiceCollection services = CreateServices();
        services.AddTeamsBotApplication();
        await using ServiceProvider provider = services.BuildServiceProvider();
        TeamsBotApplication app = provider.GetRequiredService<TeamsBotApplication>();
        app.OnMessage((_, _) => Task.CompletedTask);

        SocketDispatchResult result = await SocketModeServiceRegistration.DispatchAsync(
            app,
            new Core.Schema.CoreActivity { Type = TeamsActivityTypes.Message });

        Assert.Equal(new SocketDispatchResult(200), result);
    }

    [Fact]
    public async Task UseTeamsBotApplication_WithSocketMode_ThrowsAndMapsNothing()
    {
        await using WebApplication app = BuildWebApplication(options => options.UseSocketMode());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => app.UseTeamsBotApplication());

        Assert.Contains("UseTeamsSocketApplication", exception.Message, StringComparison.Ordinal);
        Assert.Empty(MappedRoutes(app));
    }

    [Fact]
    public async Task WebApplication_WithSocketMode_FailsToStart()
    {
        FakeConnectionFactory factory = new();
        await using WebApplication app = BuildWebApplication(options => options.UseSocketMode(), factory);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());

        Assert.Contains("without a web server", exception.Message, StringComparison.Ordinal);
        Assert.Empty(factory.Connections);
    }

    [Fact]
    public async Task GenericHost_StartsSocketModeAndExposesTheApp()
    {
        FakeConnectionFactory factory = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [] });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = ClientId,
            ["AzureAd:TenantId"] = "socket-tenant-id",
        });
        builder.Services.AddSingleton<ISocketConnectionFactory>(factory);
        builder.Services.AddTeamsBotApplication(options => options.UseSocketMode());
        using IHost host = builder.Build();

        TeamsBotApplication app = host.UseTeamsSocketApplication();
        await host.StartAsync();

        Assert.Same(host.Services.GetRequiredService<TeamsBotApplication>(), app);
        Assert.Equal(SocketModeStatus.Ready, host.Services.GetRequiredService<SocketModeTransport>().Status);
        Assert.Equal(3, factory.Connections.Count);

        await host.StopAsync();

        Assert.Equal(SocketModeStatus.Stopped, host.Services.GetRequiredService<SocketModeTransport>().Status);
    }

    [Fact]
    public void UseTeamsSocketApplication_WithoutSocketMode_Throws()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [] });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["AzureAd:ClientId"] = ClientId });
        builder.Services.AddTeamsBotApplication();
        using IHost host = builder.Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => host.UseTeamsSocketApplication());

        Assert.Contains("Socket Mode is not enabled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UseTeamsBotApplication_WithoutSocketMode_MapsTheHttpEndpoint()
    {
        await using WebApplication app = BuildWebApplication(_ => { });

        app.UseTeamsBotApplication();

        Assert.Equal(["api/messages"], MappedRoutes(app));
    }

    private static WebApplication BuildWebApplication(Action<TeamsBotApplicationOptions> configure, ISocketConnectionFactory? factory = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = ClientId,
            ["AzureAd:TenantId"] = "socket-tenant-id",
        });
        if (factory is not null)
        {
            builder.Services.AddSingleton(factory);
        }

        builder.Services.AddTeamsBotApplication(configure);
        return builder.Build();
    }

    private static string[] MappedRoutes(WebApplication app)
        => [.. ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)];

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"url":"https://example.service.signalr.net/client/?hub=bot","accessToken":"signalr-token","expiresIn":3600}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }
    }

    private sealed class FakeConnectionFactory : ISocketConnectionFactory
    {
        public Exception? StartError { get; init; }

        public List<FakeConnection> Connections { get; } = [];

        public ISocketConnection Create(Uri negotiateUri, SocketConnectionHandlers handlers)
        {
            FakeConnection connection = new(handlers, StartError);
            lock (Connections)
            {
                Connections.Add(connection);
            }

            return connection;
        }
    }

    private sealed class FakeConnection(SocketConnectionHandlers handlers, Exception? startError) : ISocketConnection
    {
        public SocketConnectionHandlers Handlers { get; } = handlers;

        public bool Disposed { get; private set; }

        public TimeSpan? TokenLifetime => null;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (startError is not null)
            {
                return Task.FromException(startError);
            }

            Handlers.OnReady(new SocketReadyFrame { ConnectionId = Guid.NewGuid().ToString() });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
