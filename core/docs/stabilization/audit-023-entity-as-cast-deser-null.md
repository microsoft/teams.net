# Audit Issue 023: Entity Property `as` Casts Silently Return `null` After Deserialization

**Severity:** High  
**Files:**
- `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/CitationEntity.cs` — line 138
- `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/MentionEntity.cs` — line 84
- `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/OMessageEntity.cs` — line ~28
- `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/SensitiveUsageEntity.cs` — line ~48  
**Category:** Type safety / Deserialization correctness

---

## Problem

Several entity classes store complex typed values in `Entity.Properties` (an `ExtendedPropertiesDictionary`, i.e., `Dictionary<string, object?>`) and retrieve them using `as` casts:

```csharp
// CitationEntity.cs
public IList<CitationClaim>? Citation
{
    get => base.Properties.TryGetValue("citation", out object? value) ? value as IList<CitationClaim> : null;
    set => base.Properties["citation"] = value;
}

// MentionEntity.cs
public ConversationAccount? Mentioned
{
    get => base.Properties.TryGetValue("mentioned", out object? value) ? value as ConversationAccount : null;
    set => base.Properties["mentioned"] = value;
}
```

**When values are set programmatically** (via the setter), the `value` stored is the actual typed object, and the `as` cast succeeds.

**When values come from JSON deserialization**, `[JsonExtensionData]` on the `Properties` dictionary causes System.Text.Json to store the values as `JsonElement` (not as the expected CLR types). The `as` cast from `JsonElement` to `IList<CitationClaim>` or `ConversationAccount` **always returns `null`** — silently discarding the data.

This means any entity that is deserialized from JSON (the primary code path) will have `null` for these properties even when the JSON contains valid data.

---

## Root Cause

`[JsonExtensionData]` stores unrecognized JSON properties as `JsonElement` objects in the dictionary. The property getters assume the dictionary contains strongly-typed CLR objects, which is only true when the setter was called directly. The two code paths (programmatic construction vs. deserialization) produce different runtime types for the same dictionary entry.

---

## Suggested Fix

### Option A — Check for `JsonElement` and deserialize on access (recommended)

```csharp
public ConversationAccount? Mentioned
{
    get
    {
        if (!base.Properties.TryGetValue("mentioned", out object? value))
            return null;
        if (value is ConversationAccount account)
            return account;
        if (value is JsonElement je)
        {
            ConversationAccount? deserialized = je.Deserialize<ConversationAccount>();
            if (deserialized is not null)
                base.Properties["mentioned"] = deserialized; // cache the deserialized value
            return deserialized;
        }
        return null;
    }
    set => base.Properties["mentioned"] = value;
}
```

Apply the same pattern to `Citation` (`IList<CitationClaim>`), `AdditionalType` (`IList<string>`), and `Pattern` (`DefinedTerm`).

### Option B — Use dedicated `[JsonPropertyName]` properties instead of extension data

Move these fields out of `Properties` and into explicit JSON-mapped properties on each entity class, so System.Text.Json deserializes them into the correct type directly. This requires removing them from the extension data flow.

### Option C — Custom `JsonConverter` for `Entity`

Write a custom converter that deserializes known properties into their correct types when reading JSON, rather than relying on `[JsonExtensionData]`.

---

## Affected Properties

| Entity | Property | Expected Type | Actual Type After Deser |
|--------|----------|---------------|------------------------|
| `CitationEntity` | `Citation` | `IList<CitationClaim>` | `JsonElement` |
| `MentionEntity` | `Mentioned` | `ConversationAccount` | `JsonElement` |
| `OMessageEntity` | `AdditionalType` | `IList<string>` | `JsonElement` |
| `SensitiveUsageEntity` | `Pattern` | `DefinedTerm` | `JsonElement` |

---

## Acceptance Criteria

- Entity properties return the correct typed value regardless of whether the entity was constructed programmatically or deserialized from JSON.
- Round-trip serialization tests verify that `Citation`, `Mentioned`, `AdditionalType`, and `Pattern` survive JSON → object → JSON without data loss.
- No silent `null` returns when the JSON contains valid data for these fields.
