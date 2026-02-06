# ConversationSample

This sample demonstrates all **ConversationUpdate** and **InstallUpdate** activity handlers available in the Teams Bot framework.

## Handlers Demonstrated

### ConversationUpdate Handlers

#### General Handlers
- **OnConversationUpdate** - Catches all conversation update activities
- **OnMembersAdded** - Triggered when members are added to a conversation
- **OnMembersRemoved** - Triggered when members are removed from a conversation

#### Channel Event Handlers
- **OnChannelCreated** - Channel is created in a team
- **OnChannelDeleted** - Channel is deleted from a team
- **OnChannelRenamed** - Channel name is changed
- **OnChannelRestored** - Deleted channel is restored
- **OnChannelShared** - Channel is shared with another team
- **OnChannelUnshared** - Channel sharing is removed
- **OnChannelMemberAdded** - Member is added to a specific channel
- **OnChannelMemberRemoved** - Member is removed from a specific channel

#### Team Event Handlers
- **OnTeamArchived** - Team is archived
- **OnTeamDeleted** - Team is soft-deleted
- **OnTeamHardDeleted** - Team is permanently deleted
- **OnTeamRenamed** - Team name is changed
- **OnTeamRestored** - Deleted team is restored
- **OnTeamUnarchived** - Archived team is unarchived

### InstallUpdate Handlers
- **OnInstallUpdate** - Catches all installation update activities
- **OnInstallAdd** - Bot is installed to a team/chat
- **OnInstallRemove** - Bot is uninstalled from a team/chat

## Running the Sample

1. Build and run the project:
   ```bash
   dotnet run --project samples/ConversationSample/ConversationSample.csproj
   ```

2. Configure your bot in the Teams Developer Portal or Bot Framework portal to point to `http://localhost:3978/api/messages`

3. Install the bot in a Teams team or chat to trigger the various conversation and installation events

## Notes

- Each handler logs to the console when triggered
- Most handlers send a confirmation message back to the conversation
- The `OnInstallRemove` handler typically cannot send messages (bot is being removed)
- Channel and Team event handlers require the activity's `ChannelData.EventType` to be set appropriately
