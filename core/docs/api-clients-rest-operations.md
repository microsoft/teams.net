# REST Operations Report — API Clients

## ConversationClient (`Microsoft.Teams.Bot.Core`)

| Method | HTTP Verb | Relative URL | Tests | Test Traits |
|--------|-----------|-------------|-------|-------------|
| `SendActivityAsync` | **POST** | `/v3/conversations/{conversationId}/activities/` | `ConversationClientTest.SendActivityToChannel`, `SendActivityToPersonalChat_FailsWithBad_ConversationId`, unit: `SendActivityAsync_*` (8 tests) | — |
| `UpdateActivityAsync` | **PUT** | `/v3/conversations/{conversationId}/activities/{activityId}` | `ConversationClientTest.UpdateActivity`, unit: `UpdateActivityAsync_WithIsTargeted_AppendsQueryString` | — |
| `UpdateTargetedActivityAsync` | **PUT** | `/v3/conversations/{conversationId}/activities/{activityId}?isTargetedActivity=true` | `ConversationClientTest.UpdateTargetedActivity`, unit: `UpdateTargetedActivityAsync_AppendsQueryStringWithoutRecipient` | — |
| `DeleteActivityAsync` | **DELETE** | `/v3/conversations/{conversationId}/activities/{activityId}` | `ConversationClientTest.DeleteActivity`, `DeleteActivity_WithCoreActivityOverload`, unit: `DeleteActivityAsync_*` (2 tests) | — |
| `DeleteTargetedActivityAsync` | **DELETE** | `/v3/conversations/{conversationId}/activities/{activityId}?isTargetedActivity=true` | `ConversationClientTest.DeleteTargetedActivity`, unit: `DeleteTargetedActivityAsync_AppendsQueryString` | — |
| `GetConversationMembersAsync` | **GET** | `/v3/conversations/{conversationId}/members` | `ConversationClientTest.GetConversationMembers`, `GetConversationMembersInChannel` | — |
| `GetConversationMemberAsync<T>` | **GET** | `/v3/conversations/{conversationId}/members/{userId}` | `ConversationClientTest.GetConversationMember` | — |
| `GetConversationsAsync` | **GET** | `/v3/conversations` | `ConversationClientTest.GetConversations` | `unsupported-api` |
| `GetActivityMembersAsync` | **GET** | `/v3/conversations/{conversationId}/activities/{activityId}/members` | `ConversationClientTest.GetActivityMembers` | — |
| `CreateConversationAsync` | **POST** | `/v3/conversations` | `ConversationClientTest.CreateConversation_WithMembers`, `_WithGroup`, `_WithTopicName`, `_WithInitialActivity`, `_WithChannelData` | Some: `unsupported-api` |
| `GetConversationPagedMembersAsync` | **GET** | `/v3/conversations/{conversationId}/pagedmembers` | `ConversationClientTest.GetConversationPagedMembers`, `_WithPageSize` | — |
| `DeleteConversationMemberAsync` | **DELETE** | `/v3/conversations/{conversationId}/members/{memberId}` | `ConversationClientTest.DeleteConversationMember` | `unsupported-api` |
| `SendConversationHistoryAsync` | **POST** | `/v3/conversations/{conversationId}/activities/history` | `ConversationClientTest.SendConversationHistory` | `unsupported-api` |
| `UploadAttachmentAsync` | **POST** | `/v3/conversations/{conversationId}/attachments` | `ConversationClientTest.UploadAttachment` | `unsupported-api` |
| `AddReactionAsync` | **PUT** | `/v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}` | `ConversationClientTest.AddRemoveReactionsToChat_Default` | — |
| `DeleteReactionAsync` | **DELETE** | `/v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}` | `ConversationClientTest.AddRemoveReactionsToChat_Default` | — |

## TeamsApiClient (`Microsoft.Teams.Bot.Apps`)

| Method | HTTP Verb | Relative URL | Tests | Test Traits |
|--------|-----------|-------------|-------|-------------|
| `FetchChannelListAsync` | **GET** | `/v3/teams/{teamId}/conversations` | `TeamsApiClientTests.FetchChannelList`, `_FailsWithInvalidTeamId` | — |
| `FetchTeamDetailsAsync` | **GET** | `/v3/teams/{teamId}` | `TeamsApiClientTests.FetchTeamDetails`, `_FailsWithInvalidTeamId` | — |
| `FetchMeetingInfoAsync` | **GET** | `/v1/meetings/{meetingId}` | `TeamsApiClientTests.FetchMeetingInfo`, `_FailsWithInvalidMeetingId` | `needs-meeting-context` |
| `FetchParticipantAsync` | **GET** | `/v1/meetings/{meetingId}/participants/{participantId}?tenantId={tenantId}` | `TeamsApiClientTests.FetchParticipant` | `needs-meeting-context` |
| `SendMeetingNotificationAsync` | **POST** | `/v1/meetings/{meetingId}/notification` | `TeamsApiClientTests.SendMeetingNotification` | `needs-meeting-context`, `needs-valid-domains` |
| `SendMessageToListOfUsersAsync` | **POST** | `/v3/batch/conversation/users/` | `TeamsApiClientTests.SendMessageToListOfUsers` | `batch-isolation` |
| `SendMessageToAllUsersInTenantAsync` | **POST** | `/v3/batch/conversation/tenant/` | `TeamsApiClientTests.SendMessageToAllUsersInTenant` | `batch-isolation` |
| `SendMessageToAllUsersInTeamAsync` | **POST** | `/v3/batch/conversation/team/` | `TeamsApiClientTests.SendMessageToAllUsersInTeam` | `batch-isolation` |
| `SendMessageToListOfChannelsAsync` | **POST** | `/v3/batch/conversation/channels/` | `TeamsApiClientTests.SendMessageToListOfChannels` | `batch-isolation` |
| `GetOperationStateAsync` | **GET** | `/v3/batch/conversation/{operationId}` | `TeamsApiClientTests.GetOperationState`, `_FailsWithInvalidOperationId` | `batch-isolation` |
| `GetPagedFailedEntriesAsync` | **GET** | `/v3/batch/conversation/failedentries/{operationId}` | `TeamsApiClientTests.GetPagedFailedEntries` | `batch-isolation` |
| `CancelOperationAsync` | **DELETE** | `/v3/batch/conversation/{operationId}` | `TeamsApiClientTests.CancelOperation` | `batch-isolation` |

## UserTokenClient (`Microsoft.Teams.Bot.Core`)

Base URL: `https://token.botframework.com` (configurable via `UserTokenApiEndpoint`)

| Method | HTTP Verb | Relative URL | Tests | Test Traits |
|--------|-----------|-------------|-------|-------------|
| `GetTokenStatusAsync` | **GET** | `/api/usertoken/GetTokenStatus` | `UserTokenClientTests.GetTokenStatusAsync_WithValidParams` | — |
| `GetTokenAsync` | **GET** | `/api/usertoken/GetToken` | `UserTokenClientTests.GetTokenAsync_WithValidParams` | `needs-oauth-connection` |
| `GetSignInResource` | **GET** | `/api/botsignin/GetSignInResource` | `UserTokenClientTests.GetSignInResource_WithValidParams` | `needs-oauth-connection` |
| `ExchangeTokenAsync` | **POST** | `/api/usertoken/exchange` | `UserTokenClientTests.ExchangeTokenAsync_WithValidParams` | `needs-oauth-connection` |
| `SignOutUserAsync` | **DELETE** | `/api/usertoken/SignOut` | `UserTokenClientTests.SignOutUserAsync_WithValidParams` | — |
| `GetAadTokensAsync` | **POST** | `/api/usertoken/GetAadTokens` | `UserTokenClientTests.GetAadTokensAsync_WithValidParams` | `needs-oauth-connection` |

## Summary

| Client | GET | POST | PUT | DELETE | Total |
|--------|-----|------|-----|--------|-------|
| **ConversationClient** | 5 | 4 | 3 | 4 | **16** |
| **TeamsApiClient** | 5 | 5 | 0 | 1 | **11** |
| **UserTokenClient** | 3 | 2 | 0 | 1 | **6** |
| **Total** | **13** | **11** | **3** | **6** | **33** |

## Test Trait Legend

- **`unsupported-api`** — API known to be unsupported by the Teams backend
- **`needs-meeting-context`** — Requires an active meeting context to run
- **`needs-oauth-connection`** — Requires OAuth connection configuration
- **`batch-isolation`** — Batch operations requiring isolation due to tenant-wide side effects
- **`needs-service-url`** — Requires a valid Teams service URL
- **`needs-valid-domains`** — Requires valid domain configuration

Additionally, all three clients have comprehensive **argument validation tests** (30+ for ConversationClient, 20+ for TeamsApiClient) verifying null/empty parameter handling. The `TeamsApiFacadeTests` class provides a second layer of integration tests covering the same operations through the high-level `TeamsApi` facade.
