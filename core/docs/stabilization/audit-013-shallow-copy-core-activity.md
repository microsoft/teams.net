# Audit Issue 013: Shallow Reference Copy in `CoreActivity` Copy Constructor

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Core/Schema/CoreActivity.cs`  
**Lines:** 140–156  
**Category:** Memory management / Immutability

---

## Problem

The `protected CoreActivity(CoreActivity activity)` copy constructor copies all properties by reference:

```csharp
protected CoreActivity(CoreActivity activity)
{
    ArgumentNullException.ThrowIfNull(activity);
    Id = activity.Id;
    ServiceUrl = activity.ServiceUrl;
    ChannelId = activity.ChannelId;
    Type = activity.Type;
    ChannelData = activity.ChannelData;        // shared reference
    From = activity.From;                      // shared reference
    Recipient = activity.Recipient;            // shared reference
    Conversation = activity.Conversation;      // shared reference
    Entities = activity.Entities;              // shared JsonArray reference
    Attachments = activity.Attachments;        // shared JsonArray reference
    Properties = activity.Properties;          // shared dictionary reference
    Value = activity.Value;                    // shared JsonNode reference
}
```

This means:

1. Mutating `ChannelData`, `From`, `Recipient`, or `Conversation` on the copy also mutates the original.
2. Adding/removing items from `Entities` or `Attachments` affects both instances.
3. The `Properties` dictionary is fully shared — setting a property on one activity sets it on the other.

The `TeamsActivity(CoreActivity)` constructor compounds this: it overwrites some properties with Teams-specific wrappers and calls `Rebase()`, which writes back into `base.Attachments` and `base.Entities` — potentially replacing the `JsonArray` that the original activity still references.

---

## Root Cause

The copy constructor was written as a shallow field-by-field copy for performance/simplicity. Rich object graphs (`ChannelData`, `ConversationAccount`, `JsonArray`, `Dictionary`) require explicit deep-copy or clone semantics to be safe.

---

## Suggested Fix

### Option A — Deep-copy mutable reference types (recommended)

Clone the objects that are known to be mutated downstream:

```csharp
protected CoreActivity(CoreActivity activity)
{
    ArgumentNullException.ThrowIfNull(activity);
    Id = activity.Id;
    ServiceUrl = activity.ServiceUrl;
    ChannelId = activity.ChannelId;
    Type = activity.Type;
    ChannelData = activity.ChannelData is not null
        ? JsonSerializer.Deserialize<ChannelData>(JsonSerializer.Serialize(activity.ChannelData))
        : null;
    From = activity.From;            // immutable after construction — acceptable
    Recipient = activity.Recipient;  // immutable after construction — acceptable
    Conversation = activity.Conversation;
    Entities = activity.Entities?.DeepClone().AsArray();
    Attachments = activity.Attachments?.DeepClone().AsArray();
    Properties = new ExtendedPropertiesDictionary(activity.Properties);
    Value = activity.Value?.DeepClone();
}
```

### Option B — Document that copy is shallow and add `DeepClone()` helper

If the shallow copy is intentional for performance, add an explicit `DeepClone()` method and document that the copy constructor creates a shallow copy. Callers that need isolation should use `DeepClone()`.

---

## Acceptance Criteria

- Mutating the copy's `Properties`, `Entities`, `Attachments`, or `ChannelData` does not affect the original.
- `TeamsActivity.Rebase()` does not stomp the original activity's `JsonArray` references.
- Serialization round-trip tests confirm no data loss.
- No measurable performance regression in activity processing throughput.
