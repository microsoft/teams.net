# Audit Issue 011: `ActivitySerializerMap` Missing `TeamsActivity` Base Type Registration

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/TeamsActivityType.cs`  
**Lines:** 34–37 (`TeamsActivity.ToJson`), 99–103 (`ActivitySerializerMap`)  
**Category:** Type dispatch / serialization correctness

---

## Note

> This issue is related to but distinct from **Audit Issue 003** (asymmetric maps). Issue 003 focuses on the missing entries for the 6 activity types that _can be deserialized_ but _cannot be serialized_ with their correct type info. This issue focuses on the **fallback path** itself and what happens when `ToJson()` is called on a `TeamsActivity` that is not in the map.

---

## Problem

`TeamsActivity.ToJson()` has the following fallback:

```csharp
// TeamsActivity.cs, lines 34-37
public override string ToJson()
    => TeamsActivityType.ActivitySerializerMap.TryGetValue(GetType(), out Func<TeamsActivity, string>? serializer)
        ? serializer(this)
        : ToJson(TeamsActivityJsonContext.Default.TeamsActivity);   // Fallback
```

The fallback serializes using `TeamsActivityJsonContext.Default.TeamsActivity`, which is the base `TeamsActivity` JSON type info. This has two problems:

### Problem A — Subtype fields are silently omitted

When `GetType()` returns a subtype not in the map (e.g., `MessageReactionActivity`), the base-type serializer is used. Fields declared on the subtype are not serialized because the `JsonTypeInfo<TeamsActivity>` does not know about them.

This is the same root issue as Issue 003 but is documented here for completeness: **the fallback path produces subtly incorrect output without any error or warning**.

### Problem B — No log or metric on fallback use

The fallback is reached silently. In a production system, unexpected use of the fallback (e.g., a new activity subtype was added without updating the map) produces corrupted outbound messages with no diagnostic information.

---

## Suggested Fix Plan

### Step 1 — Add a debug assertion on the fallback path

```csharp
public override string ToJson()
{
    if (TeamsActivityType.ActivitySerializerMap.TryGetValue(GetType(), out Func<TeamsActivity, string>? serializer))
        return serializer(this);

    System.Diagnostics.Debug.Fail(
        $"No serializer registered for activity type '{GetType().Name}'. " +
        $"Add an entry to TeamsActivityType.ActivitySerializerMap.");

    return ToJson(TeamsActivityJsonContext.Default.TeamsActivity);
}
```

This makes the fallback visible during development and testing.

### Step 2 — Add a production log on the fallback path

Ensure a logger is accessible from `TeamsActivity.ToJson()` (or accept that a static logger is used here):

```csharp
// If a static logger is available (e.g., via ILogger<TeamsActivity>):
_logger?.LogWarning(
    "Falling back to base TeamsActivity serializer for type '{ActivityType}'. " +
    "Register a serializer in ActivitySerializerMap.",
    GetType().Name);
```

### Step 3 — The complete fix: resolve Issue 003

The correct resolution of both this issue and Issue 003 is to populate `ActivitySerializerMap` completely (see **Audit Issue 003**). Once all 8 activity types have registered serializers, the fallback path should only be reached for:
- Direct `new TeamsActivity()` instances (base type, no subtype — the fallback is correct here).
- Third-party subclasses.

After Issue 003 is resolved, add a comment to the fallback explaining when it is legitimately expected:

```csharp
// Fallback: used for base TeamsActivity instances and unknown subtypes.
// If a known subtype hits this path, add it to ActivitySerializerMap.
return ToJson(TeamsActivityJsonContext.Default.TeamsActivity);
```

---

## Acceptance Criteria

- The fallback path in `TeamsActivity.ToJson()` emits a `Debug.Fail` assertion (caught in tests) and a log warning (visible in production) when reached for a type that should have a registered serializer.
- After Issue 003 is resolved, the only types that legitimately reach the fallback are `TeamsActivity` itself and unregistered third-party subtypes.
- A unit test asserts that calling `ToJson()` on a `MessageReactionActivity` instance does _not_ hit the fallback path (once Issue 003 is complete).
