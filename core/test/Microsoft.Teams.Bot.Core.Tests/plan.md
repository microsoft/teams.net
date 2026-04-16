# Integration Tests Plan

**Status (2026-04-15):** 155/155 (100%) pass rate excluding blocked categories. 162/199 (81%) full run estimate.

All blocked tests are tagged with `[Trait("Category", "...")]` for selective execution. Test parallelism is disabled via `xunit.runner.json` to avoid rate limiting.

```bash
# Clean run (all passing)
dotnet test core/test/Microsoft.Teams.Bot.Core.Tests \
  -s core/test/Microsoft.Teams.Bot.Core.Tests/integration.runsettings \
  --filter "Category!=needs-service-url&Category!=batch-isolation&Category!=needs-oauth-connection&Category!=unsupported-api&Category!=needs-valid-domains"
```

---

## Blocked categories

### `needs-service-url` — 5 tests

**What's missing:** Targeted messages (`?isTargetedActivity=true`) and reactions (`PUT .../reactions/{type}`) return errors on the current service URL (`smba.trafficmanager.net/teams/`). Targeted messages return `ServiceError: Unknown` on channel conversations. Reactions return `NotFound`.

**Action:** Get the latest service URL that supports these features. May also need a personal/groupChat conversation ID instead of a channel ID.

**Tests:**
| Test class | Test method |
|---|---|
| ConversationClientTest | UpdateTargetedActivity |
| ConversationClientTest | DeleteTargetedActivity |
| ConversationClientTest | AddRemoveReactionsToChat_Default |
| TeamsApiFacadeTests | Api_Conversations_Activities_Send_Update_DeleteTMAsync |
| TeamsApiFacadeTests | Api_Conversations_Reactions_AddAndDeleteAsync |

---

### `batch-isolation` — 20 tests

**What's missing:** The Teams API allows only 1 concurrent batch operation at a time, with 2-3 minute cooldowns between operations. Tests work individually but cannot run together even sequentially.

**Current behavior:** 2-3 tests pass per run (whichever run first), rest hit `TooManyRequests`. Test parallelism is disabled project-wide via `xunit.runner.json`, but batch operations still queue up.

**Action:** These tests are validated individually. For CI, exclude with `Category!=batch-isolation`. For manual validation, run one test at a time with pauses between:
```bash
dotnet test ... --filter "FullyQualifiedName~SendMessageToListOfUsers"
# wait 3 minutes
dotnet test ... --filter "FullyQualifiedName~GetOperationState"
```

**Tests:**
| Test class | Test method |
|---|---|
| TeamsApiClientTests | SendMessageToListOfUsers |
| TeamsApiClientTests | SendMessageToAllUsersInTenant |
| TeamsApiClientTests | SendMessageToAllUsersInTeam |
| TeamsApiClientTests | SendMessageToListOfChannels |
| TeamsApiClientTests | GetOperationState |
| TeamsApiClientTests | GetPagedFailedEntries |
| TeamsApiClientTests | CancelOperation |
| TeamsApiFacadeTests | Api_Batch_SendToUsersAsync |
| TeamsApiFacadeTests | Api_Batch_SendToTenantAsync |
| TeamsApiFacadeTests | Api_Batch_SendToTeamAsync |
| TeamsApiFacadeTests | Api_Batch_SendToChannelsAsync |
| TeamsApiFacadeTests | Api_Batch_CancelAsync |
| TeamsApiFacadeTests | Api_Batch_GetFailedEntriesAsync |
| CompatTeamsInfoTests | SendMessageToListOfUsersAsync_ReturnsOperationId |
| CompatTeamsInfoTests | SendMessageToAllUsersInTenantAsync_ReturnsOperationId |
| CompatTeamsInfoTests | SendMessageToAllUsersInTeamAsync_ReturnsOperationId |
| CompatTeamsInfoTests | SendMessageToListOfChannelsAsync_ReturnsOperationId |
| CompatTeamsInfoTests | GetOperationStateAsync_WithOperationId_ReturnsState |
| CompatTeamsInfoTests | GetPagedFailedEntriesAsync_WithOperationId_ReturnsFailedEntries |
| CompatTeamsInfoTests | CancelOperationAsync_WithOperationId_CancelsOperation |

---

### `needs-oauth-connection` — 8 tests

**What's missing:** No OAuth connection configured in Azure Bot Service. Tests need `TEST_CONNECTION_NAME` set in runsettings. The `GetAadTokens` endpoint also returns 404 — the API path may be wrong.

**Action:**
1. Configure an OAuth connection in Azure Portal > Bot Service > Settings > OAuth Connection Settings
2. Set `TEST_CONNECTION_NAME` in `integration.runsettings`
3. Investigate the `GetAadTokens` endpoint path (`/api/usertoken/GetAadTokens` returns 404)

**Tests:**
| Test class | Test method |
|---|---|
| UserTokenClientTests | GetTokenAsync_WithValidParams |
| UserTokenClientTests | GetSignInResource_WithValidParams |
| UserTokenClientTests | ExchangeTokenAsync_WithValidParams |
| UserTokenClientTests | GetAadTokensAsync_WithValidParams |
| TeamsApiFacadeTests | Api_Users_Token_GetAsync |
| TeamsApiFacadeTests | Api_Users_Token_ExchangeAsync |
| TeamsApiFacadeTests | Api_Users_Token_GetSignInResourceAsync |
| TeamsApiFacadeTests | Api_Users_Token_GetAadTokensAsync |

---

### `unsupported-api` — 8 tests

**What's missing:** These Bot Framework v3 REST APIs are not supported by Teams. Full details in `unsupported-apis.md` for the service team.

| Test class | Test method | API | Error |
|---|---|---|---|
| ConversationClientTest | GetConversations | `GET /v3/conversations` | 405 MethodNotAllowed |
| ConversationClientTest | DeleteConversationMember | `DELETE .../members/{id}` | 405 MethodNotAllowed |
| ConversationClientTest | SendConversationHistory | `POST .../history` | 400 Unknown activity type |
| ConversationClientTest | UploadAttachment | `POST .../attachments` | 404 NotFound |
| ConversationClientTest | CreateConversation_WithGroup | `POST /v3/conversations` (isGroup) | 400 Incorrect parameters |
| ConversationClientTest | CreateConversation_WithTopicName | `POST /v3/conversations` (topicName) | 400 Incorrect parameters |
| TeamsApiFacadeTests | Api_Conversations_Members_DeleteAsync | `DELETE .../members/{id}` | 405 MethodNotAllowed |
| TeamsApiFacadeTests | Api_Conversations_Activities_SendHistoryAsync | `POST .../history` | 400 Unknown activity type |

---

### `needs-valid-domains` — 3 tests

**What's missing:** Meeting notification tests get `"No valid content to render"`. The task module URL (`klljrqz0-3978.usw2.devtunnels.ms`) is not in the app manifest's `validDomains`. The meeting may also need to be active with a shareable stage.

**Action:** Add `klljrqz0-3978.usw2.devtunnels.ms` to `validDomains` in the Teams app manifest and reinstall.

**Tests:**
| Test class | Test method |
|---|---|
| TeamsApiClientTests | SendMeetingNotification |
| TeamsApiFacadeTests | Api_Meetings_SendNotificationAsync |
| CompatTeamsInfoTests | SendMeetingNotificationAsync_SendsNotification |

---

## Improvements (backlog)

### Move validation tests to unit test project
The `ThrowsOn*` parameter validation tests (~95 tests) don't need a live service. Moving them to a separate `Microsoft.Teams.Bot.Core.UnitTests` project would make CI faster and cleaner.

### Add XUnit test output logging
Use `MartinCostello.Logging.XUnit` (already in the test project) to route SDK logs to test output for easier failure diagnosis. Port approach from abandoned PR #408.

---

## SDK bugs fixed during test setup

| Bug | File | Fix |
|---|---|---|
| Batch send returns JSON string instead of operation ID | `TeamsApiClient.cs` | `SendAsync<string>` → `SendAsync<BatchOperationResponse>` + extract `.OperationId` |
| Meeting IDs unnecessarily URL-escaped | `TeamsApiClient.cs` | Removed `Uri.EscapeDataString` for meeting ID path segments |
| `CompatConnectorClient.BaseUri` throws NotImplementedException | `CompatConnectorClient.cs` | Returns service URL from `CompatConversations` |
| `CompatConnectorClient.Credentials` throws NotImplementedException | `CompatConnectorClient.cs` | Returns stub `TokenCredentials` |
| `CompatBotAdapter.CreateConversationAsync` not implemented | `CompatBotAdapter.cs` | Override delegates to `ConversationClient.CreateConversationAsync` |
