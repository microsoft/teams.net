# Audit Issue 001: Blocking Async Call in JWT Signing-Key Resolver

**Severity:** Critical  
**File:** `core/src/Microsoft.Teams.Bot.Core/Hosting/JwtExtensions.cs`  
**Line:** 222  
**Category:** Async/Await correctness

---

## Problem

Inside the `IssuerSigningKeyResolver` delegate — which is invoked synchronously by the ASP.NET Core JWT middleware during token validation — there is a blocking call:

```csharp
// JwtExtensions.cs, line 222
OpenIdConnectConfiguration config = manager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
```

`IssuerSigningKeyResolver` is a synchronous `Func<>` callback. Because ASP.NET Core uses a thread-pool-based async pipeline, calling `.GetAwaiter().GetResult()` here risks **thread-pool starvation**: the thread that calls `GetResult()` is blocked waiting for the async operation to complete, but that operation may itself need a thread-pool thread to continue, leading to a deadlock or severe latency degradation under load.

Additionally, `CancellationToken.None` is passed — if the HTTP fetch for the OIDC metadata hangs, there is no timeout or cancellation path.

---

## Root Cause

`TokenValidationParameters.IssuerSigningKeyResolver` is defined as:

```csharp
IssuerSigningKeyResolver = (string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
    => IEnumerable<SecurityKey>
```

This is a **synchronous** delegate type. There is no async equivalent in the `Microsoft.IdentityModel.Tokens` library for this callback, so an async fetch must be run synchronously here. The current approach is a common workaround that is known to be unsafe in production ASP.NET Core applications.

---

## Suggested Fix Plan

### Option A — Cache keys eagerly via background refresh (preferred)

Pre-fetch and cache the signing keys in a `Lazy<Task<IEnumerable<SecurityKey>>>` or a background `Timer`, so the synchronous resolver can return from an already-populated in-memory cache without ever blocking:

1. Replace the `ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>>` with a `ConcurrentDictionary<string, Lazy<IEnumerable<SecurityKey>>>`.
2. On first access, start the async fetch on a **background task** (fire-and-forget with error handling). Return an empty key set (authentication will fail-fast) until the cache warms up.
3. Schedule periodic refresh (e.g., every 24 hours) to pick up key rotations.
4. The resolver reads from the cache synchronously — no blocking calls.

### Option B — Use `ConfigurationManager<T>` with synchronous HTTP (acceptable interim)

The `ConfigurationManager<T>` internally uses `HttpDocumentRetriever`. Replace it with a synchronous HTTP call using `HttpClient.Send()` (not `SendAsync`) so the blocking is explicit and understood, rather than disguised as async:

```csharp
// Temporary mitigation while a full async cache is implemented.
// Clearly mark this with a TODO comment and a work item.
var httpClient = new HttpClient();
var json = httpClient.GetStringAsync(authority).GetAwaiter().GetResult(); // Known blocking call
```

This does not fix starvation but makes the intent explicit and avoids nested async context issues.

### Option C — Switch to a custom `ISecurityTokenValidator` (cleanest long-term)

Implement a custom `JwtBearerOptions.SecurityTokenValidators` entry or override `OnTokenValidated` to perform async key resolution before the synchronous validator runs, caching keys into `TokenValidationParameters.IssuerSigningKeys` on each request.

---

## Acceptance Criteria

- No `.GetAwaiter().GetResult()` or `.Result` on a `Task` inside the `IssuerSigningKeyResolver` delegate.
- Signing-key resolution does not block a thread-pool thread under concurrent load.
- Key refresh still happens — stale keys do not cause permanent authentication failures.
- Existing integration tests for JWT validation continue to pass.
