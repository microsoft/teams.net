# A2ABot — Agent-to-Agent Sample

Demonstrates two Teams bots (Alice and Bob) communicating over the [A2A protocol](https://github.com/a2aproject/A2A) using the official [A2A .NET SDK](https://github.com/a2aproject/a2a-dotnet).

## How it works

```
Alice's user                Alice bot                  Bob bot               Bob's operator
     |                          |                          |                       |
     |-- "What is X?" --------->|                          |                       |
     |                          |-- A2A SendMessage ------>|                       |
     |                          |   (AskMessage DataPart)  |                       |
     |                          |                          |-- Adaptive Card ------>|
     |                          |                          |                       |
     |                          |                          |<-- Card submit --------|
     |                          |<-- A2A SendMessage ------|                       |
     |                          |   (ReplyMessage DataPart)|                       |
     |<-- Reply card ------------|                          |                       |
```

1. A user messages Alice with a question ending in `?`
2. Alice uses `A2ACardResolver` to discover Bob's A2A endpoint, then sends an `AskMessage` via `A2AClient`
3. Bob's `IAgentHandler` receives the ask and proactively pushes an adaptive card to Bob's operator in Teams
4. Bob's operator types an answer and submits the card (`OnAdaptiveCardAction`)
5. Bob sends a `ReplyMessage` back to Alice via A2A
6. Alice's `IAgentHandler` receives the reply and pushes a reply card to the original user

## Project structure

```
A2ABot/
├── Program.cs          — Teams handlers + MapA2A + MapWellKnownAgentCard
├── A2A/
│   ├── Agent.cs        — IAgentHandler: dispatches ask/reply DataParts
│   ├── Config.cs       — Bot name, self URL, peer URL
│   ├── Messages.cs     — AskMessage / ReplyMessage records
│   ├── PeerClient.cs   — A2ACardResolver + A2AClient wrapper
│   └── State.cs        — In-memory operator conv ref + pending asks
└── Cards/
    └── Cards.cs        — Adaptive card builders (ask card, reply card)
```

## Prerequisites

- Two separate bot registrations in Azure (one for Alice, one for Bob)
- .NET 10 SDK
- [Bot Framework Emulator](https://github.com/microsoft/BotFramework-Emulator) or a tunneling tool (ngrok / dev tunnels) for local testing

## Running locally

### 1. Configure Alice (`appsettings.json` or user secrets)

```json
{
  "AzureAd": {
    "ClientId": "<Alice-App-Id>",
    "ClientSecret": "<Alice-App-Secret>",
    "TenantId": "<Tenant-Id>"
  },
  "Bot": {
    "Name": "Alice",
    "SelfUrl": "https://<alice-tunnel-url>",
    "PeerUrl": "https://<bob-tunnel-url>"
  }
}
```

### 2. Configure Bob (environment variables or a second `appsettings` file)

```bash
AzureAd__ClientId=<Bob-App-Id>
AzureAd__ClientSecret=<Bob-App-Secret>
Bot__Name=Bob
Bot__SelfUrl=https://<bob-tunnel-url>
Bot__PeerUrl=https://<alice-tunnel-url>
```

### 3. Run both instances

```bash
# Terminal 1 — Alice on port 3978 (uses the Alice launch profile)
dotnet run --launch-profile Alice

# Terminal 2 — Bob on port 3979
dotnet run --launch-profile Bob --urls http://localhost:3979
```

The `launchSettings.json` in `Properties/` includes both profiles for Visual Studio / VS Code.

## A2A endpoints

Each bot exposes two standard A2A endpoints:

| Path | Purpose |
|---|---|
| `POST /a2a` | A2A JSON-RPC endpoint (bot-to-bot messages) |
| `GET /.well-known/agent-card.json` | Agent card for discovery |

## Security note

This sample accepts A2A messages from any peer (no auth on `/a2a`). For production, add bearer token validation or mTLS on the A2A endpoint and verify the caller's identity before processing messages.
