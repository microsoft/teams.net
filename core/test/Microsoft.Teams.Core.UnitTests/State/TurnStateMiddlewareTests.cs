// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Schema;
using Microsoft.Teams.Core.State;
using Moq;

namespace Microsoft.Teams.Core.UnitTests.State;

public class TurnStateMiddlewareTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly TurnStateOptions _options = new();
    private readonly TestBotApplication _bot = new();

    private TurnStateMiddleware CreateMiddleware() =>
        new(_cacheMock.Object, Options.Create(_options));

    private static CoreActivity CreateActivity(string? conversationId = "conv1", string? fromId = "user1")
    {
        CoreActivity activity = new()
        {
            Conversation = conversationId is null ? null : new Conversation(conversationId),
            From = fromId is null ? null : new ConversationAccount { Id = fromId },
        };
        return activity;
    }

    [Fact]
    public async Task StateIsAvailableDuringHandler()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();
        ITurnState? captured = null;

        await middleware.OnTurnAsync(_bot, activity, async ct =>
        {
            captured = _bot.TurnState;
            await Task.CompletedTask;
        });

        Assert.NotNull(captured);
    }

    [Fact]
    public async Task StateIsClearedAfterHandler()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, ct => Task.CompletedTask);

        Assert.Null(_bot.TurnState);
    }

    [Fact]
    public async Task StateIsClearedWhenHandlerThrows()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await middleware.OnTurnAsync(_bot, activity, ct => throw new InvalidOperationException("boom")));

        Assert.Null(_bot.TurnState);
    }

    [Fact]
    public async Task DirtyStateIsSavedToCache()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, async ct =>
        {
            _bot.TurnState!.Set("key", "value");
            await Task.CompletedTask;
        });

        _cacheMock.Verify(
            c => c.SetAsync("ts:conv1:user1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NonDirtyStateIsNotSaved()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, ct => Task.CompletedTask);

        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExistingCachedStateIsLoaded()
    {
        TurnState existing = new();
        existing.Set("greeting", "hello");
        byte[] bytes = existing.ToJsonBytes();

        _cacheMock
            .Setup(c => c.GetAsync("ts:conv1:user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();
        string? captured = null;

        await middleware.OnTurnAsync(_bot, activity, async ct =>
        {
            captured = _bot.TurnState!.Get<string>("greeting");
            await Task.CompletedTask;
        });

        Assert.Equal("hello", captured);
    }

    [Fact]
    public async Task NullConversationSkipsStateLoading()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity(conversationId: null);
        bool handlerCalled = false;

        await middleware.OnTurnAsync(_bot, activity, ct =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.Null(_bot.TurnState);
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NullFromSkipsStateLoading()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity(fromId: null);
        bool handlerCalled = false;

        await middleware.OnTurnAsync(_bot, activity, ct =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.Null(_bot.TurnState);
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SessionKeyFormatIsCorrect()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity("myConv", "myUser");

        await middleware.OnTurnAsync(_bot, activity, async ct =>
        {
            _bot.TurnState!.Set("x", 1);
            await Task.CompletedTask;
        });

        _cacheMock.Verify(
            c => c.SetAsync("ts:myConv:myUser", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DirtyStateIsSavedEvenWhenHandlerThrows()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await middleware.OnTurnAsync(_bot, activity, ct =>
            {
                _bot.TurnState!.Set("key", "value");
                throw new InvalidOperationException("boom");
            }));

        _cacheMock.Verify(
            c => c.SetAsync("ts:conv1:user1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EmptyConversationIdSkipsStateLoading()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity(conversationId: "");
        bool handlerCalled = false;

        await middleware.OnTurnAsync(_bot, activity, ct =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.Null(_bot.TurnState);
    }

    [Fact]
    public async Task EmptyFromIdSkipsStateLoading()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = new()
        {
            Conversation = new Conversation("conv1"),
            From = new ConversationAccount { Id = "" },
        };
        bool handlerCalled = false;

        await middleware.OnTurnAsync(_bot, activity, ct =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.Null(_bot.TurnState);
    }

    private sealed class TestBotApplication : BotApplication
    {
    }
}
