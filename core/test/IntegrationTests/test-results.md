# Integration Test Results

**Date:** 2026-04-17
**Runtime:** .NET 10.0 | xUnit 3.1.4
**Duration:** 1m 17s
**Result: 55 Passed, 0 Failed, 12 Skipped (67 total)**

---

## Summary by Test Class

| Test Class | Passed | Skipped | Failed |
|---|:---:|:---:|:---:|
| ConversationClientTests | 6 | 1 | 0 |
| ApiClientTests | 14 | 8 | 0 |
| CompatTeamsInfoTests | 14 | 0 | 0 |
| CreateConversationTests | 7 | 3 | 0 |
| CreateConversationDiagnosticTests | 13 | 0 | 0 |

---

## ConversationClientTests (6/7)

| Test | Result | Duration |
|---|---|---:|
| SendActivity | Passed | 712 ms |
| UpdateActivity | Passed | 1 s |
| DeleteActivity | Passed | 2 s |
| GetConversationMembers | Passed | 543 ms |
| GetConversationMember | Passed | 1 s |
| GetPagedMembers | Passed | 1 s |
| AddAndDeleteReaction | **Skipped** | - |

## ApiClientTests (14/22)

### Activities

| Test | Result | Duration |
|---|---|---:|
| Activities_CreateAsync | Passed | 558 ms |
| Activities_UpdateAsync | Passed | 1 s |
| Activities_ReplyAsync | Passed | 1 s |
| Activities_DeleteAsync | Passed | 3 s |
| Activities_CreateTargetedAsync | **Skipped** | - |
| Activities_UpdateTargetedAsync | **Skipped** | - |
| Activities_DeleteTargetedAsync | **Skipped** | - |

### Members

| Test | Result | Duration |
|---|---|---:|
| Members_GetAsync | Passed | 862 ms |
| Members_GetByIdAsync | Passed | 2 s |
| Members_GetByIdAsync_AsTeamsConversationAccount | Passed | 1 s |

### Reactions

| Test | Result | Duration |
|---|---|---:|
| Reactions_AddAndDelete | **Skipped** | - |

### Teams

| Test | Result | Duration |
|---|---|---:|
| Teams_GetByIdAsync | Passed | 240 ms |
| Teams_GetConversationsAsync | Passed | 362 ms |

### Meetings

| Test | Result | Duration |
|---|---|---:|
| Meetings_GetByIdAsync | Passed | 2 s |
| Meetings_GetParticipantAsync | Passed | 1m 1s |

### Bots - SignIn

| Test | Result | Duration |
|---|---|---:|
| Bots_SignIn_GetUrlAsync | **Skipped** | - |
| Bots_SignIn_GetResourceAsync | **Skipped** | - |

### Users - Token

| Test | Result | Duration |
|---|---|---:|
| Users_Token_GetStatusAsync | Passed | 1 s |
| Users_Token_GetAsync | **Skipped** | - |
| Users_Token_SignOutAsync | **Skipped** | - |

### Other

| Test | Result | Duration |
|---|---|---:|
| ForServiceUrl_CreatesScopedClient | Passed | 596 ms |

## CompatTeamsInfoTests (14/14)

| Test | Result | Duration |
|---|---|---:|
| GetMemberAsync_ReturnsTeamsChannelAccount | Passed | 1 s |
| GetMembersAsync_ReturnsTeamsChannelAccounts | Passed | 828 ms |
| GetPagedMembersAsync_ReturnsPaged | Passed | 666 ms |
| GetTeamMemberAsync_ReturnsTeamsChannelAccount | Passed | 1 s |
| GetMemberAsync_WithTeamScope_DelegatesToGetTeamMember | Passed | 1 s |
| GetTeamMembersAsync_ReturnsMembers | Passed | 2 s |
| GetPagedTeamMembersAsync_ReturnsPaged | Passed | 632 ms |
| GetTeamDetailsAsync_ReturnsDetails | Passed | 350 ms |
| GetTeamDetailsAsync_InfersTeamIdFromActivity | Passed | 408 ms |
| GetTeamChannelsAsync_ReturnsChannels | Passed | 551 ms |
| GetTeamChannelsAsync_InfersTeamIdFromActivity | Passed | 538 ms |
| GetMeetingParticipantAsync_ReturnsParticipant | Passed | 1m 1s |
| GetTeamDetailsAsync_ThrowsWithoutTeamScope | Passed | 3 ms |
| GetTeamChannelsAsync_ThrowsWithoutTeamScope | Passed | 1 ms |
| GetMemberAsync_ThrowsWithNullUserId | Passed | 1 ms |

## CreateConversationTests (7/10)

| Test | Result | Duration |
|---|---|---:|
| Core_CreatePersonalChat | Passed | 1 s |
| Core_CreatePersonalChat_WithInitialActivity | Passed | 1 s |
| Core_CreatePersonalChat_AndSendMessage | Passed | 1 s |
| Core_CreateGroupChat | **Skipped** | - |
| Core_CreateGroupChat_AndSendMessage | **Skipped** | - |
| Core_CreateChannelThread | Passed | 411 ms |
| ApiClient_CreatePersonalChat | Passed | 1 s |
| ApiClient_CreatePersonalChat_AndSendViaActivities | Passed | 2 s |
| ApiClient_CreateGroupChat | **Skipped** | - |
| ApiClient_CreateChannelThread | Passed | 440 ms |
| ApiClient_CreateChannelThread_AndReply | Passed | 1 s |

## CreateConversationDiagnosticTests (13/13)

| Test | Result | Duration |
|---|---|---:|
| PersonalChat_MinimalParams | Passed | 1 s |
| PersonalChat_WithBot | Passed | 983 ms |
| PersonalChat_WithInitialActivity | Passed | 1 s |
| GroupChat_OneMember_WithBot | Passed | 948 ms |
| GroupChat_OneMember_IsGroupTrue | Passed | 776 ms |
| GroupChat_TwoMembers_WithBot | Passed | 623 ms |
| GroupChat_TwoMembers_NoBotNoChannelData | Passed | 706 ms |
| GroupChat_TwoMembers_WithTopicAndActivity | Passed | 639 ms |
| GroupChat_TwoMembers_WithBotAndChannelData | Passed | 638 ms |
| GroupChat_ThreeMembers | Passed | 636 ms |
| ChannelThread_NoActivity | Passed | 53 ms |
| ChannelThread_WithActivity | Passed | 517 ms |
| ChannelThread_WithMembersAndActivity | Passed | 2 s |

---

## Skipped Tests — Rationale

| Test | Reason |
|---|---|
| ConversationClientTests.AddAndDeleteReaction | Reactions endpoint does not exist in Teams Bot Framework API (experimental) |
| ApiClientTests.Reactions_AddAndDelete | Reactions endpoint does not exist in Teams Bot Framework API (experimental) |
| ApiClientTests.Activities_CreateTargetedAsync | Targeted activities not supported in team channel conversations |
| ApiClientTests.Activities_UpdateTargetedAsync | Targeted activities not supported in team channel conversations |
| ApiClientTests.Activities_DeleteTargetedAsync | Targeted activities not supported in team channel conversations |
| ApiClientTests.Bots_SignIn_GetUrlAsync | Requires valid OAuth connection name configured for the bot |
| ApiClientTests.Bots_SignIn_GetResourceAsync | Requires valid OAuth connection name configured for the bot |
| ApiClientTests.Users_Token_GetAsync | Requires TEST_CONNECTION_NAME configured with an OAuth connection |
| ApiClientTests.Users_Token_SignOutAsync | Requires TEST_CONNECTION_NAME configured with an OAuth connection |
| CreateConversationTests.Core_CreateGroupChat | Teams Bot Framework API does not support group chat creation |
| CreateConversationTests.Core_CreateGroupChat_AndSendMessage | Teams Bot Framework API does not support group chat creation |
| CreateConversationTests.ApiClient_CreateGroupChat | Teams Bot Framework API does not support group chat creation |

---

## API Coverage Summary

| Client | Methods | Tested | Skipped | Not Testable |
|---|:---:|:---:|:---:|:---:|
| ActivityClient | 7 | 4 | 3 (targeted) | - |
| MemberClient | 4 | 3 | - | 1 (Delete - destructive) |
| ReactionClient | 2 | - | 2 (no endpoint) | - |
| TeamClient | 2 | 2 | - | - |
| MeetingClient | 2 | 2 | - | - |
| V3ConversationClient | 1 | 1 | - | - |
| BotSignInClient | 2 | - | 2 (needs OAuth) | - |
| V3UserTokenClient | 5 | 1 | 2 (needs OAuth) | 2 (GetAad, Exchange) |
| ApiClient | 1 | 1 | - | - |
| **Total** | **26** | **14** | **9** | **3** |

---

## Notes

- **ILogger output** is routed to xUnit test output via `MartinCostello.Logging.XUnit`.
- **Meetings_GetParticipantAsync** tests iterate all conversation members looking for one with an AAD object ID. In this test tenant, none have one, so the test passes with a graceful early return.
- **Targeted activities** require a 1:1 or group chat conversation (not a team channel). The test conversation (`TEST_CONVERSATIONID`) is a team channel.
- **BotSignIn and UserToken** methods require an OAuth connection name (`TEST_CONNECTION_NAME`) configured in the bot's Azure registration.
- All tests use real API calls against the Teams Bot Framework service via environment variables in `integration.runsettings`.
