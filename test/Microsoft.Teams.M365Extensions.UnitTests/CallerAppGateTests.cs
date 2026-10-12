// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.M365Extensions.UnitTests;

public class CallerAppGateTests
{
    private const string Agent365PlatformAppId = "5a807f24-c9de-44ee-a3a7-329e88a00ffc";
    private const string OtherAppId = "22222222-2222-2222-2222-222222222222";
    private const string EntraIssuer = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000001/v2.0";

    [Fact]
    public async Task OnTurnAsync_ForeignCallerApp_ReachesNoHandlerAndDoesNotContinue()
    {
        bool handlerInvoked = false;
        bool nextInvoked = false;
        TeamsSdkMiddleware middleware = CreateMiddleware(() => handlerInvoked = true, out _);

        await middleware.OnTurnAsync(
            CreateTurnContext(CreateEntraIdentity("azp", OtherAppId)),
            _ => { nextInvoked = true; return Task.CompletedTask; },
            CancellationToken.None);

        Assert.False(handlerInvoked);
        Assert.False(nextInvoked);
    }

    [Fact]
    public async Task OnTurnAsync_Agent365CallerApp_ReachesHandler()
    {
        bool handlerInvoked = false;
        bool nextInvoked = false;
        TeamsSdkMiddleware middleware = CreateMiddleware(() => handlerInvoked = true, out _);

        await middleware.OnTurnAsync(
            CreateTurnContext(CreateEntraIdentity("azp", Agent365PlatformAppId)),
            _ => { nextInvoked = true; return Task.CompletedTask; },
            CancellationToken.None);

        Assert.True(handlerInvoked);
        Assert.False(nextInvoked);
    }

    private static TeamsSdkMiddleware CreateMiddleware(Action onMessage, out TeamsBotApplication app)
    {
        app = CreateApp();
        app.OnMessage((_, _) =>
        {
            onMessage();
            return Task.CompletedTask;
        });

        return new TeamsSdkMiddleware(
            app,
            NullLogger<TeamsSdkMiddleware>.Instance,
            new HttpContextAccessor(),
            new ServiceCollection().BuildServiceProvider());
    }

    private static TeamsBotApplication CreateApp()
    {
        Mock<UserTokenClient> userTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);
        Mock<ConversationClient> conversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);
        ApiClient apiClient = new(new HttpClient(), conversationClient.Object, userTokenClient.Object);

        return new TeamsBotApplication(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
    }

    private static ClaimsIdentity CreateEntraIdentity(string callerClaimType, string callerAppId) =>
        new([new Claim("iss", EntraIssuer), new Claim(callerClaimType, callerAppId)]);

    private static ITurnContext CreateTurnContext(ClaimsIdentity identity)
    {
        var activity = new Activity
        {
            Type = ActivityTypes.Message,
            Id = "activity-1",
            ChannelId = "msteams",
            ServiceUrl = "https://smba.trafficmanager.net/teams/",
            Conversation = new ConversationAccount { Id = "19:conversation" },
            From = new ChannelAccount { Id = "29:from" },
            Text = "hello",
        };

        var turnContext = new Mock<ITurnContext>();
        turnContext.SetupGet(c => c.Activity).Returns(activity);
        turnContext.SetupGet(c => c.Identity).Returns(identity);
        return turnContext.Object;
    }
}
