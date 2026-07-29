# Formatted Messaging

A bot that demonstrates the different text format options: `markdown`, `extendedmarkdown`, `xml`, and `plain`.

Send one of the format names to the bot and it replies with a message using that format.

## Teams CLI

Use the official Teams CLI (`@microsoft/teams.cli`) to create and manage the Teams app for this sample:

```bash
npm install -g @microsoft/teams.cli
teams --version
teams login
```

Expose this sample's local `/api/messages` endpoint with a tunnel, then create the Teams app:

```bash
teams app create --name "formatted-messaging" --endpoint "https://<your-tunnel>/api/messages" --env appsettings.json --json
```

The CLI writes `ClientId`, `ClientSecret`, and `TenantId` to your `appsettings.json` file and prints an install link for Teams.

## Run

```bash
dotnet run --urls "http://0.0.0.0:3978"
```
