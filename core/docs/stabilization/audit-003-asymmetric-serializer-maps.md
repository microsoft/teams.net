# Audit Issue 003: Asymmetric Serializer / Deserializer Maps Causing Silent Data Loss

**Severity:** High  
**File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/TeamsActivityType.cs`  
**Lines:** 83–103  
**Category:** Data integrity / type dispatch

---

## Problem

`TeamsActivityType` defines two dictionaries for activity type dispatch:

```csharp
// Deserializer map — 8 entries (line 83)
internal static readonly Dictionary<string, Func<CoreActivity, TeamsActivity>> ActivityDeserializerMap = new()
{
    [Message]            = MessageActivity.FromActivity,
    [MessageReaction]    = MessageReactionActivity.FromActivity,
    [MessageUpdate]      = MessageUpdateActivity.FromActivity,
    [MessageDelete]      = MessageDeleteActivity.FromActivity,
    [ConversationUpdate] = ConversationUpdateActivity.FromActivity,
    [InstallationUpdate] = InstallUpdateActivity.FromActivity,
    [Invoke]             = InvokeActivity.FromActivity,
    [Event]              = EventActivity.FromActivity
};

// Serializer map — only 2 entries (line 99)
internal static readonly Dictionary<Type, Func<TeamsActivity, string>> ActivitySerializerMap = new()
{
    [typeof(MessageActivity)]   = a => a.ToJson(TeamsActivityJsonContext.Default.MessageActivity),
    [typeof(StreamingActivity)] = a => a.ToJson(TeamsActivityJsonContext.Default.StreamingActivity),
};
```

When `TeamsActivity.ToJson()` is called on an instance of `MessageReactionActivity`, `InvokeActivity`, `ConversationUpdateActivity`, `MessageUpdateActivity`, `MessageDeleteActivity`, `InstallUpdateActivity`, or `EventActivity`, the serializer map lookup **misses** and the code falls back to the base `TeamsActivity` serializer:

```csharp
// TeamsActivity.cs, lines 34-37
public override string ToJson()
    => TeamsActivityType.ActivitySerializerMap.TryGetValue(GetType(), out Func<TeamsActivity, string>? serializer)
        ? serializer(this)
        : ToJson(TeamsActivityJsonContext.Default.TeamsActivity);  // Fallback — loses subtype fields!
```

This means **6 out of 8 deserializable activity types lose their subtype-specific fields** when serialized back to JSON. This affects any code path that round-trips an activity (e.g., sending a reply, logging, or passing an activity through the compat layer).

---

## Root Cause

The serializer map was not kept in sync with the deserializer map when new activity types were added. There is no compile-time or runtime enforcement of symmetry between the two dictionaries.

---

## Suggested Fix Plan

### Step 1 — Add missing serializer entries

For each activity type present in `ActivityDeserializerMap` but absent from `ActivitySerializerMap`, add a corresponding entry. Each entry needs a registered `JsonTypeInfo<T>` in `TeamsActivityJsonContext`.

Add the following entries to `ActivitySerializerMap`:

```csharp
[typeof(MessageReactionActivity)]    = a => a.ToJson(TeamsActivityJsonContext.Default.MessageReactionActivity),
[typeof(MessageUpdateActivity)]      = a => a.ToJson(TeamsActivityJsonContext.Default.MessageUpdateActivity),
[typeof(MessageDeleteActivity)]      = a => a.ToJson(TeamsActivityJsonContext.Default.MessageDeleteActivity),
[typeof(ConversationUpdateActivity)] = a => a.ToJson(TeamsActivityJsonContext.Default.ConversationUpdateActivity),
[typeof(InstallUpdateActivity)]      = a => a.ToJson(TeamsActivityJsonContext.Default.InstallUpdateActivity),
[typeof(InvokeActivity)]             = a => a.ToJson(TeamsActivityJsonContext.Default.InvokeActivity),
[typeof(EventActivity)]              = a => a.ToJson(TeamsActivityJsonContext.Default.EventActivity),
```

### Step 2 — Register types in `TeamsActivityJsonContext`

For each new entry above, ensure the corresponding type is registered in `TeamsActivityJsonContext` with `[JsonSerializable(typeof(XxxActivity))]`. Check `TeamsActivityJsonContext.cs` for what is already registered and add what is missing.

### Step 3 — Add a symmetry assertion test

Add a unit test that asserts `ActivitySerializerMap.Keys` covers every value type returned by `ActivityDeserializerMap.Values` (i.e., the return type of each factory). Run this test in CI to prevent future drift:

```csharp
[Fact]
public void SerializerMap_CoversAllDeserializerOutputTypes()
{
    var deserializedTypes = TeamsActivityType.ActivityDeserializerMap.Values
        .Select(factory => factory(new CoreActivity { Type = "message" }).GetType())
        .Distinct();

    foreach (var type in deserializedTypes)
        Assert.True(TeamsActivityType.ActivitySerializerMap.ContainsKey(type),
            $"No serializer registered for {type.Name}");
}
```

### Step 4 — Verify round-trip fidelity

Add integration tests that:
1. Deserialize a JSON payload for each activity type.
2. Call `ToJson()` on the result.
3. Assert the output JSON contains the type-specific fields (e.g., `replyToId` for `MessageReactionActivity`, `value` for `InvokeActivity`).

---

## Acceptance Criteria

- `ActivitySerializerMap` has an entry for every type that `ActivityDeserializerMap` can produce.
- Round-trip serialization preserves all subtype-specific fields for all 8 activity types.
- A new CI test fails if the two maps become asymmetric again.
