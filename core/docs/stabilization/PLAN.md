# Stabilization Audit — Consolidated Plan

## Duplicate / Overlap Analysis

| Relationship | Issues | Rationale |
|---|---|---|
| **Duplicate** | 003 + 011 | Both address the incomplete `ActivitySerializerMap`. 011 explicitly calls itself "related to but distinct from 003" but the fix is the same: populate the map and add a fallback diagnostic. **Consolidate into one work item.** |
| **Co-located** | 001 + 006 | Both live in `JwtExtensions.cs` and involve `ConfigurationManager<OpenIdConnectConfiguration>`. 001 is the blocking `.GetAwaiter().GetResult()` call; 006 is the missing `Dispose()`. A single refactor (extract an `OidcConfigCache` singleton with eager background refresh) resolves both. **Fix together.** |
| **Co-located** | 005 (partial) + 001 + 006 | One of 005's three `BuildServiceProvider()` call sites is in `JwtExtensions.cs` (line 324). The JWT refactor above can eliminate it. The other two sites (BotConfig.cs, AddBotApplicationExtensions.cs) are separate. |
| **Same pattern** | 004 + 023 | Both are silent `as`-cast → `null` returns. 004 is `TeamsActivity` property shadowing; 023 is entity properties after deserialization. Same root pattern but different files, different fixes. **Track separately but schedule in the same phase.** |
| **Same pattern** | 013 + 015 | Both are shallow-copy hazards. 013 is the `CoreActivity` copy constructor; 015 is `CitationEntity`. Independent fixes but same review theme. |
| **Same area** | 007 + 021 | Both are compat-layer type-conversion / serialization fidelity issues. 007 is `object.ToString()` on property bags; 021 is Newtonsoft→STJ cross-framework round-trip. **Schedule together for a compat-layer serialization sweep.** |

After consolidation: **22 unique work items** (003 + 011 merged).

---

## Priority List (by impact)

### Critical — Data corruption, deadlocks, or concurrency hazards in production

| # | Audits | Title | Component | Risk |
|---|--------|-------|-----------|------|
| C1 | 012 | Race condition on `OnActivity` delegate in `CompatAdapter` | Compat | Concurrent requests dispatch to wrong bot/handler; cross-request data leakage |
| C2 | 001 + 006 | Blocking async + undisposed `ConfigurationManager` in JWT key resolver | Core/Hosting | Thread-pool starvation & deadlock under load; resource leak on every OIDC authority |
| C3 | 023 | Entity property `as`-casts silently return `null` after JSON deserialization | Apps/Entities | `Citation`, `Mentioned`, `Pattern` are always `null` when deserialized from JSON — primary code path broken |
| C4 | 003 + 011 | Asymmetric serializer/deserializer maps — 6 of 8 activity types lose fields on `ToJson()` | Apps/Schema | Outbound replies, logging, and compat round-trips silently drop subtype-specific fields |

### High — Incorrect behavior, silent data loss, or DI misuse

| # | Audits | Title | Component | Risk |
|---|--------|-------|-----------|------|
| H1 | 005 | `BuildServiceProvider()` called 3× during startup | Core/Hosting | Duplicate singleton instances, resource leaks, ASP0000 warnings |
| H2 | 004 | Silent `as`-cast in `TeamsActivity` property shadowing | Apps/Schema | `From`, `Recipient`, `Conversation`, `ChannelData` return `null` if base type was set directly |
| H3 | 002 | Unsafe `(T)(object)` cast in generic HTTP response deserializer | Core/Http | `InvalidCastException` if type guard is bypassed; fragile pattern copied elsewhere |
| H4 | 021 | Cross-framework JSON serialization (Newtonsoft → STJ) | Compat | Silent property loss due to naming-convention / attribute mismatch between serializers |
| H5 | 008 | Unknown entity types silently discarded | Apps/Entities | Third-party & future entity types dropped with no log; incomplete bot behavior |

### Medium — Robustness, input validation, error handling

| # | Audits | Title | Component | Risk |
|---|--------|-------|-----------|------|
| M1 | 018 | Token parsing crash in logging path | Core/Hosting | Malformed JWT in `LogTokenClaims` crashes outgoing HTTP request |
| M2 | 017 | Unguarded `Guid.Parse` on external input | Core/Hosting + Compat | `FormatException` on malformed `AgenticUserId` fails API call |
| M3 | 013 | Shallow reference copy in `CoreActivity` copy constructor | Core/Schema | Mutating copy mutates original; `Rebase()` stomps shared `JsonArray` references |
| M4 | 016 | Unsafe direct cast `IActivity` → `Activity` in `CompatTeamsInfo` | Compat | `InvalidCastException` with no descriptive message for non-`Activity` implementations |
| M5 | 007 | Unvalidated `object.ToString()` on property bag values | Compat | `aadObjectId` etc. become `"System.Text.Json.JsonElement"` under STJ |
| M6 | 009 | Fragile exception filter by message string in streaming writer | Apps/Streaming | Exception message rewording causes unhandled `HttpRequestException` |

### Low — Code hygiene, minor perf, defensive improvements

| # | Audits | Title | Component | Risk |
|---|--------|-------|-----------|------|
| L1 | 014 | Builder `Build()` returns mutable internal reference | Apps/Schema | Post-build mutations leak back into builder; violated builder contract |
| L2 | 020 | Non-thread-safe middleware list in `TurnMiddleware` | Core | Race if middleware added after pipeline starts (unlikely but unguarded) |
| L3 | 010 | O(n²) string concatenation in streaming accumulator | Apps/Streaming | GC pressure under high-concurrency streaming; easy `StringBuilder` fix |
| L4 | 015 | Shallow clone of `CitationClaim` list | Apps/Entities | Mutating claim in copy mutates original (low likelihood) |
| L5 | 022 | Missing `ServiceUrl` null validation | Compat | `NullReferenceException` instead of descriptive `ArgumentException` |
| L6 | 019 | Unnecessary `GC.SuppressFinalize` without finalizer | Compat | Misleading code; no runtime impact |

---

## Phased Fix Plan

### Phase 1 — Critical concurrency & data-integrity fixes
**Goal:** Eliminate production-affecting bugs that corrupt data or cause deadlocks.  
**Scope:** 4 work items (6 audits).

| Work Item | Audits | What to Do | Files |
|-----------|--------|------------|-------|
| **1.1** | 012 | Replace mutable `OnActivity` delegate assignment with `AsyncLocal<>` per-request scoping (Option A) or per-request callback parameter (Option B). | `CompatAdapter.cs` |
| **1.2** | 001 + 006 | Extract `OidcConfigCache` singleton (owns cache + implements `IDisposable`). Background-refresh signing keys. Remove `.GetAwaiter().GetResult()` from resolver. Remove one `BuildServiceProvider()` call site (005 partial). | `JwtExtensions.cs` |
| **1.3** | 023 | In entity property getters, detect `JsonElement` and call `.Deserialize<T>()` on access. Cache result back into `Properties`. Apply to `CitationEntity`, `MentionEntity`, `OMessageEntity`, `SensitiveUsageEntity`. | `CitationEntity.cs`, `MentionEntity.cs`, `OMessageEntity.cs`, `SensitiveUsageEntity.cs` |
| **1.4** | 003 + 011 | Add missing 6 entries to `ActivitySerializerMap`. Register types in `TeamsActivityJsonContext`. Add symmetry assertion test. Add `Debug.Fail` on fallback path. | `TeamsActivityType.cs`, `TeamsActivity.cs`, `TeamsActivityJsonContext.cs` |

**Validation:** Run all existing unit + integration tests. Add concurrency stress test for 1.1. Add round-trip serialization tests for 1.3 and 1.4.

---

### Phase 2 — High-severity correctness & DI hygiene
**Goal:** Fix silent data loss, DI anti-patterns, and type-safety gaps.  
**Scope:** 5 work items.

| Work Item | Audits | What to Do | Files |
|-----------|--------|------------|-------|
| **2.1** | 005 | Replace remaining 2 `BuildServiceProvider()` calls: extract `IConfiguration` from `ServiceDescriptor.ImplementationInstance` or throw. Remove `GetLoggerFromServices` temp-provider fallback. | `BotConfig.cs`, `AddBotApplicationExtensions.cs` |
| **2.2** | 004 | Add `Debug.Assert` in each shadowed getter (Option A). Optionally add auto-upgrade in `Rebase()` for plain `ConversationAccount` → `TeamsConversationAccount`. | `TeamsActivity.cs` |
| **2.3** | 002 | Replace `(T)(object)` with `Unsafe.As<string, T>` or extract `ReturnRawString<T>` helper. Add unit test for plain-text HTTP response. | `BotHttpClient.cs` |
| **2.4** | 021 + 007 | **Compat serialization sweep.** (a) Replace cross-framework round-trip in `SendMeetingNotificationAsync` with single-serializer path or manual mapping. (b) Add `ExtractStringProperty` helper for property-bag `.ToString()` calls. | `CompatTeamsInfo.cs`, `CompatActivity.cs` |
| **2.5** | 008 | Change unknown-entity fallback from `null` to `item.Deserialize<Entity>(options)` so unrecognized types are preserved. Add trace log. | `Entity.cs` |

**Validation:** Verify no ASP0000 warnings. Add tests for each compat serialization path. Add test for unknown entity type preservation.

---

### Phase 3 — Robustness & input validation
**Goal:** Harden error handling and input validation at system boundaries.  
**Scope:** 6 work items.

| Work Item | Audits | What to Do | Files |
|-----------|--------|------------|-------|
| **3.1** | 018 | Wrap `LogTokenClaims` in try-catch. Replace deprecated `JwtSecurityToken` with `JsonWebToken`. | `BotAuthenticationHandler.cs` |
| **3.2** | 017 | Replace `Guid.Parse` with `Guid.TryParse` + descriptive error in both handlers. | `BotAuthenticationHandler.cs`, `KeyedBotAuthenticationHandler.cs` |
| **3.3** | 013 | Deep-copy mutable references (`ChannelData`, `Entities`, `Attachments`, `Properties`, `Value`) in `CoreActivity` copy constructor. | `CoreActivity.cs` |
| **3.4** | 016 | Replace direct cast `(Activity)activity` with safe cast + `ArgumentException`. Apply to all 5 call sites. | `CompatTeamsInfo.cs` |
| **3.5** | 009 | Extract magic string to constant. Centralize `IsCancellationByUser()` helper. Add fallback log for unmatched `HttpRequestException`. | `TeamsStreamingWriter.cs` |
| **3.6** | 022 | Add `ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl)` to 2 methods. | `CompatConversations.cs` |

**Validation:** Add tests for malformed GUIDs, malformed tokens, null ServiceUrl, non-Activity IActivity.

---

### Phase 4 — Code hygiene & minor improvements
**Goal:** Clean up low-risk issues, improve performance, enforce patterns.  
**Scope:** 5 work items.

| Work Item | Audits | What to Do | Files |
|-----------|--------|------------|-------|
| **4.1** | 010 | Replace `string +=` with `StringBuilder` in streaming accumulator. | `TeamsStreamingWriter.cs` |
| **4.2** | 014 | Return a defensive copy from `TeamsAttachmentBuilder.Build()`. | `TeamsAttachmentBuilder.cs` |
| **4.3** | 020 | Freeze middleware list after first pipeline execution. Throw on late `Use()` calls. | `TurnMiddleware.cs` |
| **4.4** | 015 | Deep-copy `CitationClaim` list or make `CitationClaim` a record with `init`-only setters. | `CitationEntity.cs` |
| **4.5** | 019 | Remove `GC.SuppressFinalize` and misleading comment from `CompatConnectorClient.Dispose()`. | `CompatConnectorClient.cs` |

**Validation:** Run full test suite. Confirm no behavioral changes.

---

## Summary

| Phase | Items | Audits Covered | Focus |
|-------|-------|----------------|-------|
| 1 | 4 | 001, 003, 006, 011, 012, 023 | Critical concurrency & data integrity |
| 2 | 5 | 002, 004, 005, 007, 008, 021 | Correctness & DI hygiene |
| 3 | 6 | 009, 013, 016, 017, 018, 022 | Robustness & validation |
| 4 | 5 | 010, 014, 015, 019, 020 | Code hygiene & perf |
| **Total** | **20** | **23 audits → 22 unique → 20 work items** | |
