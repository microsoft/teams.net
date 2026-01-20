# HTTP Header Propagation Feature

## Overview
This feature provides two capabilities for HTTP header management across the Teams.NET SDK:

1. **Custom Header Propagation**: Propagate custom user-defined headers from incoming HTTP requests to outbound API calls. This enables developers to pass context, tracking IDs, or other metadata through the entire request pipeline.

2. **Assembly-Based User-Agent Generation**: Automatically generate and append User-Agent headers based on the calling assembly name and version. This improves telemetry, debugging, and call chain visibility by identifying which bot/application made each API call.

Both features must be available across all SDK layers:
- **Core** (`Microsoft.Teams.Bot.Core`)
- **Compat** (`Microsoft.Teams.Bot.Compat`)
- **BotApps** (`Microsoft.Teams.Bot.Apps`)

# 1. Custom Header Propagation

TBD

# 2. Assembly-Based User-Agent Generation

## Design Summary

Automatically generate User-Agent headers that include the full call chain from bot application through SDK layers to identify calling applications in telemetry.

**Example User-Agent:**
```
MyTeamsBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0
```

## Design Decisions

### 1. Assembly Information Capture
- **Approach:** Use `ThisAssembly.AssemblyInfo` NuGet package with Roslyn source generators
- **Benefits:**
  - Zero runtime overhead (compile-time code generation)
  - No reflection required
  - Works consistently across all hosting scenarios (ASP.NET, Azure Functions, console apps)
  - Automatically picks up version changes from project properties
- **Properties Used:**
  - `ThisAssembly.Info.Title` - Assembly/product title
  - `ThisAssembly.Info.Version` - Semantic version
- **Dependencies:** Add `ThisAssembly.AssemblyInfo` NuGet package to all SDK projects and recommend for bot projects

### 2. User-Agent Format
- **Format:** RFC 7231 standard Product/Version format
  ```
  Product1/Version1 Product2/Version2 Product3/Version3
  ```
- **Example:**
  ```
  MyTeamsBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Compat/2.0.5 Microsoft.Teams.Bot.Core/2.1.0
  ```
- **Rationale:** Industry standard, parseable, widely supported by HTTP infrastructure

### 3. Multi-Layer Call Chain Handling
- **Approach:** Include full call chain with all SDK layers
- **Construction Method:** Append at each layer
  - Core layer: `Microsoft.Teams.Bot.Core/2.1.0`
  - Compat layer appends: `Microsoft.Teams.Bot.Compat/2.0.5 Microsoft.Teams.Bot.Core/2.1.0`
  - Apps layer appends: `Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0`
  - Bot application prepends: `MyTeamsBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 ...`
- **Benefits:** Complete visibility into which SDK layers were used in each request

### 4. HTTP Pipeline Integration
- **Approach:** Set User-Agent header via existing `DefaultHeaders` property on HTTP clients
- **Location:** Configure during client initialization (e.g., `TokenServiceClient`, `ABSChannelServiceClient`)
- **Rationale:** Leverages existing infrastructure, no need for custom DelegatingHandlers

### 5. Customization & Control
- **Policy:** Always enabled, no option to disable
- **Rationale:** Consistent telemetry across all SDK users for better support and diagnostics
- **No customization options:** Ensures standardized format for parsing and analysis

### 6. Fallback Behavior
- **If ThisAssembly info unavailable:** Omit that layer from User-Agent string
- **Example:** If bot doesn't use ThisAssembly:
  ```
  Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0
  ```
- **Rationale:** Graceful degradation, always provide available information

## HTTP Client Architecture

### Client Structure
- **BotHttpClient** (Core): Foundation class that wraps HttpClient, handles all HTTP requests
- **ConversationClient** (Core): Uses BotHttpClient, named "BotConversationClient"
- **UserTokenClient** (Core): Uses BotHttpClient, named "BotUserTokenClient"
- **TeamsApiClient** (Apps): Uses BotHttpClient, named "TeamsAPXClient"
- **Compat Layer**: Wraps Core clients via adapter pattern, no direct HTTP calls

### Header Configuration Points
1. **DefaultCustomHeaders**: Dictionary on each client (ConversationClient, UserTokenClient, TeamsApiClient)
2. **BotRequestOptions**: Contains DefaultHeaders and CustomHeaders passed to BotHttpClient.SendAsync()
3. **BotAuthenticationHandler**: DelegatingHandler in HttpClient pipeline, adds Authorization header
4. **HttpClient.DefaultRequestHeaders**: Set during client registration in AddBotApplicationExtensions

### Call Chain Flow
```
Bot Application
    ↓ (calls)
TeamsApiClient (Apps layer) → BotHttpClient → HttpClient (TeamsAPXClient)
    OR
ConversationClient (Core layer) → BotHttpClient → HttpClient (BotConversationClient)
    OR
UserTokenClient (Core layer) → BotHttpClient → HttpClient (BotUserTokenClient)

Compat Layer wraps Core clients:
CompatConversations → ConversationClient → BotHttpClient → HttpClient
CompatUserTokenClient → UserTokenClient → BotHttpClient → HttpClient
```

## Implementation Options

### Option 1: Set at HttpClient Registration (Static)
**Location**: `AddBotApplicationExtensions.cs` and `TeamsBotApplication.HostingExtensions.cs`

**Approach**: Configure User-Agent when registering named HttpClients:
```csharp
services.AddHttpClient<ConversationClient>("BotConversationClient")
    .ConfigureHttpClient(client => {
        client.DefaultRequestHeaders.Add("User-Agent",
            $"{ThisAssembly.Info.Title}/{ThisAssembly.Info.Version}");
    })
    .AddHttpMessageHandler(...);
```

**Pros**:
- Simple, centralized configuration
- Set once at startup, no runtime overhead
- Each layer automatically includes its version

**Cons**:
- Can't build full call chain (Core doesn't know if Apps/Compat is above it)
- Static - can't adapt to calling context
- Only shows the actual HTTP client layer, not intermediate layers

**Result Example**: `Microsoft.Teams.Bot.Core/2.1.0` (only Core, missing Apps/Bot info)

---

### Option 2: Each Client Sets DefaultCustomHeaders (Static per Client)
**Location**: Client constructors (ConversationClient, UserTokenClient, TeamsApiClient)

**Approach**: Each client adds User-Agent to its `DefaultCustomHeaders` dictionary:
```csharp
public class ConversationClient
{
    public ConversationClient(...)
    {
        DefaultCustomHeaders["User-Agent"] =
            $"{ThisAssembly.Info.Title}/{ThisAssembly.Info.Version}";
    }
}
```

**Pros**:
- Each client self-describes
- Uses existing DefaultCustomHeaders infrastructure
- Simple to implement per client

**Cons**:
- Last client wins (headers overwrite each other in BotHttpClient)
- Can't build additive call chain
- Compat layer adapters don't have their own HTTP clients

**Result Example**: `Microsoft.Teams.Bot.Core/2.1.0` (only the final client, not the chain)

---

### Option 3: Append in BotHttpClient.SendAsync() (Dynamic Detection)
**Location**: `BotHttpClient.CreateRequest()` method

**Approach**: Inspect calling assembly via StackTrace and build User-Agent dynamically:
```csharp
private HttpRequestMessage CreateRequest(...)
{
    var request = new HttpRequestMessage(method, relativeUri);

    // Detect calling client/layer from stack trace
    var userAgent = BuildUserAgentFromStack();
    request.Headers.Add("User-Agent", userAgent);

    // Apply DefaultHeaders and CustomHeaders...
}
```

**Pros**:
- Centralized in one location
- Can detect full call chain dynamically
- No changes needed to individual clients

**Cons**:
- Runtime overhead from stack inspection
- Fragile (depends on call stack depth/patterns)
- May be affected by compiler optimizations (inlining)
- Rejected in design phase (we chose ThisAssembly over StackTrace)

**Result Example**: `MyBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0`

---

### Option 4: Pass User-Agent Through BotRequestOptions (Explicit Chain)
**Location**: `BotRequestOptions` and client `CreateRequestOptions()` methods

**Approach**: Add `UserAgent` property to BotRequestOptions and each layer appends:
```csharp
// BotRequestOptions.cs
public record BotRequestOptions
{
    public string? UserAgent { get; init; }
    // ...existing properties...
}

// TeamsApiClient.cs
private BotRequestOptions CreateRequestOptions(..., string? parentUserAgent = null)
{
    var myUserAgent = $"{ThisAssembly.Info.Title}/{ThisAssembly.Info.Version}";
    var fullUserAgent = string.IsNullOrEmpty(parentUserAgent)
        ? myUserAgent
        : $"{myUserAgent} {parentUserAgent}";

    return new()
    {
        UserAgent = fullUserAgent,
        DefaultHeaders = DefaultCustomHeaders,
        // ...
    };
}
```

**Pros**:
- Explicit call chain building
- Each layer appends its info
- No runtime inspection needed
- Full control over ordering

**Cons**:
- Requires API changes to all client methods (add parentUserAgent parameter)
- Bot applications must explicitly pass their info
- Breaking change to public APIs
- Complex threading through multiple layers

**Result Example**: `MyBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0`

---

### Option 5: Middleware/Handler in HttpClient Pipeline (Hybrid)
**Location**: New `UserAgentHandler` (DelegatingHandler)

**Approach**: Create a new message handler that reads from HttpRequestMessage.Options:
```csharp
public class UserAgentHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...)
    {
        // Read layer info from request options
        if (request.Options.TryGetValue(UserAgentKey, out string? layerInfo))
        {
            // Append to existing User-Agent or create new
            var currentUA = request.Headers.UserAgent.ToString();
            var newUA = string.IsNullOrEmpty(currentUA)
                ? layerInfo
                : $"{layerInfo} {currentUA}";
            request.Headers.UserAgent.ParseAdd(newUA);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

// Each client sets its layer info in request options
request.Options.Set(UserAgentKey, $"{ThisAssembly.Info.Title}/{ThisAssembly.Info.Version}");
```

**Pros**:
- Uses existing HttpClient pipeline infrastructure
- Each layer's handler adds its info
- Similar to how BotAuthenticationHandler works
- Can chain multiple handlers for each layer

**Cons**:
- Need to register handlers for each layer
- Handler order matters (Core handler runs last, adds last)
- Still need clients to set their layer info in options
- Complex setup in dependency injection

**Result Example**: `MyBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0`

---

### Option 6: Ambient Context Service (Scoped Service)
**Location**: New `UserAgentService` with scoped lifetime

**Approach**: Create a scoped service that accumulates User-Agent info:
```csharp
public interface IUserAgentService
{
    void AppendLayer(string title, string version);
    string GetUserAgent();
}

// Each client constructor injects and appends
public class TeamsApiClient
{
    public TeamsApiClient(IUserAgentService uaService, ...)
    {
        uaService.AppendLayer(ThisAssembly.Info.Title, ThisAssembly.Info.Version);
    }
}

// BotHttpClient reads final value
private HttpRequestMessage CreateRequest(...)
{
    var userAgent = _uaService.GetUserAgent();
    request.Headers.Add("User-Agent", userAgent);
}
```

**Pros**:
- Clean separation of concerns
- Testable service
- Easy to extend with custom bot info

**Cons**:
- Clients are singletons, service must be scoped correctly
- Order of construction matters (may be unpredictable)
- Bot application layer may not have chance to append first
- Service lifetime management complexity

**Result Example**: Depends on construction order (may be unreliable)

---

## Recommended Implementation: Hybrid Approach

Combine aspects of **Option 1** (static per-layer) and **Option 4** (explicit passing):

### Phase 1: SDK Layer User-Agents (Always Present)
1. Set User-Agent at HttpClient registration for each SDK layer:
   - Core: `Microsoft.Teams.Bot.Core/2.1.0`
   - Apps: `Microsoft.Teams.Bot.Apps/2.1.0`
2. Each layer's HttpClient gets its identity via `DefaultRequestHeaders.UserAgent`

### Phase 2: Call Chain Building
1. Add optional `UserAgent` property to `BotRequestOptions`
2. In `BotHttpClient.CreateRequest()`:
   - Read existing User-Agent from HttpClient.DefaultRequestHeaders
   - Prepend any User-Agent from BotRequestOptions (if provided)
   - Result: `{BotUA} {LayerUA}`
3. Bot applications can optionally provide their info via request options

**Benefits**:
- SDK layers always identify themselves (no bot code needed)
- Bot applications can opt-in to prepend their info
- Backwards compatible (no breaking changes)
- Works with or without ThisAssembly in bot projects
- Minimal code changes required

**Result Examples**:
- Without bot info: `Microsoft.Teams.Bot.Apps/2.1.0`
- With bot info: `MyBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0`
- Full chain: `MyBot/1.0.0 Microsoft.Teams.Bot.Apps/2.1.0 Microsoft.Teams.Bot.Core/2.1.0`

---

## Implementation Checklist

- [ ] Add `ThisAssembly.AssemblyInfo` NuGet package to:
  - [ ] Microsoft.Teams.Bot.Core
  - [ ] Microsoft.Teams.Bot.Compat
  - [ ] Microsoft.Teams.Bot.Apps
- [ ] Implement User-Agent builder utility
  - [ ] Access `ThisAssembly.Info.Title` and `ThisAssembly.Info.Version`
  - [ ] Handle missing/null values gracefully
  - [ ] Format as RFC 7231 Product/Version pairs
- [ ] Integrate User-Agent into HTTP clients
  - [ ] Core: Set User-Agent at HttpClient registration (AddBotApplicationExtensions)
  - [ ] Apps: Set User-Agent at HttpClient registration (TeamsBotApplication.HostingExtensions)
  - [ ] Add optional UserAgent property to BotRequestOptions
  - [ ] Modify BotHttpClient.CreateRequest() to prepend custom UA from options
- [ ] Update documentation
  - [ ] Recommend bot projects use ThisAssembly for complete chain
  - [ ] Document User-Agent format and examples
  - [ ] Add troubleshooting guide for telemetry scenarios
- [ ] Testing
  - [ ] Unit tests for User-Agent builder
  - [ ] Integration tests for full call chain
  - [ ] Test with and without ThisAssembly in bot project
  - [ ] Verify format compliance with RFC 7231
