# CompatTeamsInfo API Mapping

This document provides a comprehensive mapping of Bot Framework TeamsInfo static methods to their corresponding REST API endpoints and the Teams Bot Core SDK client implementations.

## Overview

The `CompatTeamsInfo` class provides a compatibility layer that adapts the Bot Framework v4 SDK TeamsInfo API to use the Teams Bot Core SDK. It implements 19 static methods organized into four functional categories.

## API Method Mappings

### Member & Participant Methods

| Method | REST Endpoint | Client | Description |
|--------|--------------|--------|-------------|
| `GetMemberAsync` | `GET /v3/conversations/{conversationId}/members/{userId}` | ConversationClient | Gets a single conversation member by user ID |
| `GetMembersAsync` | `GET /v3/conversations/{conversationId}/members` | ConversationClient | Gets all conversation members (deprecated) |
| `GetPagedMembersAsync` | `GET /v3/conversations/{conversationId}/pagedmembers?pageSize={pageSize}&continuationToken={token}` | ConversationClient | Gets paginated list of conversation members |
| `GetTeamMemberAsync` | `GET /v3/conversations/{teamId}/members/{userId}` | ConversationClient | Gets a single team member by user ID |
| `GetTeamMembersAsync` | `GET /v3/conversations/{teamId}/members` | ConversationClient | Gets all team members (deprecated) |
| `GetPagedTeamMembersAsync` | `GET /v3/conversations/{teamId}/pagedmembers?pageSize={pageSize}&continuationToken={token}` | ConversationClient | Gets paginated list of team members |

> `GetMembersAsync` and `GetTeamMembersAsync` are deprecated by Microsoft Teams. Use paged versions instead.

### Meeting Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetMeetingInfoAsync` | `GET /v1/meetings/{meetingId}` | ApiClient.Meetings | Implemented |
| `GetMeetingParticipantAsync` | `GET /v1/meetings/{meetingId}/participants/{participantId}?tenantId={tenantId}` | ApiClient.Meetings | Implemented |
| `SendMeetingNotificationAsync` | `POST /v1/meetings/{meetingId}/notification` | — | Not yet implemented (commented out) |

### Team & Channel Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetTeamDetailsAsync` | `GET /v3/teams/{teamId}` | ApiClient.Teams | Needs update: calls `client.FetchTeamDetailsAsync()` which doesn't exist. Should use `client.Teams.GetByIdAsync()` |
| `GetTeamChannelsAsync` | `GET /v3/teams/{teamId}/conversations` | ApiClient.Teams | Needs update: calls `client.FetchChannelListAsync()` which doesn't exist. Should use `client.Teams.GetConversationsAsync()` |

### Batch Messaging Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `SendMessageToListOfUsersAsync` | `POST /v3/batch/conversation/users/` | — | Implemented in CompatTeamsInfo, but calls methods that don't exist on ApiClient yet (needs BatchClient) |
| `SendMessageToListOfChannelsAsync` | `POST /v3/batch/conversation/channels/` | — | Same — needs BatchClient |
| `SendMessageToAllUsersInTeamAsync` | `POST /v3/batch/conversation/team/` | — | Same — needs BatchClient |
| `SendMessageToAllUsersInTenantAsync` | `POST /v3/batch/conversation/tenant/` | — | Same — needs BatchClient |
| `SendMessageToTeamsChannelAsync` | Uses Bot Framework Adapter | BotAdapter.CreateConversationAsync | Implemented — does not use ApiClient |

### Batch Operation Management Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetOperationStateAsync` | `GET /v3/batch/conversation/{operationId}` | — | Calls methods that don't exist on ApiClient yet (needs BatchClient) |
| `GetPagedFailedEntriesAsync` | `GET /v3/batch/conversation/failedentries/{operationId}?continuationToken={token}` | — | Same — needs BatchClient |
| `CancelOperationAsync` | `DELETE /v3/batch/conversation/{operationId}` | — | Same — needs BatchClient |

## Client Distribution

### ConversationClient (6 methods) — Working

Used for member and participant operations in conversations and teams. Accessed via the `CompatConnectorClient` in TurnState.

- GetMemberAsync
- GetMembersAsync
- GetPagedMembersAsync
- GetTeamMemberAsync
- GetTeamMembersAsync
- GetPagedTeamMembersAsync

### ApiClient sub-clients (4 methods) — Working

`ApiClient` is stored in TurnState by `CompatAdapter`. Must be scoped to serviceUrl before use. Uses sub-clients:

- `ApiClient.Meetings.GetByIdAsync()` — GetMeetingInfoAsync
- `ApiClient.Meetings.GetParticipantAsync()` — GetMeetingParticipantAsync
- `ApiClient.Teams.GetByIdAsync()` — GetTeamDetailsAsync (needs rewiring from `FetchTeamDetailsAsync`)
- `ApiClient.Teams.GetConversationsAsync()` — GetTeamChannelsAsync (needs rewiring from `FetchChannelListAsync`)

### Bot Framework Adapter (1 method) — Working

- SendMessageToTeamsChannelAsync — uses `turnContext.Adapter.CreateConversationAsync()`

### Not yet implemented (8 methods)

These methods exist in `CompatTeamsInfo` but call ApiClient methods that don't exist yet. They need a new `BatchClient` sub-client and `MeetingClient.SendMeetingNotificationAsync`:

- SendMeetingNotificationAsync (commented out)
- SendMessageToListOfUsersAsync
- SendMessageToListOfChannelsAsync
- SendMessageToAllUsersInTeamAsync
- SendMessageToAllUsersInTenantAsync
- GetOperationStateAsync
- GetPagedFailedEntriesAsync
- CancelOperationAsync

## Migration Checklist

| Item | Status |
|---|---|
| Member operations via ConversationClient | Done |
| Meeting info via ApiClient.Meetings | Done |
| Meeting participant via ApiClient.Meetings | Done |
| Team details via ApiClient.Teams | Needs rewiring in CompatTeamsInfo |
| Team channels via ApiClient.Teams | Needs rewiring in CompatTeamsInfo |
| SendMessageToTeamsChannelAsync via adapter | Done |
| Batch messaging (4 methods) | Needs BatchClient on ApiClient |
| Batch operations (3 methods) | Needs BatchClient on ApiClient |
| Meeting notifications | Needs MeetingClient.SendMeetingNotificationAsync |
| CompatAdapter scopes ApiClient per-request | Needs update to call ForServiceUrl |

## Type Conversions

Key extension methods in `CompatActivity.cs` and `CompatTeamsInfo.Models.cs`:

| Extension Method | Source Type | Target Type | Status |
|---|---|---|---|
| `ToCompatTeamsChannelAccount` | `ConversationAccount` | BF `TeamsChannelAccount` | Working |
| `ToCompatTeamsPagedMembersResult` | `PagedMembersResult` | BF `TeamsPagedMembersResult` | Working |
| `ToCompatTeamDetails` | `Apps.Schema.Team` | BF `TeamDetails` | Working |
| `ToCompatTeamsMeetingParticipant` | `MeetingParticipant` | BF `TeamsMeetingParticipant` | Working |
| `ToCompatChannelInfo` | `TeamsChannel` | BF `ChannelInfo` | Working |
| `ToCompatBatchOperationState` | `BatchOperationState` | BF `BatchOperationState` | Commented out — needs `BatchOperationState` model |
| `ToCompatBatchFailedEntriesResponse` | `BatchFailedEntriesResponse` | BF `BatchFailedEntriesResponse` | Commented out — needs models |
| `ToCompatMeetingNotificationResponse` | `MeetingNotificationResponse` | BF `MeetingNotificationResponse` | Commented out — needs models |
| `FromCompatTeamMember` | BF `TeamMember` | `Apps.TeamMember` | Commented out — needs `TeamMember` model |

## Authentication

All methods use `AgenticIdentity` extracted from the turn context activity properties for authentication with the Teams services.

## Service URL

All API calls use the service URL from the turn context activity (`turnContext.Activity.ServiceUrl`). For `ApiClient` sub-client calls, this requires scoping via `ForServiceUrl()`:

```csharp
private static ApiClient GetTeamsApiClient(ITurnContext turnContext)
{
    return turnContext.TurnState.Get<ApiClient>()
        ?? throw new InvalidOperationException("This method requires ApiClient.");
}
```

The `CompatAdapter` must store a scoped `ApiClient` in TurnState for this to work.

## Testing

Integration tests are available in:
- `test/IntegrationTests/` — Tests for `ConversationClient` and `ApiClient` sub-clients
- `test/Microsoft.Teams.Bot.Core.Tests/CompatTeamsInfoTests.cs` — Tests for the compat layer

Tests require the `integration.runsettings` file with environment variables:
- `TEST_USER_ID`, `TEST_CONVERSATIONID`, `TEST_TEAMID`, `TEST_CHANNELID`, `TEST_MEETINGID`, `TEST_TENANTID`
- Azure AD credentials (`AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__ClientSecret`)

## References

- [ApiClient Design Document](ApiClient-Design.md) — Architecture and delegation patterns
- [CreateConversation API Behavior](CreateConversation-API-Behavior.md) — Detailed API behavior with request/response examples
- [Bot Framework TeamsInfo Source](https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder/Teams/TeamsInfo.cs)
