# Core Stabilization Plan

> Generated: 2026-04-11  
> Source: Audits 001–023 in this directory  
> Branch: `next/core-rido-audit`

---

## Step 1 — Duplicate / Overlap Analysis

Five consolidation groups were identified. Items within a group share a root cause or a single fix resolves all of them.

| ID | Merged Audits | Rationale |
|----|--------------|-----------|
| **SER-01** | 003 + 011 | Both target the same `ActivitySerializerMap` gap. Audit 011 is a downstream symptom of 003; fixing 003 eliminates 011. |
| **CAST-01** | 004 + 023 | Same anti-pattern: `as`-cast silently returns `null`. Audit 004 is in `TeamsActivity.cs` property shadowing; 023 is in entity files via `[JsonExtensionData]`. One workstream, two locations. |
| **COPY-01** | 013 + 015 | Both are shallow-copy bugs on mutable reference types. Fix together to establish a consistent deep-copy pattern. |
| **DI-01** | 005 + 006 | Same file (`JwtExtensions.cs`), same theme: DI container misuse and resource leaks. Fixing `BuildServiceProvider()` abuse also removes the scope that makes the `ConfigurationManager` leak possible. |
| **AUTH-01** | 017 + 018 | Same file (`BotAuthenticationHandler.cs`). Both crash on malformed external input on the hot request path. |

After consolidation: **23 raw audits → 18 work items.**

---

## Step 2 — Impact-Ranked Issue List

### Critical — Active production risk (data corruption / service outage)

| # | ID | Title | Affected File(s) |
|---|----|-------|-----------------|
| 1 | A-012 | Thread-safety race on `OnActivity` — concurrent requests corrupt each other's handler | `CompatAdapter.cs:57–66` |
| 2 | A-001 | Blocking `.GetAwaiter().GetResult()` in JWT key resolver — thread-pool starvation under load | `JwtExtensions.cs:222` |

### High — Silent data loss or crash on real inputs

| # | ID | Title | Affected File(s) |
|---|----|-------|-----------------|
| 3 | AUTH-01 (017+018) | Auth handler crashes on malformed GUID or token — takes down the whole request | `BotAuthenticationHandler.cs:99,108–113` |
| 4 | SER-01 (003+011) | Asymmetric serializer map — 6 of 8 activity subtypes lose all subtype fields on serialization | `TeamsActivityType.cs:83–103` |
| 5 | CAST-01 (004+023) | `as`-cast silently returns `null` for deserialized data — property reads always return `null` post-deserialization | `TeamsActivity.cs:108–142`, entity files |
| 6 | A-002 | Unsafe `(T)(object)` double-cast in HTTP deserializer — fragile runtime assumption, crashes on type mismatch | `BotHttpClient.cs:216,220` |
| 7 | DI-01 (005+006) | Multiple `BuildServiceProvider()` calls create duplicate singletons; `ConfigurationManager` never disposed | `JwtExtensions.cs`, `AddBotApplicationExtensions.cs`, `BotConfig.cs` |

### Medium — Correctness defects with narrower blast radius

| # | ID | Title | Affected File(s) |
|---|----|-------|-----------------|
| 8 | A-021 | Cross-framework JSON round-trip (Newtonsoft → STJ) drops fields silently in compat layer | `CompatTeamsInfo.cs:357–358` |
| 9 | A-016 | Direct `(Activity)` cast on `IActivity` — throws `InvalidCastException` for any non-`Activity` implementation | `CompatTeamsInfo.cs:452,484,516,543,582` |
| 10 | COPY-01 (013+015) | Shallow copy in `CoreActivity` and `CitationEntity` — mutations of a copy affect the original | `CoreActivity.cs:140–156`, `CitationEntity.cs:115–130` |
| 11 | A-014 | Builder exposes internal mutable ref — post-`Build()` mutations corrupt already-returned attachment | `TeamsAttachmentBuilder.cs:110` |
| 12 | A-008 | Unknown entity types silently discarded — any unrecognized entity type is dropped at deserialization | `Entity.cs:66–74` |
| 13 | A-007 | `object.ToString()` on `JsonElement` values produces type names, not data — silent string corruption in compat | `CompatActivity.cs:75–109,238–265` |
| 14 | A-009 | Fragile exception filter matches cancellation by message string — breaks on any SDK wording change | `TeamsStreamingWriter.cs:104–107` |

### Low — Quality / hygiene (no immediate production risk)

| # | ID | Title | Affected File(s) |
|---|----|-------|-----------------|
| 15 | A-010 | O(n²) string concatenation accumulator in streaming — degrades under high chunk counts | `TeamsStreamingWriter.cs:92` |
| 16 | A-020 | Plain `List<T>` for middleware — no enforcement that writes stop after startup | `TurnMiddleware.cs:21` |
| 17 | A-022 | Null-forgiving operator on nullable `ServiceUrl` — throws `NullReferenceException` if null | `CompatConversations.cs:123,203` |
| 18 | A-019 | `GC.SuppressFinalize` called on class with no finalizer — misleading boilerplate | `CompatConnectorClient.cs:38–42` |

---

## Step 3 — Phased Fix Plan

### Phase 1 — Stop the bleeding (Critical)

**Goal:** Eliminate active production risks. No new features until these are closed.

#### P1-1: Fix race condition on `OnActivity` (A-012)
- **File:** `core/src/Microsoft.Teams.Bot.Compat/CompatAdapter.cs`
- **Fix:** Replace the singleton `OnActivity` field with `AsyncLocal<Func<CoreActivity, CancellationToken, Task>>` scoped per-request, or refactor `ProcessActivityAsync` to accept the callback as a parameter and remove the field entirely.
- **Test:** Write a concurrent integration test that fires ≥10 simultaneous requests and asserts each request's handler is invoked exactly once on the correct activity.

#### P1-2: Fix blocking async in JWT key resolver (A-001)
- **File:** `core/src/Microsoft.Teams.Bot.Core/Hosting/JwtExtensions.cs`
- **Fix:** Move key fetching to an eagerly-started background refresh (`Lazy<Task<…>>` + `Timer`), or implement a custom `ISecurityTokenValidator` / `IOpenIdConnectConfigurationRetriever` that supports async. Remove all `.GetAwaiter().GetResult()` calls in the resolver delegate.
- **Test:** Load test JWT validation under 50 concurrent requests; assert no `ThreadPoolStarvationException` and p99 latency stays flat.

---

### Phase 2 — Data integrity (High)

**Goal:** Prevent silent data loss and request crashes.

#### P2-1: Fix auth handler crashes on malformed input (AUTH-01: 017 + 018)
- **Files:** `core/src/Microsoft.Teams.Bot.Core/Hosting/BotAuthenticationHandler.cs`, `core/src/Microsoft.Teams.Bot.Compat/KeyedBotAuthenticationHandler.cs`
- **Fix (017):** Replace `Guid.Parse(agenticUserId)` with `Guid.TryParse`; return `AuthenticateResult.Fail` with a descriptive message on invalid input.
- **Fix (018):** Wrap `new JwtSecurityToken(token)` in try/catch; replace deprecated `JwtSecurityToken` with `JsonWebToken`; log parse failure at trace level and continue (do not crash).

#### P2-2: Fix asymmetric serializer map (SER-01: 003 + 011)
- **File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/TeamsActivityType.cs`
- **Fix:** Add the 6 missing entries to `ActivitySerializerMap` to match `ActivityDeserializerMap`; register types in `TeamsActivityJsonContext`; add a CI unit test that asserts `ActivitySerializerMap.Count == ActivityDeserializerMap.Count` and that a round-trip of each type preserves all subtype fields.
- **Remove:** The silent fallback path in `ToJson()` (replace with `Debug.Fail` / production log).

#### P2-3: Fix `as`-cast silent null returns (CAST-01: 004 + 023)
- **Files:** `TeamsActivity.cs`, `CitationEntity.cs`, `MentionEntity.cs`, `OMessageEntity.cs`, `SensitiveUsageEntity.cs`
- **Fix (023/entity files):** Replace `[JsonExtensionData]`-backed property getters that use `as` with explicit `[JsonPropertyName]` properties (preferred), or add `JsonElement`-aware getters that deserialize on access and cache the result.
- **Fix (004/TeamsActivity):** Add null guards or assertions in shadowed property getters; enforce correct concrete type in the `Rebase()` path / setter so the `as` cast cannot fail at runtime.

#### P2-4: Fix unsafe generic cast (A-002)
- **File:** `core/src/Microsoft.Teams.Bot.Core/Http/BotHttpClient.cs`
- **Fix:** Extract a typed helper that checks `typeof(T)` explicitly and returns the string directly for `T == string`; use `JsonSerializer.Deserialize<T>()` for all other types. Remove the `(T)(object)` pattern entirely.

#### P2-5: Fix DI container misuse and resource leak (DI-01: 005 + 006)
- **Files:** `JwtExtensions.cs`, `AddBotApplicationExtensions.cs`, `BotConfig.cs`
- **Fix (005):** Remove all internal `BuildServiceProvider()` calls during registration. Pass `IConfiguration` explicitly through extension method parameters; fail fast with a clear `InvalidOperationException` if unavailable rather than building a throwaway container.
- **Fix (006):** Extract an `OidcConfigCache : IDisposable` singleton registered with the DI container. Move `ConfigurationManager<OpenIdConnectConfiguration>` instances into it so they are disposed with the application lifetime.

---

### Phase 3 — Correctness & reliability (Medium)

**Goal:** Eliminate narrower-scope correctness bugs before any public API stabilization.

#### P3-1: Fix cross-framework JSON round-trip (A-021)
- **File:** `core/src/Microsoft.Teams.Bot.Compat/CompatTeamsInfo.cs`
- **Fix:** Use a single serializer (STJ) for both legs of the conversion, or implement explicit manual mapping between the two object graphs. Add a round-trip fidelity integration test covering all known property names.

#### P3-2: Fix unsafe `IActivity` cast (A-016)
- **File:** `core/src/Microsoft.Teams.Bot.Compat/CompatTeamsInfo.cs`
- **Fix:** Replace the five direct `(Activity)` casts with `activity as Activity ?? throw new ArgumentException(...)`. Consider changing the public signature to accept `Activity` directly if `IActivity` is not meaningfully polymorphic here.

#### P3-3: Fix shallow copies (COPY-01: 013 + 015)
- **Files:** `CoreActivity.cs`, `CitationEntity.cs`
- **Fix (013):** In the `CoreActivity(CoreActivity)` copy constructor, deep-copy all mutable reference-type fields: `ChannelData` (re-serialize/deserialize or clone), `Entities` (new list + clone each), `Attachments`, `Properties`, `Value`.
- **Fix (015):** Deep-copy each `CitationClaim` in the `CitationEntity` clone, or convert `CitationClaim` to a `record` with `init`-only properties so shared references cannot be mutated.

#### P3-4: Fix builder mutable reference leak (A-014)
- **File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/TeamsAttachmentBuilder.cs`
- **Fix:** In `Build()`, return a copy of `_attachment` rather than the field itself. Alternatively, set a `_built` flag and throw `InvalidOperationException` on any subsequent builder call after `Build()` is called.

#### P3-5: Preserve unknown entity types (A-008)
- **File:** `core/src/Microsoft.Teams.Bot.Apps/Schema/Entities/Entity.cs`
- **Fix:** For unrecognized types, deserialize as base `Entity` (raw JSON preserved via `[JsonExtensionData]`) rather than returning `null`. Add trace logging for unknown type names to aid future registration.

#### P3-6: Fix `ToString()` on `JsonElement` values in compat (A-007)
- **File:** `core/src/Microsoft.Teams.Bot.Compat/CompatActivity.cs`
- **Fix:** Add a typed extraction helper that checks `value is JsonElement je ? je.GetString() : value?.ToString()` (with appropriate handling for non-string `JsonElement` kinds). Apply to all `object` dictionary read sites.

#### P3-7: Fix fragile stream-cancellation detection (A-009)
- **File:** `core/src/Microsoft.Teams.Bot.Apps/TeamsStreamingWriter.cs`
- **Fix:** Inspect the HTTP status code or a structured error code from the Teams API response instead of matching against the exception message string. Document the expected cancellation signal in a comment.

---

### Phase 4 — Quality & hygiene (Low)

**Goal:** Clean up low-risk items that accumulate tech debt.

#### P4-1: Replace O(n²) string accumulator with `StringBuilder` (A-010)
- **File:** `core/src/Microsoft.Teams.Bot.Apps/TeamsStreamingWriter.cs`
- **Fix:** Replace `_accumulated += chunk` with `_accumulated = new StringBuilder()` + `Append(chunk)`. Update all read sites to call `.ToString()`.

#### P4-2: Freeze middleware list after startup (A-020)
- **File:** `core/src/Microsoft.Teams.Bot.Core/TurnMiddleware.cs`
- **Fix:** Add a `Freeze()` method that converts `List<ITurnMiddleware>` to an array. Call `Freeze()` from the host's `IHostedService.StartAsync`. Guard read paths against post-freeze mutations via assertion.

#### P4-3: Add `ServiceUrl` null guard (A-022)
- **File:** `core/src/Microsoft.Teams.Bot.Compat/CompatConversations.cs`
- **Fix:** Add `ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl)` at the top of each affected method before the `new Uri(ServiceUrl!)` call.

#### P4-4: Remove misleading `GC.SuppressFinalize` (A-019)
- **File:** `core/src/Microsoft.Teams.Bot.Compat/CompatConnectorClient.cs`
- **Fix:** Delete `GC.SuppressFinalize(this)` and the referenced-but-nonexistent `Dispose(bool)` comment. Simplify `Dispose()` to a no-op with a comment explaining there is nothing to release.

---

## Summary Table

| Phase | Items | Severity | Gate |
|-------|-------|----------|------|
| 1 | P1-1, P1-2 | Critical | Must ship before any load-bearing traffic change |
| 2 | P2-1 → P2-5 | High | Must ship before public API stabilization |
| 3 | P3-1 → P3-7 | Medium | Must ship before `next/core` merges to `main` |
| 4 | P4-1 → P4-4 | Low | Can ship as clean-up PRs alongside other work |

**Total work items after consolidation: 18** (down from 23 raw audits, 5 merges applied).
