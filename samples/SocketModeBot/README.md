# SocketModeBot

This sample is an echo bot that receives activities over Socket Mode instead of an HTTP messaging endpoint. The bot opens outbound WebSocket connections to the Teams service, so it needs no public URL or tunnel.

## Prerequisites

- Bot registered in the public cloud and installed in Teams, with Socket Mode enabled for the bot.
- Bot credentials: copy `Properties/launchSettings.TEMPLATE.json` to `Properties/launchSettings.json` (git-ignored) and fill in `AzureAd__TenantId`, `AzureAd__ClientId`, and `AzureAd__ClientCredentials__0__ClientSecret`.

## What it shows

- `UseSocketMode()` on `AddTeamsBotApplication` to receive activities over Socket Mode using the production connect endpoints (`https://{geo}.smbatrafficmanager.net/v3/websockets/connect`).
- One connection per geo (`amer`, `emea`, `apac`) by default for inbound coverage across regions. Configure `Geos = [...]` in the `UseSocketMode(socket => ...)` callback with at least one geo.
- `<NoWarn>$(NoWarn);ExperimentalTeamsSocketMode</NoWarn>` in the project file, because Socket Mode is experimental.
- A generic host (`Host.CreateApplicationBuilder`) with no web server, and `UseTeamsBotApplication()` to get the app.
- Per-geo `Ready`, `Disconnected`, and `Reconnected` subscriptions in `Program.cs`, plus a status snapshot after startup.

See the [SDK Socket Mode documentation](../../src/Microsoft.Teams.Apps/README.md#socket-mode) for lifecycle and API details.

## Commands / Flows

| Flow | Behavior |
|---|---|
| send any message | Bot quotes your message and replies `You said: <your text>` |

## Running the Sample

~~~bash
dotnet run --project samples/SocketModeBot/SocketModeBot.csproj
~~~

`dotnet run` and IDEs apply the launch profile, which sets `DOTNET_ENVIRONMENT=Development` and the credentials.

Logging is configured in `appsettings.json`: Warning by default, with `Microsoft.Teams` and `SocketModeBot`
at Information.

The bot is ready when the log shows `Socket Mode ready across 3 geo(s).` (amer, emea, apac). Startup fails if any geo cannot connect within the startup timeout.
