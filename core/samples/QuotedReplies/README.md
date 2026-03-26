# QuotedReplies Sample

Demonstrates reading and sending quoted replies in a Teams bot using the `quotedReply` entity.

## Prerequisites

- Bot registered and installed in a chat or channel

---

## Commands

| Command | Behavior |
|---------|----------|
| `test reply` | `ReplyAsync()` — auto-quotes the inbound message |
| `test quote` | `QuoteAsync()` — sends a message, then quotes it by ID |
| `test add` | `AddQuote()` — sends a message, then quotes it with extension method + response |
| `test multi` | Sends three messages, then quotes all with interleaved responses |
| `test builder` | `WithQuote()` on `TeamsActivityBuilder` |
| *(quote a message)* | Bot reads and displays the quoted reply metadata |

---

## Running the Sample

1. Build and run:
   ```bash
   dotnet run --project samples/QuotedReplies/QuotedReplies.csproj
   ```
