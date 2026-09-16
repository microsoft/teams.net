# StreamingBot

Shows streaming responses in Teams using `TeamsStreamingWriter`, including incremental updates, finalization, and stream reuse.

## Prerequisites

- Bot registered and installed in Teams.
- Azure OpenAI (optional) — enables live model output on the default message path. Without it, the
  default path streams a canned response instead. To enable, set:
  - `AzureOpenAI__Endpoint`
  - `AzureOpenAI__ApiKey`
  - `AzureOpenAI__Deployment`

## What it shows

- Informative progress updates while work is running.
- Incremental token/text appends from a streaming chat response.
- Final response with an adaptive card, citation entity, and feedback entity.
- Reusing the same writer after `FinalizeResponseAsync` (`multi stream` path).

## Commands / Flows

| Input | Behavior |
|---|---|
| any text | Streams progress + model output (or a canned response when Azure OpenAI isn't configured), then sends the final response |
| `extended markdown` | Streams content that demonstrates extended-markdown features (task lists + strikethrough), setting the format per-chunk via `AppendResponseAsync(new MessageActivityInput().WithText(text, TextFormats.ExtendedMarkdown))` — the fix under test |
| `multi stream` | Runs two streamed responses back-to-back using the same writer |

## Running the Sample

~~~bash
dotnet run --project samples/StreamingBot/StreamingBot.csproj
~~~

In Teams, send a normal prompt and then `multi stream` to validate both streaming paths.
