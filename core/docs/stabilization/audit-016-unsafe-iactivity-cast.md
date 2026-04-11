# Audit Issue 016: Unsafe Direct Cast from `IActivity` to `Activity` in `CompatTeamsInfo`

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Compat/CompatTeamsInfo.cs`  
**Lines:** 452, 484, 516, 543, 582  
**Category:** Type safety

---

## Problem

Five methods in `CompatTeamsInfo` perform direct casts from `IActivity` to `Activity`:

```csharp
// Lines 452, 484, 516, 543:
CoreActivity coreActivity = ((Activity)activity).FromCompatActivity();

// Line 582:
Activity = (Activity)activity,
```

These occur in:
- `SendMessageToListOfUsersAsync` (line 452)
- `SendMessageToListOfChannelsAsync` (line 484)
- `SendMessageToAllUsersInTeamAsync` (line 516)
- `SendMessageToAllUsersInTenantAsync` (line 543)
- `SendMessageToTeamsChannelAsync` (line 582)

If any caller passes an `IActivity` implementation that is not `Activity` (e.g., a mock, a decorator, or a future alternative implementation), these casts throw `InvalidCastException` at runtime with no descriptive error message.

The method signatures accept `IActivity`, which is the Bot Framework SDK abstraction. The direct cast violates the interface contract by requiring a concrete type.

---

## Root Cause

The `FromCompatActivity()` extension method is defined on `Activity` (concrete class), not on `IActivity`. The compat layer needs to serialize the activity to JSON for conversion, which requires access to the concrete type's serialization.

---

## Suggested Fix

### Option A — Safe cast with descriptive error (minimal change)

```csharp
Activity concreteActivity = activity as Activity
    ?? throw new ArgumentException(
        $"Expected {nameof(Activity)} but received {activity.GetType().Name}. " +
        "CompatTeamsInfo requires Bot Framework Activity instances.",
        nameof(activity));
CoreActivity coreActivity = concreteActivity.FromCompatActivity();
```

### Option B — Accept `Activity` in the method signature

Change the parameter type from `IActivity` to `Activity`. This makes the requirement explicit at compile time rather than failing at runtime. This is a breaking API change but honest about the actual contract.

### Option C — Serialize via interface

If the `IActivity` contract is important, serialize through the interface using `JsonConvert.SerializeObject(activity)` rather than casting, since `IActivity` properties are sufficient for JSON serialization.

---

## Acceptance Criteria

- Passing a non-`Activity` `IActivity` produces a clear, descriptive exception (not `InvalidCastException`).
- Alternatively, compile-time type safety prevents the mismatch entirely.
- Existing callers that pass `Activity` instances are unaffected.
