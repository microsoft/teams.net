// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class StateMiddlewareTests
{
    private static CoreActivity Activity(string channelId = "msteams", string conversationId = "19:abc", string fromId = "user1")
        => new()
        {
            ChannelId = channelId,
            Conversation = new Conversation(conversationId),
            From = new ConversationAccount { Id = fromId },
        };

    private static Task RunTurnAsync(StateMiddleware middleware, CoreActivity activity, Func<TurnState, Task> handler)
        => middleware.OnTurnAsync(null!, activity, _ => handler(TurnState.Current!));

    private static Task RunTurnAsync(StateMiddleware middleware, CoreActivity activity, Action<TurnState> handler)
        => middleware.OnTurnAsync(null!, activity, _ => { handler(TurnState.Current!); return Task.CompletedTask; });

    [Fact]
    public async Task ConversationScope_PersistsAcrossTurns()
    {
        var middleware = new StateMiddleware(new MemoryStorage());
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts => ts.Conversation.Set("count", ts.Conversation.Get<int>("count") + 1));
        await RunTurnAsync(middleware, activity, ts =>
        {
            Assert.Equal(1, ts.Conversation.Get<int>("count"));
            ts.Conversation.Set("count", ts.Conversation.Get<int>("count") + 1);
        });
        await RunTurnAsync(middleware, activity, ts => Assert.Equal(2, ts.Conversation.Get<int>("count")));
    }

    [Fact]
    public async Task UserScope_FollowsUserAcrossConversations()
    {
        var middleware = new StateMiddleware(new MemoryStorage());

        await RunTurnAsync(middleware, Activity(conversationId: "convA"), ts => ts.User.Set("name", "Bob"));
        await RunTurnAsync(middleware, Activity(conversationId: "convB"), ts => Assert.Equal("Bob", ts.User.Get<string>("name")));
    }

    [Fact]
    public async Task TempScope_IsNotPersisted()
    {
        var middleware = new StateMiddleware(new MemoryStorage());
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts => ts.Temp.Set("t", 5));
        await RunTurnAsync(middleware, activity, ts => Assert.Equal(0, ts.Temp.Get<int>("t")));
    }

    [Fact]
    public async Task InPlaceMutationOfReferenceType_IsPersisted()
    {
        // Hash-based change detection must catch a mutation made without a Set().
        var middleware = new StateMiddleware(new MemoryStorage());
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts => ts.Conversation.Set("items", new List<string> { "a" }));
        await RunTurnAsync(middleware, activity, ts =>
        {
            List<string> items = ts.Conversation.Get<List<string>>("items")!;
            items.Add("b"); // mutated in place, no Set()
        });
        await RunTurnAsync(middleware, activity, ts =>
            Assert.Equal(new[] { "a", "b" }, ts.Conversation.Get<List<string>>("items")));
    }

    [Fact]
    public async Task HandlerThrows_StateIsNotSaved()
    {
        var middleware = new StateMiddleware(new MemoryStorage());
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts => ts.Conversation.Set("x", 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.OnTurnAsync(null!, activity, _ =>
            {
                TurnState.Current!.Conversation.Set("x", 999);
                throw new InvalidOperationException("boom");
            }));

        await RunTurnAsync(middleware, activity, ts => Assert.Equal(1, ts.Conversation.Get<int>("x")));
    }

    [Fact]
    public async Task ClearingPersistedScope_DeletesInsteadOfWritingEmpty()
    {
        var storage = new CountingStorage();
        var middleware = new StateMiddleware(storage);
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts => ts.Conversation.Set("x", 1));
        Assert.Equal(1, storage.Writes);
        Assert.Equal(0, storage.Deletes);

        await RunTurnAsync(middleware, activity, ts => ts.Conversation.Clear());
        Assert.Equal(1, storage.Writes);  // no empty doc written
        Assert.Equal(1, storage.Deletes); // key removed instead

        await RunTurnAsync(middleware, activity, ts => Assert.Equal(0, ts.Conversation.Get<int>("x")));
    }

    [Fact]
    public async Task UnchangedEmptyScope_DoesNotDelete()
    {
        // A scope that was never populated must not issue a spurious delete every turn.
        var storage = new CountingStorage();
        var middleware = new StateMiddleware(storage);

        await RunTurnAsync(middleware, Activity(), ts => ts.Conversation.Get<int>("x"));

        Assert.Equal(0, storage.Writes);
        Assert.Equal(0, storage.Deletes);
    }

    [Fact]
    public async Task ReadOnlyTurn_DoesNotWrite()
    {
        var storage = new CountingStorage();
        var middleware = new StateMiddleware(storage);
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts => ts.Conversation.Get<int>("count"));

        Assert.Equal(0, storage.Writes);
    }

    [Fact]
    public async Task AccessingStateAfterTurn_Throws()
    {
        var middleware = new StateMiddleware(new MemoryStorage());
        TurnState? captured = null;

        await RunTurnAsync(middleware, Activity(), ts => captured = ts);

        Assert.True(captured!.IsCompleted);
        Assert.Throws<InvalidOperationException>(() => captured!.Conversation.Get<int>("x"));
        Assert.Throws<InvalidOperationException>(() => captured!.Conversation.Set("x", 1));
    }

    [Fact]
    public async Task CurrentIsClearedAfterTurn()
    {
        var middleware = new StateMiddleware(new MemoryStorage());

        await RunTurnAsync(middleware, Activity(), ts => Assert.NotNull(ts));

        Assert.Null(TurnState.Current);
    }

    [Fact]
    public async Task PathApi_RoutesToScopes()
    {
        var middleware = new StateMiddleware(new MemoryStorage());
        var activity = Activity();

        await RunTurnAsync(middleware, activity, ts =>
        {
            ts.SetValue("conversation.k", 7);
            ts.SetValue("bare", "v"); // bare path defaults to temp
            Assert.Equal("v", ts.GetValue<string>("bare"));
            Assert.Equal("v", ts.GetValue<string>("temp.bare"));
        });
        await RunTurnAsync(middleware, activity, ts =>
        {
            Assert.Equal(7, ts.GetValue<int>("conversation.k")); // persisted
            Assert.Null(ts.GetValue<string>("temp.bare"));       // temp not persisted
        });
    }

    [Fact]
    public async Task Middleware_DoesNotDrainActivity_SoTextSurvivesForRouting()
    {
        // Regression: the middleware must NOT convert the activity to a TeamsActivity. That conversion
        // Extracts (removes) text/attachments/entities from the shared CoreActivity, so routing — which
        // builds its own MessageActivity afterward — would see null text and match no route.
        const string json = """
            { "type": "message", "channelId": "msteams", "text": "help",
              "conversation": { "id": "19:abc" }, "from": { "id": "user1" } }
            """;
        CoreActivity activity = CoreActivity.FromJsonString(json);
        var middleware = new StateMiddleware(new MemoryStorage());

        await middleware.OnTurnAsync(null!, activity, _ => Task.CompletedTask);

        // The text is still on the activity, so routing's later conversion can read it.
        Assert.Equal("help", MessageActivity.FromActivity(activity).Text);
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

    private sealed class CountingStorage : IStorage
    {
        private readonly MemoryStorage _inner = new();

        public int Writes { get; private set; }

        public int Deletes { get; private set; }

        public Task<IReadOnlyDictionary<string, StoreItem>> ReadAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(keys, cancellationToken);

        public Task WriteAsync(IReadOnlyDictionary<string, StoreItem> changes, CancellationToken cancellationToken = default)
        {
            Writes++;
            return _inner.WriteAsync(changes, cancellationToken);
        }

        public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
        {
            Deletes++;
            return _inner.DeleteAsync(keys, cancellationToken);
        }
    }
}
