# Audit Issue 012: Thread-Safety Race on `OnActivity` in `CompatAdapter`

**Severity:** Critical  
**File:** `core/src/Microsoft.Teams.Bot.Compat/CompatAdapter.cs`  
**Lines:** 57–66  
**Category:** Thread safety / Concurrency

---

## Problem

`CompatAdapter.ProcessAsync` mutates `_teamsBotApplication.OnActivity` — a public delegate property on the shared `TeamsBotApplication` singleton — on every incoming HTTP request:

```csharp
_teamsBotApplication.OnActivity = async (activity, ct) =>
{
    coreActivity = activity;
    TurnContext turnContext = new(this, activity.ToCompatActivity());
    // ...
    await MiddlewareSet.ReceiveActivityWithStatusAsync(turnContext, bot.OnTurnAsync, ct).ConfigureAwait(false);
};
```

`TeamsBotApplication` is registered as a singleton in DI. When two or more HTTP requests arrive concurrently, they race on assigning `OnActivity`. Request A may set its callback, then Request B overwrites it before the bot pipeline invokes it. Request A then executes Request B's callback (or vice versa), leading to:

- Requests dispatched to the wrong `IBot` instance.
- Leaked `TurnContext` / `CompatConnectorClient` from another request.
- Captured `coreActivity` local variable may be overwritten by the wrong request.

---

## Root Cause

The compatibility adapter bridges the legacy `IBotFrameworkHttpAdapter.ProcessAsync(HttpRequest, HttpResponse, IBot)` pattern onto the new Core bot pipeline. The new pipeline uses a single `OnActivity` delegate rather than per-request callback registration. Assigning a lambda to a shared singleton property on each request is inherently racy.

---

## Suggested Fix

Replace the shared mutable delegate with per-request dispatch. Two options:

### Option A — Use `AsyncLocal<>` to scope the callback per request

Store the per-request callback in an `AsyncLocal<Func<CoreActivity, CancellationToken, Task>>` and have the `OnActivity` delegate read from it:

```csharp
private static readonly AsyncLocal<Func<CoreActivity, CancellationToken, Task>?> _currentCallback = new();

// In constructor (once):
_teamsBotApplication.OnActivity = (activity, ct) =>
    _currentCallback.Value?.Invoke(activity, ct) ?? Task.CompletedTask;

// In ProcessAsync (per request):
_currentCallback.Value = async (activity, ct) => { /* per-request logic */ };
```

### Option B — Accept a per-request callback in the pipeline

Refactor `BotApplication.ProcessAsync` to accept an optional `Func<CoreActivity, CancellationToken, Task>` parameter, avoiding the need to mutate shared state entirely. This is more invasive but cleaner.

---

## Acceptance Criteria

- Concurrent requests in `CompatAdapter.ProcessAsync` do not share or overwrite each other's handler.
- No shared mutable state is assigned per-request on the singleton.
- Existing compat integration tests pass.
- A concurrency stress test with overlapping requests does not produce cross-request leakage.
