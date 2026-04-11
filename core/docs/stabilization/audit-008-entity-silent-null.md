# Audit Issue 008: Unknown Entity Types Silently Discarded During JSON Deserialization

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/Entity.cs`  
**Lines:** 66–74 (inside `EntityList.FromJsonArray`)  
**Category:** Data integrity / extensibility

---

## Problem

When deserializing the `entities` array from an activity payload, unknown `type` values silently produce `null` and are skipped:

```csharp
// Entity.cs, lines 66-74
Entity? entity = typeString switch
{
    "clientInfo"                              => item.Deserialize<ClientInfoEntity>(options),
    "mention"                                 => item.Deserialize<MentionEntity>(options),
    "message" or "https://schema.org/Message" => DeserializeMessageEntity(item, options),
    "ProductInfo"                             => item.Deserialize<ProductInfoEntity>(options),
    "streaminfo"                              => item.Deserialize<StreamInfoEntity>(options),
    _ => null   // <-- silently drops any unrecognised entity type
};
if (entity != null)
    entities.Add(entity);
```

There is already a TODO comment above this code acknowledging the problem:

```csharp
// TODO: Should be able to support unknown types (PA uses BotMessageMetadata).
```

The consequences:

1. **Data loss** — Third-party or future Teams entity types (e.g., `BotMessageMetadata`, `citation`, custom adaptive card entities) are silently dropped. Bot handlers never see them.
2. **Debugging difficulty** — There is no log message, no metric, and no way to know that entities were dropped in production.
3. **Incomplete bot behaviour** — If a Teams extension sends a new entity type that triggers bot logic, that logic never runs.

---

## Root Cause

The switch statement enumerates known types exhaustively. Adding a new entity type requires modifying this file. There is no extensibility mechanism (registry, plugin, etc.) — this is noted in the existing TODO comment.

---

## Suggested Fix Plan

### Step 1 — Deserialize unknown types as the base `Entity` class (immediate fix)

Instead of returning `null` for unknown types, deserialize to the base `Entity` class which preserves all JSON fields via `[JsonExtensionData]`:

```csharp
_ => item.Deserialize<Entity>(options)   // Preserves all properties via ExtensionData
```

`Entity` has `[JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; }` which captures all unknown JSON fields. Bot code can inspect `entity.Type` and `entity.Properties` to handle unrecognised types.

This is a non-breaking change: existing handlers check `entity is MentionEntity` etc. and are unaffected. Unknown types are now accessible instead of dropped.

### Step 2 — Add a trace log for unknown entity types

```csharp
_ =>
{
    logger?.LogTrace("Unknown entity type '{EntityType}' deserialized as base Entity.", typeString);
    return item.Deserialize<Entity>(options);
}
```

(Pass a logger into `FromJsonArray` or use a static `ILogger` on the `EntityList` class.)

### Step 3 — Implement a registration pattern (medium-term, addresses the TODO)

Introduce a static registry:

```csharp
public static class EntityTypeRegistry
{
    private static readonly Dictionary<string, Type> _types = new(StringComparer.OrdinalIgnoreCase);

    public static void Register<T>(string typeName) where T : Entity
        => _types[typeName] = typeof(T);

    internal static Type Resolve(string typeName)
        => _types.TryGetValue(typeName, out Type? t) ? t : typeof(Entity);
}
```

Replace the switch statement with a registry lookup:

```csharp
Type entityType = EntityTypeRegistry.Resolve(typeString);
Entity? entity = (Entity?)item.Deserialize(entityType, options);
```

Register built-in types at startup and allow third parties to register their own.

---

## Acceptance Criteria

- No `entities` entry is silently dropped; unknown types are preserved as base `Entity` instances.
- A unit test verifies that an activity payload with an unknown entity type `"BotMessageMetadata"` produces one `Entity` in the list with `Type == "BotMessageMetadata"` and the original JSON fields accessible via `Properties`.
- Existing entity type tests (mention, citation, etc.) continue to pass.
