# CommonHandlersBot

Demonstrates the everyday message and lifecycle handlers most Teams bots need first. It keeps the flow focused on the normal conversational events without the extra card, invoke, or slash-command samples.

## Prerequisites

- Bot registered and installed in Teams.

## What it shows

- `OnMessage` for the normal inbound message path.
- `OnMessageUpdate` and `OnMessageDelete` for edit/delete events.
- `OnMessageReaction` for reaction add/remove events.
- `OnMembersAdded` and `OnMembersRemoved` for conversation membership changes.
- `OnInstall` and `OnUnInstall` for app lifecycle hooks.

---

## Commands / Events

| Event | Behavior |
|------|----------|
| `help` | Shows the handler list |
| `message` | Replies with a simple prompt when no command matches |
| `message update` | Echoes the edited text |
| `message delete` | Confirms the delete event |
| `reaction` | Reports which reactions were added or removed |
| `member added/removed` | Reports membership changes |
| `install/uninstall` | Emits install lifecycle messages |

---
## Running the Sample

~~~bash
dotnet run --project samples/CommonHandlersBot/CommonHandlersBot.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.

