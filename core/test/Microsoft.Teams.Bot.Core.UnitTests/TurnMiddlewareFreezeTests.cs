// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core.Schema;
using Moq;

namespace Microsoft.Teams.Bot.Core.UnitTests;

/// <summary>
/// Verifies that TurnMiddleware.Freeze() prevents post-startup middleware registration (A-020).
/// </summary>
public class TurnMiddlewareFreezeTests
{
    private static BotApplication CreateBotApplication()
    {
        Mock<HttpClient> mockHttp = new();
        ConversationClient cc = new(mockHttp.Object);
        Mock<IConfiguration> mockCfg = new();
        UserTokenClient utc = new(mockHttp.Object, mockCfg.Object, NullLogger<UserTokenClient>.Instance);
        return new BotApplication(cc, utc, NullLogger<BotApplication>.Instance);
    }

    [Fact]
    public void Freeze_ThenUseMiddleware_ThrowsInvalidOperationException()
    {
        // Arrange
        BotApplication botApp = CreateBotApplication();
        botApp.UseMiddleware(new NoopMiddleware()); // allowed pre-freeze

        // Act – freeze (simulates IHostedService.StartAsync)
        botApp.FreezeMiddleware();

        // Assert – further registration must throw
        Assert.Throws<InvalidOperationException>(() =>
            botApp.UseMiddleware(new NoopMiddleware()));
    }

    [Fact]
    public async Task Freeze_MiddlewareStillExecutes_AfterFreeze()
    {
        // Arrange
        BotApplication botApp = CreateBotApplication();
        bool middlewareCalled = false;

        botApp.UseMiddleware(new LambdaMiddleware(async (_, _, next, ct) =>
        {
            middlewareCalled = true;
            await next(ct);
        }));

        botApp.FreezeMiddleware();

        // Act – manually invoke the pipeline
        botApp.OnActivity = (_, _) => Task.CompletedTask;

        DefaultHttpContext ctx = new();
        ctx.Request.Body = new System.IO.MemoryStream(
            System.Text.Encoding.UTF8.GetBytes("{\"type\":\"message\"}"));

        await botApp.ProcessAsync(ctx, CancellationToken.None);

        // Assert
        Assert.True(middlewareCalled);
    }

    private sealed class NoopMiddleware : ITurnMiddleware
    {
        public Task OnTurnAsync(BotApplication app, CoreActivity activity, NextTurn next, CancellationToken ct)
            => next(ct);
    }

    private sealed class LambdaMiddleware(Func<BotApplication, CoreActivity, NextTurn, CancellationToken, Task> impl) : ITurnMiddleware
    {
        public Task OnTurnAsync(BotApplication app, CoreActivity activity, NextTurn next, CancellationToken ct)
            => impl(app, activity, next, ct);
    }
}
