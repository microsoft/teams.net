# TeamsBot

`TeamsBot` is the all-features sample. It demonstrates the broad Teams bot surface in one project, including message patterns, rich formatting, card actions, and invoke/event handlers.

## Prerequisites

- Bot registered and installed in Teams.

## What it shows

- Pattern-based message routing (`OnMessage`) and regex command routing.
- Rich text responses (`Markdown`, `ExtendedMarkdown`), mentions, typing indicators.
- Citation + feedback entities in responses.
- Adaptive Card feedback submission via `OnAdaptiveCardAction`.
- Reaction add/remove APIs via `context.Api.Conversations.*ReactionAsync`.
- Generic invoke/event handling (`OnEvent`, `OnMessageSubmitAction`).

## Commands / Flows

| Input | Behavior |
|---|---|
| `help` | Sends help text and suggested actions (`hello`, `feedback`) |
| `hello` | Sends typing indicator, mention, and echo-style response |
| `markdown` | Sends markdown formatting examples |
| `extendedMarkdown` | Sends extended markdown with table/math |
| `citation` | Sends response with citations and feedback metadata |
| `feedback` | Sends an adaptive card feedback form |
| `/help`, `/about`, `/time` | Runs slash-style regex command handler |

## Invoke / Event handling

- `OnAdaptiveCardAction`: handles submitted feedback card values and returns a response card.
- `OnEvent`: logs and echoes incoming event activity names.
 
## Running the Sample

~~~bash
dotnet run --project samples/TeamsBot/TeamsBot.csproj
~~~
