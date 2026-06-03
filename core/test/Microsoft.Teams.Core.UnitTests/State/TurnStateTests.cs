// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.State;

namespace Microsoft.Teams.Core.UnitTests.State;

public class TurnStateTests
{
    // ── Key-value access ──────────────────────────────────────────────

    [Fact]
    public void Get_MissingKey_ReturnsDefault()
    {
        TurnState state = new();

        string? result = state.Get<string>("missing");

        Assert.Null(result);
    }

    [Fact]
    public void Get_MissingKeyValueType_ReturnsDefault()
    {
        TurnState state = new();

        int result = state.Get<int>("missing");

        Assert.Equal(0, result);
    }

    [Fact]
    public void SetAndGet_ReturnsStoredValue()
    {
        TurnState state = new();

        state.Set("key", "hello");

        Assert.Equal("hello", state.Get<string>("key"));
    }

    [Fact]
    public void Set_Overwrite_ReturnsNewValue()
    {
        TurnState state = new();

        state.Set("key", "first");
        state.Set("key", "second");

        Assert.Equal("second", state.Get<string>("key"));
    }

    [Fact]
    public void Remove_ExistingKey_RemovesIt()
    {
        TurnState state = new();
        state.Set("key", "value");

        state.Remove("key");

        Assert.False(state.ContainsKey("key"));
    }

    [Fact]
    public void Remove_MissingKey_DoesNotThrow()
    {
        TurnState state = new();

        state.Remove("missing");
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        TurnState state = new();
        state.Set("key", 42);

        Assert.True(state.ContainsKey("key"));
    }

    [Fact]
    public void ContainsKey_MissingKey_ReturnsFalse()
    {
        TurnState state = new();

        Assert.False(state.ContainsKey("nope"));
    }

    // ── Typed object access ───────────────────────────────────────────

    private class FakeState
    {
        public string Name { get; set; } = "default";
    }

    private class OtherState
    {
        public int Count { get; set; }
    }

    [Fact]
    public void GetTyped_NotPresent_CreatesNewInstance()
    {
        TurnState state = new();

        FakeState result = state.Get<FakeState>();

        Assert.NotNull(result);
        Assert.Equal("default", result.Name);
    }

    [Fact]
    public void SetTypedAndGetTyped_ReturnsSameInstance()
    {
        TurnState state = new();
        FakeState original = new() { Name = "custom" };

        state.Set(original);
        FakeState result = state.Get<FakeState>();

        Assert.Same(original, result);
    }

    [Fact]
    public void HasTyped_AfterSet_ReturnsTrue()
    {
        TurnState state = new();
        state.Set(new FakeState());

        Assert.True(state.Has<FakeState>());
    }

    [Fact]
    public void HasTyped_NotPresent_ReturnsFalse()
    {
        TurnState state = new();

        Assert.False(state.Has<FakeState>());
    }

    [Fact]
    public void RemoveTyped_AfterSet_RemovesIt()
    {
        TurnState state = new();
        state.Set(new FakeState());

        state.Remove<FakeState>();

        Assert.False(state.Has<FakeState>());
    }

    [Fact]
    public void TypedAccess_MultipleTypes_AreIndependent()
    {
        TurnState state = new();
        state.Set(new FakeState { Name = "a" });
        state.Set(new OtherState { Count = 7 });

        Assert.Equal("a", state.Get<FakeState>().Name);
        Assert.Equal(7, state.Get<OtherState>().Count);
    }

    // ── Dirty tracking ────────────────────────────────────────────────

    [Fact]
    public void IsDirty_NewInstance_ReturnsFalse()
    {
        TurnState state = new();

        Assert.False(state.IsDirty);
    }

    [Fact]
    public void IsDirty_AfterSet_ReturnsTrue()
    {
        TurnState state = new();

        state.Set("key", "value");

        Assert.True(state.IsDirty);
    }

    [Fact]
    public void IsDirty_AfterRemoveExisting_ReturnsTrue()
    {
        TurnState state = TurnState.FromDictionary(new Dictionary<string, object?> { ["key"] = "value" });

        state.Remove("key");

        Assert.True(state.IsDirty);
    }

    [Fact]
    public void IsDirty_AfterRemoveMissing_ReturnsFalse()
    {
        TurnState state = new();

        state.Remove("missing");

        Assert.False(state.IsDirty);
    }

    [Fact]
    public void IsDirty_AfterTypedSet_ReturnsTrue()
    {
        TurnState state = new();

        state.Set(new FakeState());

        Assert.True(state.IsDirty);
    }

    [Fact]
    public void IsDirty_FromJsonBytes_ReturnsFalse()
    {
        TurnState original = new();
        original.Set("key", "value");
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        Assert.False(loaded.IsDirty);
    }

    [Fact]
    public void IsDirty_FromDictionary_ReturnsFalse()
    {
        TurnState state = TurnState.FromDictionary(new Dictionary<string, object?> { ["x"] = 1 });

        Assert.False(state.IsDirty);
    }

    // ── Serialization ─────────────────────────────────────────────────

    [Fact]
    public void ToJsonBytesAndFromJsonBytes_RoundTrips()
    {
        TurnState original = new();
        original.Set("name", "Alice");
        original.Set("age", 30);

        byte[] bytes = original.ToJsonBytes();
        TurnState loaded = TurnState.FromJsonBytes(bytes);

        Assert.Equal("Alice", loaded.Get<string>("name"));
        Assert.Equal(30, loaded.Get<int>("age"));
    }

    [Fact]
    public void FromJsonBytes_EmptyArray_ReturnsEmptyState()
    {
        TurnState state = TurnState.FromJsonBytes([]);

        Assert.False(state.IsDirty);
        Assert.False(state.ContainsKey("anything"));
    }

    [Fact]
    public void FromJsonBytes_Null_ReturnsEmptyState()
    {
        TurnState state = TurnState.FromJsonBytes(null);

        Assert.False(state.IsDirty);
        Assert.False(state.ContainsKey("anything"));
    }

    [Fact]
    public void Get_JsonElement_DeserializesToRequestedType()
    {
        // Simulate what happens when state is loaded from JSON:
        // values come back as JsonElement, not the original type.
        TurnState original = new();
        original.Set("count", 42);
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        // The internal value is a JsonElement; Get<int> should convert it.
        Assert.Equal(42, loaded.Get<int>("count"));
    }

    [Fact]
    public void GetTyped_AfterRoundTrip_DeserializesFromJsonElement()
    {
        // Typed objects stored as $TypeName keys survive round-trip.
        TurnState original = new();
        original.Set(new FakeState { Name = "persisted" });
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        FakeState result = loaded.Get<FakeState>();
        Assert.Equal("persisted", result.Name);
    }
}
