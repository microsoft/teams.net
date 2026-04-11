# Audit Issue 005: `BuildServiceProvider()` Called Multiple Times During Startup

**Severity:** High  
**Files:**
- `core/src/Microsoft.Teams.Bot.Core/Hosting/AddBotApplicationExtensions.cs` — line 224
- `core/src/Microsoft.Teams.Bot.Core/Hosting/BotConfig.cs` — line 148
- `core/src/Microsoft.Teams.Bot.Core/Hosting/JwtExtensions.cs` — line 324

**Category:** Memory management / DI correctness

---

## Problem

In three separate places, `services.BuildServiceProvider()` is called during the DI registration phase (i.e., inside extension methods called from `Program.cs` / `Startup.cs`):

```csharp
// AddBotApplicationExtensions.cs, line 224
using ServiceProvider tempProvider = services.BuildServiceProvider();
ILoggerFactory? tempFactory = tempProvider.GetService<ILoggerFactory>();
return (ILogger?)tempFactory?.CreateLogger(...)
    ?? NullLogger.Instance;

// BotConfig.cs, line 148
IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
    ?? services.BuildServiceProvider().GetRequiredService<IConfiguration>();

// JwtExtensions.cs, line 324
IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
    ?? services.BuildServiceProvider().GetRequiredService<IConfiguration>();
```

Each call to `BuildServiceProvider()` creates a **new, independent DI container** that:

1. Instantiates all registered singleton services eagerly (or on first resolution), potentially starting background threads, opening connections, or allocating large objects.
2. **Leaks resources** unless explicitly disposed. The `using` in `AddBotApplicationExtensions.cs` disposes the temp provider correctly, but `BotConfig.cs` and `JwtExtensions.cs` do not — the returned `ServiceProvider` is abandoned without disposal.
3. Triggers ASP.NET Core's built-in analyzer warning `ASP0000`: _"Calling 'BuildServiceProvider' from application code results in an additional copy of singleton services being created."_
4. Is called potentially 3+ times per application startup (once per `AddConversationClient`, `AddUserTokenClient`, `AddBotAuthentication`, and `AddBotAuthorization` call), each producing a separate leaked container.

---

## Root Cause

The extension methods need access to `IConfiguration` and `ILoggerFactory` at registration time, before the DI container is built. Rather than requiring the caller to pass these in explicitly, the methods try to extract them from the `IServiceCollection` directly. When that fails (e.g., when the `IConfiguration` is registered as a factory rather than an instance), they fall back to building a temporary provider.

---

## Suggested Fix Plan

### Step 1 — Extract `IConfiguration` without building a provider

`IConfiguration` is almost always registered as an `ImplementationInstance` (not a factory) in ASP.NET Core. The extraction already tries this:

```csharp
IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
    ?? services.BuildServiceProvider().GetRequiredService<IConfiguration>();
```

The fallback should **throw** instead of building a provider, with a clear error message:

```csharp
IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
    ?? throw new InvalidOperationException(
        "IConfiguration must be registered before calling AddBotApplication. " +
        "Call builder.Configuration or services.AddSingleton<IConfiguration>(...) first.");
```

In practice, `IConfiguration` is always available as an instance in modern ASP.NET Core apps (`WebApplicationBuilder` registers it that way). Removing the fallback makes any misconfiguration fail fast with a clear message.

### Step 2 — Pass `IConfiguration` as a parameter where needed

Alternatively, add an overload that accepts `IConfiguration` directly:

```csharp
public static BotConfig Resolve(IConfiguration configuration, string sectionName = "AzureAd", ILogger? logger = null)
// (This overload already exists in BotConfig.cs — use it consistently)
```

All call sites that currently call `BotConfig.Resolve(services, sectionName)` should be audited to see whether the `IConfiguration` is already available in scope, and if so, call the `Resolve(IConfiguration, ...)` overload directly.

### Step 3 — Remove `GetLoggerFromServices` / replace with `NullLogger` at registration time

The `GetLoggerFromServices` helper in `AddBotApplicationExtensions.cs` builds a temporary provider to get a logger during service registration. Logging during registration is a convenience, not a requirement. The simplest fix:

```csharp
internal static ILogger GetLoggerFromServices(IServiceCollection services, Type? categoryType = null)
{
    // Only use instance-registered loggers; never build a provider.
    ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
    ILoggerFactory? factory = descriptor?.ImplementationInstance as ILoggerFactory;
    return factory?.CreateLogger(categoryType ?? typeof(AddBotApplicationExtensions))
        ?? NullLogger.Instance;
}
```

Remove the `BuildServiceProvider()` fallback entirely. Configuration-time logging is nice-to-have; causing resource leaks or DI duplication is not acceptable.

### Step 4 — Suppress or fix the `ASP0000` analyzer warning

After the fix, verify that the `dotnet build` output no longer emits `ASP0000` warnings for these files.

---

## Acceptance Criteria

- Zero calls to `services.BuildServiceProvider()` in `AddBotApplicationExtensions.cs`, `BotConfig.cs`, and `JwtExtensions.cs`.
- No `ASP0000` analyzer warnings in the build output.
- Application startup completes successfully and bot processes requests as before.
- If `IConfiguration` is unavailable (e.g., misconfigured host), a clear `InvalidOperationException` is thrown at startup rather than a silent fallback.
