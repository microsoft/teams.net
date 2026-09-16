// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

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
    public void Clear_WithValues_EmptiesStateAndMarksDirty()
    {
        TurnState state = new();
        state.Set("key", "value");

        state.Clear();

        Assert.True(state.IsDirty);
        Assert.False(state.ContainsKey("key"));
        Assert.Null(state.Get<string>("key"));
    }

    [Fact]
    public void Clear_EmptyState_DoesNotMarkDirty()
    {
        TurnState state = new();

        state.Clear();

        Assert.False(state.IsDirty);
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
    public void Clear_AfterTypedSet_EmptiesAllTypedValues()
    {
        TurnState state = new();
        state.Set(new FakeState { Name = "custom" });
        state.Set(new OtherState { Count = 7 });

        state.Clear();

        Assert.False(state.Has<FakeState>());
        Assert.False(state.Has<OtherState>());
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

    [Fact]
    public void Complete_AfterTurn_ThrowsOnFurtherAccess()
    {
        TurnState state = new();
        state.Set("name", "Alice");

        Assert.False(state.IsCompleted);
        state.Complete();
        Assert.True(state.IsCompleted);

        Assert.Throws<InvalidOperationException>(() => state.Get<string>("name"));
        Assert.Throws<InvalidOperationException>(() => state.Set("other", "value"));
        Assert.Throws<InvalidOperationException>(() => state.ContainsKey("name"));
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

    [Fact]
    public void GetTyped_AfterRoundTrip_MarksDirty()
    {
        // When a typed Get<T>() deserializes a JsonElement, it should mark dirty
        // so the deserialized object is persisted on the next save.
        TurnState original = new();
        original.Set(new FakeState { Name = "persisted" });
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);
        Assert.False(loaded.IsDirty);

        loaded.Get<FakeState>();

        Assert.True(loaded.IsDirty);
    }

    // ── TryGet ─────────────────────────────────────────────────────────

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        TurnState state = new();
        state.Set("name", "Alice");

        bool found = state.TryGet<string>("name", out string? value);

        Assert.True(found);
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        TurnState state = new();

        bool found = state.TryGet<string>("missing", out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_WrongType_ReturnsFalse()
    {
        TurnState state = new();
        state.Set("count", 42);

        bool found = state.TryGet<string>("count", out string? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_JsonElement_DeserializesAndReturnsTrue()
    {
        TurnState original = new();
        original.Set("count", 42);
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);
        bool found = loaded.TryGet<int>("count", out int value);

        Assert.True(found);
        Assert.Equal(42, value);
    }

    // ── JSON exception handling ────────────────────────────────────────

    [Fact]
    public void Get_JsonElement_IncompatibleType_ReturnsDefault()
    {
        // Store a string, round-trip through JSON, then request as int.
        TurnState original = new();
        original.Set("name", "Alice");
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        // "Alice" cannot be deserialized as int — should return default, not throw.
        int result = loaded.Get<int>("name");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Get_JsonElement_IncompatibleType_ReturnsNullForReferenceType()
    {
        // Store a number, round-trip, then request as a complex object.
        TurnState original = new();
        original.Set("count", 42);
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        // A JSON number cannot deserialize to FakeState — should return null, not throw.
        FakeState? result = loaded.Get<FakeState>("count");

        Assert.Null(result);
    }

    [Fact]
    public void TryGet_JsonElement_IncompatibleType_ReturnsFalse()
    {
        // Store a string, round-trip through JSON, then TryGet as int.
        TurnState original = new();
        original.Set("name", "Alice");
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        bool found = loaded.TryGet<int>("name", out int value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGet_JsonElement_IncompatibleComplexType_ReturnsFalse()
    {
        // Store a boolean, round-trip, then TryGet as a complex object.
        TurnState original = new();
        original.Set("flag", true);
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        bool found = loaded.TryGet<FakeState>("flag", out FakeState? value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void GetTyped_JsonElement_IncompatibleType_CreatesNewInstance()
    {
        // Simulate a typed key holding an incompatible JsonElement (e.g., cache corruption or type change).
        // Manually build a dictionary with the typed key mapped to a JsonElement number.
        using var doc = System.Text.Json.JsonDocument.Parse("42");
        var data = new Dictionary<string, object?>
        {
            [$"${typeof(FakeState).FullName}"] = doc.RootElement.Clone()
        };
        TurnState state = TurnState.FromDictionary(data);

        // A number JsonElement cannot deserialize to FakeState — should create new instance.
        FakeState result = state.Get<FakeState>();

        Assert.NotNull(result);
        Assert.Equal("default", result.Name);
    }

    [Fact]
    public void GetTyped_JsonElement_IncompatibleType_MarksDirty()
    {
        // When the typed Get<T>() falls back to creating a new instance, it should mark state dirty.
        using var doc = System.Text.Json.JsonDocument.Parse("\"not an object\"");
        var data = new Dictionary<string, object?>
        {
            [$"${typeof(FakeState).FullName}"] = doc.RootElement.Clone()
        };
        TurnState state = TurnState.FromDictionary(data);

        Assert.False(state.IsDirty);

        state.Get<FakeState>();

        Assert.True(state.IsDirty);
    }

    [Fact]
    public void FromJsonBytes_CorruptedJson_ReturnsEmptyState()
    {
        // Invalid JSON bytes should be treated as a cache miss.
        byte[] garbage = "{{not valid json!!"u8.ToArray();

        TurnState state = TurnState.FromJsonBytes(garbage);

        Assert.False(state.IsDirty);
        Assert.False(state.ContainsKey("anything"));
    }

    [Fact]
    public void FromJsonBytes_JsonArrayInsteadOfObject_ReturnsEmptyState()
    {
        // A JSON array is valid JSON but not a valid state dictionary.
        byte[] arrayJson = "[1, 2, 3]"u8.ToArray();

        TurnState state = TurnState.FromJsonBytes(arrayJson);

        Assert.False(state.IsDirty);
        Assert.False(state.ContainsKey("anything"));
    }

    [Fact]
    public void Get_JsonElement_ObjectAsWrongComplexType_ReturnsDefault()
    {
        // Store a FakeState object, round-trip, then request the same key as OtherState.
        TurnState original = new();
        original.Set("data", new FakeState { Name = "test" });
        byte[] bytes = original.ToJsonBytes();

        TurnState loaded = TurnState.FromJsonBytes(bytes);

        // The JsonElement is a {"Name":"test"} object; OtherState has different shape but should still deserialize.
        OtherState? result = loaded.Get<OtherState>("data");

        // OtherState can be deserialized (Count defaults to 0) — this is valid JSON-to-object conversion.
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }
}
