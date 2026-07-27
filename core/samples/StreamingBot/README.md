# StreamingBot

Shows streaming responses in Teams using `TeamsStreamingWriter`, including incremental updates, finalization, and stream reuse.

## Prerequisites

- Bot registered and installed in Teams.
- Azure OpenAI configured:
  - `AZURE_OPENAI_ENDPOINT`
  - `AZURE_OPENAI_KEY`
  - `AZURE_OPENAI_DEPLOYMENT`

## What it shows

- Informative progress updates while work is running.
- Incremental token/text appends from a streaming chat response.
- Final response with an adaptive card, citation entity, and feedback entity.
- Reusing the same writer after `FinalizeResponseAsync` (`multi stream` path).

## Commands / Flows

| Input | Behavior |
|---|---|
| any text | Streams progress + model output, then sends final card/citation response |
| `multi stream` | Runs two streamed responses back-to-back using the same writer |

## Running the Sample

~~~bash
dotnet run --project samples/StreamingBot/StreamingBot.csproj
~~~

In Teams, send a normal prompt and then `multi stream` to validate both streaming paths.
