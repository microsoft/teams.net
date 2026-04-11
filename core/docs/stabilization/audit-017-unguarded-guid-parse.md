# Audit Issue 017: Unguarded `Guid.Parse` on External Input

**Severity:** Medium  
**Files:**
- `core/src/Microsoft.Teams.Bot.Core/Hosting/BotAuthenticationHandler.cs` — line 99
- `core/src/Microsoft.Teams.Bot.Compat/KeyedBotAuthenticationHandler.cs` — line ~120  
**Category:** Input validation / Type safety

---

## Problem

`BotAuthenticationHandler.GetAuthorizationHeaderAsync` calls `Guid.Parse` on `AgenticUserId`, which originates from deserialized channel data (external input):

```csharp
options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));
```

`AgenticIdentity.AgenticUserId` is populated from `ExtendedPropertiesDictionary` via `AgenticIdentity.FromProperties()`, which calls `userIdObj?.ToString()`. The value comes from the incoming activity's channel data — ultimately from the Teams service, but still external to the bot process.

If `AgenticUserId` is not a valid GUID (empty string, malformed, or unexpected format), `Guid.Parse` throws a `FormatException`. This exception propagates up through the HTTP message handler pipeline, resulting in a failed outgoing API call with an unhelpful exception rather than a clear validation error.

The same pattern exists in `KeyedBotAuthenticationHandler`.

---

## Root Cause

`Guid.Parse` is a hard-parsing API that throws on invalid input. There is no validation between extracting the string value from `ExtendedPropertiesDictionary` and passing it to `Guid.Parse`.

---

## Suggested Fix

### Option A — Use `Guid.TryParse` with descriptive error (recommended)

```csharp
if (!Guid.TryParse(agenticIdentity.AgenticUserId, out Guid agenticUserId))
{
    throw new InvalidOperationException(
        $"AgenticUserId '{agenticIdentity.AgenticUserId}' is not a valid GUID.");
}

options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, agenticUserId);
```

### Option B — Validate at `AgenticIdentity.FromProperties` boundary

Add GUID format validation when constructing the `AgenticIdentity`, so that any `AgenticIdentity` instance with a non-null `AgenticUserId` is guaranteed to contain a valid GUID string:

```csharp
AgenticUserId = userIdObj?.ToString() is string uid && Guid.TryParse(uid, out _) ? uid : null
```

---

## Acceptance Criteria

- Malformed `AgenticUserId` values produce a clear, descriptive exception or are handled gracefully.
- No `FormatException` propagates from `Guid.Parse` in `BotAuthenticationHandler`.
- Valid GUID strings continue to work as before.
- Applied to both `BotAuthenticationHandler` and `KeyedBotAuthenticationHandler`.
