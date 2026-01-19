// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

public class CompatMiddlewareTests
{
    [Fact]
    public void UseCompatMiddleware_AddsMiddlewareToChain()
    {
        // Arrange
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);
        IConfiguration config = new ConfigurationBuilder().Build();
        UserTokenClient userTokenClient = new(httpClient, config, NullLogger<UserTokenClient>.Instance);
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, userTokenClient, config, logger);

        TestBfMiddleware bfMiddleware = new();

        // Act
        BotApplication result = botApp.UseCompatMiddleware(bfMiddleware);

        // Assert
        Assert.NotNull(result);
        Assert.Same(botApp, result);
    }

    [Fact]
    public void UseCompatMiddleware_ThrowsWhenAppIsNull()
    {
        // Arrange
        TestBfMiddleware bfMiddleware = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            CompatHostingExtensions.UseCompatMiddleware(null!, bfMiddleware));
    }

    [Fact]
    public void UseCompatMiddleware_ThrowsWhenMiddlewareIsNull()
    {
        // Arrange
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);
        IConfiguration config = new ConfigurationBuilder().Build();
        UserTokenClient userTokenClient = new(httpClient, config, NullLogger<UserTokenClient>.Instance);
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, userTokenClient, config, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            botApp.UseCompatMiddleware(null!));
    }

    // Test Bot Framework v4 middleware
    private class TestBfMiddleware : IMiddleware
    {
        public Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            return next(cancellationToken);
        }
    }
}

