# Sample: MCP Server

A Teams bot that doubles as an MCP server, exposing human-in-the-loop tools that
let an MCP client (an agent, an IDE, etc.) reach a real user through Teams and
wait for them to reply or approve.

## Tools

| Tool               | Description                                                                          | Parameters                          |
| ------------------ | ------------------------------------------------------------------------------------ | ----------------------------------- |
| `notify`           | Send a one-way notification to a user. No response expected.                         | `userId`, `message`                 |
| `ask`              | Ask a user a question. Returns a `requestId`.                                        | `userId`, `question`                |
| `get_reply`        | Poll for the reply to an earlier `ask`. Returns `pending` until the user responds.   | `requestId`                         |
| `request_approval` | Send an Approve/Reject card to a user. Returns an `approvalId`.                      | `userId`, `title`, `description`    |
| `get_approval`     | Poll for the decision on an earlier `request_approval`.                              | `approvalId`                        |

## Layout

| File          | Responsibility                                                                            |
| ------------- | ----------------------------------------------------------------------------------------- |
| `Program.cs`  | Wires `/api/messages` (Teams) and `/mcp` (MCP) on the same `WebApplication`.              |
| `McpTools.cs` | The five `[McpServerTool]` methods, registered via `WithTools<McpTools>()`.               |
| `Cards.cs`    | Adaptive Card factory for the Approve/Reject card.                                        |
| `Models.cs`   | Typed result records (`NotifyResult`, `AskResult`, `ReplyResult`, …) — snake_case wire.   |
| `State.cs`    | DI-singleton: conversation map, pending asks, approvals, last seen service URL.           |

## Configure

Set credentials in `appsettings.json` *or* `Properties/launchSettings.json`
(env-var form).

`appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-azure-bot-app-id>",
    "ClientCredentials": [
      { "SourceType": "ClientSecret", "ClientSecret": "<your-azure-bot-app-secret>" }
    ]
  }
}
```

Or via env vars in `launchSettings.json`:

```
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-azure-bot-app-id>
AzureAd__ClientCredentials__0__SourceType=ClientSecret
AzureAd__ClientCredentials__0__ClientSecret=<your-azure-bot-app-secret>
```

`TenantId` is required because the MCP tools open 1:1 conversations
*proactively* via `app.Api.Conversations.CreateAsync` — it can't be inferred
from the bot's own credentials.

The `userId` argument passed to `notify`, `ask`, and `request_approval` is the
Teams user id of someone in the same tenant. For the simplest setup, message
the bot once with a real user, then read the user id off the first incoming
activity in the server log and use that.

## Run

```bash
dotnet run --project samples/McpServer
```

The bot listens for Teams activity on `POST /api/messages` (port 3978 by
default) and serves the MCP endpoint at `http://localhost:3978/mcp`.

## Run with the MCP Inspector

```bash
dotnet run --project samples/McpServer
# in a second terminal:
npx @modelcontextprotocol/inspector
```

In the Inspector UI, pick **Streamable HTTP** as the transport and enter
`http://localhost:3978/mcp` as the URL, then click **Connect**.

## Example agent flow

1. Agent calls `request_approval(userId, title, description)` → gets `approvalId`.
2. The user sees an Approve/Reject card in Teams and clicks a button.
3. The `OnAdaptiveCardAction` handler records the decision in shared state.
4. Agent polls `get_approval(approvalId)` until the status flips to
   `approved` or `rejected`.

## Limitations

All state is in-memory. A server restart clears everything — pending asks and
approvals in flight will be lost.

**Only one outstanding `ask` per user.** The next message that user sends to
the bot is treated as the answer to their open ask. Calling `ask` again for
the same user while a previous ask is still pending overwrites the
correlation, and the user's reply will resolve whichever ask is current.

## Security

The `/mcp` endpoint is mounted **without authentication**. Anyone who can reach
the port can call the tools — which means they can DM arbitrary users and
mutate approval state on your behalf. This is fine for local dev (the MCP
Inspector connects from the same machine), but **do not expose `/mcp` on the
network as-is.** Add an authentication check before deploying — e.g. a bearer
token / shared secret in a header, or proper OAuth.
