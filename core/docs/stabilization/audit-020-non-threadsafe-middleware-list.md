# Audit Issue 020: Non-Thread-Safe `List` in `TurnMiddleware`

**Severity:** Low  
**File:** `core/src/Microsoft.Teams.Bot.Core/TurnMiddleware.cs`  
**Line:** 21  
**Category:** Thread safety

---

## Problem

`TurnMiddleware` stores registered middleware in a plain `IList<ITurnMiddleware>`:

```csharp
private readonly IList<ITurnMiddleware> _middlewares = [];
```

The `Use()` method adds to this list:

```csharp
internal TurnMiddleware Use(ITurnMiddleware middleware)
{
    _middlewares.Add(middleware);
    return this;
}
```

And `RunPipelineAsync` reads from it concurrently during request processing:

```csharp
if (nextMiddlewareIndex == _middlewares.Count)
    // ...
ITurnMiddleware nextMiddleware = _middlewares[nextMiddlewareIndex];
```

`List<T>` is not thread-safe for concurrent reads and writes. If `Use()` is ever called after the application starts handling requests (e.g., from a background service or a dynamic middleware registration pattern), the list could be in an inconsistent state during concurrent reads, leading to `IndexOutOfRangeException` or corrupted iteration.

---

## Root Cause

The middleware list uses a non-concurrent collection. The design assumes middleware registration only happens during startup (before any calls to `RunPipelineAsync`), but this is not enforced.

---

## Suggested Fix

### Option A — Freeze the list after startup (recommended)

Convert `_middlewares` to an array after startup and use the array for pipeline execution:

```csharp
private readonly List<ITurnMiddleware> _middlewares = [];
private ITurnMiddleware[]? _frozen;

internal TurnMiddleware Use(ITurnMiddleware middleware)
{
    if (_frozen is not null)
        throw new InvalidOperationException("Cannot add middleware after the pipeline has started.");
    _middlewares.Add(middleware);
    return this;
}

internal void Freeze() => _frozen = [.. _middlewares];

public Task RunPipelineAsync(...)
{
    ITurnMiddleware[] pipeline = _frozen ?? throw new InvalidOperationException("Pipeline not frozen.");
    // use pipeline[nextMiddlewareIndex] instead of _middlewares[nextMiddlewareIndex]
}
```

### Option B — Use `ImmutableList<T>` or `ConcurrentBag<T>`

Replace `IList<ITurnMiddleware>` with `ImmutableList<ITurnMiddleware>` using atomic swap semantics, or use a thread-safe collection. This adds complexity but allows dynamic middleware registration.

---

## Acceptance Criteria

- Adding middleware after pipeline execution throws or is handled safely.
- Concurrent `RunPipelineAsync` calls with a frozen pipeline do not race.
- Existing middleware registration at startup continues to work.
