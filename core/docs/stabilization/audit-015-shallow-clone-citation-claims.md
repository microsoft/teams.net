# Audit Issue 015: Shallow Clone of `CitationClaim` List in `CitationEntity` Copy Constructor

**Severity:** Low  
**File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/CitationEntity.cs`  
**Lines:** 115–130  
**Category:** Memory management / Cloning correctness

---

## Problem

The `CitationEntity(OMessageEntity entity)` copy constructor creates a *new list* but copies the `CitationClaim` elements by reference:

```csharp
Citation = citationEntity.Citation != null
    ? new List<CitationClaim>(citationEntity.Citation)
    : null;
```

`CitationClaim` is a mutable class with public setters (`Position`, `Appearance`). The new list shares the same `CitationClaim` objects as the source. Mutating a claim in the copy mutates the original, and vice versa.

Similarly, `AdditionalType` is shallow-copied as `new List<string>(entity.AdditionalType)`, but since `string` is immutable this is safe. The `CitationClaim` case is the concern.

---

## Root Cause

`new List<T>(source)` creates a new list with the same element references. For reference types, this is a shallow clone — only the list container is independent, not the items.

---

## Suggested Fix

### Option A — Deep-copy each `CitationClaim`

```csharp
Citation = citationEntity.Citation != null
    ? citationEntity.Citation.Select(c => new CitationClaim
    {
        Position = c.Position,
        Appearance = c.Appearance is not null
            ? new CitationAppearance { /* copy fields */ }
            : null
    }).ToList()
    : null;
```

### Option B — Make `CitationClaim` a record or immutable type

If `CitationClaim` is never mutated after construction, convert it to a `record` or make its setters `init`-only. Then the shallow list copy is safe because the elements cannot be mutated.

```csharp
public record CitationClaim
{
    public int? Position { get; init; }
    public CitationAppearance? Appearance { get; init; }
}
```

---

## Acceptance Criteria

- Mutating a `CitationClaim` in a copied `CitationEntity` does not affect the source entity.
- Serialization round-trip produces identical output before and after the fix.
- Existing entity tests continue to pass.
