# TeamsChannelBot Sample

Demonstrates handling `ConversationUpdate` channel and team events in a Teams bot.

## Prerequisites

- Bot registered and installed in a team
- Admin permissions in the team to perform most actions

---

## Manifest

For adding a bot to a shared channel, add `"supportsChannelFeatures": "tier1"` to the root in your `manifest.json`:

```json
"supportsChannelFeatures": "tier1"
```

---

## How to Trigger Each Event

### Channel Events

| Event | How to Trigger |
|---|---|
| `channelCreated` | In a team where the bot is installed: **Manage team → Channels → Add channel** |
| `channelDeleted` | **Delete channel** |
| `channelRenamed` | **Edit channel** → change name |
| `channelMemberAdded` | In a shared channel: **Share Channel → With people ** |
| `channelMemberRemoved` | In a shared channel: **Manage Channel → Members** → Remove member |
| `channelShared` | In a shared channel: **Share channel → With a team you own** |
| `channelUnshared` | In a shared channel: **Manage channe → Teams** → Remove team |

### Team Events

| Event | How to Trigger |
|---|---|
| `teamMemberAdded` | **Add member** |
| `teamMemberRemoved` | **Manage team → Members** → remove a member |
| `teamArchived` |**Archive team** |
| `teamUnarchived` | **Restore team** |
| `teamRenamed` | **Manage team → Settings** → edit team name |
| `teamDeleted` | **Delete team |
---

## Running the Sample

1. Build and run:
   ```bash
   dotnet run --project samples/TeamsChannelBot/TeamsChannelBot.csproj
   ```

---
