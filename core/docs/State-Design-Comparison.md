# State Management — Ecosystem Comparison & Design Verification

Companion to [State-Design.md](State-Design.md). This document compares the proposed `TurnState` design against the three Microsoft frameworks in this lineage, then verifies whether the proposed design is the right fit for this codebase and calls out three refinements.

## Frameworks compared

| | Status | Persisted-state model |
|---|---|---|
| **Bot Framework SDK v4** (`botbuilder-dotnet`) | Archived Dec 2025 | `BotState` (ConversationState / UserState / PrivateConversationState) + `IStatePropertyAccessor<T>` over `IStorage` |
| **Teams AI Library v1** (`microsoft/teams-ai`, tag `js-1.7.4`) | Superseded by Teams SDK v2 | Unified `TurnState` with `conversation` / `user` / `temp` scopes, built on Bot Framework's `IStorage` |
| **Microsoft 365 Agents SDK** (`Microsoft.Agents.*`) | Current / recommended successor | `TurnState` (`ConversationState` / `UserState` / `TempState`, `PrivateConversationState` on-demand) over a modernized `IStorage` |
| **Proposed (this repo)** | Design | `TurnState` (`Conversation` / `User` / `Temp`) over Core `IStorage`, source-generated JSON |

The Agents SDK is the modern consolidation: it keeps Bot Framework's `IStorage`/key scheme, adopts Teams AI's unified `TurnState` + `temp` scope + `GetValue`/`SetValue` ergonomics, and switches serialization to System.Text.Json. That convergence is the most relevant baseline — the proposed design should align with where the ecosystem *landed*, not where it started.

## Side-by-side

| Dimension | Bot Framework v4 | Teams AI v1 | Agents SDK | **Proposed** |
|---|---|---|---|---|
| **Scopes** | Conversation, User, PrivateConversation — three separate `BotState` objects | conversation, user, temp — one `TurnState` | Conversation, User, Temp (Private on-demand) | Conversation, User, Temp |
| **Conversation key** | `{channelId}/conversations/{conversationId}` | `{channelId}/{botId}/conversations/{conversationId}` | `{channelId}/conversations/{conversationId}` | `{channelId}/conversations/{conversationId}` ✅ matches BF/Agents |
| **User key** | `{channelId}/users/{userId}` | `{channelId}/{botId}/users/{userId}` | `{channelId}/users/{userId}` | `{channelId}/users/{fromId}` ✅ matches BF/Agents |
| **`IStorage` shape** | `Task<IDictionary<string,object>> ReadAsync(string[] keys)`, `WriteAsync(IDictionary<string,object>)`, `DeleteAsync(string[])` | same (reuses BF `IStorage`) | same **+ typed generic overloads** `ReadAsync<T>` / `WriteAsync<T>` | `ReadAsync(keys)→IReadOnlyDictionary<string,StoreItem>`, `WriteAsync`, `DeleteAsync` — `StoreItem { Values, ETag }` |
| **Concurrency** | `IStoreItem.ETag` optimistic; last-write-wins by default | inherited from provider | `IStoreItem.ETag` in contract, but **state layer bypasses it** (hash instead) | `ETag` reserved on `StoreItem`; state layer last-write-wins — matches Agents SDK |
| **Change detection** | hash of serialized cache (`CachedBotState`) | hash per scope (`JSON.stringify` at load vs save) | hash per scope | hash per scope (load vs save) ✅ matches ecosystem |
| **Load trigger** | lazy on first `GetAsync` | auto at top of turn | auto at top of turn (scopes loaded in parallel) | auto at turn start |
| **Save trigger** | **manual** `SaveChangesAsync` **or** `AutoSaveStateMiddleware` | auto, inline at each successful exit | auto, after handlers | auto, after the turn body |
| **On exception** | save skipped → changes discarded | save skipped → discarded | save skipped → discarded | save skipped → discarded ✅ identical |
| **Where load/save lives** | middleware (`AutoSaveStateMiddleware`) or your `OnTurnAsync` | `Application.run()` turn loop | `AgentApplication.OnTurnAsync` turn loop | **Core `StateMiddleware : ITurnMiddleware`** ✅ (deliberate — idiomatic seam here, see §2) |
| **State carrier** | `BotState` + accessors; per-turn cache hidden in `turnContext.TurnState` | `TurnState` object threaded through the turn | `TurnState` on the turn context | `Context.State` via an **`AsyncLocal` ambient** ✅ (deliberate — per-route `Context` reconstruction, see §2) |
| **Get/Set API** | `accessor.GetAsync` / `SetAsync` (per property) | `GetValue`/`SetValue` + scope props | `GetValue`/`SetValue`; **`IStatePropertyAccessor` obsolete** | `scope.Get<T>`/`Set` + path `GetValue`/`SetValue` ✅ matches modern direction |
| **Bare path → temp** | n/a | yes | yes | yes ✅ |
| **Serialization** | Newtonsoft + `TypeNameHandling.All`, reflection — **not AOT/trim-safe** | delegated to provider (Newtonsoft) | System.Text.Json (`ProtocolJsonSerializer`) with source-gen / AOT hooks | **STJ, reusing the canonical `TeamsActivityJsonContext`** + reflection fallback for user POCOs ✅ no `TypeNameHandling`, camelCase, cross-runtime |
| **Registration** | construct `BotState` over `IStorage`, register each accessor | `ApplicationBuilder.withStorage(...)` | `IStorage` DI singleton + `AgentApplicationOptions` | `UseState(storage)` (DI options / bot / builder) ✅ idiomatic here |
| **Distribution** | Azure providers in separate packages | same | same | Memory/File in Core, Redis opt-in package ✅ |
| **Per-turn service bag naming** | `turnContext.TurnState` (the *service bag*, confusingly named) | n/a | renamed to `turnContext.Services` to free up "TurnState" | no service bag exposed → name `TurnState` is unambiguous here ✅ |

## What the proposed design gets right

1. **Unified three-scope `TurnState`** (not three separate `BotState` objects) — this is the Teams AI → Agents SDK consensus and the better DX. One load, one save, one object.
2. **Key derivation matches Bot Framework and the Agents SDK exactly** — `{channelId}/conversations/{id}` and `{channelId}/users/{id}`. It deliberately avoids Teams AI's two missteps: the `{botId}` segment (unnecessary; the bot is already scoped by deployment) and the .NET v1 string-interpolation bug that put literal `$` characters in keys. A Redis document written by this repo is key-compatible with a Bot Framework / Agents SDK document.
3. **Commit-on-success semantics are identical** across all four — save only runs after the turn body returns normally; an exception discards changes. The proposed design gets this "for free" from the existing pipeline's exception flow, exactly as `AutoSaveStateMiddleware` and the Agents `AgentApplication` loop do.
4. **Modern `Get<T>`/`SetValue` API, not property accessors** — the Agents SDK explicitly marked `IStatePropertyAccessor`/`CreateProperty` `[Obsolete]`. The proposal skips that legacy surface and reuses this repo's own `ExtendedPropertiesDictionary.Get<T>` `JsonElement` round-trip.
5. **Serialization avoids the legacy footguns and reuses the canonical context.** Bot Framework's Newtonsoft + `TypeNameHandling.All` is reflection-based, AOT/trim-incompatible, payload-bloating, and a historical deserialization-security concern. A state scope is an *open-typed* bag (`Dictionary<string, object?>` of arbitrary user POCOs), so — like every framework here — serializing user types is fundamentally reflection-based; there is no honest fully-source-generated path. The proposal makes the right call: rather than stand up a parallel state-specific source-gen context (which would imply an AOT fast path that doesn't exist for `object`-typed values), `StateSerializer` reuses the repo's existing `TeamsActivityJsonContext` for the primitives/`JsonElement` that do appear and combines a reflection resolver for user types. The result is camelCase STJ with no `TypeNameHandling` type markers — cross-runtime interoperable and free of the Newtonsoft security concern, with no duplicate JSON stack to maintain.
6. **Opt-in distribution** — Memory/File in Core, Redis as a separate package, mirrors every framework's "Azure/Redis providers ship separately" packaging and this repo's own opt-in posture (see the distributed-dedup note in [OAuthFlow-Design.md](sso/OAuthFlow-Design.md)).

## Where the proposed design should change

Three decisions: one where we **conform** to the ecosystem, and two **deliberate divergences we keep**.

### 1. Change detection: hash-based, not a dirty flag ✅ (decided — conform)

A bare dirty flag misses in-place mutation of a fetched reference type:

```csharp
var items = ctx.State.Conversation.Get<List<string>>("items");
items.Add("new");   // no Set() → silently lost with a dirty flag
```

Noticing this mutation requires a serialize-and-diff, so an O(1) dirty flag can never catch it. **Decision: adopt hash-based change detection** (the ecosystem's approach) — serialize each persisted scope at load to capture a baseline hash, re-serialize at save, write only scopes whose hash changed. It removes the silent-loss class; the cost is one extra serialization per persisted scope per turn, which is accepted for the correctness. Bot Framework, Teams AI, and the Agents SDK all make exactly this trade.

### 2. Plumbing: keep the `AsyncLocal` ambient + Core `StateMiddleware` ✅ (decided — deliberate divergence)

None of the three frameworks use an `AsyncLocal` ambient — each hangs persisted state off the **turn-context object** and drives load/save from the **application turn loop** (`Application.run()` / `AgentApplication.OnTurnAsync`). An earlier draft of this document recommended copying that pattern: own load/save in `TeamsBotApplication.OnActivity` and set a plain `Context.State` property. **That recommendation is reversed.** Once ecosystem familiarity is removed as a goal, the codebase's own routing mechanics make the ambient the better engineering choice.

The deciding fact is in [`Route.InvokeRoute`](../src/Microsoft.Teams.Apps/Routing/Route.cs): every matched route constructs a **fresh** `Context` for the handler, and [`Router.DispatchAsync`](../src/Microsoft.Teams.Apps/Routing/Router.cs) loops over *all* matching routes for non-invoke activities:

```csharp
public override async Task InvokeRoute(Context<TeamsActivity> ctx, CancellationToken ct = default)
{
    Context<TActivity> typedContext = new(ctx.TeamsBotApplication, (TActivity)ctx.Activity); // NEW instance
    await Handler!(typedContext, ct);
}
```

So the `Context` a handler receives is **not** the one `OnActivity` built. Consequences for each option:

- **Plain `Context.State` property (the reversed recommendation):** the loop sets `State` on the default context, but the handler gets a different instance → `State` is `null`. Making it work requires editing both `InvokeRoute`/`InvokeRouteWithReturn` to copy the `TurnState` reference into every `typedContext`, and every future Context-construction site must remember to do the same. Forget the copy → silently `null` state. Added fragility whose only payoff was ecosystem familiarity.
- **`AsyncLocal` ambient (kept):** set once before dispatch; the default context, every per-route `typedContext`, and all routes in a multi-match turn read the same `TurnState` via `State => TurnState.Current`. Zero constructor threading, robust to new Context sites. A `StateMiddleware : ITurnMiddleware` is the codebase's purpose-built "wrap the turn" seam ([`UseMiddleware`](../src/Microsoft.Teams.Core/BotApplication.cs)), and load-before / save-after-`next()` gives atomic-save for free from the pipeline's existing exception flow.

**Decision: keep the `AsyncLocal` ambient published by a Core `StateMiddleware`.** Given per-route `Context` reconstruction this is cleaner and safer than threading a property through `Route.cs`, not a divergence to fix. Two caveats to document:

- **Background work after the turn:** capturing `ctx.State` in fire-and-forget work that outlives the turn is a misuse in *every* framework here — the auto-save already ran, so writes never persist. The naive failure is silent: the ecosystem reads a *stale* object, and `AsyncLocal` is no better than it looks — because `ExecutionContext` is captured at spawn time, a `Task.Run` started inside the handler keeps the (stale) ambient too; you only get `null` in narrower cases (e.g. a stashed `ctx` read from a foreign flow). So the behavior is "stale or null," not reliably null, and the idiomatic `ctx.State?.` swallows the null anyway. **The real fix is independent of the carrier:** a **completion guard** — `TurnState` is sealed after save, and scope reads/writes throw a descriptive `InvalidOperationException` once `IsCompleted`. That throws through `ctx.State?.` (the scope method throws, not the null-conditional), regardless of stale-vs-null. Correct pattern: read the values out during the turn and pass *those* into the background work, not the state object. See [State-Design.md → Lifetime and after-turn access](State-Design.md#lifetime-and-after-turn-access).
- It is the one piece of "ambient magic" in the design; everything else is explicit.

**Alternative if `AsyncLocal` is unwanted:** stash the `TurnState` on the inbound activity object (the same `TeamsActivity` instance flows to every per-route `Context` via `(TActivity)ctx.Activity`) and read it from `Context.State`. This also avoids the `Route.cs` threading and degrades to a stale read like the ecosystem, at the cost of hanging a behavior reference off an otherwise-pure DTO. Ranking for this codebase: **`AsyncLocal` ambient ≥ activity-stash ≫ plain property.**

### 3. `IStorage` shape: keep the `StoreItem` wrapper ✅ (decided — deliberate divergence)

Bot Framework, Teams AI, and the Agents SDK share one `IStorage` shape: `ReadAsync(string[] keys) → IDictionary<string, object>`, `WriteAsync(IDictionary<string, object>)`, `DeleteAsync(string[])`, with `IStoreItem.ETag` as an opt-in marker on the stored object. The proposal's `StoreItem { Values, ETag }` wrapper is *substantively* the same thing (a per-key property dictionary plus an ETag — `BotState` literally stores a `Dictionary<string,object>` per key), but the surface differs.

Matching the canonical `string[]` / bare-`object` signatures would let existing Bot Framework / Agents storage providers be adapted through the repo's compat layer (`Microsoft.Teams.Apps.BotBuilder`) with a thin shim instead of a rewrite. That is the only argument for changing it, and it only matters if reusing those providers is a goal.

**Decision: keep `StoreItem { Values, ETag }`.** Reusing Bot Framework storage providers is not a goal, and the explicit `StoreItem` type is the cleaner, more discoverable surface — `Values` and `ETag` are named fields instead of "an `object` that might implement `IStoreItem`." This is a deliberate, documented divergence, not an oversight. The canonical-shape realignment is **declined**; revisit only if compat-layer provider reuse becomes a requirement.

## Verdict

**The proposed design is the right model for this codebase.** On every high-value decision — unified `TurnState`, three scopes with a `temp` scratch scope, Bot-Framework-compatible key derivation, commit-on-success atomicity, modern `Get`/`SetValue` over obsolete accessors, opt-in Redis packaging — it matches the **Agents SDK**, which is the ecosystem's current consolidation of fifteen years of lessons. On serialization it is the **strongest of the four**, because being source-generated/AOT-first from day one fits this repo's `System.Text.Json` source-gen architecture better than Bot Framework's Newtonsoft baggage or the Agents SDK's reflection-default.

All three review points are now decided:

1. **Change detection (§1) — conform: hash-based.** Closes a real silent-loss footgun the whole ecosystem already avoids; the per-turn serialization cost is accepted for the correctness.
2. **State plumbing (§2) — keep `AsyncLocal` ambient + Core `StateMiddleware`.** Chosen over the ecosystem's app-loop + plain-property model because this codebase reconstructs `Context` per route in `Route.InvokeRoute`; the ambient lets every per-route context share one `TurnState` with no constructor threading.
3. **`IStorage` shape (§3) — keep the `StoreItem { Values, ETag }` wrapper.** Substantively equivalent to the ecosystem's per-key dictionary + `IStoreItem.ETag`, but a cleaner, more discoverable surface. Declined the canonical-shape realignment unless Bot Framework storage-provider reuse becomes a goal.

Separately, after-turn misuse fails loudly via a **completion guard** (`TurnState` is sealed after save; scope access then throws a descriptive error) rather than the ecosystem's silent stale read.

With these decisions, the design is faithful to the Agents SDK where it matters, better than all three on serialization, and idiomatic to this repo's middleware/Context/source-gen architecture.

## Sources

Bot Framework v4:
- `IStorage` / `IStoreItem` — https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder/IStorage.cs
- `BotState`, `ConversationState`, `UserState`, `PrivateConversationState` — https://github.com/microsoft/botbuilder-dotnet/tree/main/libraries/Microsoft.Bot.Builder
- `AutoSaveStateMiddleware` — https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder/AutoSaveStateMiddleware.cs
- Save user and conversation data — https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-state

Teams AI Library v1 (tag `js-1.7.4`):
- `TurnState` (JS) — https://github.com/microsoft/teams-ai/blob/js-1.7.4/js/packages/teams-ai/src/TurnState.ts
- `TurnState` (.NET) — https://github.com/microsoft/teams-ai/blob/js-1.7.4/dotnet/packages/Microsoft.TeamsAI/Microsoft.TeamsAI/State/TurnState.cs
- `Application` turn loop — https://github.com/microsoft/teams-ai/blob/js-1.7.4/js/packages/teams-ai/src/Application.ts

Microsoft 365 Agents SDK:
- `IStorage` — https://github.com/microsoft/Agents-for-net/blob/main/src/libraries/Storage/Microsoft.Agents.Storage/IStorage.cs
- `AgentState` (hash-based save) — https://github.com/microsoft/Agents-for-net/blob/main/src/libraries/Builder/Microsoft.Agents.Builder/State/AgentState.cs
- `TurnState` / scopes — https://github.com/microsoft/Agents-for-net/blob/main/src/libraries/Builder/Microsoft.Agents.Builder/State/TurnState.cs
- `AgentApplication` turn loop — https://github.com/microsoft/Agents-for-net/blob/main/src/libraries/Builder/Microsoft.Agents.Builder/App/AgentApplication.cs
- `ProtocolJsonSerializer` (STJ / AOT hooks) — https://github.com/microsoft/Agents-for-net/blob/main/src/libraries/Core/Microsoft.Agents.Core/Serialization/ProtocolJsonSerializer.cs
- BF→Agents .NET migration — https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-dotnet
