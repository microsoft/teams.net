// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class ApiClientAgenticIdentityTests
{
    [Fact]
    public async Task ContextApiUserToken_UsesRecipientAgenticIdentityDefault()
    {
        CapturingHandler handler = new();
        UserTokenClient userTokenClient = CreateUserTokenClient(handler);
        ApiClient apiClient = new(
            new HttpClient(),
            new ConversationClient(new HttpClient(), NullLogger<ConversationClient>.Instance),
            userTokenClient);
        TeamsBotApplication app = new(
            apiClient,
            new Microsoft.AspNetCore.Http.HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "app-id" });
        TeamsActivity activity = new()
        {
            ServiceUrl = new Uri("https://smba.test"),
            Recipient = new TeamsChannelAccount
            {
                AgenticAppId = "agentic-app",
                AgenticUserId = "agentic-user",
                AgenticAppBlueprintId = "agentic-blueprint",
                BotId = "bot-app-id",
            }
        };
        Context<TeamsActivity> context = new(app, activity);

        await context.Api.UserToken.GetAsync("user", "connection", "msteams");

        Assert.NotNull(handler.Request);
        Assert.True(handler.Request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.AgenticIdentityKey), out object? value));
        AgenticIdentity identity = Assert.IsType<AgenticIdentity>(value);
        Assert.Equal("agentic-app", identity.AgenticAppId);
        Assert.Equal("agentic-user", identity.AgenticUserId);
        Assert.Equal("agentic-blueprint", identity.AgenticAppBlueprintId);
        Assert.True(handler.Request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.BotAppIdKey), out object? botAppId));
        Assert.Equal("bot-app-id", botAppId);
    }

    [Fact]
    public async Task UserToken_PerMethodAgenticIdentity_OverridesApiClientDefault()
    {
        CapturingHandler handler = new();
        ApiClient apiClient = new(
            new HttpClient(),
            new ConversationClient(new HttpClient(), NullLogger<ConversationClient>.Instance),
            CreateUserTokenClient(handler));
        AgenticIdentity defaultIdentity = new()
        {
            AgenticAppId = "default-app",
            AgenticUserId = "default-user",
        };
        AgenticIdentity methodIdentity = new()
        {
            AgenticAppId = "method-app",
            AgenticUserId = "method-user",
        };

        await apiClient
            .ForRequestContext(BotRequestContext.FromAgenticIdentity(defaultIdentity))
            .ForServiceUrl(new Uri("https://smba.test"))
            .UserToken
            .GetAsync("user", "connection", "msteams", code: null, agenticIdentity: methodIdentity);

        Assert.NotNull(handler.Request);
        Assert.True(handler.Request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.AgenticIdentityKey), out object? value));
        AgenticIdentity identity = Assert.IsType<AgenticIdentity>(value);
        Assert.Equal("method-app", identity.AgenticAppId);
        Assert.Equal("method-user", identity.AgenticUserId);
    }

    [Fact]
    public void FromChannelAccount_PreservesBotId()
    {
        TeamsChannelAccount? account = TeamsChannelAccount.FromChannelAccount(new ChannelAccount
        {
            Id = "28:channel-id",
            BotId = "bot-app-id",
        });

        Assert.NotNull(account);
        Assert.Equal("bot-app-id", account.BotId);
    }

    private static UserTokenClient CreateUserTokenClient(HttpMessageHandler handler)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UserTokenApiEndpoint"] = "https://token.test"
            })
            .Build();

        return new UserTokenClient(new HttpClient(handler), configuration, NullLogger<UserTokenClient>.Instance);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"connectionName":"connection","token":"token"}""", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
