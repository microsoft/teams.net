# Example: Suggested Action Submit

A bot that demonstrates the `Action.Submit` suggested action and the `suggestedActions/submit` invoke it produces when clicked.

## Behavior

| Trigger | Behavior |
|---------|----------|
| Any user message | Bot replies with `Approve` / `Reject` suggested-action chips (`type: "Action.Submit"`, each with a structured `value`) |
| User clicks a chip | Platform dispatches a `suggestedActions/submit` invoke; bot reads `activity.Value` and echoes it back |

## Notes

- `Action.Submit` chips do not post a chat-visible message on the user's behalf — only the bot receives the click as a typed invoke.
- The chip's `value` is delivered verbatim on `SuggestedActionSubmitActivity.Value` (a `JsonElement` after deserialization).

## Experimental API

`CardActionType.Submit`, `SuggestedActionSubmitActivity`, and `OnSuggestedActionSubmit` are marked `[Experimental("ExperimentalTeamsSuggestedAction")]` because the underlying platform feature is still rolling out. The C# compiler reports references to them as **errors** by default, so consuming code has to opt in.

This sample opts in **per-file** with a `#pragma` at the top of `Program.cs`. For a project-wide opt-in, add to your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);ExperimentalTeamsSuggestedAction</NoWarn>
</PropertyGroup>
```

When the API stabilizes, the `[Experimental]` attribute will be removed and the opt-in can be deleted.

## Run

```bash
dotnet run
```

## Configuration

Set credentials in `appsettings.json`:

```json
{
  "Teams": {
    "ClientId": "<your-azure-bot-app-id>",
    "ClientSecret": "<your-azure-bot-app-secret>"
  }
}
```
