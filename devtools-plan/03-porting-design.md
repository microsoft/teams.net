# Porting Design: DevTools → Core Branch

How to port the DevTools plugin from `main` to `upstream/next/core`.

---

## Architecture Mapping

| DevTools on `main` (plugin) | Core branch equivalent |
|---|---|
| `IPlugin.OnActivity` (incoming) | `ITurnMiddleware.OnTurnAsync` — before `nextTurn()` |
| `IPlugin.OnActivitySent` (outgoing) | Decorator on `ConversationClient` — override virtual `SendActivityAsync` |
| `IPlugin.OnError` | Middleware try/catch wrapping `nextTurn()` |
| `IAspNetCorePlugin.Configure()` | Extension method on `IApplicationBuilder` called in `UseDevTools()` |
| `IPlugin.OnInit / OnStart` | Startup logic in `AddDevTools()` / `UseDevTools()` extensions |
| `[Dependency]` injection | Constructor injection via standard DI |
| `AddTeamsPlugin<T>()` | `services.AddDevTools()` + `app.UseDevTools()` |
| `ISenderPlugin.Do()` (test injection) | `BotApplication.ProcessAsync(HttpContext)` |
| MVC controllers + `[ApiController]` | Minimal API endpoints (`MapGet`, `MapPost`) |
| `Activity` / `IActivity` | `CoreActivity` |
| `Conversation` (id, type, name) | `Conversation` (id only) + extension data |

---

## New Project Structure

```
core/src/Microsoft.Teams.Bot.DevTools/
    Microsoft.Teams.Bot.DevTools.csproj
    DevToolsMiddleware.cs               ← ITurnMiddleware (incoming + errors)
    DevToolsConversationClient.cs       ← ConversationClient decorator (outgoing)
    DevToolsService.cs                  ← Shared state: WebSocketCollection, metadata, emit helpers
    DevToolsSettings.cs                 ← Config POCO (adapted from main)
    DevToolsHostingExtensions.cs        ← AddDevTools() + UseDevTools()
    Events/
        IDevToolsEvent.cs               ← Renamed from IEvent (avoid collision)
        ActivityEvent.cs                ← Rewritten for CoreActivity (preserve wire format!)
        MetaDataEvent.cs                ← Copy from main
    Models/
        Page.cs                         ← Copy from main
        MetaData.cs                     ← Copy from main
    WebSocketCollection.cs              ← Copy from main (namespace change only)
    Extensions/
        WebSocketExtensions.cs          ← Copy from main
    web/                                ← Copy entire embedded UI from main (unchanged)
```

---

## Key Components

### `DevToolsMiddleware` — implements `ITurnMiddleware`

Intercepts incoming activities and errors.

```csharp
public class DevToolsMiddleware : ITurnMiddleware
{
    private readonly DevToolsService _service;

    public DevToolsMiddleware(DevToolsService service)
    {
        _service = service;
    }

    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity,
        NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        // Emit received event BEFORE processing
        await _service.EmitReceived(activity, cancellationToken);

        try
        {
            await nextTurn(cancellationToken);
        }
        catch (Exception ex)
        {
            // Emit error event
            await _service.EmitError(activity, ex, cancellationToken);
            throw;  // re-throw so BotApplication's error handling still works
        }
    }
}
```

### `DevToolsConversationClient` — extends `ConversationClient`

Intercepts outgoing activities by overriding the virtual `SendActivityAsync`.

```csharp
public class DevToolsConversationClient : ConversationClient
{
    private readonly DevToolsService _service;

    public DevToolsConversationClient(HttpClient httpClient, ILogger<ConversationClient> logger,
        DevToolsService service) : base(httpClient, logger)
    {
        _service = service;
    }

    public override async Task<SendActivityResponse> SendActivityAsync(CoreActivity activity,
        CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        var response = await base.SendActivityAsync(activity, customHeaders, cancellationToken);

        // Emit sent event AFTER successful send
        await _service.EmitSent(activity, cancellationToken);

        return response;
    }
}
```

### `DevToolsService` — singleton shared state

Central service that holds WebSocket connections, metadata, and provides emit helpers.

```csharp
public class DevToolsService
{
    public WebSocketCollection Sockets { get; } = new();
    public DevToolsSettings Settings { get; }

    public string? AppId { get; set; }
    public string? AppName { get; set; }

    public MetaData MetaData => new()
    {
        Id = AppId,
        Name = AppName,
        Pages = Settings.Pages
    };

    public DevToolsService(DevToolsSettings settings)
    {
        Settings = settings;
    }

    public Task EmitReceived(CoreActivity activity, CancellationToken ct)
        => Sockets.Emit(ActivityEvent.Received(activity), ct);

    public Task EmitSent(CoreActivity activity, CancellationToken ct)
        => Sockets.Emit(ActivityEvent.Sent(activity), ct);

    public Task EmitError(CoreActivity activity, object error, CancellationToken ct)
        => Sockets.Emit(ActivityEvent.Err(activity, error), ct);
}
```

### `DevToolsSettings.cs`

```csharp
public class DevToolsSettings
{
    public IList<Page> Pages { get; set; } = [];
}
```

### `DevToolsHostingExtensions.cs`

```csharp
public static class DevToolsHostingExtensions
{
    public static IServiceCollection AddDevTools(this IServiceCollection services)
    {
        // Register settings from configuration
        services.AddSingleton<DevToolsSettings>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return config.GetSection("DevTools").Get<DevToolsSettings>() ?? new();
        });

        // Register shared service
        services.AddSingleton<DevToolsService>();

        // Register middleware
        services.AddSingleton<DevToolsMiddleware>();

        // Replace ConversationClient registration with DevToolsConversationClient
        // Remove existing ConversationClient descriptor
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ConversationClient));
        if (descriptor != null) services.Remove(descriptor);

        // Re-register with DevToolsConversationClient using same named HttpClient
        services.AddHttpClient<ConversationClient, DevToolsConversationClient>(
            ConversationClient.ConversationHttpClientName);

        return services;
    }

    public static IEndpointRouteBuilder UseDevTools(this IEndpointRouteBuilder endpoints)
    {
        var app = endpoints as IApplicationBuilder;

        // Enable WebSockets
        app?.UseWebSockets(new WebSocketOptions { AllowedOrigins = { "*" } });

        // Serve embedded static files
        app?.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new ManifestEmbeddedFileProvider(
                Assembly.GetExecutingAssembly(), "web"),
            ServeUnknownFileTypes = true,
            RequestPath = "/devtools"
        });

        // Register middleware on bot application
        var botApp = endpoints.ServiceProvider.GetRequiredService<BotApplication>();
        var middleware = endpoints.ServiceProvider.GetRequiredService<DevToolsMiddleware>();
        botApp.UseMiddleware(middleware);

        // Resolve services for endpoint closures
        var service = endpoints.ServiceProvider.GetRequiredService<DevToolsService>();
        var lifetime = endpoints.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var files = new ManifestEmbeddedFileProvider(
            Assembly.GetExecutingAssembly(), "web");

        // Populate AppId/AppName from BotApplicationOptions
        var options = endpoints.ServiceProvider.GetService<BotApplicationOptions>();
        service.AppId = options?.AppId;

        // Log DevTools URL
        var server = endpoints.ServiceProvider.GetRequiredService<IServer>();
        var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;
        var logger = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevTools");
        foreach (var address in addresses)
            logger.LogInformation("DevTools available at {Address}/devtools", address);

        // Map endpoints (see "Minimal API Endpoints" section below)
        MapDevToolsEndpoints(endpoints, service, lifetime, files, botApp);

        return endpoints;
    }
}
```

---

## Minimal API Endpoints (replaces MVC controllers)

### Serve React UI

```csharp
// SPA fallback: serve embedded file or index.html
endpoints.MapGet("/devtools/{*path}", (string? path) =>
{
    var file = files.GetFileInfo(path ?? "index.html");
    if (!file.Exists)
        file = files.GetFileInfo("index.html");
    return Results.File(file.CreateReadStream(), contentType: "text/html");
}).AllowAnonymous();

endpoints.MapGet("/devtools", () =>
{
    var file = files.GetFileInfo("index.html");
    return Results.File(file.CreateReadStream(), contentType: "text/html");
}).AllowAnonymous();
```

### WebSocket endpoint

```csharp
endpoints.MapGet("/devtools/sockets", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid().ToString();
    var buffer = new byte[1024];

    service.Sockets.Add(id, socket);
    await service.Sockets.Emit(id, new MetaDataEvent(service.MetaData), lifetime.ApplicationStopping);

    try
    {
        while (socket.State.HasFlag(WebSocketState.Open))
            await socket.ReceiveAsync(buffer, lifetime.ApplicationStopping);
    }
    catch (Exception) when (e is ConnectionAbortedException or OperationCanceledException) { }
    finally
    {
        if (socket.IsCloseable())
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", lifetime.ApplicationStopping);
    }

    service.Sockets.Remove(id);
}).AllowAnonymous();
```

### Test activity injection

```csharp
endpoints.MapPost("/v3/conversations/{conversationId}/activities",
    async (string conversationId, HttpContext context, JsonNode body, CancellationToken ct) =>
{
    var isDevTools = context.Request.Headers.TryGetValue("x-teams-devtools", out var vals)
        && vals.Any(h => h == "true");

    body["id"] ??= Guid.NewGuid().ToString();

    if (!isDevTools)
        return Results.Json(new { id = body["id"] }, statusCode: 201);

    // Build test activity
    body["from"] ??= JsonSerializer.SerializeToNode(new ConversationAccount
    {
        Id = "devtools", Name = "devtools"
    });
    body["conversation"] = JsonSerializer.SerializeToNode(new { id = conversationId });
    body["recipient"] = JsonSerializer.SerializeToNode(new ConversationAccount
    {
        Id = service.AppId ?? "", Name = service.AppName
    });

    // Create a new HttpContext with the activity body and route through ProcessAsync
    var activityJson = body.ToJsonString();
    var stream = new MemoryStream(Encoding.UTF8.GetBytes(activityJson));
    var testContext = new DefaultHttpContext { RequestServices = context.RequestServices };
    testContext.Request.Body = stream;
    testContext.Request.ContentType = "application/json";

    await botApp.ProcessAsync(testContext, ct);

    return Results.Json(new { id = body["id"] }, statusCode: 201);
}).AllowAnonymous();
```

---

## Wire Format Compatibility

The React UI expects exact JSON property names. This is the critical compatibility constraint.

### `ActivityEvent.cs` (rewritten for CoreActivity)

```csharp
public class ActivityEvent : IDevToolsEvent
{
    [JsonPropertyName("id")]     public Guid Id { get; }
    [JsonPropertyName("type")]   public string Type { get; }
    [JsonPropertyName("body")]   public object? Body { get; }
    [JsonPropertyName("chat")]   public object Chat { get; set; }    // ← see below
    [JsonPropertyName("error")]  public object? Error { get; set; }
    [JsonPropertyName("sentAt")] public DateTime SentAt { get; }

    public ActivityEvent(string type, CoreActivity activity)
    {
        Id = Guid.NewGuid();
        Type = $"activity.{type}";
        Body = activity;
        SentAt = DateTime.Now;

        // Build "chat" object matching what React UI expects
        Chat = new
        {
            id = activity.Conversation?.Id ?? "unknown",
            type = "personal",     // Core doesn't have ConversationType — default to personal
            name = "default"       // Core's Conversation has no Name — default
        };
    }

    public static ActivityEvent Received(CoreActivity activity) => new("received", activity);
    public static ActivityEvent Sent(CoreActivity activity) => new("sent", activity);
    public static ActivityEvent Err(CoreActivity activity, object error)
        => new("error", activity) { Error = error };
}
```

### Wire format: `chat` property mapping

| Property | Main branch source | Core branch source |
|----------|-------------------|-------------------|
| `chat.id` | `Conversation.Id` | `CoreActivity.Conversation.Id` |
| `chat.type` | `Conversation.Type` (enum: Personal/Group/Channel) | Not available — default to `"personal"` |
| `chat.name` | `Conversation.Name` | Not available — default to `"default"` |

The `chat.type` and `chat.name` values can potentially be extracted from `Conversation.Properties` (extension data) if the channel provides them. A future improvement could check:
```csharp
Chat = new
{
    id = activity.Conversation?.Id ?? "unknown",
    type = activity.Conversation?.Properties.GetValueOrDefault("type")?.ToString() ?? "personal",
    name = activity.Conversation?.Properties.GetValueOrDefault("name")?.ToString() ?? "default"
};
```

### Wire format: `body` property

`CoreActivity` serializes with `System.Text.Json` using `[JsonPropertyName]` attributes that match the Bot Framework Activity Protocol spec (camelCase). The React UI reads standard activity properties like `type`, `text`, `from`, `conversation` — these all match.

---

## ConversationClient DI Replacement Strategy

This is the trickiest part of the port. `AddBotApplication` registers `ConversationClient` via:

```csharp
services.AddHttpClient<ConversationClient>(ConversationClient.ConversationHttpClientName)
    .AddHttpMessageHandler(sp => new BotAuthenticationHandler(...));
```

`AddDevTools()` must:
1. Remove the existing `ConversationClient` service descriptor
2. Re-register using `DevToolsConversationClient` with the **same named HttpClient** and auth handler

```csharp
// In AddDevTools(), called AFTER AddTeamsBotApplication():
var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ConversationClient));
if (descriptor != null) services.Remove(descriptor);

services.AddHttpClient<ConversationClient, DevToolsConversationClient>(
    ConversationClient.ConversationHttpClientName)
    .AddHttpMessageHandler(sp => new BotAuthenticationHandler(...));
```

Alternative approach if the auth handler re-registration is problematic: use `services.Decorate<ConversationClient>()` from a DI decoration library, or manually wrap the existing instance.

---

## Code Reuse Assessment

| File | Action | Reason |
|------|--------|--------|
| `web/` (entire UI) | **Copy as-is** | Framework-agnostic React app |
| `WebSocketCollection.cs` | **Copy**, change namespace | No SDK dependencies (uses `IEvent` interface → rename to `IDevToolsEvent`) |
| `Models/Page.cs` | **Copy**, change namespace | Pure POCO |
| `Models/MetaData.cs` | **Copy**, change namespace | Pure POCO |
| `Events/MetaDataEvent.cs` | **Copy**, change namespace + interface rename | Pure POCO |
| `Extensions/WebSocket.cs` | **Copy**, change namespace | Pure helper |
| `TeamsDevToolsSettings.cs` | **Copy**, change namespace + rename to `DevToolsSettings` | Pure POCO |
| `Events/ActivityEvent.cs` | **Rewrite** | Uses `IActivity`/`Conversation` → must use `CoreActivity` + build `chat` adapter |
| `DevToolsPlugin.cs` | **Rewrite** → 3 classes | Split into `DevToolsMiddleware` + `DevToolsConversationClient` + `DevToolsService` |
| `Controllers/DevToolsController.cs` | **Rewrite** → minimal APIs | MVC → `MapGet` in `UseDevTools()` |
| `Controllers/ActivityController.cs` | **Rewrite** → minimal API | `ISenderPlugin.Do()` → `BotApplication.ProcessAsync()` |
| `Extensions/HostApplicationBuilder.cs` | **Rewrite** | Different DI registration pattern |
| `Extensions/ConfigurationManager.cs` | **Inline** into `AddDevTools()` | Single use, simpler to inline |
| `Event.cs` (IEvent) | **Rewrite** → `IDevToolsEvent` | Remove `[TrueTypeJson]` dependency, add `[JsonDerivedType]` or keep simple |

---

## Potential Challenges

### 1. ConversationClient DI replacement

`AddBotApplication` registers `ConversationClient` with a named HttpClient and `BotAuthenticationHandler`. When `AddDevTools` replaces it, the auth handler pipeline must be preserved. The `AddHttpClient<TClient, TImplementation>()` overload should handle this, but needs testing.

**Mitigation:** If replacement is fragile, use the decorator pattern — `DevToolsConversationClient` takes the original `ConversationClient` as a constructor parameter and delegates to it, rather than extending it.

### 2. Test activity injection without auth

On `main`, `ActivityController` creates a fake JWT to bypass auth. On core, `ProcessAsync` is called behind `RequireAuthorization()`. The DevTools test injection endpoint must either:
- Be mapped separately (not through the auth-protected route)
- Call `ProcessAsync` directly (bypasses the HTTP pipeline)
- Use `AllowAnonymous()` on the DevTools POST endpoint

The recommended approach is option 3: map a separate `AllowAnonymous()` POST endpoint at `/v3/conversations/{conversationId}/activities` that constructs a `DefaultHttpContext` and calls `botApp.ProcessAsync(testContext)`.

### 3. Wire format for `chat.type` and `chat.name`

Core's `Conversation` class has only `Id` + extension data. The React UI expects `chat.type` and `chat.name`. Options:
- Default to `"personal"` / `"default"` (simplest, matches DevTools test injection behavior)
- Read from `Conversation.Properties` extension data (Teams may provide these)
- Both: try properties first, fall back to defaults

### 4. `BotApplication` vs `TeamsBotApplication` resolution

`UseDevTools()` needs to resolve the bot application from DI to call `UseMiddleware()`. When using `TeamsBotApplication`, it's registered as `TeamsBotApplication` singleton. The extension should resolve `BotApplication` (base type) or accept a generic:
```csharp
var botApp = endpoints.ServiceProvider.GetService<TeamsBotApplication>()
    ?? endpoints.ServiceProvider.GetRequiredService<BotApplication>();
```

Or make `UseDevTools` generic: `app.UseDevTools<TeamsBotApplication>()`.

### 5. Embedded file provider assembly reference

The `ManifestEmbeddedFileProvider` must reference the DevTools assembly (where `web/` is embedded), not the calling assembly. This is handled correctly by using `Assembly.GetExecutingAssembly()` inside the DevTools project.

---

## Sample Usage

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();
builder.Services.AddDevTools();                    // NEW — registers middleware, replaces ConversationClient

var app = builder.Build();
var teamsApp = app.UseTeamsBotApplication();
app.UseDevTools();                                 // NEW — enables WebSockets, maps endpoints, registers middleware

teamsApp.OnMessage(async (context, ct) =>
{
    // DevTools will automatically capture incoming and outgoing activities
    await context.SendActivityAsync(new CoreActivity("message") { ... }, ct);
});

app.Run();
```

---

## Implementation Sequence

1. Create project `Microsoft.Teams.Bot.DevTools.csproj` with embedded resources + dependencies
2. Copy unchanged files: `web/`, `WebSocketCollection`, `Page`, `MetaData`, `MetaDataEvent`, `WebSocketExtensions`, `DevToolsSettings`
3. Create `IDevToolsEvent` interface (simplified, no `[TrueTypeJson]`)
4. Rewrite `ActivityEvent` for `CoreActivity` with wire-format compatibility
5. Create `DevToolsService` (shared state)
6. Create `DevToolsMiddleware` (incoming + errors)
7. Create `DevToolsConversationClient` (outgoing)
8. Create `DevToolsHostingExtensions` with `AddDevTools()` + `UseDevTools()`
9. Add to solution, verify build
10. Test with a sample bot
