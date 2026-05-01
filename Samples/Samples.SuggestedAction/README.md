# Samples.SuggestedAction

Demonstrates handling the `suggestedAction/submit` invoke activity that is dispatched when a user clicks an `Action.Submit` suggested action button.

## Experimental API

The components used in this sample are marked `[Experimental("ExperimentalTeamsSuggestedAction")]`, because the underlying platform feature is still rolling out and the API shape may change. The C# compiler reports references to experimental APIs as **errors** by default, so consuming code has to opt in.

This sample opts in **per-usage** with a `#pragma` block in `Program.cs`. If you'd rather opt in for the **whole project**, add `ExperimentalTeamsSuggestedAction` to `NoWarn` in your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);ExperimentalTeamsSuggestedAction</NoWarn>
</PropertyGroup>
```

When the API stabilizes, the `[Experimental]` attribute will be removed and the opt-in can be deleted.

## Run

```sh
dotnet run --project Samples/Samples.SuggestedAction/Samples.SuggestedAction.csproj
```

The handler logs the incoming activity and echoes the `value` payload back to chat.
