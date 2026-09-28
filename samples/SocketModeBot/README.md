# SocketModeBot

This sample is an echo bot that receives activities over Socket Mode instead of an HTTP messaging endpoint. The bot opens outbound WebSocket connections to the Teams service, so it needs no public URL or tunnel.

## Prerequisites

- Bot registered in the public cloud and installed in Teams, with Socket Mode enabled for the bot.
- Bot credentials in `appsettings.Development.json` (git-ignored) or environment variables:

~~~json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "<your-entra-app-secret>"
      }
    ]
  }
}
~~~

## What it shows

- `UseSocketMode()` on `AddTeamsBotApplication` to receive activities over Socket Mode.
- A generic host (`Host.CreateApplicationBuilder`) with no web server, and `UseTeamsSocketApplication()` to get the app.

## Commands / Flows

| Flow | Behavior |
|---|---|
| send any message | Bot replies with `Echo: <your text>` |

## Running the Sample

~~~bash
DOTNET_ENVIRONMENT=Development dotnet run --project samples/SocketModeBot/SocketModeBot.csproj
~~~

The bot is ready when the log shows `Socket Mode ready across 3 geo(s).` (amer, emea, apac). Startup fails if any geo cannot connect within the startup timeout.
