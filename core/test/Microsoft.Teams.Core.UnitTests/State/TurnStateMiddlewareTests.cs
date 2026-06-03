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

    // ── Load / Clear lifecycle ───────────────────────────────────────

    [Fact]
    public async Task BothStatesAvailableDuringHandler()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        bool convAvailable = false;
        bool userAvailable = false;
        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            convAvailable = _bot.State?.ConversationState is not null;
            userAvailable = _bot.State?.UserState is not null;
            return Task.CompletedTask;
        });

        Assert.True(convAvailable);
        Assert.True(userAvailable);
    }

    [Fact]
    public async Task StateClearedAfterHandler()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, (ct) => Task.CompletedTask);

        Assert.Null(_bot.State);
    }

    [Fact]
    public async Task StateClearedWhenHandlerThrows()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.OnTurnAsync(_bot, activity, (ct) =>
                throw new InvalidOperationException("boom")));

        Assert.Null(_bot.State);
    }

    // ── Save behavior ────────────────────────────────────────────────

    [Fact]
    public async Task DirtyConversationStateIsSaved()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            _bot.State!.ConversationState.Set("key", "value");
            return Task.CompletedTask;
        });

        _cacheMock.Verify(c => c.SetAsync(
            "ts:conv:conv1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DirtyUserStateIsSaved()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            _bot.State!.UserState!.Set("key", "value");
            return Task.CompletedTask;
        });

        _cacheMock.Verify(c => c.SetAsync(
            "ts:user:conv1:user1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NonDirtyStateIsNotSaved()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(_bot, activity, (ct) => Task.CompletedTask);

        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DirtyStateIsSavedEvenWhenHandlerThrows()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.OnTurnAsync(_bot, activity, (ct) =>
            {
                _bot.State!.ConversationState.Set("key", "value");
                throw new InvalidOperationException("boom");
            }));

        _cacheMock.Verify(c => c.SetAsync(
            "ts:conv:conv1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Cache round-trip ─────────────────────────────────────────────

    [Fact]
    public async Task ExistingConversationStateIsLoaded()
    {
        TurnState existing = new();
        existing.Set("shared", "hello");

        _cacheMock.Setup(c => c.GetAsync("ts:conv:conv1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToJsonBytes());

        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();
        string? loaded = null;

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            loaded = _bot.State!.ConversationState.Get<string>("shared");
            return Task.CompletedTask;
        });

        Assert.Equal("hello", loaded);
    }

    [Fact]
    public async Task ExistingUserStateIsLoaded()
    {
        TurnState existing = new();
        existing.Set("private", "world");

        _cacheMock.Setup(c => c.GetAsync("ts:user:conv1:user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToJsonBytes());

        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity();
        string? loaded = null;

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            loaded = _bot.State!.UserState!.Get<string>("private");
            return Task.CompletedTask;
        });

        Assert.Equal("world", loaded);
    }

    // ── Skip behavior ────────────────────────────────────────────────

    [Fact]
    public async Task NullConversation_SkipsBothStates()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity(conversationId: null);
        bool handlerCalled = false;

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.Null(_bot.State);
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmptyConversationId_SkipsBothStates()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity(conversationId: "");
        bool handlerCalled = false;

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.Null(_bot.State);
    }

    [Fact]
    public async Task NullFrom_LoadsConversationState_SkipsUserState()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity(fromId: null);

        bool convAvailable = false;
        bool userNull = false;
        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            convAvailable = _bot.State?.ConversationState is not null;
            userNull = _bot.State?.UserState is null;
            return Task.CompletedTask;
        });

        Assert.True(convAvailable);
        Assert.True(userNull);
    }

    [Fact]
    public async Task EmptyFromId_LoadsConversationState_SkipsUserState()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = new()
        {
            Conversation = new Conversation("conv1"),
            From = new ConversationAccount { Id = "" },
        };

        bool convAvailable = false;
        bool userNull = false;
        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            convAvailable = _bot.State?.ConversationState is not null;
            userNull = _bot.State?.UserState is null;
            return Task.CompletedTask;
        });

        Assert.True(convAvailable);
        Assert.True(userNull);
    }

    // ── Key format ───────────────────────────────────────────────────

    [Fact]
    public async Task KeysUseCorrectFormat()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        CoreActivity activity = CreateActivity("myConv", "myUser");

        await middleware.OnTurnAsync(_bot, activity, (ct) =>
        {
            _bot.State!.ConversationState.Set("x", 1);
            _bot.State!.UserState!.Set("y", 2);
            return Task.CompletedTask;
        });

        _cacheMock.Verify(c => c.SetAsync(
            "ts:conv:myConv",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _cacheMock.Verify(c => c.SetAsync(
            "ts:user:myConv:myUser",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class TestBotApplication : BotApplication
    {
    }
}
