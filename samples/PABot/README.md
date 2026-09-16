# PABot

`PABot` shows the Bot Framework compatibility path with Teams auth and dialog-style bots. It is useful when you already have a Bot Framework app and want to understand how it plugs into the Teams Core hosting model.

## Prerequisites

- Bot registered and installed in Teams.
- Azure AD app credentials configured in launch settings.

## What it shows

- Compat adapter setup and request routing.
- Teams bot registration alongside the older Bot Framework abstractions.
- A Bot Framework bot wired through the Teams adapter so you can compare the compat layer with the newer Core APIs.

This sample is mainly for existing Bot Framework apps that want to move onto the Teams Core stack without rewriting everything at once.

## Running the Sample

~~~bash
dotnet run --project samples/PABot/PABot.csproj
~~~
