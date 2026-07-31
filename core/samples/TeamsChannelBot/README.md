# TeamsChannelBot Sample

Demonstrates handling `ConversationUpdate` channel and team events in a Teams bot.

## Prerequisites

- Bot registered and installed in a team
- Admin permissions in the team to perform most actions

---

## What it shows

- Channel lifecycle events (`channelCreated`, `channelDeleted`, `channelRenamed`, `channelRestored`).
- Channel membership/share events in shared channels.
- Team lifecycle and membership events (`teamMemberAdded`, `teamArchived`, `teamRestored`, etc.).

---

## Manifest (relevant part)

Apps using manifest version 1.25 or higher that support the `team` scope must declare
`supportsChannelFeatures` at the root of `manifest.json`. `tier1` declares support for all
channel features, including shared channels:

```json
"supportsChannelFeatures": "tier1"
```

---

## How to Trigger Each Event

### Channel Events

| Event | How to Trigger |
|---|---|
| `channelCreated` | In a team where the bot is installed: **Manage team → Channels → Add channel** |
| `channelDeleted` | **Delete channel** (logged only — the channel no longer exists, so the bot can't reply) |
| `channelRenamed` | **Edit channel** → change name |
| `channelRestored` | **Manage team → Channels → Deleted** → **Restore** a deleted channel |
| `channelMemberAdded` | In a shared channel: **Share Channel → With people** |
| `channelMemberRemoved` | In a shared channel: **Manage Channel → Members** → Remove member |
| `channelShared` | In a shared channel: **Share channel → With a team you own** |
| `channelUnshared` | In a shared channel: **Manage channel → Teams** → Remove team |

### Team Events

| Event | How to Trigger |
|---|---|
| `teamMemberAdded` | **Add member** |
| `teamMemberRemoved` | **Manage team → Members** → remove a member |
| `teamArchived` | **Archive team** |
| `teamUnarchived` | **Manage teams → Archived** → **Restore** an archived team |
| `teamRenamed` | **Manage team → Settings** → edit team name |
| `teamRestored` | Restore a previously deleted team (within the deleted-team retention window) |
| `teamDeleted` | **Delete team** (logged only) |
---

## Running the Sample

~~~bash
dotnet run --project samples/TeamsChannelBot/TeamsChannelBot.csproj
~~~
