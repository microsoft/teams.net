# Targeted Messages Sample

Demonstrates sending, updating, and deleting targeted (ephemeral) messages in a Teams bot. Targeted messages are visible only to a specific recipient in a group chat or channel.

## Prerequisites

- Bot registered and installed in a group chat or channel (targeted messages are not supported in personal 1:1 chats).
- Install via the included [`manifest.json`](./manifest.json). Replace the `YOUR_BOT_ID` placeholders (`id`, `bots[].botId`) with your Azure bot's app ID, package together with `color.png` and `outline.png` icons of your choice, and sideload into Teams. The manifest's `commandLists` registers `test send`, `test reply`, `test update`, `test delete`, and `test inbound` as slash-command autocomplete suggestions in group chats and channels.

---

## Commands

| Command | Behavior |
|---------|----------|
| `test send` | Send a targeted message via `Context.SendActivityAsync` with `WithRecipient(account, isTargeted: true)` |
| `test reply` | Reply with a targeted message via `Context.Reply` |
| `test update` | Send a targeted message, then update it after 3 seconds via `Api.Conversations.Activities.UpdateTargetedAsync` |
| `test delete` | Send a targeted message, then delete it after 3 seconds via `Api.Conversations.Activities.DeleteTargetedAsync` |
| `test inbound` | Read `Context.Activity.Recipient?.IsTargeted` and report whether the inbound message was targeted at the bot |
| `help` | List available commands |

---

## Running the Sample

1. Build and run:
   ```bash
   dotnet run --project samples/TargetedMessages/TargetedMessages.csproj
   ```
