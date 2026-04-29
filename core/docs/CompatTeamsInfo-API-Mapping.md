# TeamsApiClient API Mapping

This document provides a comprehensive mapping of Bot Framework TeamsInfo static methods to their corresponding REST API endpoints and the Teams Bot Core SDK client implementations.

## Overview

The `TeamsApiClient` class provides a compatibility layer that adapts the Bot Framework v4 SDK TeamsInfo API to use the Teams Bot Core SDK. It maps 19 static methods organized into four functional categories.

## API Method Mappings

### Member & Participant Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetMemberAsync` | `GET /v3/conversations/{conversationId}/members/{userId}` | ConversationClient | Implemented |
| `GetMembersAsync` | `GET /v3/conversations/{conversationId}/members` | ConversationClient | Implemented (deprecated) |
| `GetPagedMembersAsync` | `GET /v3/conversations/{conversationId}/pagedmembers?pageSize=&continuationToken=` | ConversationClient | Implemented |
| `GetTeamMemberAsync` | `GET /v3/conversations/{teamId}/members/{userId}` | ConversationClient | Implemented |
| `GetTeamMembersAsync` | `GET /v3/conversations/{teamId}/members` | ConversationClient | Implemented (deprecated) |
| `GetPagedTeamMembersAsync` | `GET /v3/conversations/{teamId}/pagedmembers?pageSize=&continuationToken=` | ConversationClient | Implemented |

> `GetMembersAsync` and `GetTeamMembersAsync` are deprecated by Microsoft Teams. Use paged versions instead.

### Meeting Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetMeetingInfoAsync` | `GET /v1/meetings/{meetingId}` | ApiClient.Meetings | Implemented |
| `GetMeetingParticipantAsync` | `GET /v1/meetings/{meetingId}/participants/{participantId}?tenantId={tenantId}` | ApiClient.Meetings | Implemented |
| `SendMeetingNotificationAsync` | `POST /v1/meetings/{meetingId}/notification` | — | Commented out (needs `MeetingClient.SendMeetingNotificationAsync`) |

> `GetMeetingParticipantAsync` requires an AAD object ID for `participantId`, not a Bot Framework MRI or pairwise ID.

### Team & Channel Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetTeamDetailsAsync` | `GET /v3/teams/{teamId}` | ApiClient.Teams | Implemented — uses `client.Teams.GetByIdAsync()` |
| `GetTeamChannelsAsync` | `GET /v3/teams/{teamId}/conversations` | ApiClient.Teams | Implemented — uses `client.Teams.GetConversationsAsync()` |

### Batch Messaging Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `SendMessageToListOfUsersAsync` | `POST /v3/batch/conversation/users/` | — | Commented out (needs BatchClient) |
| `SendMessageToListOfChannelsAsync` | `POST /v3/batch/conversation/channels/` | — | Commented out (needs BatchClient) |
| `SendMessageToAllUsersInTeamAsync` | `POST /v3/batch/conversation/team/` | — | Commented out (needs BatchClient) |
| `SendMessageToAllUsersInTenantAsync` | `POST /v3/batch/conversation/tenant/` | — | Commented out (needs BatchClient) |
| `SendMessageToTeamsChannelAsync` | Uses Bot Framework Adapter | BotAdapter.CreateConversationAsync | Implemented |

### Batch Operation Management Methods

| Method | REST Endpoint | Client | Status |
|--------|--------------|--------|--------|
| `GetOperationStateAsync` | `GET /v3/batch/conversation/{operationId}` | — | Commented out (needs BatchClient) |
| `GetPagedFailedEntriesAsync` | `GET /v3/batch/conversation/failedentries/{operationId}?continuationToken=` | — | Commented out (needs BatchClient) |
| `CancelOperationAsync` | `DELETE /v3/batch/conversation/{operationId}` | — | Commented out (needs BatchClient) |

## Client Distribution

### ConversationClient (6 methods) — Implemented

Used for member and participant operations in conversations and teams. Accessed via the `CompatConnectorClient` in TurnState (`turnContext.TurnState.Get<IConnectorClient>()` → cast to `CompatConnectorClient` → `CompatConversations._client`).

- GetMemberAsync
- GetMembersAsync
- GetPagedMembersAsync
- GetTeamMemberAsync
- GetTeamMembersAsync
- GetPagedTeamMembersAsync

### ApiClient sub-clients (4 methods) — Implemented

`ApiClient` is stored in TurnState by `TeamsBotFrameworkHttpAdapter`. Must be scoped to serviceUrl via `ForServiceUrl()` before use. Uses sub-clients:

- `ApiClient.Meetings.GetByIdAsync()` — GetMeetingInfoAsync
- `ApiClient.Meetings.GetParticipantAsync()` — GetMeetingParticipantAsync
- `ApiClient.Teams.GetByIdAsync()` — GetTeamDetailsAsync
- `ApiClient.Teams.GetConversationsAsync()` — GetTeamChannelsAsync

### Bot Framework Adapter (1 method) — Implemented

- SendMessageToTeamsChannelAsync — uses `turnContext.Adapter.CreateConversationAsync()`

### Not yet implemented (8 methods) — Commented out

These methods are commented out in `TeamsApiClient` pending new client support:

- SendMeetingNotificationAsync — needs `MeetingClient.SendMeetingNotificationAsync`
- SendMessageToListOfUsersAsync — needs BatchClient
- SendMessageToListOfChannelsAsync — needs BatchClient
- SendMessageToAllUsersInTeamAsync — needs BatchClient
- SendMessageToAllUsersInTenantAsync — needs BatchClient
- GetOperationStateAsync — needs BatchClient
- GetPagedFailedEntriesAsync — needs BatchClient
- CancelOperationAsync — needs BatchClient

## Migration Checklist

| Item | Status |
|---|---|
| Member operations via ConversationClient | Done |
| Meeting info via ApiClient.Meetings | Done |
| Meeting participant via ApiClient.Meetings | Done |
| Team details via ApiClient.Teams | Done |
| Team channels via ApiClient.Teams | Done |
| SendMessageToTeamsChannelAsync via adapter | Done |
| Batch messaging (4 methods) | Needs BatchClient on ApiClient |
| Batch operations (3 methods) | Needs BatchClient on ApiClient |
| Meeting notifications | Needs MeetingClient.SendMeetingNotificationAsync |
| TeamsBotFrameworkHttpAdapter scopes ApiClient per-request | Needs update to call ForServiceUrl |

## Type Conversions

Key extension methods in `ActivitySchemaMapper.cs` and `TeamsApiClient.Models.cs`:

| Extension Method | Source Type | Target Type | Used By | Status |
|---|---|---|---|---|
| `ToCompatTeamsChannelAccount` | `ConversationAccount` | BF `TeamsChannelAccount` | GetMember/GetMembers/GetTeamMember/GetTeamMembers | Working |
| `ToCompatTeamsPagedMembersResult` | `PagedMembersResult` | BF `TeamsPagedMembersResult` | GetPagedMembers/GetPagedTeamMembers | Working |
| `ToCompatChannelInfo` | `TeamsChannel` | BF `ChannelInfo` | GetTeamChannelsAsync | Working |
| `ToCompatTeamDetails` | `Apps.Schema.Team` | BF `TeamDetails` | Defined but unused — GetTeamDetailsAsync uses inline mapping | Available |
| `ToCompatTeamsMeetingParticipant` | `MeetingParticipant` | BF `TeamsMeetingParticipant` | Defined but unused — GetMeetingParticipantAsync uses inline mapping | Available |
| `ToCompatBatchOperationState` | `BatchOperationState` | BF `BatchOperationState` | — | Commented out (needs models) |
| `ToCompatBatchFailedEntriesResponse` | `BatchFailedEntriesResponse` | BF `BatchFailedEntriesResponse` | — | Commented out (needs models) |
| `ToCompatMeetingNotificationResponse` | `MeetingNotificationResponse` | BF `MeetingNotificationResponse` | — | Commented out (needs models) |
| `FromCompatTeamMember` | BF `TeamMember` | `Apps.TeamMember` | — | Commented out (needs models) |

## Authentication

All methods use `AgenticIdentity` extracted from the turn context activity properties for authentication with the Teams services. The identity is obtained by converting the Bot Framework `Activity` to a `CoreActivity` and extracting agentic properties from `From.Properties`.

## Service URL

All API calls use the service URL from the turn context activity (`turnContext.Activity.ServiceUrl`):

- **ConversationClient** methods receive `serviceUrl` as a `Uri` parameter directly
- **ApiClient** sub-client methods use the serviceUrl baked into the scoped client instance

The `TeamsBotFrameworkHttpAdapter` must store a **scoped** `ApiClient` in TurnState for Teams/Meetings sub-clients to work. Currently it stores the unscoped base instance — this is a known pending fix (see [ApiClient Design](ApiClient-Design.md#integration-with-compatteamsinfo)).

## Testing

Integration tests are available in `core/test/IntegrationTests/`:

| Test File | Coverage |
|---|---|
| `TeamsApiClientTests.cs` | 14 tests covering all implemented TeamsApiClient methods via real API calls with a simulated TurnContext |
| `ApiClientTests.cs` | Direct tests for ApiClient sub-clients (Activities, Members, Teams, Meetings, UserToken, BotSignIn) |
| `ConversationClientTests.cs` | Core ConversationClient operations |
| `CreateConversationTests.cs` | Conversation creation patterns |

Tests require the `integration.runsettings` file with environment variables:
- `TEST_USER_ID`, `TEST_CONVERSATIONID`, `TEST_TEAMID`, `TEST_CHANNELID`, `TEST_MEETINGID`, `TEST_TENANTID`
- Azure AD credentials (`AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__ClientSecret`)

## References

- [ApiClient Design Document](ApiClient-Design.md) — Architecture, delegation patterns, and scoping
- [CreateConversation API Behavior](CreateConversation-API-Behavior.md) — Detailed API behavior with request/response examples
- [Bot Framework TeamsInfo Source](https://github.com/microsoft/botbuilder-dotnet/blob/main/libraries/Microsoft.Bot.Builder/Teams/TeamsInfo.cs)
