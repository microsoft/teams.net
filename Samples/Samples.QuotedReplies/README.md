# Example: Quoted Replies

A bot that demonstrates quoted reply features in Microsoft Teams — referencing previous messages when sending responses.

## Commands

| Command | Description |
|---------|-------------|
| `test reply` | `Reply()` — auto-quotes the inbound message |
| `test quote` | `QuoteReply()` — sends a message, then quotes it by ID |
| `test add` | `AddQuotedReply()` — sends a message, then quotes it with builder + response |
| `test multi` | Sends two messages, then quotes both with interleaved responses |
| `test manual` | `AddQuotedReply()` + `AddText()` — manual control |
| `test obsolete` | `ToQuoteReply()` — deprecated method (temporary) |
| `help` | Shows available commands |
| *(quote a message)* | Bot reads and displays the quoted reply metadata |

## Running the Sample

1. Create and start a dev tunnel:
   ```bash
   devtunnel user login
   devtunnel create quoted-replies --allow-anonymous
   devtunnel port create quoted-replies -p 3978
   devtunnel host quoted-replies
   ```

2. Configure your bot in Azure Portal:
   - Set Messaging Endpoint to: `https://<your-tunnel-url>/api/messages`

3. Update `appsettings.json`:
   ```json
   {
     "Teams": {
       "TenantId": "your-tenant-id",
       "ClientId": "your-bot-app-id",
       "ClientSecret": "your-bot-client-secret"
     }
   }
   ```

4. Run the sample:
   ```bash
   cd Samples/Samples.QuotedReplies
   dotnet run
   ```

5. In Teams, quote any message to the bot or use the commands above.

## Code Highlights

### Reading Inbound Quoted Replies

When a user quotes a message and sends it to the bot:

```csharp
var quotes = activity.GetQuotedMessages();
if (quotes.Count > 0)
{
    var quote = quotes[0].QuotedReply!;
    // quote.MessageId, quote.SenderName, quote.Preview, etc.
}
```

### Reply() — Auto-Quotes the Inbound Message

```csharp
await context.Reply("Got it!", cancellationToken);
```

### QuoteReply() — Quote a Specific Message by ID

```csharp
var sent = await context.Send("This message will be quoted next...", cancellationToken);
await context.QuoteReply(sent.Id, "This quotes the message above", cancellationToken);
```

### AddQuotedReply() — Builder for Proactive / Multi-Quote Scenarios

```csharp
// Single quote with response below it
var sent = await context.Send("This message will be quoted next...", cancellationToken);
var msg = new MessageActivity()
    .AddQuotedReply(sent.Id, "Here is my response");
await context.Send(msg, cancellationToken);

// Multiple quotes with interleaved responses
var sentA = await context.Send("Message A — will be quoted", cancellationToken);
var sentB = await context.Send("Message B — will be quoted", cancellationToken);
var msg = new MessageActivity()
    .AddQuotedReply(sentA.Id, "Response to A")
    .AddQuotedReply(sentB.Id, "Response to B");
await context.Send(msg, cancellationToken);

// Grouped quotes — omit response to group them
var msg = new MessageActivity("see below for previous messages")
    .AddQuotedReply("msg-1")
    .AddQuotedReply("msg-2", "Response to both");
await context.Send(msg, cancellationToken);
```
