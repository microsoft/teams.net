# OAuthFlow Design & Implementation Review

**Date:** 2026-06-11
**Scope:** OAuthFlow design doc, implementation code, samples (OAuthFlowBot, SsoBot), trace summaries, security audit
**Branch:** `feature/turn-state`

---

## Overall Assessment

The design is well-documented and the implementation is solid for the happy path. The design doc is unusually thorough — the breaking-changes table, the trace summaries, and the security audit are excellent artifacts. That said, there are issues at several levels: **architectural gaps, implementation bugs, API ergonomics problems, and doc/code inconsistencies**.

---

## 1. DESIGN ISSUES

### 1.1 `signin/failure` fires on ALL flows — wrong semantics

**Design doc:** "fires `OnSignInFailure` on **all** registered flows (since the invoke carries no connection name)."

The implementation in `OAuthFlowExtensions.cs` tries to be smarter — it checks `HasPendingSignIn` first and falls back to all flows. But this is fragile:

- The pending-sign-in tracking is in-memory by default. In a multi-instance deployment, the instance receiving `signin/failure` likely has no pending state, so it **always falls back to all flows**.
- Even with distributed state, a `signin/failure` after a `login` command for GitHub will fire `OnSignInFailure` on the Graph flow too if both happen to have pending sign-ins (user clicked "login" for both).
- The user sees **two** failure messages — one per flow — for a single SSO failure. This is a UX bug.

**Fix:** Fire only on the **most recently initiated** flow by tracking timestamps in pending state. Fall back to all flows only when no pending flow is found.

### 1.2 `IsSignedIn` sync-over-async is a production hazard

`Context.cs` does `.GetAwaiter().GetResult()` against a remote HTTP call to `token.botframework.com`. This blocks a thread-pool thread for hundreds of milliseconds (the trace shows 214-568ms). Under load, this causes thread-pool starvation.

It's marked `[Obsolete]`, which is good, but it's still in the public API and the design doc suggests it for "backwards-compat, single connection only."

**Fix:** Replace with `throw new NotSupportedException()` pointing to `IsSignedInAsync`.

### 1.3 `verifyState` tries ALL flows sequentially — O(N) token service calls

`OAuthFlowExtensions.cs` iterates every registered flow and calls `GetTokenAsync(code: state)` on each. With 5 OAuth connections, the user waits for up to 5 sequential HTTP calls to the token service (~300ms each = 1.5s).

**Fix:** Try the flow with a pending sign-in first. Only fall back to iterating all flows if no pending flow is found. This makes the common case O(1).

### 1.4 No token caching at the context level

Every call to `context.SignIn()`, `context.IsSignedInAsync()`, or `flow.GetTokenAsync()` makes a fresh HTTP call to `token.botframework.com`. The SsoBot sample's `profile` handler calls `context.SignIn()` — if the user is already signed in, this is a ~200ms round-trip to the token service on every single message.

**Fix:** Cache the `GetTokenResult` per-turn per-connection on the `Context`. Return it on subsequent calls within the same turn.

### 1.5 Group chat handling removed with no migration path

The design doc acknowledges this and says "the developer must create the 1:1 conversation manually." But there's no sample showing how and no helper method. This is a regression that will silently break group-chat bots that upgrade. (Not fixed in this PR — needs a separate design decision.)

---

## 2. IMPLEMENTATION BUGS

### 2.1 Dedup clears on completion — defeats the purpose

`OAuthFlow.cs` clears the conversation-state dedup key immediately after the first exchange completes. But the duplicate exchange from a second Teams endpoint may arrive **after** the first completes. The state key is already gone, so the second exchange is treated as new.

**Fix:** Don't clear the dedup key on completion. Let it expire naturally via the 5-min TTL.

### 2.2 Logger is always `NullLogger`

`OAuthFlowExtensions.cs:GetLogger()` always returns `NullLogger.Instance`. All `_logger.LogDebug(...)` and `_logger.LogWarning(...)` calls in `OAuthFlow.cs` go nowhere.

**Fix:** Resolve `ILoggerFactory` from the `TeamsBotApplication` and create a proper `ILogger<OAuthFlow>`.

### 2.3 `_pendingSignIns` cleanup only runs in `IsDuplicateExchange`

`CleanupExpiredEntries()` is only called from `IsDuplicateExchange`. If `HasPendingSignIn` is called without any exchange happening, stale entries persist indefinitely.

**Fix:** Also run cleanup in `HasPendingSignIn`.

### 2.4 `verifyState` returns 400 when no flow matches — should be 404

`OAuthFlowExtensions.cs` returns `new InvokeResponse(400)` but the design doc says "No registered flow matched → returns 404."

**Fix:** Change to 404.

---

## 3. API ERGONOMICS (not addressed in this PR)

- Two parallel APIs (`context.SignIn()` and `flow.SignInAsync(context)`) doing the same thing
- `GetConnectionStatusAsync` is on `OAuthFlow` but returns all connections regardless of which flow
- `OAuthOptions.ConnectionName` is nullable but required in different contexts

---

## 4. DOC vs CODE INCONSISTENCIES

| Doc says | Code does |
|---|---|
| Namespace `Microsoft.Teams.Apps.Auth` | Actual namespace is `Microsoft.Teams.Apps.OAuth` |
| Files in `Auth/OAuthFlow.cs` | Files are in `OAuth/OAuthFlow.cs` |
| `verifyState` no-match returns 404 | Returns 400 |
| `Context.IsSignedIn` — no mention of deprecation | Marked `[Obsolete]` |

---

## 5. SAMPLE ISSUES

### OAuthFlowBot
- `login` command sends two OAuthCards when neither connection has a cached token
- `graphAuth.GetConnectionStatusAsync(context, ct)` returns all connections but reads as "Graph only"
- `using HttpClient http = new()` creates a new HttpClient per request

### SsoBot
- Connection name `"sso"` in code vs `"GraphConnection"` in the doc comment — mismatch

---

## 6. PRIORITY FIXES (this PR)

| Priority | Issue | Type |
|---|---|---|
| **P0** | Logger is always NullLogger — all OAuth logs are dead | Bug |
| **P0** | Dedup clears on completion, allowing late duplicates through | Bug |
| **P1** | `verifyState` returns 400 instead of 404 | Bug |
| **P1** | `signin/failure` fires on all flows — duplicate UX messages | Design |
| **P1** | `verifyState` iterates all flows sequentially (O(N) HTTP calls) | Perf |
| **P1** | No per-turn token caching — redundant HTTP calls | Perf |
| **P2** | `IsSignedIn` sync-over-async — throw instead | API |
| **P2** | OAuthFlowBot `login` sends two OAuthCards | Sample |
| **P2** | Doc namespace/path mismatches | Doc |
| **P2** | `_pendingSignIns` cleanup only runs in `IsDuplicateExchange` | Bug |
