# Audit Issue 004: Silent `as`-Cast Failures in `TeamsActivity` Property Shadowing

**Severity:** High  
**File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/TeamsActivity.cs`  
**Lines:** 108–142  
**Category:** Type safety / Liskov Substitution Principle

---

## Problem

`TeamsActivity` shadows four base-class properties using the `new` keyword. Each getter performs an `as` cast on the base property:

```csharp
// TeamsActivity.cs, line 108-142
[JsonPropertyName("from")]
public new TeamsConversationAccount? From
{
    get => base.From as TeamsConversationAccount;   // silently null if wrong type
    set => base.From = value;
}

[JsonPropertyName("recipient")]
public new TeamsConversationAccount? Recipient
{
    get => base.Recipient as TeamsConversationAccount;
    set => base.Recipient = value;
}

[JsonPropertyName("conversation")]
public new TeamsConversation? Conversation
{
    get => base.Conversation as TeamsConversation;
    set => base.Conversation = value;
}

[JsonPropertyName("channelData")]
public new TeamsChannelData? ChannelData
{
    get => base.ChannelData as TeamsChannelData;
    set => base.ChannelData = value;
}
```

The `as` operator returns `null` if the object is not of the target type — **it never throws**. This creates two categories of silent failure:

1. **Type mismatch at runtime** — If the base property was set to a `ConversationAccount` (not a `TeamsConversationAccount`), `From` returns `null` with no diagnostic. Callers that assume the property is non-null (because it was set) receive `null` and may NullReferenceException later, far from the assignment site.

2. **Polymorphism violation** — Code holding a `CoreActivity` reference gets different property values than code holding a `TeamsActivity` reference to the _same object_. This violates the Liskov Substitution Principle and can cause subtle bugs in generic code that accepts `CoreActivity`.

The constructor (`TeamsActivity(CoreActivity activity)`) at lines 62–89 does set the Teams-typed subtypes explicitly, so the normal deserialization path is safe. The risk arises from:
- Direct assignment to `base.From = someConversationAccount` (which bypasses the Teams subtype).
- Activities created via the compat layer (`CompatActivity.cs`) that may set non-Teams types.
- Future code changes that assign to the base type without realising the derived getter will silently return null.

---

## Root Cause

The `new` keyword intentionally shadows the base property to return a more-derived type. However, the getter assumes the base field always holds the derived subtype — an invariant that is not enforced by the type system and can be broken externally.

---

## Suggested Fix Plan

### Option A — Add a debug-mode assertion in each getter (minimal change)

Add a `Debug.Assert` so the mismatch is caught during development/testing without impacting production performance:

```csharp
public new TeamsConversationAccount? From
{
    get
    {
        System.Diagnostics.Debug.Assert(
            base.From is null or TeamsConversationAccount,
            $"TeamsActivity.From expected TeamsConversationAccount but found {base.From?.GetType().Name}");
        return base.From as TeamsConversationAccount;
    }
    set => base.From = value;
}
```

Apply the same pattern to `Recipient`, `Conversation`, and `ChannelData`.

### Option B — Enforce the invariant in the setter (preferred)

Wrap the base setter to ensure only the correct subtype can be stored, upgrading a plain `ConversationAccount` if one is passed:

```csharp
public new TeamsConversationAccount? From
{
    get => base.From as TeamsConversationAccount;
    set => base.From = value;  // Already typed as TeamsConversationAccount? — no plain ConversationAccount can be set through this property.
}
```

The setter is already typed correctly (`TeamsConversationAccount?`). The issue is that `base.From = value` is `ConversationAccount?`, so a caller accessing `base.From` directly and assigning a plain `ConversationAccount` bypasses the Teams-typed setter. To guard against this, seal or hide the base setter:

If `CoreActivity.From` is not `virtual` (check the base class), consider adding validation in `Rebase()` that re-promotes any plain `ConversationAccount` to `TeamsConversationAccount`:

```csharp
internal TeamsActivity Rebase()
{
    // Promote base-typed From to TeamsConversationAccount if needed
    if (base.From is not null and not TeamsConversationAccount)
        base.From = TeamsConversationAccount.FromConversationAccount(base.From);

    // ... same for Recipient, Conversation, ChannelData
    base.Attachments = Attachments?.ToJsonArray();
    base.Entities = Entities?.ToJsonArray();
    return this;
}
```

### Option C — Add a unit test for type consistency (minimum viable)

At a minimum, add tests that:

```csharp
[Fact]
public void TeamsActivity_From_ReturnsNull_WhenBaseIsPlainConversationAccount()
{
    // Document the known limitation explicitly so future developers understand the contract.
    var activity = new TeamsActivity();
    ((CoreActivity)activity).From = new ConversationAccount { Id = "x" };
    Assert.Null(activity.From); // Known: returns null, not the base value
}
```

This makes the behaviour explicit rather than surprising.

---

## Acceptance Criteria

- All property getters that perform `as` casts have either a `Debug.Assert`, runtime validation in `Rebase()`, or an explicit test documenting the null-return contract.
- No production code path silently reads `null` from `From`, `Recipient`, `Conversation`, or `ChannelData` on a `TeamsActivity` that had those fields set on the base.
