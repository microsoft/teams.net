# Audit Issue 018: Token Parsing in Logging Path Can Crash Requests

**Severity:** Medium  
**File:** `core/src/Microsoft.Teams.Bot.Core/Hosting/BotAuthenticationHandler.cs`  
**Lines:** 108–113  
**Category:** Error handling / Logging safety

---

## Problem

`LogTokenClaims` constructs a `JwtSecurityToken` to extract claims for trace-level logging:

```csharp
private void LogTokenClaims(string token)
{
    if (!_logger.IsEnabled(LogLevel.Trace))
    {
        return;
    }

    JwtSecurityToken jwtToken = new(token);
    string claims = Environment.NewLine + string.Join(
        Environment.NewLine,
        jwtToken.Claims.Select(c => $"  {c.Type}: {c.Value}"));
    _logTokenClaims(_logger, claims, null);
}
```

Two issues:

1. **Crash risk:** If `token` is malformed (e.g., truncated, corrupted by a middleware, or not actually a JWT), `new JwtSecurityToken(token)` throws `ArgumentException` or `SecurityTokenMalformedException`. This exception propagates out of `LogTokenClaims` and fails the entire `SendAsync` pipeline — meaning a **logging side-effect crashes the outgoing HTTP request**.

2. **Deprecated API:** `JwtSecurityToken` (from `System.IdentityModel.Tokens.Jwt`) is deprecated in favor of `JsonWebToken` (from `Microsoft.IdentityModel.JsonWebTokens`). The deprecated class has known performance and correctness issues.

---

## Root Cause

The logging method does not have a try-catch guard around token parsing. Since the method is called for every outgoing authenticated request when trace logging is enabled, any token parsing failure becomes a request failure.

---

## Suggested Fix

### Option A — Wrap in try-catch (minimal, recommended)

```csharp
private void LogTokenClaims(string token)
{
    if (!_logger.IsEnabled(LogLevel.Trace))
    {
        return;
    }

    try
    {
        JsonWebToken jwtToken = new(token);
        string claims = Environment.NewLine + string.Join(
            Environment.NewLine,
            jwtToken.Claims.Select(c => $"  {c.Type}: {c.Value}"));
        _logTokenClaims(_logger, claims, null);
    }
    catch (Exception ex)
    {
        _logger.LogTrace("Failed to parse token for logging: {Error}", ex.Message);
    }
}
```

### Option B — Use `JsonWebTokenHandler.ReadJsonWebToken` with validation

```csharp
JsonWebTokenHandler handler = new();
if (handler.CanReadToken(token))
{
    JsonWebToken jwtToken = handler.ReadJsonWebToken(token);
    // log claims
}
```

---

## Acceptance Criteria

- A malformed token does not cause `LogTokenClaims` to throw.
- Trace logging still outputs claims for valid tokens.
- `JwtSecurityToken` is replaced with `JsonWebToken`.
- No request failures caused by logging side-effects.
