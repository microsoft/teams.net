# GraphBot sample

`GraphBot` is a Teams bot sample that calls Microsoft Graph with **app credentials** (client credentials flow), not user OAuth.

## What it does

- `help` - Shows available commands
- `org` - Reads tenant organization info from Graph
- `users` - Lists top users from Graph

## Azure setup

Use the same Entra app registration as your bot (`AzureAd:ClientId`) and configure:

1. **Client secret**
   - Entra ID -> App registrations -> your app -> Certificates & secrets -> New client secret
2. **Graph application permissions**
   - `Organization.Read.All` (for `org`)
   - `User.Read.All` (for `users`)
3. **Admin consent**
   - Entra ID -> App registrations -> your app -> API permissions -> **Grant admin consent for \<tenant\>**

Without admin consent, app-only Graph calls fail with "Insufficient privileges to complete the operation."

## Local config

Set these values in `appsettings.Development.json` or environment variables:

```json
{
  "AzureAd": {
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "<client-secret>"
      }
    ]
  }
}
```

## Run

```powershell
dotnet run --project .\samples\GraphBot\GraphBot.csproj
```

Then message the bot in Teams with `help`, `org`, and `users`.
