# CustomHosting

This sample shows how to use a custom `TeamsBotApplication` subclass. Instead of wiring handlers only in `Program.cs`, it moves the bot behavior into a derived app type so the app itself owns the default setup.

## Prerequisites

- Bot registered and installed in Teams.

## What it shows

- `AddTeamsBotApplication<MyTeamsBotApp>()` to register a derived bot application.
- Custom bot initialization in the derived app class.
- Default message handling coming from the custom app instead of top-level setup code.

This pattern is useful when you want to wrap shared bot behavior in a reusable application type and keep `Program.cs` small.
## Running the Sample

~~~bash
dotnet run --project samples/CustomHosting/CustomHosting.csproj
~~~
