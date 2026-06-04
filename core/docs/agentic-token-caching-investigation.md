# Agentic Token Caching Investigation

> **Date**: 2026-06-03
> **Context**: An AppInsights trace shows 4 HTTP calls to `login.microsoftonline.com` per single incoming message, adding ~1s of latency.

## Problem Statement

When processing a single agentic message, `BotAuthenticationHandler` acquires tokens for `https://botapi.skype.com/.default` via the agentic (User FIC / ROPC) flow. MSAL goes to the Entra network endpoint **on every outbound HTTP request**, even though the token is still valid. The in-memory cache is never consulted for the final token exchange.

In a single message turn with 4 total Entra round-trips, 2 are for the outbound Bot Framework API calls (typing + reply). The table below shows those 2 calls as a partial example:

| Call | Target | Duration | Token Source |
|------|--------|----------|-------------|
| Typing indicator | `login.microsoftonline.com` | 272ms | IdentityProvider |
| Reply message | `login.microsoftonline.com` | 246ms | IdentityProvider |

The intermediate `api://AzureAdTokenExchange/.default` tokens (FIC client credentials) ARE cached. The final user-scoped token is NOT.

## Root Cause

The issue is in how `BotAuthenticationHandler` calls `IAuthorizationHeaderProvider.CreateAuthorizationHeaderAsync()` with a **null ClaimsPrincipal**. This prevents MSAL's silent token flow from ever executing.

### Call Chain

```
BotAuthenticationHandler.SendAsync()
  -> GetAuthorizationHeaderAsync(agenticIdentity)
    -> options.WithAgentUserIdentity(appId, userId)     // sets ExtraParameters
    -> _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
         scopes, options, claimsPrincipal: null, ct)    // <-- NULL
```

### Inside microsoft-identity-web

**`DefaultAuthorizationHeaderProvider.CreateAuthorizationHeaderAsync()`**
File: `src/Microsoft.Identity.Web.TokenAcquisition/DefaultAuthorizationHeaderProvider.cs:68-110`

Routes to `_tokenAcquisition.GetAuthenticationResultForUserAsync()` passing the null ClaimsPrincipal through.

**`TokenAcquisition.TryGetAuthenticationResultForConfidentialClientUsingRopcAsync()`**
File: `src/Microsoft.Identity.Web.TokenAcquisition/TokenAcquisition.cs:398-530`

The silent flow check at line ~440:
```csharp
if (!forceRefresh && user != null && user.GetMsalAccountId() != null)
{
    var account = await application.GetAccountAsync(user.GetMsalAccountId());
    return await application.AcquireTokenSilent(scopes, account).ExecuteAsync();
}
```

**`user` is always null** -> silent flow never executes -> falls through to ROPC:

```csharp
AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder =
    ((IByUsernameAndPassword)application)
    .AcquireTokenByUsernamePassword(scopes, username, password);
```

After the ROPC call succeeds (lines 516-527), the account ID is written back to the ClaimsPrincipal:
```csharp
if (user != null && user.GetMsalAccountId() == null)
{
    user.AddIdentity(new CaseSensitiveClaimsIdentity(...));
}
```

But since `user` is null, this never executes either. Even if it did, the ClaimsPrincipal is request-scoped and discarded after each call.

### Why App-Only Tokens Work

`CreateAuthorizationHeaderForAppAsync()` uses `AcquireTokenForClient()`, which:
- Does NOT require a ClaimsPrincipal
- Uses application-level caching keyed by `clientId + scope + tenant`
- Tokens are cached globally for the process lifetime

### Why Agentic Tokens Don't Cache

- The agentic flow requires user identification (agent OID + agent app ID)
- MSAL's user token cache requires a persistent account ID from the ClaimsPrincipal
- Without it, MSAL cannot look up the cached token
- Each request appears as a "new user" to MSAL

## Impact per Message Turn

For a bot with agentic identity, a single message generates:

1. **Send typing indicator** -> `auth.outbound` (278ms): ROPC hits Entra
2. **App code calls 2 APIs** (MCP scopes) -> ROPC hits Entra for each new scope
3. **Send reply message** -> `auth.outbound` (250ms): ROPC hits Entra again

Total: **~500-1000ms of unnecessary Entra round-trips** per message, on top of the LLM latency.

## Possible Fixes

### Option A: Persist ClaimsPrincipal with Account ID in BotAuthenticationHandler

Cache the MSAL account ID after the first successful token acquisition, keyed by `(agenticAppId, agenticUserId)`. On subsequent calls, construct a ClaimsPrincipal with the cached account ID and pass it to `CreateAuthorizationHeaderAsync()`.

```csharp
// Pseudocode
private readonly ConcurrentDictionary<(string appId, string userId), ClaimsPrincipal> _accountCache = new();

private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken ct)
{
    // ... existing setup ...

    // Try to get cached ClaimsPrincipal with account ID
    var key = (agenticIdentity.AgenticAppId, agenticIdentity.AgenticUserId);
    _accountCache.TryGetValue(key, out ClaimsPrincipal? cachedPrincipal);

    // Pass the cached principal (or null on first call)
    string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
        [AgenticScope], options, cachedPrincipal, ct);

    // After first call, cache the principal for next time
    // (Need to capture the account ID from the result somehow)
    return token;
}
```

**Problem**: `CreateAuthorizationHeaderAsync` returns a string (the header value), not the `AuthenticationResult`. We don't get back the account ID to cache. This would require changes to microsoft-identity-web or using a lower-level API.

### Option B: Use ITokenAcquisition Directly with Account Caching

Instead of going through `IAuthorizationHeaderProvider`, use `ITokenAcquisition.GetAuthenticationResultForUserAsync()` directly and manage the ClaimsPrincipal lifecycle ourselves.

```csharp
private readonly ConcurrentDictionary<string, ClaimsPrincipal> _userPrincipalCache = new();

// After first ROPC call, the ClaimsPrincipal gets the account ID populated
// Cache it and reuse on subsequent calls
```

**Problem**: The account ID is populated on the ClaimsPrincipal by `TokenAcquisition` internally, but only if `user != null`. We'd need to pass a non-null (possibly empty) ClaimsPrincipal.

### Option C: Pass Non-Null Empty ClaimsPrincipal (Simplest Fix)

The simplest change: pass a **non-null but empty** `ClaimsPrincipal` and cache it per agent identity. The TokenAcquisition code will:
1. First call: `user != null` but `user.GetMsalAccountId() == null` -> skip silent, do ROPC, then populate the account ID on the ClaimsPrincipal
2. Second call (same ClaimsPrincipal instance): `user != null` AND `user.GetMsalAccountId() != null` -> **silent flow succeeds**, cache hit!

```csharp
private readonly ConcurrentDictionary<string, ClaimsPrincipal> _principalCache = new();

private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken ct)
{
    // ... existing setup ...

    string cacheKey = $"{agenticIdentity.AgenticAppId}:{agenticIdentity.AgenticUserId}";
    ClaimsPrincipal principal = _principalCache.GetOrAdd(cacheKey, _ => new ClaimsPrincipal());

    string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
        [AgenticScope], options, principal, ct);

    return token;
}
```

**This is the most promising approach** because:
- Minimal code change (just in `BotAuthenticationHandler`)
- Uses MSAL's built-in silent flow / cache lookup
- No changes to microsoft-identity-web needed
- The cached ClaimsPrincipal persists across requests for the lifetime of the handler instance

**Risks (and how the shipped implementation mitigates them)**:
- `BotAuthenticationHandler` is created per `IHttpClientFactory` handler chain. Handler chains are pooled and rotated by `IHttpClientFactory` based on `HandlerLifetime` (default ~2 minutes), so the in-handler cache is bounded by that rotation rather than the process lifetime. This is acceptable because the dominant cost — intra-turn round-trips — is eliminated.
- Multi-user scenarios: the cache key must include the user OID to avoid cross-user token leaks. The shipped key is `"{agenticAppId}:{agenticUserGuid:D}"`.
- Memory growth: a naïve `ConcurrentDictionary` would grow without bound. The shipped implementation uses `MemoryCache` with `SizeLimit = 10_000` and a 1-hour sliding expiry, plus a post-eviction callback that removes the matching per-key `SemaphoreSlim` from `_agenticLocks` so both collections stay bounded together.
- Concurrent mutation of a shared `ClaimsPrincipal` (MSAL writes the account ID back via `user.AddIdentity(...)`) is not thread-safe. The shipped implementation serialises requests for the same identity through a per-key `SemaphoreSlim`; requests for different identities still run concurrently.

### Option D: File Issue with microsoft-identity-web

The ROPC flow in `TokenAcquisition.TryGetAuthenticationResultForConfidentialClientUsingRopcAsync()` should handle the `user == null` case better for agent identities. It has enough information from `ExtraParameters` (agent app ID + user OID) to construct a cache key and attempt silent acquisition without a ClaimsPrincipal.

This is the "correct" long-term fix but requires upstream changes.

## Recommendation

**Start with Option C** — cache ClaimsPrincipal per `(agenticAppId, agenticUserId)` in `BotAuthenticationHandler`. Validate that the silent flow kicks in on the second call within the same turn. This should eliminate 3 of the 4 Entra round-trips per message (the first call will still hit the network, but subsequent calls within the same turn and across turns will use the cache).

**Also file an issue with microsoft-identity-web** (Option D) for the proper long-term fix.

## Implementation

The shipped fix in `BotAuthenticationHandler.cs` adopts Option C with the following concrete design:

| Concern | Decision |
|---------|----------|
| Principal storage | `MemoryCache` keyed by `"{agenticAppId}:{agenticUserGuid:D}"`, `SizeLimit = 10_000`, `SlidingExpiration = 1h`, `Size = 1` per entry. |
| Per-identity serialisation | `ConcurrentDictionary<string, SemaphoreSlim>` (`_agenticLocks`). Each `SendAsync` for a given identity acquires the matching semaphore before reading/mutating the cached `ClaimsPrincipal`. |
| Bounding `_agenticLocks` | A `PostEvictionCallback` on every cache entry calls `_agenticLocks.TryRemove(key, …)` so the two collections shrink together. The semaphore is **not** disposed in the callback — an in-flight request may still hold it; GC reclaims it once all references release. |
| Handler-level disposal | `Dispose(disposing)` disposes the cache then iterates and disposes any remaining semaphores. |
| `IHttpClientFactory` lifecycle | Handler chains are pooled by `IHttpClientFactory` and rotated based on `HandlerLifetime` (default ~2 minutes). The cache therefore lives at most for that window, which is sufficient to collapse the per-turn redundant round-trips that motivated this change. |

Verified by `BotAuthenticationHandlerTests`:
- Same identity reuses the same `ClaimsPrincipal` across calls.
- Concurrent calls for the same identity are serialised; different identities run concurrently.
- Cache eviction removes the matching lock entry from `_agenticLocks` and a subsequent call still succeeds.
- An in-flight call holding a semaphore is not disrupted by concurrent eviction (no `ObjectDisposedException`).
- `Dispose` cleans up remaining semaphores and the cache.

## Key Files

| File | Role |
|------|------|
| `src/Microsoft.Teams.Core/Hosting/BotAuthenticationHandler.cs` | Where the fix goes — pass cached ClaimsPrincipal instead of null |
| `microsoft-identity-web/src/.../TokenAcquisition.cs:440` | Silent flow guard: `user != null && user.GetMsalAccountId() != null` |
| `microsoft-identity-web/src/.../TokenAcquisition.cs:465-514` | ROPC fallback that always executes today |
| `microsoft-identity-web/src/.../TokenAcquisition.cs:516-527` | Account ID written to ClaimsPrincipal (only if non-null) |
| `microsoft-identity-web/src/.../AgentUserIdentityMsalAddIn.cs` | Transforms ROPC into User FIC grant type |
| `microsoft-identity-web/src/.../AgentIdentitiesExtension.cs:54-92` | `WithAgentUserIdentity()` sets ExtraParameters |
