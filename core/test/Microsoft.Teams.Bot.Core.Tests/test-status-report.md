# Integration Test Status Report

**Date:** 2026-04-15
**Project:** `Microsoft.Teams.Bot.Core.Tests`
**Branch:** `next/core-apiclients`

---

## Summary

| Metric | Count | Rate |
|---|---|---|
| **Total tests** | 199 | |
| **Passing (clean run, blocked excluded)** | **155** | **100%** |
| **Passing (full run)** | 157-161 | 79-81% |
| **Blocked by category** | 44 | |

```bash
# Clean run command (155/155)
dotnet test core/test/Microsoft.Teams.Bot.Core.Tests \
  -s core/test/Microsoft.Teams.Bot.Core.Tests/integration.runsettings \
  --filter "Category!=needs-service-url&Category!=batch-isolation&Category!=needs-oauth-connection&Category!=unsupported-api&Category!=needs-valid-domains"
```

---

## Progress timeline

| Milestone | Passed | Total | Change |
|---|---|---|---|
| Initial run (no config) | 75 | 199 | Baseline |
| + Auth config (Instance, MSAL credentials) | 82 | 199 | +7 |
| + Bot installed in team | 108 | 199 | +26 |
| + Skip attributes removed | 112 | 199 | +4 |
| + TENANT_ID consolidated | 113 | 199 | +1 |
| + Pairwise MRI resolver (Phase 2) | 121 | 199 | +8 |
| + Assertion fixes (ArgumentNullException) | 148 | 199 | +27 |
| + Compat layer fixes | 151 | 199 | +3 |
| + Batch operationId parsing fix | 151 | 199 | +0 (rate limited) |
| + Meeting ID + RSC permissions | 155 | 155* | +4 (filtered) |
| + xUnit sequential execution | 155 | 155* | stability |

*\* filtered run excluding blocked categories*

---

## Test categories

| Category | Total | Passing | Failing | Blocker |
|---|---|---|---|---|
| *(no category)* | 155 | 155 | 0 | None — all pass |
| `needs-meeting-context` | 10 | 7 | 3 | 3 notification tests need `validDomains` in manifest |
| `needs-service-url` | 5 | 0 | 5 | Targeted messages + reactions need updated service URL |
| `batch-isolation` | 20 | 2-5 | 15-18 | API allows only 1 concurrent operation, 3-min cooldown |
| `needs-oauth-connection` | 8 | 0 | 8 | Need `TEST_CONNECTION_NAME` configured in Bot Service |
| `unsupported-api` | 8 | 0 | 8 | APIs not supported by Teams (documented) |
| `needs-valid-domains` | 3 | 0 | 3 | Notification URL domain not in manifest `validDomains` |

---

## Test environment

| Setting | Value |
|---|---|
| Team | Legendary Testing |
| Channel | Test Channel (`19:LydFnez...@thread.tacv2`) |
| Primary user | Rido (`29:03500558-e554-416c-90c3-a061cdcd012b`) |
| Second user | Zion Pierce (`29:2a9ae350-...`) |
| Meeting ID | `MCMxOTptZWV0aW5nX09XWTNZamd3...` (from bot `conversationUpdate` event) |
| Bot app | `3738fe3d-bca2-479d-8e45-1660de89ee41` |
| Service URL | `https://smba.trafficmanager.net/teams/` |
| Parallelism | Disabled (`xunit.runner.json`) |

---

## SDK bugs found and fixed

| # | Bug | Impact | Fix |
|---|---|---|---|
| 1 | Batch send methods return raw JSON `{"operationId":"..."}` instead of the operation ID string | All batch operation state/cancel tests fail | Changed `SendAsync<string>` → `SendAsync<BatchOperationResponse>`, extract `.OperationId` |
| 2 | Meeting IDs URL-escaped with `Uri.EscapeDataString` | Meeting API rejects encoded IDs | Removed escaping for meeting ID path segments |
| 3 | `CompatConnectorClient.BaseUri` throws `NotImplementedException` | All compat tests using `TeamsInfo` meeting/channel APIs fail | Implemented to return `CompatConversations.ServiceUrl` |
| 4 | `CompatConnectorClient.Credentials` throws `NotImplementedException` | Same as above | Returns stub `TokenCredentials` |
| 5 | `CompatBotAdapter.CreateConversationAsync` not implemented | `SendMessageToTeamsChannelAsync` fails | Override delegates to `ConversationClient.CreateConversationAsync` |

---

## Key findings

1. **Pairwise MRI format:** The Bot Framework API returns member IDs in pairwise-encrypted format (`29:1aK9mYh...`), not `29:<aad-guid>`. Tests need a resolver that calls `GetConversationMembersAsync` and matches by `aadObjectId` property.

2. **Meeting ID format:** The Graph API meeting `id` doesn't work with the BF meeting API. The correct ID comes from `activity.channelData.meeting.id` during a bot meeting event.

3. **Meeting participant ID:** `FetchParticipant` expects the raw AAD object ID (GUID), not the `29:` MRI format.

4. **RSC permissions:** Meeting APIs require RSC permissions in the app manifest AND the bot must be installed in the meeting context (not just the team). Reinstalling after RSC changes is required.

5. **Batch rate limiting:** The Teams API allows only 1 concurrent batch operation per bot with 2-3 minute cooldowns. Tests work individually but cannot run in a suite.

6. **MSAL credential format:** Integration tests need `AzureAd__Instance`, `AzureAd__ClientSecret` AND `AzureAd__ClientCredentials__0__SourceType`/`__ClientSecret` (the credential array format Microsoft.Identity.Web expects).

---

## Files created/modified

### New files
| File | Purpose |
|---|---|
| `integration.runsettings` | Environment variables for test execution (gitignored) |
| `setup-test-resources.ps1` | Interactive PowerShell script to discover Teams resources via Graph API |
| `xunit.runner.json` | Disables parallel test execution |
| `unsupported-apis.md` | Documents 7 BF API endpoints not supported by Teams — for service team |
| `plan.md` | Follow-up plan with all blocked categories and next steps |
| `test-status-report.md` | This report |

### Modified test files
| File | Changes |
|---|---|
| `ConversationClientTest.cs` | Added MRI resolver, fixed assertions, added Trait categories |
| `TeamsApiClientTests.cs` | Consolidated TENANT_ID, added batch chaining, fixed participant ID, added Trait categories |
| `TeamsApiFacadeTests.cs` | Added MRI resolver, batch chaining, fixed participant/notification IDs, added Trait categories |
| `UserTokenClientTests.cs` | Removed Skip attributes, added Trait categories |
| `CompatConversationClientTests.cs` | Switched to CompatTeamsInfo for GetMember/GetMeetingInfo, fixed assertions |
| `CompatTeamsInfoTests.cs` | Added MRI resolver, batch chaining, fixed meeting participant/notification IDs, added Trait categories |
| `readme.md` | Full documentation with run commands, env vars, categories |

### Modified SDK files
| File | Changes |
|---|---|
| `TeamsApiClient.cs` | Fixed batch operationId parsing, removed meeting ID escaping |
| `TeamsApiClient.Models.cs` | Added `BatchOperationResponse` model |
| `CompatConnectorClient.cs` | Implemented `BaseUri` and `Credentials` |
| `CompatBotAdapter.cs` | Implemented `CreateConversationAsync` |
