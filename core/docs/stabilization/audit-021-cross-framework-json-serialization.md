# Audit Issue 021: Cross-Framework JSON Serialization (Newtonsoft → System.Text.Json)

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Compat/CompatTeamsInfo.cs`  
**Lines:** 357–358  
**Category:** Type safety / Serialization correctness

---

## Problem

`SendMeetingNotificationAsync` converts a Bot Framework type to a Core type by serializing with Newtonsoft.Json and deserializing with System.Text.Json:

```csharp
string json = Newtonsoft.Json.JsonConvert.SerializeObject(notification);
AppsTeams.TargetedMeetingNotification? coreNotification =
    System.Text.Json.JsonSerializer.Deserialize<AppsTeams.TargetedMeetingNotification>(json, s_jsonOptions);
```

This cross-framework round-trip is fragile because:

1. **Property naming conventions differ.** Newtonsoft defaults to PascalCase; System.Text.Json requires explicit `JsonPropertyName` attributes or a `PropertyNamingPolicy`. If the serialized JSON uses PascalCase but the target type expects camelCase (or vice versa), properties silently deserialize as `null`/default.

2. **Type handling attributes are incompatible.** `[Newtonsoft.Json.JsonProperty]` attributes are ignored by System.Text.Json, and `[System.Text.Json.Serialization.JsonPropertyName]` attributes are ignored by Newtonsoft. If either type uses library-specific attributes, the JSON field names may not match.

3. **Enum serialization may differ.** Newtonsoft serializes enums as integers by default; System.Text.Json may expect string values depending on configuration.

4. **Null handling differs.** `NullValueHandling` (Newtonsoft) vs `DefaultIgnoreCondition` (STJ) can cause properties to be present/absent inconsistently.

The same pattern appears in `CompatActivity.ToCompatActivity()` and `FromCompatActivity()`, though those go through `BotMessageSerializer` (Newtonsoft) to/from `CoreActivity.FromJsonString` (STJ).

---

## Root Cause

The compat layer bridges two SDK generations that use different JSON libraries. Direct JSON round-tripping between the two libraries works for simple cases but has no compile-time safety net.

---

## Suggested Fix

### Option A — Use a single serializer for the round-trip

If possible, use the same JSON library for both serialization and deserialization. Since the compat layer already depends on Newtonsoft, use Newtonsoft for both:

```csharp
string json = Newtonsoft.Json.JsonConvert.SerializeObject(notification);
AppsTeams.TargetedMeetingNotification? coreNotification =
    Newtonsoft.Json.JsonConvert.DeserializeObject<AppsTeams.TargetedMeetingNotification>(json);
```

Or use `System.Text.Json` for both if the source type supports it.

### Option B — Manual mapping

Write an explicit mapping method that copies properties between the Bot Framework type and the Core type without serialization. This is more verbose but eliminates serialization ambiguity:

```csharp
AppsTeams.TargetedMeetingNotification coreNotification = new()
{
    Value = new()
    {
        Recipients = notification.Value?.Recipients?.Select(r => /* map */).ToList(),
        // ...
    }
};
```

### Option C — Add integration tests for serialization fidelity

If the cross-framework approach is kept, add tests that verify every property survives the Newtonsoft→STJ round-trip for all types used in cross-serialization.

---

## Acceptance Criteria

- No silent data loss during cross-framework serialization.
- All properties of the notification object survive the conversion.
- Integration test covers the Newtonsoft → STJ path with a fully-populated object.
