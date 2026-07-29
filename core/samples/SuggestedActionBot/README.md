# SuggestedActionBot

This sample shows suggested actions with submit handling. It is a focused example of how to present quick one-tap choices and receive a structured response when the user taps one.

## Prerequisites

- Bot registered and installed in Teams.

## What it shows

- `SuggestedActions` with two submit chips.
- `Action.Submit` payloads for a small approval/rejection flow.
- `OnSuggestedActionSubmit` to receive and log the submitted value.

It is a small, focused reference for quick action chips that collect a structured response from the user.

## Commands / Flows

| Flow | Behavior |
|---|---|
| send any message | Bot sends suggested-action chips |
| click a suggested action | Bot handles submit payload and returns the selected value |

## Running the Sample

~~~bash
dotnet run --project samples/SuggestedActionBot/SuggestedActionBot.csproj
~~~
