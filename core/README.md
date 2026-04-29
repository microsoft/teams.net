# Microsoft Teams Bot Core SDK

The core SDK for building Microsoft Teams bots in .NET. It implements the [Activity Protocol](https://github.com/microsoft/Agents/blob/main/specs/activity/protocol-activity.md) and provides a modern, layered framework with first-class support for ASP.NET Core dependency injection, authentication via MSAL, and extensible activity schemas.

## Packages

| Package | Description |
|---------|-------------|
| [Microsoft.Teams.Core](src/Microsoft.Teams.Core/) | Foundational library &mdash; activity protocol, conversation client, user token client, middleware pipeline, and authentication |
| [Microsoft.Teams.Apps](src/Microsoft.Teams.Apps/) | High-level Teams framework &mdash; typed activity routing, handler registration, OAuth flows, Teams API clients, and streaming |
| [Microsoft.Teams.Apps.BotBuilder](src/Microsoft.Teams.Apps.BotBuilder/) | Compatibility bridge for existing Bot Framework SDK v4 bots to run on the new Core infrastructure |

## Quick Start

```csharp
using Microsoft.Teams.Apps;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();

var app = builder.Build();
var teams = app.UseTeams(); // maps POST /api/messages

teams.OnMessage(async (context, ct) =>
{
    await context.Send($"You said: {context.Activity.Text}");
});

app.Run();
```

## Design Principles

- **Loose schema** &mdash; `CoreActivity` contains only strictly required fields; additional fields are captured via `JsonExtensionData`
- **Simple serialization** &mdash; Standard `System.Text.Json` with source generation, no custom converters
- **Extensible schema** &mdash; `ChannelData` and entities support extension properties; generics allow custom types
- **MSAL-based auth** &mdash; Token acquisition built on Microsoft Identity Web, supporting client secrets, managed identities, and agentic (user-delegated) tokens
- **ASP.NET DI** &mdash; All dependencies configured via `IServiceCollection`, reusing the built-in `HttpClient` factory
- **ILogger & IConfiguration** &mdash; Standard .NET logging and configuration throughout

## Configuration

Create a Teams Application, configure it in Azure Bot Service, and provide credentials via `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "Scope": "https://api.botframework.com/.default",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "<your-entra-app-secret>"
      }
    ]
  }
}
```

Or via environment variables:

```env
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-client-id>
AzureAd__Scope=https://api.botframework.com/.default
AzureAd__ClientCredentials__0__SourceType=ClientSecret
AzureAd__ClientCredentials__0__ClientSecret=<your-entra-app-secret>
```

## Testing in Localhost (Anonymous)

When no MSAL configuration is provided, all communication happens as anonymous REST calls, suitable for local development.

### Install Playground

Linux:
```sh
curl -s https://raw.githubusercontent.com/OfficeDev/microsoft-365-agents-toolkit/dev/.github/scripts/install-agentsplayground-linux.sh | bash
```

Windows:
```sh
winget install m365agentsplayground
```

### Run a Scenario

```sh
dotnet samples/scenarios/middleware.cs -- --urls "http://localhost:3978"
```

## Samples

| Sample | Description |
|--------|-------------|
| [TeamsBot](samples/TeamsBot/) | Basic Teams bot with message handling |
| [TeamsChannelBot](samples/TeamsChannelBot/) | Channel-scoped messaging |
| [AllInvokesBot](samples/AllInvokesBot/) | Handles all invoke activity types |
| [MessageExtensionBot](samples/MessageExtensionBot/) | Message extension search and actions |
| [MeetingsBot](samples/MeetingsBot/) | Meeting events and participant APIs |
| [OAuthFlowBot](samples/OAuthFlowBot/) | OAuth sign-in and token management |
| [SsoBot](samples/SsoBot/) | Single sign-on (SSO) token exchange |
| [StreamingBot](samples/StreamingBot/) | Progressive streaming responses |
| [Proactive](samples/Proactive/) | Proactive messaging from external triggers |
| [TabApp](samples/TabApp/) | Tab application with backend API |
| [CompatBot](samples/CompatBot/) | Migrating a Bot Framework v4 bot |
| [CoreBot](samples/CoreBot/) | Using Microsoft.Teams.Core directly |

## Project Structure

```
core/
├── src/
│   ├── Microsoft.Teams.Core/              # Foundation: protocol, clients, middleware, auth
│   ├── Microsoft.Teams.Apps/              # Framework: routing, handlers, OAuth, API clients
│   └── Microsoft.Teams.Apps.BotBuilder/   # Compat bridge for Bot Framework SDK v4
├── samples/                               # Sample bot applications
└── test/
    ├── Microsoft.Teams.Core.UnitTests/
    ├── Microsoft.Teams.Apps.UnitTests/
    ├── Microsoft.Teams.Apps.BotBuilder.UnitTests/
    └── IntegrationTests/
```
