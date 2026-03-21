# DevTools Architecture on `main`

How the DevTools plugin works in the current `main` branch of the .NET SDK.

## Project

```
Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore.DevTools/
```

Package: `Microsoft.Teams.Plugins.AspNetCore.DevTools`
Target: `net8.0`
Embedded UI: React/TypeScript app in `web/` folder, served via `ManifestEmbeddedFileProvider`

### Dependencies

- `Microsoft.Teams.Plugins.AspNetCore` (plugin host)
- `Microsoft.Teams.Apps` (app model, plugin interfaces)
- `Microsoft.Teams.Api` (activity types)
- `Microsoft.Teams.Common` (logging, JSON utilities)
- `Microsoft.Teams.Extensions.Hosting`
- `Microsoft.Extensions.FileProviders.Embedded` (9.0.0)
- `System.IdentityModel.Tokens.Jwt` (8.8.0)

---

## Plugin Class — `DevToolsPlugin.cs`

Implements `IAspNetCorePlugin`, decorated with `[Plugin]`.

```csharp
[Plugin]
public class DevToolsPlugin : IAspNetCorePlugin
{
    [Dependency] public ILogger Logger { get; set; }
    [Dependency("AppId", optional: true)] public string? AppId { get; set; }
    [Dependency("AppName", optional: true)] public string? AppName { get; set; }

    public event EventFunction Events;

    internal MetaData MetaData => new() { Id = AppId, Name = AppName, Pages = _pages };
    internal readonly WebSocketCollection Sockets = [];

    private readonly ISenderPlugin _sender;
    private readonly IServiceProvider _services;
    private readonly IList<Page> _pages = [];
    private readonly TeamsDevToolsSettings _settings;

    public DevToolsPlugin(AspNetCorePlugin sender, IServiceProvider provider) { ... }
```

### Lifecycle Methods

| Method | What it does |
|--------|-------------|
| `Configure(IApplicationBuilder)` | Enables WebSockets (`AllowedOrigins = { "*" }`), serves embedded static files at `/devtools` path, adds error-logging middleware |
| `OnInit(App)` | Loads custom pages from `TeamsDevToolsSettings`, logs security warning |
| `OnStart(App)` | Resolves `IServer` addresses, logs `Available at {address}/devtools` for each |
| `OnActivity(App, ISenderPlugin, ActivityEvent)` | Emits `ActivityEvent.Received(activity, conversation)` to all WebSocket clients |
| `OnActivitySent(App, ISenderPlugin, ActivitySentEvent)` | Emits `ActivityEvent.Sent(activity, conversation)` to all WebSocket clients |
| `OnActivityResponse(...)` | No-op (logs debug) |
| `OnError(...)` | No-op (logs debug) |
| `Do(ActivityEvent)` | Delegates to `AspNetCorePlugin` sender — used by `ActivityController` for test injection |

---

## Controllers

### `DevToolsController.cs` — UI + WebSocket

```csharp
[ApiController]
public class DevToolsController : ControllerBase
{
    private readonly DevToolsPlugin _plugin;
    private readonly IFileProvider _files;
    private readonly IHostApplicationLifetime _lifetime;

    public DevToolsController(DevToolsPlugin plugin, IHostApplicationLifetime lifetime) { ... }
```

**Endpoints:**

| Route | Method | Behavior |
|-------|--------|----------|
| `GET /devtools` | `Get(null)` | Serves `index.html` from embedded files |
| `GET /devtools/{*path}` | `Get(path)` | Serves requested file; falls back to `index.html` (SPA routing) |
| `GET /devtools/sockets` | `GetSocket()` | WebSocket upgrade → adds to `Sockets` collection → sends `MetaDataEvent` → loops until close |

**WebSocket lifecycle:**
1. Accept WebSocket connection
2. Assign GUID id, add to `_plugin.Sockets`
3. Send `MetaDataEvent` with app id, name, and custom pages
4. Block on `socket.ReceiveAsync()` until socket closes
5. Remove from `_plugin.Sockets` on disconnect

### `ActivityController.cs` — Test Activity Injection

```csharp
[ApiController]
[Obsolete("Use Minimal APIs instead.")]
public class ActivityController : ControllerBase
{
    private readonly DevToolsPlugin _plugin;
    private readonly SecurityKey _securityKey;
```

**Endpoint:** `POST /v3/conversations/{conversationId}/activities`

**Logic:**
1. Check for `x-teams-devtools: true` header
2. If **not** from DevTools client: return `201` with `{ id }` (passthrough for outgoing activities from `ConversationClient`)
3. If **from** DevTools client:
   - Set `from` to `{ id: "devtools", name: "devtools", role: "user" }`
   - Set `conversation` to `{ id: conversationId, type: "personal", name: "default" }`
   - Set `recipient` to `{ id: appId, name: appName, role: "bot" }`
   - Deserialize to `Activity`
   - Create fake JWT with `serviceurl` claim pointing at localhost
   - Call `_plugin.Do(activityEvent)` — runs through the full sender pipeline
4. Return `201` with `{ id }`

---

## Event System

### `IEvent` Interface (`Event.cs`)

```csharp
[TrueTypeJson<IEvent>]
public interface IEvent
{
    public Guid Id { get; }
    public string Type { get; }
    public object? Body { get; }
    public DateTime SentAt { get; }
}
```

The `[TrueTypeJson]` attribute enables polymorphic JSON serialization — the serializer writes the concrete type's properties, not just the interface.

### `ActivityEvent.cs`

```csharp
public class ActivityEvent : IEvent
{
    [JsonPropertyName("id")]     public Guid Id { get; }
    [JsonPropertyName("type")]   public string Type { get; }
    [JsonPropertyName("body")]   public object? Body { get; }
    [JsonPropertyName("chat")]   public Conversation Chat { get; set; }
    [JsonPropertyName("error")]  public object? Error { get; set; }
    [JsonPropertyName("sentAt")] public DateTime SentAt { get; }

    public ActivityEvent(string type, IActivity body, Conversation chat)
    {
        Id = Guid.NewGuid();
        Type = $"activity.{type}";   // → "activity.received", "activity.sent", "activity.error"
        Body = body;
        Chat = chat;
        SentAt = DateTime.Now;
    }

    public static ActivityEvent Received(IActivity body, Conversation chat) => new("received", body, chat);
    public static ActivityEvent Sent(IActivity body, Conversation chat) => new("sent", body, chat);
    public static ActivityEvent Err(IActivity body, Conversation chat, object error) => new("error", body, chat) { Error = error };
}
```

### `MetaDataEvent.cs`

```csharp
public class MetaDataEvent : IEvent
{
    [JsonPropertyName("id")]     public Guid Id { get; }
    [JsonPropertyName("type")]   public string Type { get; }       // always "metadata"
    [JsonPropertyName("body")]   public object? Body { get; }      // MetaData object
    [JsonPropertyName("sentAt")] public DateTime SentAt { get; }

    public MetaDataEvent(MetaData body) { ... }
}
```

---

## Wire Format (Critical for React UI)

The embedded React app expects these exact JSON shapes over WebSocket:

### Activity events

```json
{
  "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "type": "activity.received",
  "body": { /* full Activity object */ },
  "chat": {
    "id": "conversation-id",
    "type": "personal",
    "name": "default"
  },
  "sentAt": "2026-03-18T10:30:00"
}
```

`type` values: `"activity.received"`, `"activity.sent"`, `"activity.error"`

Error events additionally include:
```json
{
  "error": { /* error object */ }
}
```

### Metadata events

```json
{
  "id": "guid",
  "type": "metadata",
  "body": {
    "id": "app-id",
    "name": "app-name",
    "pages": [
      { "icon": "...", "name": "...", "displayName": "...", "url": "..." }
    ]
  },
  "sentAt": "2026-03-18T10:30:00"
}
```

### Key detail: `chat` property

The `chat` property in `ActivityEvent` maps to `Microsoft.Teams.Api.Conversation` on main, which has properties `Id`, `Type` (enum: `Personal`, `Group`, `Channel`), and `Name`. The React UI reads `chat.id`, `chat.type`, and `chat.name`.

---

## WebSocket Management — `WebSocketCollection.cs`

```csharp
public class WebSocketCollection : IEnumerable<KeyValuePair<string, WebSocket>>
{
    protected IDictionary<string, WebSocket> _store = new Dictionary<string, WebSocket>();

    public WebSocket? Get(string key) { ... }
    public WebSocketCollection Add(string key, WebSocket value) { ... }
    public WebSocketCollection Remove(params string[] keys) { ... }

    // Broadcast to ALL connected clients
    public async Task Emit(IEvent @event, CancellationToken ct)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var buffer = new ArraySegment<byte>(payload, 0, payload.Length);
        foreach (var socket in _store.Values)
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
    }

    // Send to a SINGLE client by id
    public async Task Emit(string key, IEvent @event, CancellationToken ct) { ... }
}
```

---

## Models

### `MetaData.cs`

```csharp
public class MetaData
{
    [JsonPropertyName("id")]    public string? Id { get; set; }
    [JsonPropertyName("name")]  public string? Name { get; set; }
    [JsonPropertyName("pages")] public IList<Page> Pages { get; set; } = [];
}
```

### `Page.cs`

```csharp
public class Page
{
    [JsonPropertyName("icon")]        public string? Icon { get; set; }
    [JsonPropertyName("name")]        public required string Name { get; set; }
    [JsonPropertyName("displayName")] public required string DisplayName { get; set; }
    [JsonPropertyName("url")]         public required string Url { get; set; }
}
```

---

## Registration — `HostApplicationBuilder.cs`

```csharp
public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddTeamsDevTools(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetTeamsDevTools());
        builder.Services.AddTeamsPlugin<DevToolsPlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }
}
```

Configuration binding via `ConfigurationManager.cs`:
```csharp
public static TeamsDevToolsSettings GetTeamsDevTools(this IConfigurationManager manager)
{
    return manager.GetSection("Teams").GetSection("Plugins.DevTools").Get<TeamsDevToolsSettings>() ?? new();
}
```

Settings POCO:
```csharp
public class TeamsDevToolsSettings
{
    public IList<Page> Pages { get; set; } = [];
}
```

---

## Extension Helpers

### `WebSocket.cs`

```csharp
public static class WebSocketExtensions
{
    public static bool IsCloseable(this WebSocket socket)
    {
        return socket.State != WebSocketState.Closed &&
               socket.State != WebSocketState.Aborted;
    }
}
```

---

## Usage (main branch)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
builder.AddTeamsDevTools();  // registers plugin + settings + MVC controllers

var app = builder.Build();
app.MapControllers();        // needed for DevTools MVC controllers
// ... plugin lifecycle handled automatically by Teams app
```

---

## File Inventory

| File | Type | SDK Dependencies |
|------|------|-----------------|
| `DevToolsPlugin.cs` | Core plugin | `IAspNetCorePlugin`, `ISenderPlugin`, `[Plugin]`, `[Dependency]`, `App` |
| `Controllers/DevToolsController.cs` | MVC controller | `DevToolsPlugin` (injected) |
| `Controllers/ActivityController.cs` | MVC controller | `DevToolsPlugin`, `Activity`, `Conversation`, `Account`, `Role` |
| `Events/ActivityEvent.cs` | Event DTO | `IActivity`, `Conversation` (from `Microsoft.Teams.Api`) |
| `Events/MetaDataEvent.cs` | Event DTO | None (uses `MetaData` model) |
| `Event.cs` (IEvent) | Interface | `[TrueTypeJson]` from `Microsoft.Teams.Common` |
| `WebSocketCollection.cs` | Infrastructure | `IEvent` interface only |
| `Models/Page.cs` | POCO | None |
| `Models/MetaData.cs` | POCO | `Page` |
| `Extensions/HostApplicationBuilder.cs` | DI registration | `AddTeamsPlugin<T>()` from `Microsoft.Teams.Apps.Extensions` |
| `Extensions/ConfigurationManager.cs` | Config binding | None |
| `Extensions/WebSocket.cs` | Helper | None |
| `TeamsDevToolsSettings.cs` | Config POCO | `Page` |
| `Microsoft.Teams.Plugins.AspNetCore.DevTools.csproj` | Project file | References 4 SDK projects |
| `web/` | Embedded React UI | None (framework-agnostic) |
