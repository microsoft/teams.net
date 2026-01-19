# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

### Building
```bash
# Build entire solution
dotnet build core.slnx

# Build specific project
dotnet build src/Microsoft.Teams.Bot.Core/
dotnet build src/Microsoft.Teams.Bot.Apps/
dotnet build src/Microsoft.Teams.Bot.Compat/

# Build in Release mode
dotnet build core.slnx -c Release
```

### Testing
```bash
# Run all tests
dotnet test core.slnx

# Run specific test project
dotnet test test/Microsoft.Teams.Bot.Core.UnitTests/
dotnet test test/Microsoft.Teams.Bot.Apps.UnitTests/
dotnet test test/Microsoft.Teams.Bot.Compat.UnitTests/
dotnet test test/Microsoft.Teams.Bot.Core.Tests/

# Run a single test by filter
dotnet test --filter "FullyQualifiedName~TeamsActivityTests"
dotnet test --filter "FullyQualifiedName~ConversationClientTests.SendActivity"

# Run tests with verbosity
dotnet test --verbosity normal
```

### Running Samples
```bash
# Run samples from solution
dotnet run --project samples/CoreBot/
dotnet run --project samples/TeamsBot/
dotnet run --project samples/CompatBot/

# Run with specific URL
dotnet run --project samples/CoreBot/ -- --urls "http://localhost:3978"
```

### Integration Testing
```bash
# Use Integration Tests solution
dotnet test test/IntegrationTests.slnx
```

## Architecture Overview

### Three-Layer Architecture

This repository implements a modern Teams bot SDK with three distinct layers:

#### 1. **Microsoft.Teams.Bot.Core** (Foundation Layer)
Core bot communication and authentication primitives:
- **Activity Protocol**: `CoreActivity` implements the Activity Protocol Specification
- **HTTP Communication**: `BotHttpClient` handles all Bot Framework API calls
- **Clients**: `ConversationClient` (send/update/delete activities), `UserTokenClient` (OAuth)
- **Middleware Pipeline**: `TurnMiddleware` implements composable middleware pattern via `ITurnMiddleWare`
- **Authentication**: `BotAuthenticationHandler` provides MSAL-based token acquisition
  - **App-only tokens**: Default bot application identity
  - **Agentic tokens**: User-delegated (OBO) flow using `AgenticIdentity` from activity properties
- **Hosting**: `AddBotApplicationExtensions` registers services via dependency injection

#### 2. **Microsoft.Teams.Bot.Apps** (Teams Abstractions)
Teams-specific types, handlers, and API client:
- **TeamsActivity**: Extends `CoreActivity` with Teams properties (`TeamsConversationAccount`, `TeamsChannelData`, entities)
- **Typed Handlers**: Delegate pattern for different activity types:
  - `OnMessage` → `MessageHandler(MessageArgs, Context, CancellationToken)`
  - `OnInvoke` → `InvokeHandler(Context, CancellationToken)` returns `CoreInvokeResponse`
  - `OnMessageReaction`, `OnConversationUpdate`, `OnInstallationUpdate`
- **Context Object**: Provides `Context(botApp, activity)` for turn-scoped operations
- **TeamsApiClient**: Teams-specific APIs (team details, channels, members, meetings)
- **Fluent Builders**: `TeamsActivityBuilder` for constructing activities
- **Builder Pattern**: `TeamsBotApplicationBuilder.CreateBuilder()` for simplified setup

#### 3. **Microsoft.Teams.Bot.Compat** (Bot Framework v4 Bridge)
Compatibility layer enabling gradual migration from Bot Framework SDK v4:
- **Activity Conversion**: `CompatActivity` converts between `CoreActivity` and Bot Framework `Activity` via JSON serialization (uses Newtonsoft.Json for BF compatibility)
- **Adapter**: `CompatAdapter` implements `IBotFrameworkHttpAdapter` to bridge legacy code
- **Service Adapters**: Wraps new SDK clients to expose Bot Framework interfaces:
  - `CompatConnectorClient` → `IConnectorClient`
  - `CompatConversations` → `IConversations`
  - `CompatUserTokenClient` → `IUserTokenClient`
  - `CompatTeamsInfo` → Static methods matching Bot Framework's `TeamsInfo` API
- **Middleware Adaptation**: `CompatAdapterMiddleware` wraps Bot Framework middleware for new pipeline
- **Use Case**: Run existing Bot Framework v4 bots on new SDK without full rewrite

### Key Architectural Patterns

#### Serialization Architecture
- **System.Text.Json with Source Generators**: All serialization uses source-generated contexts for AOT compatibility and performance
  - `CoreActivityJsonContext` - Core types (CoreActivity, ConversationAccount, etc.)
  - `TeamsActivityJsonContext` - Teams types (TeamsActivity, TeamsChannelData, entities)
- **Extension Points**: `JsonExtensionData` on `Properties` dictionary allows channel-specific fields
- **Dual Strategy**: Source-generated for known types, reflection-based fallback for custom activity types
- **Naming**: CamelCase in JSON, `JsonIgnoreCondition.WhenWritingNull` for nulls

#### Middleware Pipeline
```
HTTP Request → BotApplication.ProcessAsync()
  → TurnMiddleware.RunPipelineAsync()
    → Middleware 1 → Middleware 2 → ... → OnActivity handler
  → Activity routing by type (Message/Invoke/etc.)
  → Handler delegate execution with Context
```

Each `ITurnMiddleWare` implements:
```csharp
Task OnTurnAsync(BotApplication app, CoreActivity activity, NextTurn next);
```

#### Authentication Flow
```
HTTP Request → BotHttpClient.SendAsync()
  → BotAuthenticationHandler intercepts
  → Check request options for AgenticIdentity
    ├── If present: Acquire user-delegated (OBO) token
    └── If null: Acquire app-only token
  → Attach "Authorization: Bearer {token}" header
  → Send to Bot Framework service
```

Configuration via `IServiceCollection.ConfigureMSAL()` reading `AzureAd` section:
- `TenantId`, `ClientId` for app identity
- `ClientCredentials` with `ClientSecret` or certificate
- `Scope`: "https://api.botframework.com/.default"

#### Handler Routing (TeamsBotApplication)
```csharp
OnActivity(CoreActivity activity)
  → Convert to TeamsActivity
  → Create Context(this, teamsActivity)
  → Switch on activity.Type:
      case "message" → OnMessage?(MessageArgs, context, ct)
      case "invoke" → OnInvoke?(context, ct) → write response
      case "messageReaction" → OnMessageReaction?(args, context, ct)
      case "conversationUpdate" → OnConversationUpdate?(args, context, ct)
      case "installationUpdate" → OnInstallationUpdate?(args, ct)
```

### Design Principles (from README)

1. **Loose Schema**: `TeamsActivity` contains only strictly required fields; additional fields captured in `Properties` dictionary with `JsonExtensionData`
2. **Simple Serialization**: No custom converters; direct System.Text.Json with source generators
3. **Extensible Schema**: Channel-specific types (like `ChannelData`) define their own `Properties` for unknown fields
4. **Auth Based on MSAL**: Token acquisition uses Microsoft.Identity.Web
5. **ASP.NET DI**: All services registered via standard `IServiceCollection` extensions
6. **ILogger and IConfiguration**: Follow ASP.NET Core conventions

### Custom Activity Types

Extend CoreActivity with type-safe channel-specific properties:

```csharp
public class MyChannelData : ChannelData
{
    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }
}

public class MyActivity : TeamsActivity
{
    [JsonPropertyName("channelData")]
    public new MyChannelData? ChannelData { get; set; }
}

// Deserialize with custom type
var activity = TeamsActivity.FromJsonString<MyActivity>(json);
```

No need to override `FromJsonString` - it's generic on the base class.

## Testing Strategies

### Local Development (Anonymous)
When MSAL configuration is not provided, all communication happens as anonymous REST calls suitable for localhost testing:

1. Install Microsoft 365 Agents Playground:
   ```bash
   # Linux
   curl -s https://raw.githubusercontent.com/OfficeDev/microsoft-365-agents-toolkit/dev/.github/scripts/install-agentsplayground-linux.sh | bash

   # Windows
   winget install m365agentsplayground
   ```

2. Run bot locally:
   ```bash
   dotnet run --project samples/CoreBot/ -- --urls "http://localhost:3978"
   ```

### Teams Deployment (Authenticated)
Configure Azure AD credentials via appsettings.json or environment variables:

**appsettings.json:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "Scope": "https://api.botframework.com/.default",
    "ClientCredentials": [{
      "SourceType": "ClientSecret",
      "ClientSecret": "<your-entra-app-secret>"
    }]
  }
}
```

**Environment variables:**
```bash
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-client-id>
AzureAd__Scope=https://api.botframework.com/.default
AzureAd__ClientCredentials__0__SourceType=ClientSecret
AzureAd__ClientCredentials__0__ClientSecret=<your-entra-app-secret>
```

## Common Development Patterns

### Creating a Basic Bot
```csharp
var builder = TeamsBotApplication.CreateBuilder();
var app = builder.Build();

app.OnMessage = async (messageArgs, context, ct) =>
{
    await context.SendTypingActivityAsync(ct);
    var reply = TeamsActivity.CreateBuilder()
        .WithText($"You sent: {messageArgs.Text}")
        .Build();
    await context.SendActivityAsync(reply, ct);
};

app.Run();
```

### Using Fluent Activity Builder
```csharp
TeamsActivity activity = TeamsActivity.CreateBuilder()
    .WithText("Hello!")
    .WithConversationReference(incomingActivity) // Copy routing info
    .WithAttachment(new TeamsAttachment { /* ... */ })
    .WithEntities(new EntityList { /* mentions */ })
    .Build();
```

### Adding Middleware
```csharp
public class LoggingMiddleware : ITurnMiddleWare
{
    public async Task OnTurnAsync(BotApplication app, CoreActivity activity, NextTurn next)
    {
        Console.WriteLine($"Processing: {activity.Type}");
        await next(); // Call next middleware or handler
        Console.WriteLine($"Completed: {activity.Type}");
    }
}

// Register
botApp.Use(new LoggingMiddleware());
```

### Bot Framework v4 Migration
```csharp
// Use CompatAdapter to run existing Bot Framework code
services.AddSingleton<IBotFrameworkHttpAdapter, CompatAdapter>();
services.AddTransient<IBot, MyExistingBot>();

// Legacy middleware wrapping
var legacyMiddleware = new ShowTypingMiddleware();
botApp.Use(new CompatAdapterMiddleware(legacyMiddleware));

// Access Bot Framework services in turn context (via compat layer)
var connector = turnContext.TurnState.Get<IConnectorClient>();
var tokenClient = turnContext.TurnState.Get<IUserTokenClient>();
```

## Working with InternalsVisibleTo

Several projects expose internals for testing:
- `Microsoft.Teams.Bot.Compat` → `Microsoft.Teams.Bot.Core.Tests`, `Microsoft.Teams.Bot.Compat.UnitTests`

Add test assemblies to `InternalsVisibleTo.cs` when testing internal classes.

## File Naming Conventions

- **Schema types**: Match JSON property names (e.g., `CoreActivity`, `ConversationAccount`)
- **Clients**: End with `Client` (e.g., `ConversationClient`, `TeamsApiClient`)
- **Handlers**: End with `Handler` (e.g., `BotAuthenticationHandler`, `MessageHandler`)
- **Extensions**: End with `Extensions` (e.g., `AddBotApplicationExtensions`, `CompatHostingExtensions`)
- **Builders**: End with `Builder` (e.g., `CoreActivityBuilder`, `TeamsBotApplicationBuilder`)
- **Compat layer**: Prefix with `Compat` (e.g., `CompatActivity`, `CompatAdapter`)

## Version Management

Version is controlled by `version.json` at repository root using Nerdbank.GitVersioning:
```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/master/src/NerdBank.GitVersioning/version.schema.json",
  "version": "0.0.1",
  "publicReleaseRefSpec": [ "^refs/heads/main$" ]
}
```

Version is automatically calculated from Git history during build.

## CI/CD

- **CI Workflow**: `.github/workflows/core-ci.yaml` - builds and runs tests
- **Test Workflow**: `.github/workflows/core-test.yaml` - runs test suite
- **CD Pipeline**: `.azdo/cd-core.yaml` - Azure DevOps deployment

## Important Gotchas

1. **JSON Serialization**: Always use source-generated contexts (`CoreActivityJsonContext.Default.CoreActivity`) for performance. Only fall back to reflection-based options for custom types.

2. **Activity Conversion**: Compat layer uses **JSON roundtrip** (not object mapping) for CoreActivity ↔ Activity conversion. This ensures format compatibility but has serialization overhead.

3. **Agentic Identity**: Extract from activity properties using `AgenticIdentity.FromProperties(activity.Properties)`. Required for user-delegated (OBO) operations.

4. **Middleware Order**: Middleware executes in registration order. Always register logging/error handling first.

5. **Invoke Responses**: `OnInvoke` handler MUST return `CoreInvokeResponse` which is written to HTTP response. Don't send activities in invoke handlers; return response object.

6. **Service URL**: Activities require `ServiceUrl` for routing. Always preserve from incoming activity or set explicitly.

7. **Channel-Specific Data**: Use `Properties` dictionary for channel-specific fields. Don't add strongly-typed properties unless they're in the Activity Protocol spec.

8. **Testing with Real Credentials**: Never commit secrets. Use user-secrets, environment variables, or Azure Key Vault for credentials.

9. **Gen0 Collections**: When optimizing serialization, always benchmark with realistic payload sizes. Small payloads may show worse performance with "optimized" byte-based approaches vs. simple string serialization.

10. **Bot Framework Interop**: When using compat layer, `TurnContext.TurnState` is populated with wrapped service adapters. Access them via `turnContext.TurnState.Get<IConnectorClient>()` not direct DI.
