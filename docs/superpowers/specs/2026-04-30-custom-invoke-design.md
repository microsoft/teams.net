# Support `suggestedAction/submit` invoke activity in .NET

ADO: [5323021](https://dev.azure.com/domoreexp/MSTeams/_workitems/edit/5323021)
Reference: [SuggestedActionInvoke design spec](https://domoreexp.visualstudio.com/Teamspace/_git/teams-conv-platform-specs?path=/features/suggested-action-invoke/SuggestedActionInvoke-DesignSpec.md)

## Context

The Teams platform is introducing a new suggested action type (`Action.Submit`) that, when clicked, dispatches a structured invoke activity to the bot — without sending a chat-visible message on behalf of the user. The invoke arrives at the bot with `name: "suggestedAction/submit"` and a structured `value` payload authored by the bot.

The Declarative Agents team (the first partner) is currently blocked: the Teams .NET SDK's `InvokeActivity.JsonConverter` throws when it sees `name: "suggestedAction/submit"` because no typed activity matches that name. Their workaround is to forge the activity as `task/submit` so it slips through.

Acceptance criteria from the work item: the .NET SDK should handle `suggestedAction/submit` cleanly so partners (DA team and beyond) can route and process it as a first-class activity.

## Approach

Add a typed activity subclass `SuggestedActionSubmitActivity` and a dedicated `OnSuggestedActionSubmit` route extension, mirroring the existing `HandoffActivity` / `OnHandoff` pattern. Wire the typed activity into the JSON converter's exact-name dispatch (Read and Write) and into `Name.ToType()` and `InvokeActivity.ToType()`.

The behavior for **other** unknown invoke names is unchanged — the converter still throws. Generic-fallback support for unknown names is explicitly NOT in scope for this change; if future custom invoke names are introduced, each gets its own typed subclass following the same pattern.

## Change set

### `Libraries/Microsoft.Teams.Api`

- **New file** `Activities/Invokes/SuggestedActionSubmitActivity.cs`:
  - Extends the partial `Name` enum with `Name.SuggestedActionSubmit = new("suggestedAction/submit")` and a corresponding `IsSuggestedActionSubmit` predicate.
  - Defines `class SuggestedActionSubmitActivity() : InvokeActivity(Name.SuggestedActionSubmit)`.
  - The activity's `Value` is intentionally not strongly typed; partners read the structured payload from the inherited `object?` `Value` (typically a `JsonElement`). The payload schema is bot-authored and varies by use case (vote, approval, etc.), so a fixed value type would over-constrain partners.

- **Modified** `Activities/Invokes/InvokeActivity.cs`: add `if (IsSuggestedActionSubmit) return typeof(SuggestedActionSubmitActivity);` to `Name.ToType()`.

- **Modified** `Activities/InvokeActivity.cs`: add `ToSuggestedActionSubmit()` helper and a corresponding line in the `ToType` dispatcher.

- **Modified** `Activities/InvokeActivity.JsonConverter.cs`:
  - In `Read`, add `"suggestedAction/submit" => JsonSerializer.Deserialize<Invokes.SuggestedActionSubmitActivity>(...)` to the exact-name `switch` expression.
  - In `Write`, add a `value is Invokes.SuggestedActionSubmitActivity` branch that delegates to the typed serializer.
  - The `_ => throw new JsonException(...)` fallback for unknown names is preserved unchanged.

### `Libraries/Microsoft.Teams.Apps`

- **New file** `Activities/Invokes/SuggestedActionSubmitActivity.cs`:
  - `[AttributeUsage(...)] public class SuggestedActionSubmitAttribute()` — for declarative handler registration on bot classes.
  - Four `OnSuggestedActionSubmit` extension methods on `App` matching the overload set used by `OnHandoff` (with/without cancellation token, void/object/Response return shapes).

### Tests — `Tests/Microsoft.Teams.Api.Tests`

- **New fixture** `Json/Activity/Invokes/SuggestedActionSubmitActivity.json` — minimal `suggestedAction/submit` payload with id/channelId/name/value.
- **New tests** `Activities/Invokes/SuggestedActionSubmitActivityTests.cs`:
  - Deserialize as `SuggestedActionSubmitActivity` directly.
  - Deserialize as `InvokeActivity` and verify polymorphic dispatch into `SuggestedActionSubmitActivity`.
  - Deserialize as `Activity` and verify polymorphic dispatch.
  - Verify `GetPath()` returns `"Activity.Invoke.SuggestedAction/submit"`.

### Sample — `Samples/Samples.Dialogs/Program.cs`

- Replace any prior generic `OnInvoke` exploration with `teams.OnSuggestedActionSubmit(...)`. The handler logs the activity, extracts a `vote` field from `activity.Value` (matching the design spec example payload), and echoes the result back to chat.

## Out of scope

- Generic fallback for unknown invoke names. The converter continues to throw for names other than `suggestedAction/submit` (and the existing typed dispatches).
- A name-filtered `OnInvoke(name, handler)` overload.
- Documentation changes beyond the sample.
- Strongly typed `Value` schema. Partner-authored payloads are arbitrary JSON; partners cast `activity.Value` to `JsonElement`.

## Implementation decision (2026-04-30)

**Approach: typed activity subclass + dedicated route extension** (the same pattern used by `HandoffActivity` / `OnHandoff`).

We initially explored a generic-fallback approach — relax the deserializer so any unknown invoke name produces a base `InvokeActivity`, then partners dispatch on `Name` themselves. After review with the team, this was rejected:

- A custom JSON-mapper fallback inside the converter required either manual field mapping (silent rot when `Activity` adds fields) or restructuring the converter registration (semver-breaking for consumers using default `JsonSerializerOptions`). Neither tradeoff was acceptable.
- The platform spec for `Action.Submit` defines a specific, named activity type. Modeling it as a first-class subclass matches the precedent set by every other named invoke (`HandoffActivity`, `TaskActivity`, etc.) and gives partners a typed handler with all the routing benefits (attribute-based registration, `IContext<SuggestedActionSubmitActivity>`).
- Future custom invoke names — when they arrive — should follow this same pattern (typed subclass + route extension) rather than relying on a permissive deserializer.
