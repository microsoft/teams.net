# Microsoft.Teams.Bot.Core.Tests

Integration tests for the Teams Bot Core SDK API clients: `ConversationClient`, `TeamsApiClient`, `UserTokenClient`, the `TeamsApi` facade, and the compatibility layer (`CompatAdapter`, `CompatTeamsInfo`).

## Prerequisites

- A registered Azure AD app with Bot Channel Registration.
- The bot app must be **installed in the target team** (sideload via Teams UI or Admin Center using the app ZIP package).
- A Teams team with at least one channel and two members.
- A scheduled meeting in that team (for meeting/participant tests).

### Required Graph API permissions (for setup script)

| Permission | Purpose |
|---|---|
| `Group.Read.All` | List teams |
| `Team.ReadBasic.All` | Get team details |
| `Channel.ReadBasic.All` | List channels |
| `TeamMember.Read.All` | List team members |
| `User.Read.All` | Resolve user details |
| `OnlineMeetings.ReadWrite.All` | Create test meetings |

## Running the tests

```bash
# Run all passing tests (excludes blocked categories)
dotnet test core/test/Microsoft.Teams.Bot.Core.Tests \
  -s core/test/Microsoft.Teams.Bot.Core.Tests/integration.runsettings \
  --filter "Category!=needs-meeting-context&Category!=needs-service-url&Category!=batch-isolation&Category!=needs-oauth-connection&Category!=unsupported-api"

# Run everything (includes tests that may fail due to env/config)
dotnet test core/test/Microsoft.Teams.Bot.Core.Tests \
  -s core/test/Microsoft.Teams.Bot.Core.Tests/integration.runsettings

# Run only batch tests (one at a time to avoid rate limiting)
dotnet test core/test/Microsoft.Teams.Bot.Core.Tests \
  -s core/test/Microsoft.Teams.Bot.Core.Tests/integration.runsettings \
  --filter "Category=batch-isolation"
```

### Test categories

Tests are tagged with `[Trait("Category", "...")]` for selective execution:

| Category | Tests | Reason blocked |
|---|---|---|
| `needs-meeting-context` | 10 | Need real meeting ID from bot `conversationUpdate` event |
| `needs-service-url` | 5 | Targeted messages and reactions need updated service URL |
| `batch-isolation` | 20 | Must run sequentially to avoid `TooManyRequests` |
| `needs-oauth-connection` | 8 | Need `TEST_CONNECTION_NAME` configured in Bot Service |
| `unsupported-api` | 8 | APIs not supported by Teams — see `unsupported-apis.md` |

## Run Settings

Copy `integration.runsettings` and fill in the placeholder values. The file sets environment variables consumed by the test constructors via `AddEnvironmentVariables()`.

### Azure AD credentials (required)

These are resolved by `BotConfig.Resolve` from the `AzureAd` configuration section. MSAL requires both the flat `ClientSecret` key **and** the credential array format.

| Variable | Description |
|---|---|
| `AzureAd__Instance` | Azure AD login endpoint (`https://login.microsoftonline.com/`) |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientId` | App registration client (application) ID |
| `AzureAd__ClientSecret` | App registration client secret |
| `AzureAd__ClientCredentials__0__SourceType` | Set to `ClientSecret` |
| `AzureAd__ClientCredentials__0__ClientSecret` | Same client secret value |

> The `Instance` URL is required for MSAL to construct the authority URI. The credential array (`ClientCredentials__0__*`) is the format Microsoft.Identity.Web uses to bind client credentials. Both the flat `ClientSecret` and the array must be set.

### Test identifiers (required for most tests)

| Variable | Description | Example format |
|---|---|---|
| `TEST_CONVERSATIONID` | A conversation the bot is part of | `19:...@thread.tacv2` or `a:...` |
| `TEST_USER_ID` | A user's 29-prefixed MRI in the tenant | `29:...` |
| `TEST_TEAMID` | The team ID the bot is installed in | `19:...@thread.tacv2` |
| `TEST_CHANNELID` | A channel ID within that team | `19:...@thread.tacv2` |
| `TEST_MEETINGID` | A meeting ID (for participant tests) | Base64-encoded meeting ID |
| `TEST_TENANTID` | Tenant ID (used in meeting, batch, and conversation creation calls) | GUID |

### Optional

| Variable | Description | When needed |
|---|---|---|
| `TEST_SERVICEURL` | Bot Framework service URL. Defaults to `https://smba.trafficmanager.net/teams/` | Override for sovereign clouds |
| `TEST_USER_ID_2` | A second user MRI | Group conversation tests (currently skipped) |
| `TEST_CONNECTION_NAME` | OAuth connection name from Bot Channel Registration | User token tests (currently skipped) |
| `TEST_OPERATION_ID` | A batch operation ID | Batch state/cancel tests (currently skipped) |

### Agentic identity (optional, not yet covered)

| Variable | Description |
|---|---|
| `TEST_AGENTIC_APPID` | Agentic app ID |
| `TEST_AGENTIC_USERID` | Agentic user ID |

When both are provided, tests exercise agentic identity flows. When absent, tests fall back to `null` agentic identity and still pass.

## Test coverage by class

| Test class | Covers | Key env vars |
|---|---|---|
| `ConversationClientTest` | Send, update, delete activities; members; paged members; reactions; targeted ops | `TEST_CONVERSATIONID`, `TEST_USER_ID`, `TEST_CHANNELID` |
| `TeamsApiClientTests` | Team details, channel list, meeting info, participant, batch operations | `TEST_TEAMID`, `TEST_MEETINGID`, `TEST_TENANTID` |
| `TeamsApiFacadeTests` | `TeamsApi` hierarchical facade delegation to underlying clients | All of the above |
| `UserTokenClientTests` | Get/exchange/sign-out tokens, AAD tokens, sign-in resource | `TEST_USER_ID`, `TEST_CONNECTION_NAME` |
| `CompatConversationClientTests` | Bot Framework compat adapter conversation operations | `TEST_CONVERSATIONID`, `TEST_USER_ID` |
| `CompatTeamsInfoTests` | Compat `TeamsInfo` static methods (members, team details, channels, meetings, batch) | All of the above |
