# Targeted Messages Sample

This sample demonstrates how to send **targeted messages** in Microsoft Teams - messages that are only visible to a specific user within a conversation.

## What are Targeted Messages?

Targeted messages (also known as "user-specific views" or "private messages in group chats") allow a bot to send a message that only one specific user can see, even in a group conversation. This is useful for:

- **Private notifications** - Alert a specific user without cluttering the group chat
- **Confirmation messages** - Confirm an action was completed for just the user who triggered it
- **Sensitive information** - Share personal data that shouldn't be visible to others
- **Personalized content** - Show different information to different users

## Running the Sample

### Option 1: Using Teams Toolkit (Recommended)

1. Install [Teams Toolkit](https://marketplace.visualstudio.com/items?itemName=TeamsDevApp.ms-teams-vscode-extension) extension in VS Code
2. Open this sample folder in VS Code
3. Press F5 to start debugging - Teams Toolkit will handle tunneling and app registration

### Option 2: Using Dev Tunnels

1. Install [dev tunnels CLI](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started):
   ```bash
   # Install dev tunnel CLI
   winget install Microsoft.devtunnel  # Windows
   brew install --cask devtunnel       # macOS
   curl -sL https://aka.ms/DevTunnelCliInstall | bash  # Linux
   ```

2. Create and start a tunnel:
   ```bash
   devtunnel user login
   devtunnel create targeted-messages --allow-anonymous
   devtunnel port create targeted-messages -p 3978
   devtunnel host targeted-messages
   ```

3. Copy the tunnel URL (e.g., `https://abc123.devtunnels.ms`)

4. Configure your bot in Azure Portal:
   - Go to [Azure Bot Service](https://portal.azure.com/#create/Microsoft.AzureBot)
   - Create a new Azure Bot or use existing
   - Set Messaging Endpoint to: `https://<your-tunnel-url>/api/messages`
   - Copy the App ID and create a client secret

5. Update `appsettings.json`:
   ```json
   {
     "Teams": {
       "ClientId": "your-bot-app-id",
       "ClientSecret": "your-bot-client-secret"
     }
   }
   ```

6. Run the sample:
   ```bash
   cd Samples/Samples.TargetedMessages
   dotnet run
   ```

7. In Teams:
   - Upload your app manifest (with correct bot ID)
   - Add the bot to a group chat or channel
   - Start chatting!

### Option 3: Using ngrok

1. Install [ngrok](https://ngrok.com/download)

2. Start ngrok:
   ```bash
   ngrok http 3978
   ```

3. Follow steps 4-7 from Option 2 above, using the ngrok URL

## Commands

| Command | Type | Description |
|---------|------|-------------|
| `send` | Reactive | Create a new targeted message |
| `update` | Reactive | Send a message, then update it after 3 seconds |
| `delete` | Reactive | Send a message, then delete it after 3 seconds |
| `reply` | Reactive | Get a targeted reply (threaded) |
| `help` | - | Show available commands |

## Code Highlights

### Sending a Targeted Message (Reactive)

The simplest way to send a targeted message in a reactive context (responding to a user's message):

```csharp
// Target the sender of the incoming message
await context.Send(
    new MessageActivity("Only you can see this!")
        .WithRecipient(context.Activity.From, true)  
);
```

### Sending a Targeted Message (Proactive)

When sending proactively (bot-initiated), you must specify the recipient explicitly:

```csharp
// Target a specific user by their ID
await teams.Send(
    conversationId,
    new MessageActivity("This is for you specifically!")
        .WithRecipient(new Account { Id = userId }, true)  // Must provide explicit user ID
);
```

### Updating a Targeted Message

Use the API client to update an existing targeted message:

```csharp
var updatedMessage = new MessageActivity("Updated content!")
    .WithRecipient(new Account { Id = userId }, true);

await context.Api.Conversations.Activities.UpdateTargetedAsync(
    conversationId, 
    messageId, 
    updatedMessage
);
```

### Deleting a Targeted Message

Use the API client to delete a targeted message:

```csharp
await context.Api.Conversations.Activities.DeleteTargetedAsync(
    conversationId, 
    messageId
);
```

## API Details

Under the hood, targeted messages use the Teams conversation API with a special query parameter:

| Operation | Endpoint |
|-----------|----------|
| **Create** | `POST /v3/conversations/{id}/activities?isTargetedActivity=true` |
| **Update** | `PUT /v3/conversations/{id}/activities/{activityId}?isTargetedActivity=true` |
| **Delete** | `DELETE /v3/conversations/{id}/activities/{activityId}?isTargetedActivity=true` |

The `MessageActivity.Recipient` property must be set to the target user's account for the message to be targeted correctly.

## Reactive vs Proactive Scenarios

| Scenario | Description | Recipient Setting |
|----------|-------------|-------------------|
| **Reactive** | Bot responds to a user message | `WithRecipient(context.Activity.From, true)` - uses incoming sender |
| **Proactive** | Bot initiates message (timer, webhook, etc.) | `WithRecipient(new Account { Id = userId }, true)` - must be explicit |

## Limitations

- Targeted messages only work in **group chats** and **channels** - in 1:1 conversations, all messages are already private
- The recipient must be a member of the conversation
- Targeted messages cannot be sent proactively without specifying an explicit recipient
- Reply threading with targeted messages works the same as regular messages

## Learn More

- [Targeted Messages Documentation](https://learn.microsoft.com/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages#send-a-message-to-a-specific-user-in-a-group)
- [Microsoft Teams Bot Framework](https://learn.microsoft.com/microsoftteams/platform/bots/what-are-bots)
- [Azure Bot Service](https://learn.microsoft.com/azure/bot-service/)
