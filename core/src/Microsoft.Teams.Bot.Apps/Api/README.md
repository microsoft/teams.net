# TeamsApi REST Endpoint Mapping

This document maps the `TeamsApi` facade methods to their underlying REST endpoints.

## Conversations

### Activities

| Facade Method | HTTP Method | REST Endpoint |
|---------------|-------------|---------------|
| `Api.Conversations.Activities.SendAsync` | POST | `/v3/conversations/{conversationId}/activities/` |
| `Api.Conversations.Activities.UpdateAsync` | PUT | `/v3/conversations/{conversationId}/activities/{activityId}` |
| `Api.Conversations.Activities.DeleteAsync` | DELETE | `/v3/conversations/{conversationId}/activities/{activityId}` |
| `Api.Conversations.Activities.SendHistoryAsync` | POST | `/v3/conversations/{conversationId}/activities/history` |
| `Api.Conversations.Activities.GetMembersAsync` | GET | `/v3/conversations/{conversationId}/activities/{activityId}/members` |

### Members

| Facade Method | HTTP Method | REST Endpoint |
|---------------|-------------|---------------|
| `Api.Conversations.Members.GetAllAsync` | GET | `/v3/conversations/{conversationId}/members` |
| `Api.Conversations.Members.GetByIdAsync` | GET | `/v3/conversations/{conversationId}/members/{userId}` |
| `Api.Conversations.Members.GetPagedAsync` | GET | `/v3/conversations/{conversationId}/pagedmembers` |
| `Api.Conversations.Members.DeleteAsync` | DELETE | `/v3/conversations/{conversationId}/members/{memberId}` |

## Users

### Token

| Facade Method | HTTP Method | REST Endpoint | Base URL |
|---------------|-------------|---------------|----------|
| `Api.Users.Token.GetAsync` | GET | `/api/usertoken/GetToken` | `token.botframework.com` |
| `Api.Users.Token.ExchangeAsync` | POST | `/api/usertoken/exchange` | `token.botframework.com` |
| `Api.Users.Token.SignOutAsync` | DELETE | `/api/usertoken/SignOut` | `token.botframework.com` |
| `Api.Users.Token.GetAadTokensAsync` | POST | `/api/usertoken/GetAadTokens` | `token.botframework.com` |
| `Api.Users.Token.GetStatusAsync` | GET | `/api/usertoken/GetTokenStatus` | `token.botframework.com` |
| `Api.Users.Token.GetSignInResourceAsync` | GET | `/api/botsignin/GetSignInResource` | `token.botframework.com` |

## Teams

| Facade Method | HTTP Method | REST Endpoint |
|---------------|-------------|---------------|
| `Api.Teams.GetByIdAsync` | GET | `/v3/teams/{teamId}` |
| `Api.Teams.GetChannelsAsync` | GET | `/v3/teams/{teamId}/conversations` |

## Meetings

| Facade Method | HTTP Method | REST Endpoint |
|---------------|-------------|---------------|
| `Api.Meetings.GetByIdAsync` | GET | `/v1/meetings/{meetingId}` |
| `Api.Meetings.GetParticipantAsync` | GET | `/v1/meetings/{meetingId}/participants/{participantId}?tenantId={tenantId}` |
| `Api.Meetings.SendNotificationAsync` | POST | `/v1/meetings/{meetingId}/notification` |

## Batch

### Send Operations

| Facade Method | HTTP Method | REST Endpoint |
|---------------|-------------|---------------|
| `Api.Batch.SendToUsersAsync` | POST | `/v3/batch/conversation/users/` |
| `Api.Batch.SendToTenantAsync` | POST | `/v3/batch/conversation/tenant/` |
| `Api.Batch.SendToTeamAsync` | POST | `/v3/batch/conversation/team/` |
| `Api.Batch.SendToChannelsAsync` | POST | `/v3/batch/conversation/channels/` |

### Operation Management

| Facade Method | HTTP Method | REST Endpoint |
|---------------|-------------|---------------|
| `Api.Batch.GetStateAsync` | GET | `/v3/batch/conversation/{operationId}` |
| `Api.Batch.GetFailedEntriesAsync` | GET | `/v3/batch/conversation/failedentries/{operationId}` |
| `Api.Batch.CancelAsync` | DELETE | `/v3/batch/conversation/{operationId}` |

## Notes

- All endpoints under `Conversations`, `Teams`, `Meetings`, and `Batch` use the service URL from the activity context (e.g., `https://smba.trafficmanager.net/teams/`).
- All endpoints under `Users.Token` use the Bot Framework Token Service URL (`https://token.botframework.com`).
- Path parameters in `{braces}` are URL-encoded when constructing the request.
