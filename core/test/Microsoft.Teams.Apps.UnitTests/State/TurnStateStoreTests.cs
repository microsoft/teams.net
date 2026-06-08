// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class TurnStateStoreTests
{
    private static CoreActivity Activity(string channelId = "msteams", string conversationId = "19:abc", string fromId = "user1")
        => new()
        {
            ChannelId = channelId,
            Conversation = new Conversation(conversationId),
            From = new ConversationAccount { Id = fromId },
        };

    // Mirrors the orchestration in TeamsBotApplication.OnActivity: load, run the handler, save on
    // success (skipped on throw), seal in finally.
    private static async Task RunTurnAsync(TurnStateStore store, CoreActivity activity, Func<TurnState, Task> handler)
    {
        TurnState ts = await store.LoadAsync(activity);
        try
        {
            await handler(ts);
            await store.SaveAsync(ts);
        }
        finally
        {
            ts.Complete();
        }
    }

    private static Task RunTurnAsync(TurnStateStore store, CoreActivity activity, Action<TurnState> handler)
        => RunTurnAsync(store, activity, ts => { handler(ts); return Task.CompletedTask; });

    [Fact]
    public async Task ConversationScope_PersistsAcrossTurns()
    {
        var store = new TurnStateStore(new FakeDistributedCache());
        var activity = Activity();

        await RunTurnAsync(store, activity, ts => ts.Conversation.Set("count", ts.Conversation.Get<int>("count") + 1));
        await RunTurnAsync(store, activity, ts =>
        {
            Assert.Equal(1, ts.Conversation.Get<int>("count"));
            ts.Conversation.Set("count", ts.Conversation.Get<int>("count") + 1);
        });
        await RunTurnAsync(store, activity, ts => Assert.Equal(2, ts.Conversation.Get<int>("count")));
    }

    [Fact]
    public async Task UserScope_FollowsUserAcrossConversations()
    {
        var store = new TurnStateStore(new FakeDistributedCache());

        await RunTurnAsync(store, Activity(conversationId: "convA"), ts => ts.User.Set("name", "Bob"));
        await RunTurnAsync(store, Activity(conversationId: "convB"), ts => Assert.Equal("Bob", ts.User.Get<string>("name")));
    }

    [Fact]
    public async Task InPlaceMutationOfReferenceType_IsPersisted()
    {
        // Byte-baseline change detection must catch a mutation made without a Set().
        var store = new TurnStateStore(new FakeDistributedCache());
        var activity = Activity();

        await RunTurnAsync(store, activity, ts => ts.Conversation.Set("items", new List<string> { "a" }));
        await RunTurnAsync(store, activity, ts =>
        {
            List<string> items = ts.Conversation.Get<List<string>>("items")!;
            items.Add("b"); // mutated in place, no Set()
        });
        await RunTurnAsync(store, activity, ts =>
            Assert.Equal(new[] { "a", "b" }, ts.Conversation.Get<List<string>>("items")));
    }

    [Fact]
    public async Task HandlerThrows_StateIsNotSaved()
    {
        var store = new TurnStateStore(new FakeDistributedCache());
        var activity = Activity();

        await RunTurnAsync(store, activity, ts => ts.Conversation.Set("x", 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => RunTurnAsync(store, activity, ts =>
        {
            ts.Conversation.Set("x", 999);
            throw new InvalidOperationException("boom");
        }));

        await RunTurnAsync(store, activity, ts => Assert.Equal(1, ts.Conversation.Get<int>("x")));
    }

    [Fact]
    public async Task ClearingPersistedScope_DeletesInsteadOfWritingEmpty()
    {
        var cache = new FakeDistributedCache();
        var store = new TurnStateStore(cache);
        var activity = Activity();

        await RunTurnAsync(store, activity, ts => ts.Conversation.Set("x", 1));
        Assert.Equal(1, cache.Writes);
        Assert.Equal(0, cache.Deletes);

        await RunTurnAsync(store, activity, ts => ts.Conversation.Clear());
        Assert.Equal(1, cache.Writes);  // no empty doc written
        Assert.Equal(1, cache.Deletes); // key removed instead

        await RunTurnAsync(store, activity, ts => Assert.Equal(0, ts.Conversation.Get<int>("x")));
    }

    [Fact]
    public async Task UnchangedEmptyScope_DoesNotDelete()
    {
        // A scope that was never populated must not issue a spurious delete every turn.
        var cache = new FakeDistributedCache();
        var store = new TurnStateStore(cache);

        await RunTurnAsync(store, Activity(), ts => ts.Conversation.Get<int>("x"));

        Assert.Equal(0, cache.Writes);
        Assert.Equal(0, cache.Deletes);
    }

    [Fact]
    public async Task ReadOnlyTurn_DoesNotWrite()
    {
        var cache = new FakeDistributedCache();
        var store = new TurnStateStore(cache);
        var activity = Activity();

        await RunTurnAsync(store, activity, ts => ts.Conversation.Get<int>("count"));

        Assert.Equal(0, cache.Writes);
    }

    [Fact]
    public async Task AccessingStateAfterTurn_Throws()
    {
        var store = new TurnStateStore(new FakeDistributedCache());
        TurnState? captured = null;

        await RunTurnAsync(store, Activity(), ts => captured = ts);

        Assert.True(captured!.IsCompleted);
        Assert.Throws<InvalidOperationException>(() => captured!.Conversation.Get<int>("x"));
        Assert.Throws<InvalidOperationException>(() => captured!.Conversation.Set("x", 1));
    }

    [Fact]
    public async Task PathApi_RoutesToScopes()
    {
        var store = new TurnStateStore(new FakeDistributedCache());
        var activity = Activity();

        await RunTurnAsync(store, activity, ts =>
        {
            ts.SetValue("conversation.k", 7);
            ts.SetValue("user.name", "Bob");
            Assert.Throws<ArgumentException>(() => ts.SetValue("bare", "v")); // bare paths must be scope-qualified
        });
        await RunTurnAsync(store, activity, ts =>
        {
            Assert.Equal(7, ts.GetValue<int>("conversation.k")); // persisted
            Assert.Equal("Bob", ts.GetValue<string>("user.name"));
        });
    }

    [Fact]
    public void DeriveKeys_DerivesChannelScopedKeys()
    {
        (string? conversationKey, string? userKey) = TurnState.DeriveKeys(Activity());
        Assert.Equal("msteams/conversations/19:abc", conversationKey);
        Assert.Equal("msteams/users/user1", userKey);
    }

    [Fact]
    public void DeriveKeys_ReturnsNullWhenPartsMissing()
    {
        (string? conversationKey, string? userKey) = TurnState.DeriveKeys(new CoreActivity { ChannelId = "msteams" });
        Assert.Null(conversationKey);
        Assert.Null(userKey);
    }

    /// <summary>
    /// In-memory <see cref="IDistributedCache"/> for tests, counting writes (<see cref="SetAsync"/>) and
    /// deletes (<see cref="RemoveAsync"/>) so change-detection / delete-on-empty behavior is assertable.
    /// </summary>
    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public int Writes { get; private set; }

        public int Deletes { get; private set; }

        public byte[]? Get(string key) => _store.TryGetValue(key, out byte[]? value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Writes++;
            _store[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key)
        {
            Deletes++;
            _store.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
