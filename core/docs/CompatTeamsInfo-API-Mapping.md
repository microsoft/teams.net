# CompatTeamsInfo API Mapping

This document provides a comprehensive mapping of Bot Framework TeamsInfo static methods to their corresponding REST API endpoints and the Teams Bot Core SDK client implementations.

## Overview

The `CompatTeamsInfo` class provides a compatibility layer that adapts the Bot Framework v4 SDK TeamsInfo API to use the Teams Bot Core SDK. It implements 19 static methods organized into four functional categories.

## API Method Mappings

### Member & Participant Methods

| Method | REST Endpoint | Client | Description |
|--------|--------------|--------|-------------|
| `GetMemberAsync` | `GET /v3/conversations/{conversationId}/members/{userId}` | ConversationClient | Gets a single conversation member by user ID |
| `GetMembersAsync` ⚠️ | `GET /v3/conversations/{conversationId}/members` | ConversationClient | Gets all conversation members (deprecated - use paged version) |
| `GetPagedMembersAsync` | `GET /v3/conversations/{conversationId}/pagedmembers?pageSize={pageSize}&continuationToken={token}` | ConversationClient | Gets paginated list of conversation members |
| `GetTeamMemberAsync` | `GET /v3/conversations/{teamId}/members/{userId}` | ConversationClient | Gets a single team member by user ID |
| `GetTeamMembersAsync` ⚠️ | `GET /v3/conversations/{teamId}/members` | ConversationClient | Gets all team members (deprecated - use paged version) |
| `GetPagedTeamMembersAsync` | `GET /v3/conversations/{teamId}/pagedmembers?pageSize={pageSize}&continuationToken={token}` | ConversationClient | Gets paginated list of team members |

⚠️ *Deprecated by Microsoft Teams - use paged versions instead*

### Meeting Methods

| Method | REST Endpoint | Client | Description |
|--------|--------------|--------|-------------|
| `GetMeetingInfoAsync` | `GET /v1/meetings/{meetingId}` | TeamsApiClient | Gets meeting information by meeting ID |
| `GetMeetingParticipantAsync` | `GET /v1/meetings/{meetingId}/participants/{participantId}?tenantId={tenantId}` | TeamsApiClient | Gets a specific meeting participant's information |
| `SendMeetingNotificationAsync` | `POST /v1/meetings/{meetingId}/notification` | TeamsApiClient | Sends an in-meeting notification to participants |

### Team & Channel Methods

| Method | REST Endpoint | Client | Description |
|--------|--------------|--------|-------------|
| `GetTeamDetailsAsync` | `GET /v3/teams/{teamId}` | TeamsApiClient | Gets detailed information about a team |
| `GetTeamChannelsAsync` | `GET /v3/teams/{teamId}/channels` | TeamsApiClient | Gets list of channels in a team |

### Batch Messaging Methods

| Method | REST Endpoint | Client | Description |
|--------|--------------|--------|-------------|
| `SendMessageToListOfUsersAsync` | `POST /v3/batch/conversation/users/` | TeamsApiClient | Sends a message to a list of users |
| `SendMessageToListOfChannelsAsync` | `POST /v3/batch/conversation/channels/` | TeamsApiClient | Sends a message to a list of channels |
| `SendMessageToAllUsersInTeamAsync` | `POST /v3/batch/conversation/team/` | TeamsApiClient | Sends a message to all users in a team |
| `SendMessageToAllUsersInTenantAsync` | `POST /v3/batch/conversation/tenant/` | TeamsApiClient | Sends a message to all users in a tenant |
| `SendMessageToTeamsChannelAsync` | Uses Bot Framework Adapter | BotAdapter.CreateConversationAsync | Creates a conversation in a Teams channel and sends a message |

### Batch Operation Management Methods

| Method | REST Endpoint | Client | Description |
|--------|--------------|--------|-------------|
| `GetOperationStateAsync` | `GET /v3/batch/conversation/{operationId}` | TeamsApiClient | Gets the state of a batch operation |
| `GetPagedFailedEntriesAsync` | `GET /v3/batch/conversation/failedentries/{operationId}?continuationToken={token}` | TeamsApiClient | Gets failed entries from a batch operation |
| `CancelOperationAsync` | `DELETE /v3/batch/conversation/{operationId}` | TeamsApiClient | Cancels a batch operation |

## Client Distribution

The implementation uses two primary clients from the Teams Bot Core SDK:

### ConversationClient (6 methods)
Used for member and participant operations in conversations and teams. Accessed via the `IConnectorClient` in TurnState.

**Methods:**
- GetMemberAsync
- GetMembersAsync
- GetPagedMembersAsync
- GetTeamMemberAsync
- GetTeamMembersAsync
- GetPagedTeamMembersAsync

### TeamsApiClient (12 methods)
Used for Teams-specific operations including meetings, team details, channels, and batch messaging. Added to TurnState by the CompatAdapter.

**Methods:**
- GetMeetingInfoAsync
- GetMeetingParticipantAsync
- SendMeetingNotificationAsync
- GetTeamDetailsAsync
- GetTeamChannelsAsync
- SendMessageToListOfUsersAsync
- SendMessageToListOfChannelsAsync
- SendMessageToAllUsersInTeamAsync
- SendMessageToAllUsersInTenantAsync
- GetOperationStateAsync
- GetPagedFailedEntriesAsync
- CancelOperationAsync

### Bot Framework Adapter (1 method)
One method uses the Bot Framework adapter directly for backward compatibility.

**Methods:**
- SendMessageToTeamsChannelAsync

## Implementation Details

### Model Conversion Strategy

The implementation uses two strategies for converting between Bot Framework and Core SDK models:

1. **Direct Property Mapping**: For simple models like `TeamsChannelAccount`, `ChannelInfo`, etc.
2. **JSON Round-Trip**: For complex models like `TeamDetails`, `MeetingNotificationResponse`, `BatchOperationState`, etc.

### Type Conversions

Key extension methods in `CompatActivity.cs`:

| Extension Method | Source Type | Target Type | Strategy |
|------------------|-------------|-------------|----------|
| `ToCompatTeamsChannelAccount` | Core TeamsConversationAccount | BF TeamsChannelAccount | Direct mapping |
| `ToCompatMeetingInfo` | Core MeetingInfo | BF MeetingInfo | Direct mapping |
| `ToCompatTeamsMeetingParticipant` | Core MeetingParticipant | BF TeamsMeetingParticipant | Direct mapping |
| `ToCompatChannelInfo` | Core Channel | BF ChannelInfo | Direct mapping |
| `ToCompatTeamsPagedMembersResult` | Core PagedMembersResult | BF TeamsPagedMembersResult | Direct mapping |
| `ToCompatTeamDetails` | Core TeamDetails | BF TeamDetails | JSON round-trip |
| `ToCompatMeetingNotificationResponse` | Core MeetingNotificationResponse | BF MeetingNotificationResponse | JSON round-trip |
| `ToCompatBatchOperationState` | Core BatchOperationState | BF BatchOperationState | JSON round-trip |
| `ToCompatBatchFailedEntriesResponse` | Core BatchFailedEntriesResponse | BF BatchFailedEntriesResponse | JSON round-trip |
| `FromCompatTeamMember` | BF TeamMember | Core TeamMember | JSON round-trip |

### Authentication

All methods use `AgenticIdentity` extracted from the turn context activity properties for authentication with the Teams services.

### Service URL

All API calls use the service URL from the turn context activity (`turnContext.Activity.ServiceUrl`), which points to the appropriate Teams channel service endpoint.

## Usage Examples

### Getting a Team Member

```csharp
var member = await TeamsInfo.GetMemberAsync(turnContext, userId, cancellationToken);
Console.WriteLine($"Member: {member.Name} ({member.Email})");
```

### Getting Meeting Information

```csharp
var meetingInfo = await TeamsInfo.GetMeetingInfoAsync(turnContext, meetingId, cancellationToken);
Console.WriteLine($"Meeting: {meetingInfo.Details.Title}");
```

### Sending a Batch Message

```csharp
var activity = MessageFactory.Text("Hello from bot!");
var members = new List<TeamMember> { new TeamMember(userId1), new TeamMember(userId2) };
var operationId = await TeamsInfo.SendMessageToListOfUsersAsync(
    turnContext, activity, members, tenantId, cancellationToken);

// Check operation status
var state = await TeamsInfo.GetOperationStateAsync(turnContext, operationId, cancellationToken);
Console.WriteLine($"Operation state: {state.State}");
```

### Getting Team Channels

```csharp
var channels = await TeamsInfo.GetTeamChannelsAsync(turnContext, teamId, cancellationToken);
foreach (var channel in channels)
{
    Console.WriteLine($"Channel: {channel.Name} ({channel.Id})");
}
```

## Testing

Comprehensive integration tests are available in `test/Microsoft.Teams.Bot.Core.Tests/CompatTeamsInfoTests.cs`. All tests are marked with `[Fact(Skip = "Requires live service credentials")]` and require environment variables to be set for live testing:

- `TEST_USER_ID`
- `TEST_CONVERSATIONID`
- `TEST_TEAMID`
- `TEST_CHANNELID`
- `TEST_MEETINGID`
- `TEST_TENANTID`

## Modified Core Models

To support full compatibility, the following Core SDK models were enhanced:

### TeamsConversationAccount
Added properties to match Bot Framework `TeamsChannelAccount`:
- `GivenName`
- `Surname`
- `Email`
- `UserPrincipalName`
- `UserRole`
- `TenantId`

### MeetingInfo
Changed `Organizer` property type from `ConversationAccount` to `TeamsConversationAccount` to match Bot Framework schema.

## References

- [Bot Framework TeamsInfo Source](https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder/Teams/TeamsInfo.cs)
- [Teams REST API Documentation](https://docs.microsoft.com/en-us/azure/bot-service/rest-api/bot-framework-rest-connector-api-reference)
- [Teams Meeting Notifications](https://docs.microsoft.com/en-us/microsoftteams/platform/apps-in-teams-meetings/meeting-apps-apis)
