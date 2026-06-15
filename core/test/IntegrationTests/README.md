# Teams SDK Integration Tests

This project runs integration tests against Teams Server (SMBA/APX) using bot and agentic identities.

## Prerequisites

- .NET 10 SDK
- A BAMI tenant with:
  - Bot app registration (client ID + secret)
  - Agentic app registration (client ID + secret) — optional
  - A team with at least one channel
  - A scheduled meeting
  - At least 2 test users in the conversation

## RunSettings

Tests are configured via `.runsettings` files that set environment variables. Four configurations exist:

| File | Identity | Environment |
|------|----------|-------------|
| `botid-prod.runsettings` | Bot (app-only) | Production |
| `botid-canary.runsettings` | Bot (app-only) | Canary |
| `agenticid-prod.runsettings` | Agentic | Production |
| `agenticid-canary.runsettings` | Agentic | Canary |

Place your `.runsettings` files in the `.runsettings/` directory (gitignored).

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
  <TEST_MEETINGID>MCM...</TEST_MEETINGID>
  <TEST_TENANTID>YOUR_TENANT_ID</TEST_TENANTID>

  <!-- Agentic identity (optional — set both or neither) -->
  <TEST_AGENTIC_APPID></TEST_AGENTIC_APPID>
  <TEST_AGENTIC_USERID></TEST_AGENTIC_USERID>

  <!-- Optional -->
  <TEST_USER_ID_2>29:...</TEST_USER_ID_2>
  <TEST_CONNECTION_NAME>aadv2</TEST_CONNECTION_NAME>
</EnvironmentVariables>
```

## Running Tests

```bash
# From core/test directory
dotnet test IntegrationTests/IntegrationTests.csproj \
  --settings IntegrationTests/.runsettings/botid-prod.runsettings -v d

# With TRX output for CI
dotnet test IntegrationTests/IntegrationTests.csproj \
  --settings IntegrationTests/.runsettings/botid-prod.runsettings \
  --logger "trx;LogFileName=botid-prod.trx"
```

## Architecture

- **`IntegrationTestFixture`** — Shared xUnit fixture that configures DI, acquires auth tokens, and caches conversation members (to avoid 429 throttling).
- Tests use `IClassFixture<IntegrationTestFixture>` so auth + member lookup happens once per test class.
- Parallelization is disabled (`xunit.runner.json`) since tests share the same conversation.

## Known Limitations

- **Agentic identity**: Targeted activities, paged members, and reactions return 500/404 with agentic identity. These are service-side limitations pending investigation.
- **Group chat creation**: Bot-only identity cannot create group chats with `IsGroup=true` + multiple members via the conversations API.
- **User token tests**: `SignIn` and `Users.Token` tests are skipped when agentic identity is configured (not supported).
- **BAMI tenant expiration**: Test resources expire every few months. Re-provision and update runsettings when the tenant rotates.

## Cross-SDK Runbook

For provisioning, secret rotation, tenant renewal, and troubleshooting across all SDKs, see the shared runbook:

👉 [INTEGRATION-TESTS.md](https://github.com/microsoft/teams-sdk/blob/main/INTEGRATION-TESTS.md)
