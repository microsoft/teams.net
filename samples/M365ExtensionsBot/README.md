# Example: M365 Extension (.NET)

This sample embeds the Teams SDK (`Microsoft.Teams.Apps`) inside a Microsoft 365 Agents SDK
`AgentApplication` using the [`Microsoft.Teams.M365Extensions`](../../src/Microsoft.Teams.M365Extensions)
package. Teams turns that match a Teams SDK route are handled by the Teams SDK; everything else —
non-Teams channels and Teams turns with no matching route — falls through to the Agents SDK.

## How it works

`AddTeamsSdk<MyTeamsBot>()` reads the Agents SDK connection identity, wires the Teams SDK's
outbound tokens to the same connections, registers the Teams SDK bot, and installs the routing
middleware on the Agents SDK `CloudAdapter` pipeline:

```csharp
builder.AddAgent<MyAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
builder.Services.AddTeamsSdk<MyTeamsBot>(shouldBypassTeams: turnContext =>
    turnContext.Activity.Type == ActivityTypes.Invoke
    && !string.IsNullOrEmpty(turnContext.Activity.Name)
    && turnContext.Activity.Name.StartsWith("signin/", StringComparison.OrdinalIgnoreCase));
```

Matched Teams turns are replayed through `TeamsBotApplication.ProcessAsync(...)` on a synthetic
`HttpContext` built from the current Agents SDK turn, so Teams message turns still work when the
`CloudAdapter` processes them on a background thread. The optional `shouldBypassTeams` predicate
runs only for Teams-channel activities and can force a fallthrough to the Agents SDK even when the
Teams SDK has a matching route — this sample uses it to keep `signin/*` invokes on the Agents SDK
auth pipeline.

## Commands

Teams SDK routes (`MyTeamsBot`, Teams channel only):

- `help` — Adaptive Card listing every command
- `react` — bot adds then removes an emoji reaction
- `quote` — bot replies with a quoted reply
- `targeted` — ephemeral message visible only to the sender
- `task` — task module fetch/submit flow

Agents SDK routes (`MyAgent`, fallthrough + non-Teams channels):

- `channel` — report the channel and routing path
- `agents sdk react` — reach the Teams SDK API client from an Agents SDK handler
- `agents sdk proactive` — trigger a proactive send from an Agents SDK handler
- `whoami` / `mail` — Microsoft Graph via two separate OAuth connections
- `signout` — sign out of both Graph handlers
- anything else — echoed by the Agents SDK

## Setup

Install the official [Teams CLI](https://microsoft.github.io/teams-sdk/cli/) and sign in:

```bash
npm install -g @microsoft/teams.cli
teams login
```

Expose this sample's local `/api/messages` endpoint with a dev tunnel, then create the app. The
`whoami`/`mail` sign-in demo needs an Azure Bot resource, so use `--azure`:

```bash
teams app create \
  --name "m365extensions" \
  --azure --resource-group <rg> --create-resource-group \
  --endpoint "https://<your-tunnel>/api/messages"
```

Put the generated credentials in `appsettings.Development.json` (git-ignored) or environment
variables such as `Connections__ServiceConnection__Settings__ClientId`. The `appsettings.json` in
this folder stays checked in with placeholders; a minimal local override is:

```json
{
  "TokenValidation": {
    "Enabled": true,
    "Audiences": ["<client-id>"],
    "TenantId": "<tenant-id>"
  },
  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret",
        "AuthorityEndpoint": "https://login.microsoftonline.com/<tenant-id>",
        "ClientId": "<client-id>",
        "ClientSecret": "<client-secret>",
        "Scopes": ["https://api.botframework.com/.default"]
      }
    }
  }
}
```

## Running

```powershell
dotnet build samples/M365ExtensionsBot/M365ExtensionsBot.csproj
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project samples/M365ExtensionsBot --urls http://localhost:3978
```

Install the app in Teams via the Teams CLI (see Setup above), then send `help`.

## Multi Authentication

`whoami` and `mail` both call Microsoft Graph, but through separate OAuth connections on the same
AAD app, so each keeps its own token cache.

| Command | Handler | ABS connection | Scopes |
| --- | --- | --- | --- |
| `whoami` | `graphuser` | `graphuser` | `User.Read` |
| `mail` | `graphmail` | `graphmail` | `User.Read Mail.Read` |

Create the two OAuth connections on the Azure Bot registration — via the Azure Portal or `az`,
since the Teams CLI does not manage OAuth connections — then keep the handler entries in
`appsettings.Development.json`:

```bash
az bot authsetting create --name <bot> --resource-group <rg> --setting-name graphmail \
  --client-id <aad-app> --client-secret <secret> --service Aadv2 \
  --provider-scope-string "User.Read Mail.Read" --parameters tenantId=<tenant>
```

Auth lives on the Agents SDK side because the auth intercept runs inside `AgentApplication`. The
sample passes a `shouldBypassTeams` predicate so `signin/*` invokes always stay with the Agents SDK.

## Multichannel

The M365 Extension routes to the Teams SDK only for Teams activities; every other channel passes
straight through to the Agents SDK.

| | Teams | Web Chat / Direct Line | Email |
| --- | --- | --- | --- |
| `channel` | fell through | passed through | passed through |
| `help` | Adaptive Card (Teams SDK) | plain text (Agents SDK) | plain text (Agents SDK) |
| `quote`, `task`, `react`, `targeted` | handled by Teams SDK | no route → echoed | no route → echoed |
| `whoami`, `mail` | OAuth card | OAuth card | declined (cards are inert on email) |
