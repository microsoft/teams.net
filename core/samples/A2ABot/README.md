# A2ABot — A2A Proactive Handoff

Two Teams bots (**Alice**, **Bob**) that hand a user off to each other over
the [A2A protocol](https://github.com/a2aproject/A2A) using the
[A2A .NET SDK](https://github.com/a2aproject/a2a-dotnet). Each bot has its
own Teams app registration, so each user has a **separate DM** with Alice
and Bob. The receiving bot **proactively opens a 1:1** with the user and
greets them with the context the sending bot passed over A2A.

In this sample:

- **Alice** answers questions about **cats**.
- **Bob** answers questions about **dogs**.
- Either LLM can decide to hand off; both are symmetric. (Deliberately
  toy descriptions so the routing is obvious — ask Alice about dogs and
  watch Bob start a DM with you.)

## Flow

```
User-A    Alice (LLM)               Bob (HandoffHandler + LLM)
  |           |                                |
  |- "best    |                                |
  |  dog      | LLM: "dogs → Bob".             |
  |  breed?" >| Calls handoff_to_peer          |
  |           |--- A2A handoff ---------------->|
  |           |  (DataPart carries AadObjectId,| ConversationClient.CreateConversationAsync
  |           |   TenantId, ServiceUrl,        | → new 1:1 conv with the user
  |           |   summary)                     | → seed history with handoff context + greeting
  |           |<------- ack -------------------| → send greeting via proactive message
  |<- "I've handed you to Bob" -|              |
  |                                            |
  |   (Bob's DM lights up with a new message)  |
  |- reply --->|<- delivered in Bob's DM ------|
  |            | LLM sees seeded history, picks up coherently
```

## How it works

1. User DMs **Alice**. Alice's LLM has a single `handoff_to_peer(summary)`
   tool whose description carries Bob's live A2A `AgentCard.description`.
2. When the LLM decides Bob is a better fit, it calls the tool. The tool
   sends an A2A `SendMessage` to Bob with a `DataPart` carrying:
   ```
   { Kind: "handoff", AadObjectId, UserName, Summary, From, TenantId, ServiceUrl }
   ```
3. Bob's `A2AServer` (an `IAgentHandler`) validates the payload, then uses
   `ConversationClient.CreateConversationAsync` to open a 1:1 with the user,
   asks `Agent.GreetWithHandoffAsync` to run the LLM with the handoff context
   (which leaves the turn in Bob's `AgentThread` for that new conversation),
   and sends the resulting greeting as a proactive message.
4. The user sees Bob's DM light up. When they reply, Bob's LLM has both
   the handoff context and its own greeting already in history, so it
   picks up coherently.

The bots are symmetric — same flow runs in reverse from Bob to Alice.

## Prerequisites

- Two separate bot registrations in Azure (one for Alice, one for Bob),
  each installed for the user in the same tenant (so `CreateConversation`
  can open a proactive DM).
- An Azure OpenAI resource with a chat deployment (e.g. `gpt-4o-mini`).
- .NET 10 SDK.

## Configuration

The sample uses ASP.NET's `appsettings.<Environment>.json` layering — set
`ASPNETCORE_ENVIRONMENT` to `Alice` or `Bob` and the matching file is
overlaid on top of the shared `appsettings.json`.

Fill in:

- `appsettings.json` — your shared `AzureOpenAI` credentials.
- `appsettings.Alice.json` — Alice's `AzureAd` creds + `Bot:*` config.
- `appsettings.Bob.json` — Bob's `AzureAd` creds + `Bot:*` config.

## Run

Two launch profiles in `Properties/launchSettings.json` set
`ASPNETCORE_ENVIRONMENT` to `Alice` or `Bob`, picking which per-bot
appsettings file gets layered on top of the shared one. In two terminals:

```bash
dotnet run --launch-profile Alice
dotnet run --launch-profile Bob
```

Both bots talk to each other on `localhost` for A2A. For the user-facing
side (Teams reaching each bot), expose each port through a tunnel
(ngrok / dev tunnels) and register that URL as each bot's messaging
endpoint in Azure.

## Caveats

- **Same-tenant assumption.** The handoff carries `AadObjectId` + `TenantId`
  + `ServiceUrl` — Bob uses these to call `CreateConversationAsync` in his
  own bot context. Cross-tenant handoff would need OAuth flow and a
  different identity translation.
- **Bob must be installed for the user.** Proactive `CreateConversation`
  only succeeds if the receiving bot is installable to that user (tenant
  app catalog, user installed, etc.). If Bob isn't installed, the create
  call fails and no DM opens.
- **No auth on `/a2a`.** This sample accepts handoff messages from any
  peer. For production, validate the caller's identity (bearer token or
  mTLS) before opening a conversation with someone they named.
