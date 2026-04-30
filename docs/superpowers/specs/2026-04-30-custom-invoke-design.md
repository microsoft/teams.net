# Support custom invoke activity types in .NET

## Context

The Declarative Agents team is implementing a new invoke activity called `suggestedActions/submit` (HITL approve/reject from suggested-action chips). The Teams .NET SDK's `InvokeActivity.JsonConverter` throws a `JsonException` when it encounters an invoke `name` it doesn't recognize, so this activity cannot reach a handler today. Their workaround is to send the activity as `task/submit` so it slips through.

Acceptance criteria from the work item: the .NET SDK should handle `suggestedActions/submit` (and, by extension, any future custom invoke name) without errors.

## Approach

Relax the `InvokeActivity.JsonConverter` deserializer. When the invoke `name` does not match any known prefix or exact-match case, fall back to constructing a base `InvokeActivity` populated from the JSON instead of throwing. Partners route the activity using the existing `app.OnInvoke((ctx, ct) => ...)` extension and dispatch on `ctx.Activity.Name` themselves.

No new public API is introduced — no name-filtered overload, no typed subclass for `suggestedActions/submit`. This is the minimal change that resolves the class of problem.

## Change set

**Single source change** — `Libraries/Microsoft.Teams.Api/Activities/InvokeActivity.JsonConverter.cs`:

- In `Read`, replace the terminal `_ => throw new JsonException(...)` arm with a fallback that returns a base `InvokeActivity` populated from the full `JsonElement` — `Name`, `Value`, and every inherited `Activity` field (id, timestamp, channelId, from, conversation, recipient, serviceUrl, etc.).

**Requirements the fallback must satisfy:**

- Must populate every inherited `Activity` field, not just `Name` and `Value`. Future fields added to `Activity` should not require touching this code.
- Must not recurse into itself — the `JsonConverter` is registered on `InvokeActivity`, so a naive `JsonSerializer.Deserialize<InvokeActivity>(...)` call would loop.

**Mechanism is deliberately left open.** Several approaches can satisfy the requirements (deserializing against a cloned `JsonSerializerOptions` with this converter removed; manual field population from the `JsonElement`; restructuring how the converter is registered; etc.). The right tradeoff between simplicity, performance, and maintainability will be evaluated and chosen during the implementation phase.
- All existing prefix and exact-match cases are unchanged. Known names continue to deserialize into their typed subclasses.
- The `Write` path is unchanged: its existing fallback at the bottom of the method already serializes a base-class instance correctly.

**Tests** — `tests/Microsoft.Teams.Api.Tests`:

- New test: deserialize a JSON payload with `name: "suggestedActions/submit"` and assert the result is an `InvokeActivity` with `Name == "suggestedActions/submit"`, `Value` populated, **and** a sampling of inherited `Activity` fields populated (e.g., `Id`, `ChannelId`, `From`, `Conversation`) to confirm the fallback doesn't drop base-class fields.
- New regression test: deserialize a known invoke (e.g., `task/submit`) and assert it still produces the typed `TaskActivity`.

**Sample** — `samples/Samples.Dialogs`:

- Add an `OnInvoke` handler that recognizes `suggestedActions/submit`, reads the value payload, and responds (e.g., echoes back which suggested action was chosen).
- Sample already deals with invoke activities, so the addition reinforces the existing pattern. No new sample project.

## Out of scope

- New typed activity subclass for `suggestedActions/submit`.
- New `OnInvoke(name, handler)` filtered overload.
- Documentation changes beyond the sample.
- Any change to the `Write` path.

## Implementation decision (2026-04-30)

**Mechanism: manual field mapping from `JsonElement`.**

During implementation planning we evaluated three candidate mechanisms (cloned `JsonSerializerOptions` with the converter removed; manual mapping; restructured converter registration). Findings:

- **Cloned options doesn't work.** The `JsonConverter` is registered on `InvokeActivity` via the `[JsonConverter(typeof(JsonConverter))]` attribute. Type-level attributes take precedence over `options.Converters`, so removing the converter from a cloned options bundle does not disable it — the fallback would still recurse.
- **Restructured registration would be a breaking change.** Moving the converter off the type-level attribute and onto options-level registration means any consumer calling `JsonSerializer.Deserialize<InvokeActivity>(json)` with default options would silently lose typed-subclass dispatch (e.g. would receive a base `InvokeActivity` instead of `HandoffActivity`). External consumers of `Microsoft.Teams.Api` and existing test sites in this repo rely on this default-options behavior, so the change would have a real semver-breaking surface.
- **Manual mapping is non-breaking and localized.** A single-file change inside `InvokeActivity.JsonConverter.Read`'s fallback arm. No public surface change. The known cost is maintenance: when `Activity` gains a new field, the fallback must be updated or the field is silently dropped for unknown invoke names.

**Mitigation for the maintenance risk:** add a unit test that uses reflection over `Activity`'s public properties to assert each one was populated on a round-tripped unknown invoke. When a future contributor adds a new `Activity` field, this test fails and points them directly at the fallback.

This expands the test plan above. The "sampling of inherited fields" check is replaced with the reflection-based "no field dropped" check, which is strictly stronger.
