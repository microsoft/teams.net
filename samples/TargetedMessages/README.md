# Targeted Messages Sample

Demonstrates sending, updating, and deleting targeted (ephemeral) messages in a Teams bot, plus the slash-command surface (in developer preview) that delivers user prompts as targeted message activities. Targeted messages are visible only to a specific recipient in a group chat or channel.

## What it shows

- Sending targeted replies visible only to one recipient.
- Updating and deleting targeted activities after send.
- Slash command integration for targeted-message workflows (`supportsTargetedMessages` + `commandLists`).

## Prerequisites

- Bot registered and installed in a group chat or channel (targeted messages are not supported in personal 1:1 chats).
- Create a Teams app package from the inline manifest below. Replace `YOUR_BOT_ID` placeholders (`id`, `bots[].botId`) with your Azure bot app ID, package with `color.png` and `outline.png`, and sideload into Teams.

### Manifest (inline)

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vDevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "id": "YOUR_BOT_ID",
  "bots": [
    {
      "botId": "YOUR_BOT_ID",
      "scopes": [
        "personal",
        "team",
        "groupChat"
      ],
      "isNotificationOnly": false,
      "supportsCalling": false,
      "supportsVideo": false,
      "supportsFiles": false,
      "supportsTargetedMessages": true,
      "commandLists": [
        {
          "scopes": [
            "team",
            "groupChat"
          ],
          "triggers": [
            "slash"
          ],
          "commands": [
            {
              "title": "test send",
              "description": "Send a targeted message visible only to you"
            },
            {
              "title": "test reply",
              "description": "Reply with a targeted message"
            },
            {
              "title": "test update",
              "description": "Send a targeted message then update it after 3 seconds"
            },
            {
              "title": "test delete",
              "description": "Send a targeted message then delete it after 3 seconds"
            },
            {
              "title": "test inbound",
              "description": "Show whether the inbound message was targeted at the bot"
            }
          ]
        }
      ]
    }
  ]
}
```

### Manifest configuration

The manifest uses `manifestVersion: "devPreview"` because the slash-command opt-in fields are only defined in the devPreview schema:

- `bots[].supportsTargetedMessages: true` — opts the bot into receiving slash-command-style targeted messages.
- `bots[].commandLists[].triggers: ["slash"]` — declares the listed commands (`test send`, `test reply`, `test update`, `test delete`, `test inbound`) as slash commands. They appear in the Teams `/` picker for group chats and channels.

Slash commands arrive at the bot as regular `MessageActivity` events with `Activity.Recipient.IsTargeted == true`, which the `test inbound` handler in this sample demonstrates.

---

## Commands

| Command | Behavior |
|---------|----------|
| `test send` | Send a targeted message via `Context.SendActivityAsync` with `WithRecipient(account, isTargeted: true)` |
| `test reply` | Reply with a targeted message via `Context.Reply` |
| `test update` | Send a targeted message, then update it after 3 seconds via `Api.Conversations.UpdateTargetedActivityAsync` |
| `test delete` | Send a targeted message, then delete it after 3 seconds via `Api.Conversations.DeleteTargetedActivityAsync` |
| `test inbound` | Read `Context.Activity.Recipient?.IsTargeted` and report whether the inbound message was targeted at the bot |
| `help` | List available commands |

---

## Running the Sample

~~~bash
dotnet run --project samples/TargetedMessages/TargetedMessages.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.
