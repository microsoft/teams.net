# Custom Invoke Activity Support — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop the .NET SDK from throwing on unknown invoke activity `name`s. Unknown names deserialize into a base `InvokeActivity` so existing `OnInvoke` routes can handle them.

**Architecture:** Replace the terminal `throw` arm in `InvokeActivity.JsonConverter.Read` with a fallback that deserializes the `JsonElement` into a base `InvokeActivity`, populating every inherited `Activity` field. The fallback mechanism (cloned `JsonSerializerOptions`, manual mapping, or restructured converter registration) is decided in Task 1 — the spec deliberately leaves it open. Routing layer is unchanged: partners use the existing `app.OnInvoke((ctx, ct) => ...)` and dispatch on `ctx.Activity.Name`.

**Tech Stack:** .NET, `System.Text.Json`, xUnit.

**Spec:** `docs/superpowers/specs/2026-04-30-custom-invoke-design.md`
**ADO:** [5323021](https://dev.azure.com/domoreexp/MSTeams/_workitems/edit/5323021)

---

### Task 1: Decide the fallback mechanism — DONE

Mechanism chosen: **manual field mapping from `JsonElement`**.

Options A (cloned options) and C (restructured registration) ruled out: A doesn't work because the `[JsonConverter]` attribute beats `options.Converters`; C would be a semver-breaking change for consumers using default options. See the "Implementation decision" section in `docs/superpowers/specs/2026-04-30-custom-invoke-design.md` for the full rationale.

To mitigate the manual-mapping rot risk, the test plan in Task 3 uses **reflection over `Activity`'s public properties** to assert every field is populated — a future field addition will fail this test and point at the fallback.

---

### Task 2: Add JSON fixture for an unknown invoke name

**Files:**
- Create: `tests/Microsoft.Teams.Api.Tests/Json/Activity/Invokes/CustomInvokeActivity.json`

- [ ] **Step 1: Create the fixture file.**

```json
{
  "id": "customInvokeId",
  "type": "invoke",
  "channelId": "channelId",
  "from": {
    "id": "userId",
    "name": "Test User"
  },
  "conversation": {
    "id": "conversationId"
  },
  "name": "suggestedActions/submit",
  "value": {
    "actionId": "approve",
    "context": "ticket-42"
  }
}
```

- [ ] **Step 2: Verify the test project copies JSON fixtures to the output directory (other `*.json` files under `Json/Activity/Invokes/` already work — same pattern applies). Open `tests/Microsoft.Teams.Api.Tests/Microsoft.Teams.Api.Tests.csproj` and confirm there is either a `<Content Include="Json\**\*.json">` block or wildcard copy rule. If absent, no action — sibling fixtures already prove the convention works.**

---

### Task 3: Write failing tests for unknown invoke fallback (TDD)

**Files:**
- Create: `tests/Microsoft.Teams.Api.Tests/Activities/Invokes/CustomInvokeActivityTests.cs`

- [ ] **Step 1: Write the test class with three tests — fallback for unknown name, reflection-based "no field dropped" check, and a regression test for a known name.**

```csharp
using System.Reflection;
using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities;

public class CustomInvokeActivityTests
{
    [Fact]
    public void CustomInvokeActivity_DeserializesAsBaseInvokeActivity()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CustomInvokeActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);

        Assert.NotNull(activity);
        // Must NOT be any typed subclass.
        Assert.Equal(typeof(InvokeActivity), activity!.GetType());

        // Name and Value populated.
        Assert.Equal("suggestedActions/submit", activity.Name.Value);
        Assert.NotNull(activity.Value);
    }

    [Fact]
    public void CustomInvokeActivity_PopulatesEveryActivityProperty()
    {
        // Guard against silent field-drop. The fallback uses manual JsonElement mapping
        // (see spec "Implementation decision"), so every property declared on Activity
        // and on InvokeActivity must be populated. When Activity gains a new field,
        // this test fails and points at InvokeActivity.JsonConverter.Read.
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CustomInvokeActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        Assert.NotNull(activity);

        // Every public instance property declared on Activity OR InvokeActivity that
        // has a corresponding JSON property in the fixture must be non-null.
        var doc = JsonDocument.Parse(json);
        var jsonProperties = doc.RootElement.EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unpopulated = new List<string>();
        foreach (var prop in typeof(InvokeActivity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Match by JsonPropertyName attribute or property name (case-insensitive).
            var jsonName = prop.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name
                ?? char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
            if (!jsonProperties.Contains(jsonName)) continue;
            if (prop.GetValue(activity) is null) unpopulated.Add(prop.Name);
        }

        Assert.Empty(unpopulated);
    }

    [Fact]
    public void KnownInvokeName_StillDeserializesAsTypedSubclass()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/HandoffActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);

        Assert.IsType<HandoffActivity>(activity);
    }
}
```

**Note for the implementer:** the JSON fixture in Task 2 must include every top-level `Activity` field that the fallback mapping handles, so the reflection test exercises them all. If the property-name conversion above doesn't match how `JsonPropertyName` is applied across this codebase, adjust the convention (e.g., snake_case, camelCase) by inspecting `Activity.cs` attributes.

- [ ] **Step 2: Run the new tests. The first must fail (current converter throws); the second should already pass.**

```bash
dotnet test tests/Microsoft.Teams.Api.Tests/Microsoft.Teams.Api.Tests.csproj --filter "FullyQualifiedName~CustomInvokeActivityTests"
```

Expected: `CustomInvokeActivity_DeserializesAsBaseInvokeActivity` FAILS with a `JsonException` ("doesn't match any known types"). `KnownInvokeName_StillDeserializesAsTypedSubclass` PASSES.

If `From` or `Conversation` property names on `Activity` differ from what's used above, fix the assertions to match the actual property names — read `Libraries/Microsoft.Teams.Api/Activities/Activity.cs` if needed.

---

### Task 4: Implement the fallback in the converter

**Files:**
- Modify: `Libraries/Microsoft.Teams.Api/Activities/InvokeActivity.JsonConverter.cs:74-81` (the `switch` expression with the throw arm)

- [ ] **Step 1: Implement manual mapping from `JsonElement` to a base `InvokeActivity`. The `switch` expression's `_` arm calls a private static helper that constructs a new `InvokeActivity(name)` and populates every property declared on `InvokeActivity` and on `Activity` from the corresponding `JsonElement` properties.**

Outline:

```csharp
return name switch
{
    "actionableMessage/executeAction" => JsonSerializer.Deserialize<Invokes.ExecuteActionActivity>(element.ToString(), options),
    "fileConsent/invoke" => JsonSerializer.Deserialize<Invokes.FileConsentActivity>(element.ToString(), options),
    "handoff/action" => JsonSerializer.Deserialize<Invokes.HandoffActivity>(element.ToString(), options),
    "application/search" => JsonSerializer.Deserialize<Invokes.SearchActivity>(element.ToString(), options),
    _ => DeserializeBase(name, element, options)
};
```

`DeserializeBase` should:
- Construct `var activity = new InvokeActivity(new Invokes.Name(name));`
- For each property on `InvokeActivity` and `Activity` (the full list — check `Activity.cs` for the current set; at the time of writing: `Id`, `Type`, `ReplyToId`, `ChannelId`, `From`, `Recipient`, `Conversation`, `RelatesTo`, `ServiceUrl`, `Locale`, `Timestamp`, `LocalTimestamp`, `Entities`, `ChannelData`, `Properties`, plus `Name` and `Value`), if the JSON has the corresponding property, deserialize it using `options` and set it.
- Return the populated activity.

Use the existing `JsonPropertyName` attributes on `Activity`/`InvokeActivity` properties to determine JSON property names — don't hardcode strings.

For complex-typed properties (e.g., `From` → `Account`, `Conversation` → `Conversation`), call `propertyElement.Deserialize<T>(options)`. The `options` here is fine to pass through — none of those types are wired to `InvokeActivity.JsonConverter`, so no recursion.

The `Type` property is always `"invoke"` for InvokeActivity — it will be set by the base `Activity(ActivityType.Invoke)` constructor chain via `new InvokeActivity(name)`. Don't overwrite it from JSON.

- [ ] **Step 2: Re-run the tests from Task 3. Both must pass.**

```bash
dotnet test tests/Microsoft.Teams.Api.Tests/Microsoft.Teams.Api.Tests.csproj --filter "FullyQualifiedName~CustomInvokeActivityTests"
```

Expected: both tests PASS.

- [ ] **Step 3: Run the full Api tests to make sure nothing else regressed.**

```bash
dotnet test tests/Microsoft.Teams.Api.Tests/Microsoft.Teams.Api.Tests.csproj
```

Expected: full suite PASSES. If a typed-subclass test fails, the fallback is intercepting a known name — debug and fix.

- [ ] **Step 4: Commit converter + tests + fixture together.**

```bash
git add Libraries/Microsoft.Teams.Api/Activities/InvokeActivity.JsonConverter.cs \
        tests/Microsoft.Teams.Api.Tests/Activities/Invokes/CustomInvokeActivityTests.cs \
        tests/Microsoft.Teams.Api.Tests/Json/Activity/Invokes/CustomInvokeActivity.json
git commit -m "fix: deserialize unknown invoke names as base InvokeActivity (ADO 5323021)"
```

---

### Task 5: Demonstrate `suggestedActions/submit` handling in `Samples.Dialogs`

**Files:**
- Modify: `samples/Samples.Dialogs/Program.cs`

- [ ] **Step 1: Add a generic `OnInvoke` handler that recognizes `suggestedActions/submit` and logs/echoes the value. Place it after the existing `OnTaskSubmit` registration (around line 144).**

```csharp
teams.OnInvoke(async (context, cancellationToken) =>
{
    var activity = context.Activity;
    if (activity.Name.Value != "suggestedActions/submit")
    {
        // Only this sample-handled custom invoke; other invokes are covered by
        // their dedicated routes (OnTaskFetch / OnTaskSubmit above).
        return;
    }

    context.Log.Info("[CUSTOM_INVOKE] suggestedActions/submit received");

    var value = activity.Value as JsonElement?;
    var actionId = value?.TryGetProperty("actionId", out var idEl) == true && idEl.ValueKind == JsonValueKind.String
        ? idEl.GetString()
        : null;

    await context.Send($"Got suggestedActions/submit (actionId={actionId ?? "<none>"})", cancellationToken);
});
```

Verify the exact namespace/property name for `activity.Name`. If `Name` is a `StringEnum`-style wrapper, the comparison may need to be `activity.Name.Value` or `activity.Name.ToString()` — match the convention used elsewhere in the file. (Other invoke route handlers in this file don't compare `Name`, so cross-check against `Libraries/Microsoft.Teams.Api/Activities/Invokes/Name.cs`.)

- [ ] **Step 2: Build the sample to confirm it compiles.**

```bash
dotnet build samples/Samples.Dialogs/Samples.Dialogs.csproj
```

Expected: build succeeds, no errors.

- [ ] **Step 3: Commit.**

```bash
git add samples/Samples.Dialogs/Program.cs
git commit -m "sample: handle suggestedActions/submit in Samples.Dialogs (ADO 5323021)"
```

---

### Task 6: Final verification

- [ ] **Step 1: Run the full solution build.**

```bash
dotnet build Microsoft.Teams.sln
```

Expected: SUCCESS.

- [ ] **Step 2: Run the full test suite.**

```bash
dotnet test Microsoft.Teams.sln
```

Expected: SUCCESS. All tests pass.

- [ ] **Step 3: Run `dotnet format` to make sure changes match repo style.**

```bash
dotnet format
```

If `dotnet format` makes changes, review and commit them as a formatting-only commit.

- [ ] **Step 4: Sanity check the diff.**

```bash
git log main..HEAD --oneline
git diff main...HEAD --stat
```

Expected commits (in order):
1. `docs: lock in fallback mechanism for custom invoke ...`
2. `fix: deserialize unknown invoke names as base InvokeActivity ...`
3. `sample: handle suggestedActions/submit in Samples.Dialogs ...`

(Plus, if applicable, a follow-up `style: dotnet format` commit.)

Diff stat should show changes only to:
- `Libraries/Microsoft.Teams.Api/Activities/InvokeActivity.JsonConverter.cs`
- `tests/Microsoft.Teams.Api.Tests/Activities/Invokes/CustomInvokeActivityTests.cs`
- `tests/Microsoft.Teams.Api.Tests/Json/Activity/Invokes/CustomInvokeActivity.json`
- `samples/Samples.Dialogs/Program.cs`
- `docs/superpowers/specs/2026-04-30-custom-invoke-design.md`
- `docs/superpowers/plans/2026-04-30-custom-invoke.md`

If the diff touches anything else, investigate and roll back unintended changes.
