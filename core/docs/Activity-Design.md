# Activity Design

## Overview

The SDK uses a two-layer activity model:

- `Microsoft.Teams.Core` provides `CoreActivity` (channel-agnostic transport shape).
- `Microsoft.Teams.Apps` provides `TeamsActivity` and Teams-specific derived types used by handlers.

This keeps protocol plumbing in Core while exposing Teams-friendly types in Apps.

```csharp
// Inbound read model used by handlers
bot.OnMessage(async (ctx, ct) =>
{
    string? text = ctx.Activity.TextWithoutMentions;
    await ctx.SendAsync($"Echo: {text}", ct);
});
```

## Model structure

| Layer | Main types | Responsibility |
|---|---|---|
| Core | `CoreActivity`, `Conversation`, `ConversationAccount` | Stable protocol fields (`type`, `serviceUrl`, `conversation`, `from`, `recipient`) and extension-data storage |
| Apps | `TeamsActivity`, `MessageActivity`, `InvokeActivity`, `EventActivity`, etc. | Teams-specific typed properties (`channelData`, `entities`, text/attachments/value projections), handler-facing shape |
| Outbound input | `TeamsActivityInput`, `MessageActivityInput`, `StreamingActivityInput` | Builder-first outbound payload types for sending activities |

## Inbound flow

1. JSON request is deserialized into `CoreActivity`.
2. Core typed fields are read directly from declared properties.
3. Remaining JSON fields land in extension data.
4. `TeamsActivity.FromActivity` converts Core -> Teams types.
5. Per-activity converters project relevant extension fields to typed properties (message text/attachments, invoke value, event name/value, etc.).
6. Router dispatches to typed handlers.

```csharp
CoreActivity core = await CoreActivity.FromJsonStreamAsync(bodyStream, ct);
TeamsActivity teams = TeamsActivity.FromActivity(core);
```

## Outbound flow

1. Handlers build outbound payloads with fluent `*ActivityInput` APIs.
2. `ConversationClient` serializes and sends using the activity input JSON context.
3. Reply helpers apply conversation reference data from the inbound turn activity.

```csharp
MessageActivityInput reply = new MessageActivityInput()
    .WithText("Hello", TextFormats.Markdown);
await context.SendAsync(reply, ct);
```

## Serialization strategy

- Source-generated JSON contexts are used for AOT-friendly serialization/deserialization.
- Core and Apps keep separate contexts aligned to their type sets.
- Extension-data remains the compatibility mechanism for fields not yet promoted to typed members.

## Design rules

1. Keep protocol-critical fields typed in Core.
2. Prefer builders for outbound creation; inbound activity types are primarily read models.
3. Avoid duplicate storage for the same logical property across Core/Teams layers.

## Current tradeoffs

- Extension-data projection is flexible but depends on key names and conversion behavior.
- Converting Core -> Teams introduces mapping cost, but keeps handler APIs strongly typed and predictable.
- Builder-based outbound APIs are the preferred design, but `MessageActivityInput` exposes fluent methods directly for back-compat and easier `new`-based usage.
