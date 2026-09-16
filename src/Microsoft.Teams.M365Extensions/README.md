# Microsoft.Teams.M365Extensions

Bridge the [Microsoft 365 Agents SDK](https://github.com/microsoft/Agents) and the
[Microsoft Teams SDK](https://github.com/microsoft/teams.net) in a single ASP.NET Core app.

Teams-channel turns are routed to a `TeamsBotApplication` (Teams SDK) when a Teams route
matches; everything else — non-Teams channels and unmatched Teams turns — continues through
the Agents SDK pipeline. Outbound Teams API calls are authenticated with the Agents SDK's
connection manager, so no separate `AzureAd` configuration section is required.

## Install

```xml
<PackageReference Include="Microsoft.Teams.M365Extensions" Version="*" />
```

## Usage

A single call wires everything up:

```csharp
using Microsoft.Teams.M365Extensions;

builder.AddAgent<MyAgent>();
builder.Services.AddTeamsSdk<MyTeamsBot>();
```

Optionally bypass Teams routing for specific activities so they stay on the Agents SDK
(for example, to keep `signin/*` invokes on the Agents SDK auth pipeline):

```csharp
builder.Services.AddTeamsSdk<MyTeamsBot>(shouldBypassTeams: turnContext =>
    turnContext.Activity.Type == ActivityTypes.Invoke
    && !string.IsNullOrEmpty(turnContext.Activity.Name)
    && turnContext.Activity.Name.StartsWith("signin/", StringComparison.OrdinalIgnoreCase));
```

`AddTeamsSdk<T>()` registers the Teams SDK bot and its Teams API/auth chain, bridges outbound
auth through the Agents SDK connection manager, and installs `TeamsSdkMiddleware` on the
`CloudAdapter` pipeline. Matched Teams turns are replayed through
`TeamsBotApplication.ProcessAsync(...)` on a synthetic `HttpContext` built from the current
Agents SDK turn, so Teams message turns still work when the `CloudAdapter` processes them on a
background thread.

## Key types

| Type | Purpose |
|---|---|
| `TeamsSdkExtensions.AddTeamsSdk<T>` | One-call registration of the Teams SDK bot and routing middleware. |
| `TeamsSdkMiddleware` | Agents SDK `IMiddleware` that routes matched Teams turns to the Teams SDK. |
| `TeamsSdkMiddleware.IsTeamsChannel` | Detects Teams channels, including sub-channels such as `msteams:COPILOT`. |

See the `M365ExtensionsBot` sample for an end-to-end multichannel app.
