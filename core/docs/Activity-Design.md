# CoreActivity / TeamsActivity Design

## Overview

The activity model is the central abstraction for all bot communication. It follows a two-layer architecture:

- **CoreActivity** (`Microsoft.Teams.Core`): Channel-agnostic activity model with typed core properties and dynamic extension properties via `[JsonExtensionData]`. AOT-compatible via source-generated JSON.
- **TeamsActivity** (`Microsoft.Teams.Apps`): Teams-specific extension that shadows base properties with Teams-specific types and promotes additional extension properties into typed fields.

```
CoreActivity (Core) — internal constructors, created via CreateBuilder() or deserialization
  ├── Declared properties: type, channelId, id, serviceUrl,
  │    replyToId, conversation (non-nullable), from, recipient
  ├── [JsonExtensionData] Properties bag for everything else (incl. value)
  ├── AOT serialization via CoreActivityJsonContext
  └── CoreActivityBuilder (fluent builder)

ConversationAccount (Core)
  ├── Declared properties: id, name, isTargeted,
  │    agenticAppId, agenticUserId, agenticAppBlueprintId
  └── [JsonExtensionData] Properties bag for channel-specific fields

AgenticIdentity (Core)
  ├── AgenticAppId, AgenticUserId, AgenticAppBlueprintId
  └── FromAccount(ConversationAccount?) — factory from typed fields

TeamsActivity (Apps) : CoreActivity
  ├── Shadows: From, Recipient (as TeamsConversationAccount)
  │            Conversation (as TeamsConversation)
  │            — via getter/setter delegates to base slot (single storage)
  ├── Additional typed properties: ChannelData, Entities,
  │    Timestamp, LocalTimestamp, Locale, LocalTimezone, SuggestedActions
  ├── Polymorphic deserialization via ActivityDeserializerMap
  ├── Type-specific serialization via ActivitySerializerMap
  ├── TeamsActivityBuilder (fluent builder)
  └── Derived types: MessageActivity, InvokeActivity, ConversationUpdateActivity, ...

MessageActivity (Apps) : TeamsActivity
  ├── Attachments, Text, TextFormat, AttachmentLayout
  └── SuggestedActions extracted from Properties

InvokeActivity (Apps) : TeamsActivity
  ├── Name, Value (JsonNode?)
  └── InvokeActivity<TValue> shadows Value with strongly-typed access

EventActivity (Apps) : TeamsActivity
  ├── Name, Value (JsonNode?)
  └── EventActivity<TValue> shadows Value with strongly-typed access
```

## Activity Lifecycle

```
Incoming:
  HTTP body (JSON)
    → CoreActivity.FromJsonStreamAsync()        // from, recipient → typed properties
    → TurnMiddleware pipeline                    // channelData, entities, text → Properties bag
    → TeamsActivity.FromActivity(coreActivity)   // Converts typed + Extract remaining
    → ActivityDeserializerMap dispatches to       // MessageActivity, InvokeActivity, etc.
      concrete type
    → Router dispatches to handler

Outgoing:
  Handler builds reply
    → TeamsActivityBuilder.WithConversationReference(incoming)
       └── WithFrom(incoming.Recipient)          // Swaps from/recipient for reply
    → ConversationClient.SendActivityAsync(activity)
       ├── Reads activity.Recipient.IsTargeted and AgenticIdentity.FromAccount(activity.From)
       └── activity.ToJson() serializes for HTTP POST
    → POST {serviceUrl}/v3/conversations/{id}/activities/
```

## Core Design Decisions

### 1. Typed Properties for Protocol-Level Fields

CoreActivity declares typed `[JsonPropertyName]` properties for fields that are part of the Activity Protocol Specification and needed by the Core layer:

| Property | Type | Why typed |
|----------|------|-----------|
| `Type` | `string` | Routing decisions |
| `ChannelId` | `string?` | URL construction |
| `Id` | `string?` | Reply targeting |
| `ServiceUrl` | `Uri?` | HTTP endpoint |
| `ReplyToId` | `string?` | Reply threading |
| `Conversation` | `Conversation` (non-nullable) | URL construction, always initialized |
| `From` | `ConversationAccount?` | AgenticIdentity extraction |
| `Recipient` | `ConversationAccount?` | IsTargeted flag for targeted messaging |

Everything else (`text`, `attachments`, `entities`, `channelData`, `value`, `timestamp`, etc.) remains in the `[JsonExtensionData] Properties` dictionary, promoted to typed properties at the TeamsActivity layer or its derived types (e.g., `value` is promoted by `InvokeActivity` and `EventActivity`).

### 2. ConversationAccount with Typed Agentic Identity Fields

`ConversationAccount` declares the agentic identity fields as typed properties rather than relying on the extension data dictionary:

```csharp
[JsonPropertyName("agenticAppId")]    public string? AgenticAppId { get; set; }
[JsonPropertyName("agenticUserId")]   public string? AgenticUserId { get; set; }
[JsonPropertyName("agenticAppBlueprintId")] public string? AgenticAppBlueprintId { get; set; }
```

The `AgenticIdentity` class is a separate DTO used by `BotRequestOptions` and `BotAuthenticationHandler` for token acquisition. It is constructed from a `ConversationAccount`'s typed fields via `AgenticIdentity.FromAccount(account)` at the point of use — there is no computed property or duplication on `ConversationAccount` itself.

### 3. Property Shadowing with Getter/Setter Delegates

TeamsActivity shadows base properties with more specific types using the `new` keyword, but delegates storage to the base slot via getter/setter properties:

```csharp
// CoreActivity
[JsonPropertyName("from")] public ConversationAccount? From { get; set; }
[JsonPropertyName("conversation")] public Conversation Conversation { get; set; }

// TeamsActivity — single storage, delegates to base
[JsonPropertyName("from")]
public new TeamsConversationAccount? From
{
    get => base.From as TeamsConversationAccount;
    set => base.From = value;
}

[JsonPropertyName("conversation")]
public new TeamsConversation? Conversation
{
    get => base.Conversation as TeamsConversation;
    set => base.Conversation = value!;
}
```

Since `TeamsConversationAccount` extends `ConversationAccount` and `TeamsConversation` extends `Conversation`, the derived type is stored directly in the base slot. The getter casts back. This eliminates dual storage and the need for manual sync — code accessing through either a `CoreActivity` or `TeamsActivity` reference sees the same value.

The `TeamsActivity(CoreActivity)` constructor stores converted types in the base slots:
```csharp
base.From = TeamsConversationAccount.FromConversationAccount(activity.From) ?? new TeamsConversationAccount();
base.Recipient = TeamsConversationAccount.FromConversationAccount(activity.Recipient) ?? new TeamsConversationAccount();
base.Conversation = TeamsConversation.FromConversation(activity.Conversation) ?? new TeamsConversation();
```

**Serialization:** The `[JsonPropertyName]` attribute on the `new` property (not `[JsonIgnore]`) ensures the source-generated serializer for TeamsActivity uses the correctly-typed property (e.g., `TeamsConversation?` instead of `Conversation`), preserving fields like `TenantId` and `ConversationType`.

### 4. Extension Data for Remaining Properties

`ExtendedPropertiesDictionary.Extract<T>(key)` is used by TeamsActivity subtypes to promote remaining properties from the untyped bag to typed fields:

```csharp
public T? Extract<T>(string key)
{
    if (!TryGetValue(key, out object? raw)) return default;
    Remove(key);                         // Remove to avoid duplicate serialization
    if (raw is T typed) return typed;    // Already the right type
    if (raw is JsonElement element)      // Deserialized from JSON
        return JsonSerializer.Deserialize<T>(element.GetRawText());
    return default;                      // Unknown type — data is lost
}
```

This pattern is used for: `channelData`, `entities`, `value`, `attachments`, `text`, `textFormat`, `attachmentLayout`, `suggestedActions`, `name`, `action`, `membersAdded`, `membersRemoved`, `reactionsAdded`, `reactionsRemoved`.

### 5. Dual Serialization Strategy

| Path | When | Mechanism |
|------|------|-----------|
| AOT (source-gen) | `CoreActivity.ToJson()` | `CoreActivityJsonContext.Default.CoreActivity` |
| Reflection | `CoreActivity.ToJson<T>(instance)` | `ReflectionJsonOptions` with camelCase |
| Type-specific | `TeamsActivity.ToJson()` | `ActivitySerializerMap` dispatch by runtime type |

### 6. Builder Pattern

Both layers provide fluent builders with `With*` (replace) and `Add*` (append) methods:

- `CoreActivityBuilder` — core-level activities with `WithFrom()`, `WithRecipient()`, `WithConversation()`, `WithProperty()`. Builder parameters accept nullable types where appropriate (`Uri?`, `string?`, `ConversationAccount?`).
- `TeamsActivityBuilder` — Teams-specific, shadows `WithFrom`/`WithRecipient`/`WithConversation` (via `new`) to convert to `TeamsConversationAccount`/`TeamsConversation`. Attachment methods (`WithAttachments`, `AddAttachment`, etc.) set the typed property when the underlying activity is a `MessageActivity`, otherwise store in Properties as fallback.

`TeamsActivityBuilder.WithConversationReference(activity)` is the canonical way to build a reply — it copies `ServiceUrl`, `ChannelId`, `Conversation` from the incoming activity and swaps `From`/`Recipient`.

## Serialization Architecture

### CoreActivity JSON Fields

```
Declared properties (deserialized into typed fields):
  type, channelId, id, serviceUrl, replyToId, conversation, from, recipient

Extension properties (deserialized into [JsonExtensionData] Properties):
  value, text, textFormat, attachments, entities, channelData, timestamp,
  locale, ... (anything not declared above)
```

### ConversationAccount JSON Fields

```
Declared properties:
  id, name, isTargeted, agenticAppId, agenticUserId, agenticAppBlueprintId

Extension properties:
  aadObjectId, userRole, userPrincipalName, givenName, surname, email,
  tenantId, ... (anything not declared above)
```

### TeamsActivity JSON Fields

```
Inherited declared (shadowed with Teams types):
  from (TeamsConversationAccount), recipient (TeamsConversationAccount),
  conversation (TeamsConversation)

Inherited declared (used as-is):
  type, channelId, id, serviceUrl, replyToId

Promoted from Properties during construction:
  channelData, entities, timestamp, localTimestamp,
  locale, localTimezone, suggestedActions

Promoted by derived types:
  MessageActivity: text, textFormat, attachmentLayout, attachments
  InvokeActivity: name, value
  EventActivity: name, value
  ConversationUpdateActivity: membersAdded, membersRemoved
  InstallUpdateActivity: action
  MessageReactionActivity: reactionsAdded, reactionsRemoved, replyToId

Remaining extension properties (via [JsonExtensionData]):
  Any fields not declared or promoted above
```

### Source-Generated JSON Contexts

| Context | Project | Types |
|---------|---------|-------|
| `CoreActivityJsonContext` | Core | CoreActivity, ChannelData, Conversation, ConversationAccount, ExtendedPropertiesDictionary, primitives |
| `TeamsActivityJsonContext` | Apps | TeamsActivity, MessageActivity, StreamingActivity, all Entity types, SuggestedActions, TeamsAttachment, TeamsConversation, TeamsConversationAccount, TeamsChannelData |

## Class Hierarchy

```
CoreActivity
└── TeamsActivity
    ├── MessageActivity
    ├── StreamingActivity
    ├── InvokeActivity
    │   └── InvokeActivity<TValue>
    ├── ConversationUpdateActivity
    ├── EventActivity
    │   └── EventActivity<TValue>
    ├── InstallUpdateActivity
    ├── MessageReactionActivity
    ├── MessageUpdateActivity
    └── MessageDeleteActivity

Conversation
└── TeamsConversation

ConversationAccount
└── TeamsConversationAccount

CoreActivityBuilder<TActivity, TBuilder>
├── CoreActivityBuilder
└── TeamsActivityBuilder
```

## Property Flow (Incoming Activity)

```
JSON → CoreActivity deserialization
  │
  │  Typed properties populated directly by JSON deserializer:
  │    type, channelId, id, serviceUrl, replyToId, conversation, from, recipient
  │
  │  Remaining fields go to [JsonExtensionData] Properties:
  │    value, channelData, entities, attachments, text, textFormat, timestamp, ...
  │
  ├── TeamsActivity(CoreActivity) constructor:
  │     From base typed properties (converted, stored in base slot):
  │       base.From       ← TeamsConversationAccount.FromConversationAccount(activity.From)
  │       base.Recipient  ← TeamsConversationAccount.FromConversationAccount(activity.Recipient)
  │       base.Conversation ← TeamsConversation.FromConversation(activity.Conversation)
  │     From Properties via Extract<T>:
  │       ChannelData  ← Extract<TeamsChannelData>("channelData")
  │       Entities     ← Extract<EntityList>("entities")
  │
  ├── MessageActivity(CoreActivity) constructor:
  │     Attachments     ← Extract<IList<TeamsAttachment>>("attachments")
  │     Text            ← Extract<string>("text")
  │     TextFormat      ← Extract<string>("textFormat")
  │     AttachmentLayout ← Extract<string>("attachmentLayout")
  │     SuggestedActions ← Extract<SuggestedActions>("suggestedActions")
  │
  ├── InvokeActivity:     Name  ← Extract<string>("name")
  │                       Value ← Extract<JsonNode>("value")
  ├── EventActivity:      Name  ← Extract<string>("name")
  │                       Value ← Extract<JsonNode>("value")
  ├── InstallUpdateActivity: Action ← Extract<string>("action")
  ├── ConversationUpdateActivity:
  │     MembersAdded   ← Extract<IList<TeamsConversationAccount>>("membersAdded")
  │     MembersRemoved ← Extract<IList<TeamsConversationAccount>>("membersRemoved")
  └── MessageReactionActivity:
        ReactionsAdded   ← Extract<IList<MessageReaction>>("reactionsAdded")
        ReactionsRemoved ← Extract<IList<MessageReaction>>("reactionsRemoved")
        ReplyToId        ← Extract<string>("replyToId")
```

## Agentic Identity Flow

```
Incoming activity JSON:
  { "from": { "id": "bot1", "agenticAppId": "app-123", "agenticUserId": "user-456" } }

CoreActivity.FromJsonStreamAsync()
  → activity.From = ConversationAccount { Id="bot1", AgenticAppId="app-123", AgenticUserId="user-456" }

ConversationClient.SendActivityAsync(activity):
  1. AgenticIdentity.FromAccount(activity.From)
     → Reads AgenticAppId, AgenticUserId, AgenticAppBlueprintId from typed fields
     → Returns AgenticIdentity { AgenticAppId="app-123", AgenticUserId="user-456" }
  2. CreateRequestOptions(agenticIdentity, ...)
     → BotRequestOptions.AgenticIdentity = agenticIdentity
  3. BotHttpClient.SendAsync(...)
     → request.Options.Set(AgenticIdentityKey, agenticIdentity)
  4. BotAuthenticationHandler middleware
     → Uses AgenticIdentity for user-delegated token acquisition
```

## Remaining Considerations

### Shared Mutable Properties Dictionary (Shallow Copy)

**Files: CoreActivity.cs, TeamsConversationAccount.cs**

The copy constructor shares the Properties reference:

```csharp
Properties = activity.Properties;  // Reference copy, not deep copy
```

When `TeamsActivity(CoreActivity)` calls `Extract<>()`, it removes keys from the shared dictionary, mutating the source activity. This is currently safe because the source isn't used after conversion, but it's fragile. Consider a shallow clone or document the contract that the source is consumed.

### Extract<T> Silently Loses Data for Unknown Types

When `raw` is neither `T` nor `JsonElement`, `Extract<T>` removes the key and returns `default`. This only affects Properties-based fields (channelData, attachments, entities, etc.) since `from`/`recipient`/`conversation` are now typed properties and never go through Extract.

### Context.SendActivityAsync Overwrites Conversation Reference

`Context.SendActivityAsync(TeamsActivity)` always applies `WithConversationReference(Activity)`, which overwrites `ServiceUrl`, `ChannelId`, `Conversation`, and `From`. For cross-conversation or proactive messaging, use `TeamsBotApplication.SendActivityAsync` directly.

### CoreActivity Constructors are Internal

CoreActivity constructors are `internal` — external consumers create instances via `CoreActivity.CreateBuilder()` or JSON deserialization (`FromJsonString`, `FromJsonStreamAsync`). The single `[JsonConstructor]` parameterized constructor handles both direct construction and deserialization, defaulting to `ActivityType.Message` and initializing `Conversation` to a non-null empty instance.

## Test Coverage

| Area | Coverage |
|------|----------|
| ConversationClient URL construction | Good |
| ConversationClient isTargeted from Recipient property | Good |
| ConversationClient AgenticIdentity from From property | Good |
| CoreActivity JSON round-trip (from/recipient as typed props) | Good |
| TeamsActivity.FromActivity() conversion | Good |
| TeamsActivity.ToJson() single from/recipient in output | Good |
| AgenticIdentity.FromAccount factory | Good |
| Extract<T> with JsonElement (for channelData, entities, etc.) | Good |
| TeamsActivityBuilder getter/setter property access (From/Recipient) | Good |
| TeamsActivityBuilder.WithConversationReference | Partial |
| Context.SendActivityAsync conversation ref application | Missing |
