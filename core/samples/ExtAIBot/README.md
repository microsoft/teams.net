# ExtAIBot — Microsoft.Extensions.AI sample

A Teams bot powered by [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions) and Azure OpenAI. Demonstrates streaming responses, per-conversation memory, local AI tools, remote MCP server tools, inline citations, and a feedback form.

## Features

- **Streaming** — token-by-token replies via `TeamsStreamingWriter`
- **Conversation memory** — each conversation keeps its own `List<ChatMessage>` so the bot remembers context across turns
- **Local tool** — the model calls `send_welcome_card` (an `AIFunction`) on first greeting, attaching a typed Adaptive Card (`Microsoft.Teams.Cards`) to the reply
- **MCP client** — connects to the [Microsoft Learn docs MCP server](https://learn.microsoft.com/api/mcp) at startup; its tools are passed alongside local tools in every `ChatOptions`
- **Inline citations** — MCP tool results are intercepted by `CitationCapturingTool` to extract source URLs; citations render as `[1]`, `[2]`, etc. in the Teams message
- **Feedback** — every reply includes thumbs up/down buttons; clicking one opens a task module with a follow-up text form (`OnMessageFetchTask` + `OnMessageSubmitAction`)

## Prerequisites

- .NET 10 SDK
- Azure OpenAI resource with a deployed model (e.g. `gpt-4o`)
- Teams bot registration (App ID + client secret)

## Setup

Fill in `appsettings.json` with your Azure OpenAI details:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com",
    "ApiKey":   "<your-api-key>",
    "ModelId":  "<deployment-name>"
  }
}
```

`ModelId` is the **deployment name**, not the base model name.

Configure bot credentials via environment variables (or `launchSettings.json`):

```
AzureAD__Instance=https://login.microsoftonline.com/
AzureAD__TenantId=<tenant-id>
AzureAD__ClientId=<app-id>
AzureAD__ClientCredentials__0__SourceType=ClientSecret
AzureAD__ClientCredentials__0__ClientSecret=<client-secret>
```

Then point your bot's messaging endpoint at this service (e.g. using [Dev Tunnels](https://learn.microsoft.com/azure/developer/dev-tunnels/overview) for local development).

## Running

```bash
cd samples/ExtAIBot
dotnet run
```

The bot connects to the MS Learn MCP server at startup and lists its tools before accepting messages. If the MCP server is unreachable the app will fail to start.

## Example interactions

- `Hi!` — model calls `send_welcome_card`, bot replies with a greeting and an Adaptive Card listing its capabilities
- `How do I stream in teams.net?` — model calls an MS Learn search tool, replies with docs-grounded answer and inline citations
- `What did I just say?` — bot recalls earlier messages in the conversation

## Architecture

```
Program.cs
├── AzureOpenAIClient → IChatClient (Microsoft.Extensions.AI)
│     └── UseFunctionInvocation()          ← handles tool calls transparently during streaming
├── McpClient (HttpClientTransport, StreamableHttp)
│     └── https://learn.microsoft.com/api/mcp  ← MS Learn docs search tools
│           └── CitationCapturingTool           ← wraps each McpClientTool to extract citations
├── CitationCollector                           ← accumulates citations per turn
├── ConcurrentDictionary<conversationId, List<ChatMessage>>  ← conversation memory
├── AIFunctionFactory.Create(send_welcome_card)              ← local AI tool
└── TeamsStreamingWriter                                      ← streams reply into Teams
```

### How MCP tools are wired in

At startup, `McpClient.CreateAsync` connects to the MS Learn MCP server using the Streamable HTTP transport. `ListToolsAsync()` returns `IList<McpClientTool>`, where each `McpClientTool` is an `AIFunction` that holds a reference to the client and calls the server when invoked.

Each tool is wrapped in a `CitationCapturingTool` (a `DelegatingAIFunction`) that intercepts the result to extract citation URLs before passing it back to the model. These are spread into `ChatOptions.Tools` alongside the local `send_welcome_card` tool:

```csharp
ChatOptions options = new() { Tools = [welcomeCardTool, .. mcpTools.GetTools(citations)] };
```

`UseFunctionInvocation()` then handles all tool calls — local or remote — transparently during streaming.

### Feedback flow

When the user clicks thumbs up or thumbs down on a bot reply, Teams sends a `message/fetchTask` invoke. `OnMessageFetchTask` returns a task module containing a feedback form (built with `Microsoft.Teams.Cards`). On submit, `OnMessageSubmitAction` receives the reaction and free-text feedback.
