# M365ExtensionsBot — Teams SDK + Microsoft 365 Agents SDK

This sample runs the **Microsoft Teams SDK** (`Microsoft.Teams.Apps`) and the **Microsoft 365
Agents SDK** side-by-side in a single ASP.NET Core app, wired together by
[`Microsoft.Teams.M365Extensions`](../../src/Microsoft.Teams.M365Extensions).

- **Teams SDK owns matched Teams routes**: `help`, `react`, `quote`, `targeted`, `task`, plus reaction events.
- **Agents SDK owns everything else**: unmatched Teams turns, all non-Teams channels, `signin/*` invokes, and the `help`, `channel`, `agents sdk react`, `agents sdk proactive`, `whoami`, `mail`, `signout`, and echo routes.
- **Channel quirks are explicit**: Teams subchannels such as `msteams:COPILOT` are treated as Teams; email auth is declined up front because OAuth cards do not work there.

## Wiring

```csharp
builder.AddAgent<MyAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
builder.Services.AddTeamsSdk<MyTeamsBot>(shouldBypassTeams: turnContext =>
    turnContext.Activity.Type == ActivityTypes.Invoke
    && !string.IsNullOrEmpty(turnContext.Activity.Name)
    && turnContext.Activity.Name.StartsWith("signin/", StringComparison.OrdinalIgnoreCase));
```

`AddTeamsSdk<MyTeamsBot>()` is the only integration call. It registers the Teams SDK bot,
bridges outbound auth through the Agents SDK connection manager plus the ambient Agents SDK
turn context, and installs the middleware that decides whether a turn stays in the Agents SDK
or is handed to the Teams SDK. Matched Teams turns are replayed through
`TeamsBotApplication.ProcessAsync(...)` on a synthetic `HttpContext` built from the current
Agents SDK turn, so Teams message turns still work when the `CloudAdapter` processes them on a
background thread.

The optional bypass runs only for Teams-channel activities and can force a fallthrough to
the Agents SDK even when the Teams SDK has a matching route. This sample uses it to keep
`signin/*` invokes owned by the Agents SDK auth pipeline instead of the Teams SDK.

## Route split

| Surface | Commands / events | Owner |
|---|---|---|
| Teams messages | `help`, `react`, `quote`, `targeted`, `task` | Teams SDK |
| Teams events | message reactions | Teams SDK |
| Teams invokes | task module submit/fetch | Teams SDK |
| Teams auth invokes | `signin/*` | Agents SDK |
| Non-Teams messages | `help`, `channel`, `agents sdk react`, `agents sdk proactive`, `whoami`, `mail`, `signout`, echo | Agents SDK |

## Local config

`appsettings.json` stays checked in with placeholders. Put real credentials in one of:

- `appsettings.Development.json`
- environment variables such as `Connections__ServiceConnection__Settings__ClientId`

The minimal override file is:

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

## Running the sample

```powershell
dotnet build samples/M365ExtensionsBot/M365ExtensionsBot.csproj
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project samples/M365ExtensionsBot --urls http://localhost:3978
```

To exercise it end-to-end in Teams, expose the port with a tunnel (e.g. `devtunnel`), register
an Azure Bot pointing at `https://<tunnel>/api/messages`, create the `graphuser` / `graphmail`
Azure Bot OAuth connections used by the `whoami` / `mail` commands, and sideload the manifest in
`appManifest/` after filling in the bot/app id.

## Testing matrix

| Channel | Input | Expected owner | Expected outcome |
|---|---|---|---|
| Teams chat | `help` | Teams SDK | Adaptive Card listing Teams commands |
| Teams chat | `react` | Teams SDK | Bot posts a message, adds 👍, then removes it |
| Teams chat | `quote` | Teams SDK | Quoted reply |
| Teams chat | `targeted` | Teams SDK | Targeted/private message |
| Teams chat | `task` | Teams SDK | Task module button, fetch, submit |
| Teams chat | react to bot message | Teams SDK | Reaction event summary |
| Teams chat | `channel` | Agents SDK | Reports `msteams` / subchannel |
| Teams chat | `whoami` | Agents SDK + OAuth | Sign-in confirmation, then Graph-backed user identity |
| Teams chat | `mail` | Agents SDK + OAuth | Sign-in confirmation, then Graph mail summary |
| Teams chat | `signout` | Agents SDK + OAuth | Sign-out confirmation |
| Teams chat | any other text | Agents SDK | `[Agent SDK]` echo |
| Web Chat / Direct Line | `agents sdk react` | Agents SDK via Teams API client | Explains that the reactions API is unavailable on Direct Line |
| Web Chat / Direct Line | `agents sdk proactive` | Agents SDK via Teams API client | Sends a second message created through the Teams API client |
| Email | `whoami` / `mail` / `signout` | Agents SDK | Auth declined with email-specific explanation |
