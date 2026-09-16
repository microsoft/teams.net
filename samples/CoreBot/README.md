# CoreBot

Demonstrates `Microsoft.Teams.Core` directly, without the higher-level apps layer. It is the most minimal sample in the repo and shows the raw bot/application wiring with almost no Teams-specific sugar.

## Prerequisites

- A Teams bot registration and endpoint.

## What it shows

- `AddBotApplication()` and `UseBotApplication()` for the bare Core SDK setup.
- Binding credentials from a **custom configuration section** (`CustomAuth`) instead of the
  default `AzureAd` section, via `AddBotApplication("CustomAuth")`.
- `OnActivity` for direct activity handling.
- A reply built with `CoreActivityInput` and sent with `ConversationClient`.

---

## Configuration

Copy `Properties/launchSettings.TEMPLATE.json` to `Properties/launchSettings.json` and fill in
the `CustomAuth__*` values. The section name is passed explicitly to `AddBotApplication`, so
these keys must stay in sync with `Program.cs`:

~~~json
{
  "CustomAuth": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      { "SourceType": "ClientSecret", "ClientSecret": "your-client-secret" }
    ]
  }
}
~~~

---

## Behavior

| Activity | Behavior |
|---------|----------|
| inbound message | Replies with the SDK version |
| root route | Returns a simple health text |

---

It is useful when you want to understand the raw activity pipeline, or when you need a foundation for a custom integration that does not want the higher-level Teams apps helpers.
## Running the Sample

~~~bash
dotnet run --project samples/CoreBot/CoreBot.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.

