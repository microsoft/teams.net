# Design: Decouple CancellationToken from Incoming HTTP Request

## Problem

When a bot handler performs long-running work — most notably streaming LLM responses back to Teams — the `CancellationToken` passed into the handler is tied to the lifetime of the **incoming HTTP request** (`HttpContext.RequestAborted`). Teams closes that connection once it receives the initial HTTP response (typically within ~15 seconds), which fires the cancellation token and aborts any in-flight outbound calls the handler is still making.

### Observed behavior

```
dbug: HTTP POST .../v3/conversations/.../activities/... Response Status 202
fail: Error processing activity: Id=...
      System.Threading.Tasks.TaskCanceledException: The operation was canceled.
       ---> System.IO.IOException: Unable to read data from the transport connection:
            The I/O operation has been aborted because of either a thread exit or an application request.
```

The exception propagates through the OpenAI streaming pipeline, through `BotApplication.ProcessAsync`, and surfaces as a 500 to the ASP.NET middleware — even though the bot was functioning correctly.

### Why this matters

Streaming bots send responses via the **Bot Connector API** (`ConversationClient.SendActivityAsync`), not through the original HTTP response body. The handler legitimately outlives the HTTP request, so cancellation of that request should **not** cancel the handler's work.

## Solution

Replace the HTTP-bound `CancellationToken` with a **configurable timeout-based token** inside `BotApplication.ProcessAsync`.

### Changes

#### 1. `BotApplicationOptions.ProcessActivityTimeout`

A new property on `BotApplicationOptions`:

```csharp
public TimeSpan ProcessActivityTimeout { get; set; } = TimeSpan.FromMinutes(5);
```

- **Default: 5 minutes** — long enough for streaming LLM responses, short enough to prevent runaway handlers.
- Set to `Timeout.InfiniteTimeSpan` to disable the timeout entirely.
- Configurable per application instance via DI / builder options.

#### 2. `BotApplication.ProcessAsync` — token replacement

Before this change:

```csharp
CancellationToken token = Debugger.IsAttached ? CancellationToken.None : cancellationToken;
await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, token);
```

After:

```csharp
using var cts = new CancellationTokenSource(_processActivityTimeout);
CancellationToken token = Debugger.IsAttached ? CancellationToken.None : cts.Token;
await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, token);
```

The HTTP request's `cancellationToken` is no longer forwarded to the handler pipeline.

#### 3. Graceful timeout handling

A new catch clause handles the timeout without crashing:

```csharp
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    _logger.LogWarning("Activity processing timed out after {Timeout}: Id={Id}",
        _processActivityTimeout, activity.Id);
}
```

This prevents `BotHandlerException` from being thrown when the timeout fires, which is a recoverable situation (the handler simply took too long).

## Design Decisions

### Why not keep the HTTP token as a linked source?

Using `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` would still propagate HTTP disconnection to the handler — defeating the purpose. The HTTP request completing is an **expected** event for streaming handlers, not an error signal.

### Why a timeout instead of `CancellationToken.None`?

Unbounded processing is a resource leak risk. A timeout provides a safety net:
- Prevents handlers from running indefinitely if the LLM or external service hangs.
- Gives operators a tuning knob via `ProcessActivityTimeout`.
- Preserves the existing `Debugger.IsAttached` → `CancellationToken.None` escape hatch for debugging.

### Why handle this at the framework level?

- Every streaming bot would need the same workaround in user code.
- The framework owns the token plumbing and is the right place to define its semantics.
- Non-streaming bots are unaffected — 5 minutes is generous for synchronous handlers and can be reduced via options.

### Impact on non-streaming bots

Non-streaming handlers that complete within the HTTP request lifetime are unaffected. The 5-minute default is well above typical synchronous handler durations. Apps that want tighter timeouts can set `ProcessActivityTimeout` to a lower value.

## Alternatives Considered

| Alternative | Drawback |
|---|---|
| Catch `TaskCanceledException` in each sample/handler | Pushes framework responsibility to every consumer; easy to forget |
| Use `CancellationToken.None` unconditionally | No timeout safety net; runaway handlers can leak resources |
| Expose a `bool IsStreaming` flag to switch behavior | Over-engineered; all handlers benefit from decoupling |
| Let ASP.NET Core's `RequestTimeout` middleware handle it | That controls the *HTTP* timeout, not the *handler processing* timeout — different concerns |
