# Audit Issue 014: `TeamsAttachmentBuilder.Build()` Returns Internal Mutable Reference

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/TeamsAttachmentBuilder.cs`  
**Line:** 110  
**Category:** Memory management / Builder pattern correctness

---

## Problem

`TeamsAttachmentBuilder.Build()` returns its internal `_attachment` field directly:

```csharp
public TeamsAttachment Build() => _attachment;
```

This breaks the standard builder contract: the caller receives the *same* object the builder still holds internally. Continued calls to the builder (e.g., `WithContent(...)`, `WithName(...)`) after `Build()` silently mutate the supposedly "built" attachment through shared reference.

Example of the bug:

```csharp
var builder = new TeamsAttachmentBuilder()
    .WithContentType("image/png")
    .WithName("photo.png");

TeamsAttachment attachment1 = builder.Build();

builder.WithName("other.png");          // mutates attachment1 too
TeamsAttachment attachment2 = builder.Build();

// attachment1.Name == "other.png" — unexpected
// attachment1 and attachment2 are the same object reference
```

---

## Root Cause

`Build()` returns the internal reference without creating a defensive copy. This is a common shortcut in builder implementations that becomes a problem when the builder is reused or further modified after calling `Build()`.

---

## Suggested Fix

Return a copy or make the `Build()` method terminal:

### Option A — Return a new instance (recommended)

```csharp
public TeamsAttachment Build()
{
    return new TeamsAttachment
    {
        ContentType = _attachment.ContentType,
        Content = _attachment.Content,
        ContentUrl = _attachment.ContentUrl,
        Name = _attachment.Name,
        ThumbnailUrl = _attachment.ThumbnailUrl,
        Properties = new ExtendedPropertiesDictionary(_attachment.Properties)
    };
}
```

### Option B — Freeze-on-build pattern

Add a `_built` flag and throw `InvalidOperationException` on any setter call after `Build()`:

```csharp
private bool _built;
public TeamsAttachment Build()
{
    _built = true;
    return _attachment;
}
// In each With* method:
if (_built) throw new InvalidOperationException("Builder has already been built.");
```

---

## Acceptance Criteria

- `Build()` returns an object that is not mutated by subsequent builder calls.
- Existing tests that call `Build()` once continue to pass.
- No behavior change for the single-build-per-builder usage pattern.
