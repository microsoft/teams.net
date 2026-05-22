<!-- Copyright (c) Microsoft Corporation. All rights reserved.-->
<!-- Licensed under the MIT License.-->

# Microsoft.Teams.Core

The foundational .NET library for building Microsoft Teams bots. It implements the [Activity Protocol](https://github.com/microsoft/Agents/blob/main/specs/activity/protocol-activity.md), providing the core bot application framework, conversation client, user token client, middleware pipeline, and support for both Bot and Agentic identities.

## Key Features

- **Activity Processing** &mdash; Receive, deserialize, and dispatch activities through a middleware pipeline
- **Conversation Client** &mdash; Send, update, and delete activities; manage conversation members and metadata
- **User Token Client** &mdash; OAuth token management, sign-in flows, and token exchange (SSO)
- **Middleware Pipeline** &mdash; Extensible `ITurnMiddleware` chain for cross-cutting concerns
- **Flexible Authentication** &mdash; Client secrets, managed identities (system/user-assigned), federated identities, and agentic (user-delegated) tokens via MSAL
- **Extensible Schema** &mdash; Loose `CoreActivity` model with `JsonExtensionData` for channel-specific properties
- **AOT-Compatible** &mdash; Source-generated JSON serialization via `CoreActivityJsonContext`

## Installation

```shell
dotnet add package Microsoft.Teams.Core
```

## Quick Start

### Register Services & Map Endpoint

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddBotApplication();

var app = builder.Build();
var bot = app.UseBotApplication(); // maps POST /api/messages

bot.OnActivity = async (activity, ct) =>
{
    if (activity.Type == ActivityType.Message)
    {
        var reply = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithConversation(activity.Conversation)
            .WithServiceUrl(activity.ServiceUrl)
            .WithProperty("text", "Hello from the bot!")
            .Build();

        await bot.SendActivityAsync(reply, ct);
    }
};

app.Run();
```

### Custom Bot Subclass

```csharp
public class MyBot : BotApplication
{
    public MyBot(
        ConversationClient conversationClient,
        UserTokenClient tokenClient,
        ILogger<MyBot> logger)
        : base(conversationClient, tokenClient, logger)
    {
        OnActivity = HandleActivityAsync;
    }

    private async Task HandleActivityAsync(CoreActivity activity, CancellationToken ct)
    {
        // your logic here
    }
}

// Registration
builder.AddBotApplication<MyBot>();
var bot = app.UseBotApplication<MyBot>();
```

### Middleware

```csharp
public class LoggingMiddleware : ITurnMiddleware
{
    public async Task OnTurnAsync(
        BotApplication bot, CoreActivity activity, NextTurn next, CancellationToken ct)
    {
        Console.WriteLine($"Activity: {activity.Type} from {activity.From?.Name}");
        await next(ct);
    }
}

bot.UseMiddleware(new LoggingMiddleware());
```

### Extensible Activity Schema

```csharp
public class MyChannelData : ChannelData
{
    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }
}

public class MyActivity : CoreActivity
{
    [JsonPropertyName("channelData")]
    public new MyChannelData? ChannelData { get; set; }
}
```

## Configuration

Provide credentials via `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "<your-secret>"
      }
    ]
  }
}
```

Or via environment variables:

```env
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-client-id>
AzureAd__ClientCredentials__0__SourceType=ClientSecret
AzureAd__ClientCredentials__0__ClientSecret=<your-secret>
```

When no MSAL configuration is provided, communication happens as anonymous REST calls, suitable for localhost testing.

## Design Principles

- **Loose schema** &mdash; `CoreActivity` contains only strictly required fields; additional fields are captured via `JsonExtensionData`
- **Simple serialization** &mdash; No custom converters; standard `System.Text.Json` with source generation
- **Extensible schema** &mdash; `ChannelData` and `ConversationAccount` support extension properties; generics allow custom types
- **MSAL-based auth** &mdash; Token acquisition built on top of Microsoft Identity Web
- **ASP.NET DI** &mdash; All dependencies configured via `IServiceCollection` extensions, reusing the existing `HttpClient` factory
- **ILogger & IConfiguration** &mdash; Standard .NET logging and configuration throughout

## Main Types

| Type | Description |
|------|-------------|
| `BotApplication` | Core entry point &mdash; processes HTTP requests, runs middleware, dispatches to handlers |
| `ConversationClient` | HTTP client for Bot Framework conversation APIs (send, update, delete, members) |
| `UserTokenClient` | HTTP client for Bot Framework Token Service (OAuth, SSO, sign-in) |
| `CoreActivity` | Activity data model following the Activity Protocol specification |
| `CoreActivityBuilder` | Fluent builder for constructing `CoreActivity` instances |
| `ITurnMiddleware` | Interface for middleware in the activity processing pipeline |
| `AgenticIdentity` | Represents user-delegated token acquisition identity |
| `BotHandlerException` | Exception wrapper preserving the activity that caused the error |
