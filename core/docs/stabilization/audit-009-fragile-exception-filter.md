# Audit Issue 009: Fragile Exception Filter by Message String in Streaming Writer

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Apps/TeamsStreamingWriter.cs`  
**Lines:** 104–107  
**Category:** Error handling correctness

---

## Problem

`AppendResponseAsync` catches a cancellation-by-user event by matching on the exception message text:

```csharp
// TeamsStreamingWriter.cs, lines 104-107
catch (HttpRequestException ex) when (ex.Message.Contains("Content stream was cancelled by user", StringComparison.OrdinalIgnoreCase))
{
    _cancelled = true;
}
```

This is fragile for several reasons:

1. **Message strings are not part of any API contract.** The Teams API or the underlying HTTP stack can change the wording of this error at any time (e.g., capitalisation change, translation, rewording). When that happens, the `when` filter stops matching, the exception propagates uncaught, and streaming fails with an unhandled exception rather than setting `_cancelled = true`.

2. **Locale sensitivity.** If the exception message is ever localised, `StringComparison.OrdinalIgnoreCase` may not be sufficient.

3. **Overly narrow catch.** Other transient cancellation-related HTTP errors that should also set `_cancelled = true` may have different messages and will not be caught.

4. **No fallback handling.** If the message changes, callers of `AppendResponseAsync` receive an `HttpRequestException` that bubbles up through their code with no indication that it was a user-initiated cancellation.

---

## Root Cause

The Teams Streaming API does not appear to use a typed exception or a specific `HttpStatusCode` to signal user-side stream cancellation. The message-string filter is a workaround for an ambiguous API contract.

---

## Suggested Fix Plan

### Step 1 — Audit the actual HTTP response for a distinguishing status code or header

Before changing anything in code, investigate what the Teams API actually returns when a user cancels a stream:

- Check the HTTP status code (e.g., `499 Client Closed Request`, `400`, `408`).
- Check the response body for a structured error code.
- Review the Teams Streaming API documentation for the expected error shape.

If a specific status code or structured error code exists, prefer it over message matching.

### Step 2 — Use `HttpRequestException.StatusCode` if available

`HttpRequestException` in .NET 5+ exposes `StatusCode`:

```csharp
catch (HttpRequestException ex) when (IsCancellationByUser(ex))
{
    _cancelled = true;
}

private static bool IsCancellationByUser(HttpRequestException ex)
{
    // Prefer status-code check if the Teams API uses a specific code
    if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
        // Narrow further if needed
        return ex.Message.Contains("cancelled by user", StringComparison.OrdinalIgnoreCase);
    }
    return false;
}
```

Centralising the check in a named method makes it testable and easier to update.

### Step 3 — Add a fallback log for unmatched `HttpRequestException`

Ensure that `HttpRequestException` instances that do _not_ match the filter are at least logged:

```csharp
catch (HttpRequestException ex)
{
    if (IsCancellationByUser(ex))
    {
        _cancelled = true;
        return;
    }
    // Re-throw non-cancellation errors — but log them first so they're diagnosable.
    // (Use the logger passed to the writer if one is added.)
    throw;
}
```

### Step 4 — Add a constant for the matched string

At a minimum, if the string match must remain, extract the magic string to a named constant and add a comment:

```csharp
// This string is matched against the Teams API error message for user-initiated stream cancellation.
// If streaming breaks unexpectedly, verify this string still matches the Teams API response.
private const string UserCancelledStreamMessage = "Content stream was cancelled by user";
```

---

## Acceptance Criteria

- The cancellation detection does not rely solely on an uncontracted error message string.
- If the detection logic changes or the message changes, the failure mode is a thrown `HttpRequestException` (diagnosable) rather than a silent logic error.
- A unit test mocks an `HttpRequestException` with the cancellation message and verifies `_cancelled` is set to `true`.
- A unit test mocks an `HttpRequestException` with an unrelated message and verifies it is re-thrown.
