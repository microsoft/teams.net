# AdaptiveCardTaskModuleBot

Demonstrates how Teams invoke handlers work together for adaptive cards, task modules, and file consent. The bot sends a welcome card from `OnMessage`, then uses that card to drive the different invoke paths in one place.

## Prerequisites

- Bot registered and installed in Teams.
- In the Teams app manifest, include the bot entry with `supportsFiles: true` for the file-consent flow:

```json
"bots": [
  {
    "botId": "<your-bot-app-id>",
    "scopes": [
      "personal",
      "team",
      "groupChat"
    ],
    "supportsFiles": true
  }
]
```

## What it shows

- `OnMessage` sends the welcome card that exposes the other flows.
- `OnAdaptiveCardAction` handles Action.Execute clicks and echoes the verb/data payload back in a response card.
- `OnTaskFetch` opens a task module dialog, and `OnTaskSubmit` shows the submitted form values.
- `OnFileConsent` demonstrates the accept/decline file-upload flow and the follow-up file info card.

---

## Commands / Flows

| Flow | Behavior |
|------|----------|
| `welcome card` | Any message triggers the starter card with buttons |
| `adaptive card action` | Click an Action.Execute button and inspect the returned verb/data |
| `task module` | Open the task module and submit the form |
| `file consent` | Request upload, accept/decline, then inspect the file info card |

---
## Running the Sample

~~~bash
dotnet run --project samples/AdaptiveCardTaskModuleBot/AdaptiveCardTaskModuleBot.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.

