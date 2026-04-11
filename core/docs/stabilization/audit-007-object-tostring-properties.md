# Audit Issue 007: Unvalidated `object.ToString()` on Property Bag Values

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Compat/CompatActivity.cs`  
**Lines:** 75–109 (in `ToCompatChannelAccount`) and 238–265 (in `ToCompatTeamsChannelAccount`)  
**Category:** Type safety / data correctness

---

## Problem

`ConversationAccount.Properties` is a `Dictionary<string, object?>`. When converting to compat types, values are extracted and converted with `.ToString()`:

```csharp
// CompatActivity.cs, line 75-77
if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
{
    channelAccount.AadObjectId = aadObjectId?.ToString();
}

// Same pattern repeated for: userRole, userPrincipalName, givenName, surname, email, tenantId
```

The problem: `object.ToString()` has no defined behaviour for non-string types. If the JSON deserializer stored the value as a `JsonElement`, `JToken`, `JsonNode`, `int`, `bool`, or `string[]`, calling `.ToString()` produces:

| Stored type | `.ToString()` result | Expected result |
|---|---|---|
| `JsonElement` (string) | `"hello"` (correct) | `"hello"` |
| `JsonElement` (number) | `"42"` (correct) | `"42"` |
| `JsonElement` (array) | `"System.Text.Json.JsonElement"` | `null` or throw |
| `string` | `"hello"` (correct) | `"hello"` |
| `object[]` | `"System.Object[]"` | `null` or throw |
| `null` | `null` (correct via `?.`) | `null` |

In practice, the `Properties` dictionary is populated from JSON deserialization. Depending on which JSON library populated it (`System.Text.Json` vs `Newtonsoft.Json`), the stored type varies:

- **`System.Text.Json`** stores `JsonElement` values.
- **`Newtonsoft.Json`** stores `JToken` values (which have a useful `.ToString()`, so this is safer there).

The code uses both serializers (the compat layer uses Newtonsoft). But a future migration or mixed-serializer path could cause silent data corruption where `aadObjectId` becomes `"System.Text.Json.JsonElement"` in the compat object.

---

## Suggested Fix Plan

### Step 1 — Add a typed extraction helper

Replace raw `.ToString()` calls with a helper that understands the known stored types:

```csharp
private static string? ExtractStringProperty(Dictionary<string, object?> properties, string key)
{
    if (!properties.TryGetValue(key, out object? value))
        return null;

    return value switch
    {
        null => null,
        string s => s,
        System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.String
            => je.GetString(),
        System.Text.Json.JsonElement je
            => je.ToString(), // Numbers, bools — ToString() is well-defined
        Newtonsoft.Json.Linq.JValue jv => jv.Value?.ToString(),
        _ => value.ToString() // Fallback with debug assertion
    };
}
```

Apply this helper to all `TryGetValue` calls in `ToCompatChannelAccount` and `ToCompatTeamsChannelAccount`.

### Step 2 — Add a debug assertion for unexpected types

In the fallback branch, add:

```csharp
_ =>
{
    System.Diagnostics.Debug.Fail($"Unexpected type '{value.GetType().Name}' for property '{key}'");
    return value.ToString();
}
```

This surfaces unexpected types during development without impacting production.

### Step 3 — Document the contract of `Properties`

Add an XML doc comment to `ConversationAccount.Properties` (or wherever the dictionary is declared) stating the expected value types, so future code does not store non-string values without thought.

---

## Acceptance Criteria

- `aadObjectId`, `userRole`, `userPrincipalName`, `givenName`, `surname`, `email`, and `tenantId` are never set to strings like `"System.Text.Json.JsonElement"`.
- A unit test verifies `ToCompatChannelAccount` with a `JsonElement`-backed property dictionary produces the correct string values.
- All existing compat conversion tests pass.
