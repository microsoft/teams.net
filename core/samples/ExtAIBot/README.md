# ExtAIBot — Microsoft.Extensions.AI sample

A Teams bot powered by [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions) and Azure OpenAI. Demonstrates streaming responses, per-conversation memory, a local clarification tool, remote MCP server tools, inline citations, follow-up suggestions, and custom feedback.

## What it shows

- **Streaming** — token-by-token replies via `TeamsStreamingWriter`
- **Conversation memory** — each conversation keeps its own `List<ChatMessage>` so the bot remembers context across turns
- **Local tool** — the model calls `request_clarification` when the user's request is ambiguous; the bot replies with an Adaptive Card listing 2–4 candidate interpretations
- **MCP client** — connects to the [Microsoft Learn docs MCP server](https://learn.microsoft.com/api/mcp) at startup; its tools are passed alongside the local tool in every `ChatOptions`
- **Inline citations** — MCP tool results are intercepted by `CitationCapturingTool` to extract source URLs; citations render as `[1]`, `[2]`, etc. in the Teams message
- **Follow-up suggestions** — after each reply, a structured-output call produces two short follow-up prompts shown as suggested-action chips
- **Custom feedback** — every text reply enables `FeedbackType.Custom`; clicking thumbs up/down opens a bot-rendered task module form, and submissions are handled by the typed `OnMessageSubmitFeedback` route

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

The bot initializes the MS Learn MCP tool set at startup before accepting messages. If the MCP server is unreachable the app will fail to start.

## Example interactions

- `Tell me about streaming` — ambiguous request: the model calls `request_clarification` and the bot replies with a clarification card.
- `How do I stream in teams.net?` — model calls an MS Learn search tool, replies with docs-grounded answer and inline citations, plus two follow-up chips
- `How do I list users with Microsoft Graph?` — same MCP search path, but lands on Graph documentation; reply cites the relevant `/users` endpoint docs and shows a code snippet

### Clarification flow

When the user's message is ambiguous, the model calls `request_clarification` with a question and 2–4 options. `LocalTools` builds an Adaptive Card with an `Action.Execute` whose `verb` is `"clarification"`. The bot finalizes the reply as an attachment-only message (no text, no feedback loop) so the card stands alone.

When the user picks an option, Teams sends an `adaptiveCard/action` invoke. `OnAdaptiveCardAction` reads `clarificationChoice` from the action's data and feeds it back through the agent as the next user turn.

### Feedback flow

Every text reply is finalized with `FeedbackType.Custom`, which renders thumbs up/down on the bot bubble. Clicking either button sends a `message/fetchTask` invoke; `OnMessageFetchTask` returns a task module containing a follow-up text form built with `Microsoft.Teams.Cards`. On submit, the typed `OnMessageSubmitFeedback` route fires with `context.Activity.Value` already deserialized to `MessageSubmitFeedbackValue { Reaction, Feedback }`. (`Feedback` is the form payload as a JSON-encoded string — Teams wraps the inputs the bot defined in its task module.)

### How MCP tools are wired in

At startup, `McpToolSet.CreateAsync` connects to the MS Learn MCP server using the Streamable HTTP transport and lists its tools. Each `McpClientTool` is an `AIFunction` that holds a reference to the client and calls the server when invoked.

Each tool is wrapped in a `CitationCapturingTool` (a `DelegatingAIFunction`) that intercepts the result to extract citation URLs before passing it back to the model. These are spread into `ChatOptions.Tools` alongside the local clarification tool:

```csharp
ChatOptions options = new()
{
    Tools =
    [
      LocalTools.CreateClarificationCardTool(pendingCards, _logger),
        .. _mcpTools.GetTools(citations)
    ]
};
```

`UseFunctionInvocation()` then handles all tool calls — local or remote — transparently during streaming.
## Running the Sample

~~~bash
dotnet run --project samples/ExtAIBot/ExtAIBot.csproj
~~~
