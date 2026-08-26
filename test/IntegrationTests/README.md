# Teams SDK Integration Tests

This project runs integration tests against Teams Server using bot and agentic identities.

## Prerequisites

- .NET 10 SDK
- A BAMI tenant with:
  - Bot app registration (client ID + secret)
  - Agentic app registration (client ID + secret) — optional
  - A team with at least one channel
  - At least 3 non-bot users in the test conversation (`GroupChat_ThreeMembers` needs three)
  - A regular scheduled meeting (not a channel meeting) with the bot app installed *in the meeting*
  - An OAuth connection on the Azure Bot resource named to match `TEST_CONNECTION_NAME`

## RunSettings

Tests are configured via `.runsettings` files that set environment variables. Four configurations exist:

| File | Identity | Environment |
|------|----------|-------------|
| `botid-prod.runsettings` | Bot (app-only) | Production |
| `botid-canary.runsettings` | Bot (app-only) | Canary |
| `agenticid-prod.runsettings` | Agentic | Production |
| `agenticid-canary.runsettings` | Agentic | Canary |

Place your `.runsettings` files in the `.runsettings/` directory (gitignored).

> Verify that before you write a secret into it. The ignore rule pointed at a
> pre-migration path until [#659](https://github.com/microsoft/teams.net/pull/659), so on
> older branches this directory is **not** ignored despite what this line says:
>
> ```bash
> git check-ignore -v test/IntegrationTests/.runsettings/botid-prod.runsettings
> # no output means NOT ignored — fix .gitignore first
> ```

### Required environment variables

```xml
<EnvironmentVariables>
  <!-- Azure AD App Registration -->
  <AzureAd__Instance>https://login.microsoftonline.com/</AzureAd__Instance>
  <AzureAd__TenantId>YOUR_TENANT_ID</AzureAd__TenantId>
  <AzureAd__ClientId>YOUR_CLIENT_ID</AzureAd__ClientId>
  <AzureAd__ClientCredentials__0__SourceType>ClientSecret</AzureAd__ClientCredentials__0__SourceType>
  <AzureAd__ClientCredentials__0__ClientSecret>YOUR_SECRET</AzureAd__ClientCredentials__0__ClientSecret>

  <!-- Teams Service URL -->
  <TEST_SERVICEURL>https://smba.trafficmanager.net/amer/YOUR_TENANT_ID/</TEST_SERVICEURL>

  <!-- Core test identifiers -->
  <TEST_CONVERSATIONID>19:...@thread.tacv2</TEST_CONVERSATIONID>
  <TEST_USER_ID>29:...</TEST_USER_ID>
  <TEST_TEAMID>19:...@thread.tacv2</TEST_TEAMID>
  <TEST_CHANNELID>19:...@thread.tacv2</TEST_CHANNELID>
  <!-- base64 of "0#<19:meeting_...@thread.v2>#0" -->
  <TEST_MEETINGID>MCM...</TEST_MEETINGID>
  <TEST_TENANTID>YOUR_TENANT_ID</TEST_TENANTID>

  <!-- Agentic identity (optional — set both or neither) -->
  <TEST_AGENTIC_APPID></TEST_AGENTIC_APPID>
  <TEST_AGENTIC_USERID></TEST_AGENTIC_USERID>

  <!-- Optional -->
  <!-- TEST_USER_ID_2 is read by the fixture but consumed by no test today; safe to omit.
       Multi-member tests read the live conversation roster instead. -->
  <TEST_USER_ID_2>29:...</TEST_USER_ID_2>
  <TEST_CONNECTION_NAME>aadv2</TEST_CONNECTION_NAME>
</EnvironmentVariables>
```

## Running Tests

```bash
# From the repository's test/ directory:
dotnet test IntegrationTests/IntegrationTests.csproj \
  --settings IntegrationTests/.runsettings/botid-prod.runsettings -v d

# By category
dotnet test IntegrationTests/IntegrationTests.csproj \
  --settings IntegrationTests/.runsettings/botid-prod.runsettings \
  --filter "Category=Activities"

# Exclude slow diagnostic tests
dotnet test IntegrationTests/IntegrationTests.csproj \
  --settings IntegrationTests/.runsettings/botid-prod.runsettings \
  --filter "Category!=Diagnostic"

# With TRX output for CI
dotnet test IntegrationTests/IntegrationTests.csproj \
  --settings IntegrationTests/.runsettings/botid-prod.runsettings \
  --logger "trx;LogFileName=botid-prod.trx"
```

### Throttling

The tenant enforces a call quota that a single full run already approaches. Running the full suite twice in quick succession produces a large batch of `TooManyRequests` / `"API calls quota exceeded"` failures that look exactly like real breakage. One observed back-to-back run reported 36 failures, 35 of which were quota errors rather than genuine problems.

- Leave roughly 10 minutes between full runs.
- When triaging any unexpected failure, grep the output for `TooManyRequests` before investigating anything else.
- While iterating on one area, use `--filter` to run just that category instead of the full suite.

### Trait Categories

| Category | Tests | Description |
|----------|-------|-------------|
| `Activities` | 14 | Send, update, delete, reply (including targeted) |
| `Members` | 11 | Get members, paged, by ID |
| `Conversations` | 11 | Create 1:1, group, channel thread |
| `Reactions` | 1 | Add and delete reactions |
| `Teams` | 6 | Get team details, channels |
| `Meetings` | 3 | Get participant, meeting details |
| `Users` | 5 | Sign-in URL and resource, token get, status, sign-out |
| `Client` | 1 | ForServiceUrl scoped client |
| `Diagnostic` | 13 | Conversation creation matrix |
| `ErrorHandling` | 3 | Error cases (compat layer) |

> These account for 68 of the suite's 72 tests; the remaining 4 carry no `Category` trait. A filter naming a category that does not exist prints `No test matches the given testcase filter` and **exits 0**, so a typo looks exactly like a clean pass.

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Many unrelated tests fail with `TooManyRequests` / `"API calls quota exceeded"` | Tenant quota exhausted by consecutive runs | Wait ~10 minutes and re-run. These are not real failures. |
| Every test fails during fixture initialization with an `AADSTS` error | Tenant expired or credentials wrong | Re-provision the tenant and regenerate the runsettings |
| Fixture throws `... environment variable not set` | A required variable is missing; `TEST_MEETINGID` and `TEST_TENANTID` are required even for unrelated categories | Populate the runsettings fully |
| `GroupChat_*` fail on `Assert.NotNull` before any API call | Conversation has fewer than 3 non-bot members | Add users to the channel |
| Meetings tests return `404 ConversationNotFound` | `TEST_MEETINGID` is wrong, stale, or a placeholder | Re-encode from the current meeting thread ID |
| Meetings tests return `403 BotNotInConversationRoster` | App is not installed in the meeting | Install it in the meeting, then message the bot in the meeting chat |
| `Meetings_GetByIdAsync` returns `403 NotEnoughPermissions` | Manifest lacks RSC `OnlineMeeting.ReadBasic.Chat` | Add the RSC permission, bump the manifest version, reinstall in the meeting |
| `Users_GetSignInResourceAsync` returns `400 Could not find Connection Setting` | OAuth connection missing on the Azure Bot resource | Create a connection named to match `TEST_CONNECTION_NAME` |
| A filtered run reports "No test matches" and exits 0 | Category name does not exist | Check the [Trait Categories](#trait-categories) table |

## Architecture

- **`IntegrationTestFixture`** — Shared xUnit fixture that configures DI, acquires auth tokens, and caches conversation members (to avoid 429 throttling).
- Tests use `IClassFixture<IntegrationTestFixture>` so auth + member lookup happens once per test class.
- Parallelization is disabled (`xunit.runner.json`) since tests share the same conversation.

## Known Limitations

- **Expected skips**: 5 tests are skipped by design via `Skip.If` guards (paged members and reactions on canary). Skips are not failures. A correctly provisioned tenant should report 72 total with 5 skipped and 0 failed.
- **Agentic identity**: Targeted activities, paged members, and reactions return 500/404 with agentic identity. These are service-side limitations pending investigation.
- **Group chat creation**: Bot-only identity cannot create group chats with `IsGroup=true` + multiple members via the conversations API.
- **User token tests**: `SignIn` and `Users` token tests are skipped when agentic identity is configured (not supported).
- **BAMI tenant expiration**: Test resources expire every few months. Re-provision and update runsettings when the tenant rotates.

## Cross-SDK Runbook

For provisioning, secret rotation, tenant renewal, and troubleshooting across all SDKs, see the shared runbook:

👉 [Integration Test Runbook](https://dev.azure.com/DomoreexpGithub/Github_Pipelines/_wiki/wikis/Github%20Pipelines%20Wiki/1/Teams-SDK-Integration-Test-Runbook) (internal only)
