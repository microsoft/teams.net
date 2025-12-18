# TeamsBot

## Configuration

Before running the bot, configure your Bot Framework credentials in `appsettings.json`:

you can use the script in `tools/get-profile.ps1` (need to be logged with az cli, and provide the AppId for the Bot)

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "<Bot_DisplayName>": {
      "commandName": "Project",
      "launchBrowser": false,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "CLIENT_ID": "",
        "TENANT_ID": "",
        "CLIENT_SECRET": ""
      },
      "applicationUrl": "http://localhost:3978"
    }
  }
}
```


## Running the Bot

Build and run the bot:

```bash
dotnet build
dotnet run --no-restore --no-build -- --urls "http://localhost:3978"
```

The bot will start on `http://localhost:3978`

