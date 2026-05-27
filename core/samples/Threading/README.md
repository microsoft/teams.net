# Example: Threading

A bot that demonstrates reactive and proactive threading in Microsoft Teams channels.

## Commands

| Command | Behavior |
|---------|----------|
| `test reply` | `context.ReplyAsync()` — reactive in-thread reply (sets `ReplyToId`) |
| `test send` | `context.SendActivityAsync()` — reactive send to the same conversation as the inbound activity |
| `test proactive` | `teamsApp.ReplyAsync()` — proactive threaded reply via `;messageid=` |
| `test manual` | `ConversationExtensions.ToThreadedConversationId()` + `teamsApp.SendAsync()` — advanced manual control |
| `help` | Shows available commands |

## Notes

- `test reply` and `test send` use the inbound conversation reference. Both are visible to the user; `test reply` additionally sets `ReplyToId` so the Teams client renders it as a reply.
- `test proactive` constructs a threaded conversation ID via `;messageid=<rootId>` and sends to that thread. It exercises the `ToThreadedConversationId` validator (non-zero numeric `messageId`).
- `test manual` does the same as `test proactive` using `ConversationExtensions.ToThreadedConversationId()` + `teamsApp.SendAsync()` directly.
- `test proactive` and `test manual` may return a service error in conversation types that do not currently support threading (e.g. meetings, group chats).
- **Personal / 1:1 chats**: `test proactive` and `test manual` currently return `BadArgument: Failed to decrypt conversation id` from the service until the relevant ECS rollout is complete. Channel scopes work today.

## Run

```bash
dotnet run
```
