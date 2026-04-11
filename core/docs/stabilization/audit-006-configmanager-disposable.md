# Audit Issue 006: `ConfigurationManager<OpenIdConnectConfiguration>` (IDisposable) Never Disposed

**Severity:** High  
**File:** `core/src/Microsoft.Teams.Bot.Core/Hosting/JwtExtensions.cs`  
**Lines:** 192–224  
**Category:** Memory management / resource leak

---

## Problem

Inside `AddTeamsJwtBearer`, a `ConcurrentDictionary` is used to cache one `ConfigurationManager<OpenIdConnectConfiguration>` instance per OIDC authority:

```csharp
// JwtExtensions.cs, lines 192-224
ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> configManagerCache = new(StringComparer.OrdinalIgnoreCase);

builder.AddJwtBearer(schemeName, jwtOptions =>
{
    // ...
    IssuerSigningKeyResolver = (_, securityToken, _, _) =>
    {
        // ...
        ConfigurationManager<OpenIdConnectConfiguration> manager = configManagerCache.GetOrAdd(authority, a =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                a,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = jwtOptions.RequireHttpsMetadata }));
        // ...
    }
});
```

`ConfigurationManager<T>` implements `IDisposable`. The instances are stored in `configManagerCache`, but:

1. `configManagerCache` is a **local variable** in `AddTeamsJwtBearer`. It is captured by the lambda closure and will remain alive for the lifetime of the application — but there is no registered disposal path. When the application shuts down (or if the authentication scheme is reconfigured), the `ConfigurationManager<T>` instances are never disposed.

2. `ConfigurationManager<T>` holds an internal `HttpDocumentRetriever` which wraps an `HttpClient`, and has a background refresh timer. Neither will be cleaned up without `Dispose()`.

3. `HttpDocumentRetriever` is also instantiated directly (`new HttpDocumentRetriever {...}`) and passed to `ConfigurationManager<T>`. If the `ConfigurationManager<T>` constructor throws after accepting the retriever but before taking ownership, the retriever leaks.

---

## Root Cause

The cache is a local closure variable with no hook into the application's lifetime events. The pattern is common but incorrect — `IDisposable` types cached for application lifetime need to be tied to a disposable container or to `IHostApplicationLifetime.ApplicationStopped`.

---

## Suggested Fix Plan

### Step 1 — Move the cache to a registered singleton service

Create a small class that owns the cache and implements `IDisposable`:

```csharp
internal sealed class OidcConfigCache : IDisposable
{
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>>
        _cache = new(StringComparer.OrdinalIgnoreCase);

    public ConfigurationManager<OpenIdConnectConfiguration> GetOrAdd(
        string authority,
        bool requireHttps)
        => _cache.GetOrAdd(authority, a =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                a,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = requireHttps }));

    public void Dispose()
    {
        foreach (ConfigurationManager<OpenIdConnectConfiguration> mgr in _cache.Values)
            mgr.Dispose();
        _cache.Clear();
    }
}
```

Register it as a singleton:

```csharp
services.AddSingleton<OidcConfigCache>();
```

Resolve it inside the JWT options configuration via `IServiceProvider` (available through `jwtOptions` or by resolving from the service provider passed to `AddJwtBearer`'s `configure` callback).

### Step 2 — Alternatively, use `IOptions<JwtBearerOptions>` post-configuration

Use `services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, OidcKeyResolverPostConfigure>()` to inject `OidcConfigCache` (registered singleton) and configure the `IssuerSigningKeyResolver` from there. The singleton is disposed automatically when the DI container is disposed.

### Step 3 — Dispose `HttpDocumentRetriever` explicitly on construction failure

Wrap the `ConfigurationManager<T>` construction to ensure the retriever is not leaked if construction fails:

```csharp
HttpDocumentRetriever retriever = new() { RequireHttps = jwtOptions.RequireHttpsMetadata };
try
{
    return new ConfigurationManager<OpenIdConnectConfiguration>(
        authority,
        new OpenIdConnectConfigurationRetriever(),
        retriever);
}
catch
{
    // ConfigurationManager<T> did not take ownership.
    // HttpDocumentRetriever is not IDisposable in current Microsoft.IdentityModel versions,
    // but document this assumption with a comment for future-proofing.
    throw;
}
```

---

## Acceptance Criteria

- `ConfigurationManager<OpenIdConnectConfiguration>` instances are stored in a DI-registered singleton that is disposed when the application stops.
- No `IDisposable` instances are abandoned in closure-captured local variables.
- Running the application in a test host and stopping it cleanly produces no finalizer warnings or resource-leak diagnostics.
