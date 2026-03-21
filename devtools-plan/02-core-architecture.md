# Core Branch Architecture (`upstream/next/core`)

How the target `upstream/next/core` branch works — the architecture DevTools must integrate with.

## Overview

The core branch is a complete rewrite of the .NET SDK. Key differences from `main`:

- **No plugin system** — `IPlugin`, `IAspNetCorePlugin`, `ISenderPlugin`, `[Plugin]`, `[Dependency]` are all removed
- **3 layers**: `Microsoft.Teams.Bot.Core` → `Microsoft.Teams.Bot.Apps` → `Microsoft.Teams.Bot.Compat`
- **Middleware-based pipeline** instead of plugin event callbacks
- **Virtual methods on `ConversationClient`** for extensibility
- **Minimal API hosting** (`MapPost`) instead of MVC controllers

---

## Layer 1: `Microsoft.Teams.Bot.Core`

The protocol-level foundation. No Teams-specific concepts.

### `BotApplication.cs`

The central class that processes incoming activities.

```csharp
public class BotApplication
{
    private readonly ConversationClient? _conversationClient;
    private readonly UserTokenClient? _userTokenClient;
    internal TurnMiddleware MiddleWare { get; }

    public BotApplication(ConversationClient conversationClient, UserTokenClient userTokenClient,
        ILogger<BotApplication> logger, BotApplicationOptions? options = null)
    {
        MiddleWare = new TurnMiddleware();
        _conversationClient = conversationClient;
        _userTokenClient = userTokenClient;
        // ...
    }

    public ConversationClient ConversationClient => _conversationClient ?? throw ...;
    public UserTokenClient UserTokenClient => _userTokenClient ?? throw ...;

    // Terminal handler — invoked after all middleware runs
    public virtual Func<CoreActivity, CancellationToken, Task>? OnActivity { get; set; }

    // Entry point for incoming HTTP requests
    public virtual async Task ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        CoreActivity activity = await CoreActivity.FromJsonStreamAsync(httpContext.Request.Body, cancellationToken)
            ?? throw new InvalidOperationException("Invalid Activity");

        try
        {
            CancellationToken token = Debugger.IsAttached ? CancellationToken.None : cancellationToken;
            await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, token);
        }
        catch (Exception ex)
        {
            throw new BotHandlerException("Error processing activity", ex, activity);
        }
    }

    // Sends activity via ConversationClient
    public async Task<SendActivityResponse?> SendActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        return await _conversationClient.SendActivityAsync(activity, cancellationToken: cancellationToken);
    }

    // Register middleware
    public ITurnMiddleware UseMiddleware(ITurnMiddleware middleware)
    {
        MiddleWare.Use(middleware);
        return MiddleWare;
    }
}
```

**Key integration points for DevTools:**
- `ProcessAsync` — where incoming activities enter the system
- `SendActivityAsync` — delegates to `ConversationClient` (interceptable via virtual override)
- `UseMiddleware` — how to register middleware that sees every incoming activity
- `OnActivity` — terminal callback, runs after all middleware

### `ITurnMiddleware.cs`

```csharp
public delegate Task NextTurn(CancellationToken cancellationToken);

public interface ITurnMiddleware
{
    Task OnTurnAsync(BotApplication botApplication, CoreActivity activity,
        NextTurn nextTurn, CancellationToken cancellationToken = default);
}
```

Middleware can:
- Run code **before** `nextTurn()` (pre-processing)
- Run code **after** `nextTurn()` (post-processing)
- Wrap `nextTurn()` in try/catch (error handling)
- Short-circuit by not calling `nextTurn()` at all

### `TurnMiddleware.cs` (internal)

Chain-of-responsibility pipeline executor:

```csharp
internal sealed class TurnMiddleware : ITurnMiddleware, IEnumerable<ITurnMiddleware>
{
    private readonly IList<ITurnMiddleware> _middlewares = [];

    internal TurnMiddleware Use(ITurnMiddleware middleware) { _middlewares.Add(middleware); return this; }

    public Task RunPipelineAsync(BotApplication botApplication, CoreActivity activity,
        Func<CoreActivity, CancellationToken, Task>? callback, int nextMiddlewareIndex, CancellationToken ct)
    {
        if (nextMiddlewareIndex == _middlewares.Count)
            return callback?.Invoke(activity, ct) ?? Task.CompletedTask;

        ITurnMiddleware nextMiddleware = _middlewares[nextMiddlewareIndex];
        return nextMiddleware.OnTurnAsync(
            botApplication, activity,
            (ct) => RunPipelineAsync(botApplication, activity, callback, nextMiddlewareIndex + 1, ct),
            ct);
    }
}
```

### `ConversationClient.cs`

All methods are `virtual` — this is how DevTools can intercept outgoing activities:

```csharp
public class ConversationClient(HttpClient httpClient, ILogger<ConversationClient> logger = default!)
{
    internal const string ConversationHttpClientName = "BotConversationClient";

    public CustomHeaders DefaultCustomHeaders { get; } = [];

    public virtual async Task<SendActivityResponse> SendActivityAsync(CoreActivity activity,
        CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        // Builds URL from activity.ServiceUrl + conversation ID
        // Serializes activity to JSON, sends via HTTP POST
        // Returns SendActivityResponse with activity ID
    }

    public virtual async Task<SendActivityResponse> UpdateActivityAsync(...) { ... }
    public virtual async Task DeleteActivityAsync(...) { ... }
    public virtual async Task<IList<ConversationAccount>> GetConversationMembersAsync(...) { ... }
    public virtual async Task<T> GetConversationMemberAsync<T>(...) { ... }
    // ... all methods are virtual
}
```

### `CoreActivity.cs` (Schema)

The activity DTO — replaces `Activity`/`IActivity` from main:

```csharp
public class CoreActivity
{
    [JsonPropertyName("type")]         public string Type { get; set; }
    [JsonPropertyName("channelId")]    public string? ChannelId { get; set; }
    [JsonPropertyName("id")]           public string? Id { get; set; }
    [JsonPropertyName("serviceUrl")]   public Uri? ServiceUrl { get; set; }
    [JsonPropertyName("channelData")]  public ChannelData? ChannelData { get; set; }
    [JsonPropertyName("from")]         public ConversationAccount? From { get; set; }
    [JsonPropertyName("recipient")]    public ConversationAccount? Recipient { get; set; }
    [JsonPropertyName("conversation")] public Conversation? Conversation { get; set; }
    [JsonPropertyName("entities")]     public JsonArray? Entities { get; set; }
    [JsonPropertyName("attachments")]  public JsonArray? Attachments { get; set; }
    [JsonPropertyName("value")]        public JsonNode? Value { get; set; }
    [JsonPropertyName("replyToId")]    public string? ReplyToId { get; set; }
    [JsonExtensionData]                public ExtendedPropertiesDictionary Properties { get; set; } = [];

    // AOT-compatible serialization
    public virtual string ToJson() => JsonSerializer.Serialize(this, CoreActivityJsonContext.Default.CoreActivity);
    public static CoreActivity FromJsonString(string json) => ...;
    public static ValueTask<CoreActivity?> FromJsonStreamAsync(Stream stream, CancellationToken ct) => ...;
}
```

### `ConversationAccount.cs` (Schema)

```csharp
public class ConversationAccount
{
    [JsonPropertyName("id")]   public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonExtensionData]        public ExtendedPropertiesDictionary Properties { get; set; } = [];
}
```

### `Conversation.cs` (Schema)

```csharp
public class Conversation
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonExtensionData]      public ExtendedPropertiesDictionary Properties { get; set; } = [];
}
```

> **Note:** Core's `Conversation` has only `Id` + extension data. Main's `Conversation` has `Id`, `Type`, `Name`. This affects the `chat` wire format — see porting design doc.

---

## Layer 2: `Microsoft.Teams.Bot.Apps`

Teams-specific application layer built on Core.

### `TeamsBotApplication.cs`

Extends `BotApplication` with Teams routing:

```csharp
public class TeamsBotApplication : BotApplication
{
    private readonly Router _router = new();

    // Handler registration
    public TeamsBotApplication OnMessage(Func<Context, CancellationToken, Task> handler) { ... }
    public TeamsBotApplication OnInvoke(string name, Func<Context, CancellationToken, Task> handler) { ... }
    // ... other handler types
}
```

### Hosting Extensions

**Service registration:**
```csharp
// AddTeamsBotApplication registers:
// - TeamsApiClient (with auth handler)
// - Then calls AddBotApplication<TeamsBotApplication>() which registers:
//   - BotApplicationOptions
//   - HttpContextAccessor
//   - JWT auth + authorization
//   - ConversationClient (with named HttpClient "BotConversationClient" + auth handler)
//   - UserTokenClient
//   - TeamsBotApplication as singleton
```

**Endpoint mapping:**
```csharp
public static TApp UseBotApplication<TApp>(this IEndpointRouteBuilder endpoints, string routePath = "api/messages")
    where TApp : BotApplication
{
    // Adds auth/authz middleware
    if (endpoints is IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    TApp botApp = endpoints.ServiceProvider.GetService<TApp>()
        ?? throw new InvalidOperationException("Application not registered");

    // Maps POST endpoint that calls ProcessAsync
    endpoints.MapPost(routePath, (HttpContext httpContext, CancellationToken cancellationToken)
        => botApp.ProcessAsync(httpContext, cancellationToken)
    ).RequireAuthorization();

    return botApp;
}
```

---

## Typical Usage

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();

var app = builder.Build();
var teamsApp = app.UseTeamsBotApplication();

teamsApp.OnMessage(async (context, ct) =>
{
    await context.SendActivityAsync(new CoreActivity("message") { ... }, ct);
});

app.Run();
```

---

## Summary: What DevTools Needs to Hook Into

| Concern | Core mechanism |
|---------|---------------|
| Intercept incoming activities | `ITurnMiddleware` — registered via `botApp.UseMiddleware()` |
| Intercept outgoing activities | Subclass `ConversationClient` — override `virtual SendActivityAsync()` |
| Intercept errors | Middleware wraps `nextTurn()` in try/catch |
| Serve static files | `IApplicationBuilder` middleware / `IEndpointRouteBuilder` endpoints |
| WebSocket connections | `IApplicationBuilder.UseWebSockets()` + endpoint mapping |
| DI registration | `IServiceCollection` extensions |
| Configuration | `IConfiguration` binding |
