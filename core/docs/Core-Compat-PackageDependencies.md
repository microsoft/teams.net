# Package Dependencies Design Document

This document describes the package dependency changes introduced in the `next/core-decouple-fe` PR within the `core/` SDK. The key change is decoupling `Bot.Compat` from `Bot.Apps` so that both depend directly on `Bot.Core` as independent siblings.

---

## Before: Linear Dependency Chain

Prior to this PR, `Bot.Compat` depended on `Bot.Apps`, which in turn depended on `Bot.Core`. This created a **linear chain** where Compat transitively pulled in everything from Apps.

```mermaid
graph BT
    Core["Microsoft.Teams.Bot.Core<br/><i>net8.0 · net10.0</i><br/>Foundation Layer"]
    Apps["Microsoft.Teams.Bot.Apps<br/><i>net8.0 · net10.0</i><br/>Teams Features Layer"]
    Compat["Microsoft.Teams.Bot.Compat<br/><i>net8.0 · net10.0</i><br/>Compatibility Layer"]

    Apps -->|"ProjectReference"| Core
    Compat -->|"ProjectReference"| Apps

    style Core fill:#e1f5ff,stroke:#0d6efd
    style Apps fill:#fff4e1,stroke:#fd7e14
    style Compat fill:#ffe1f5,stroke:#d63384
```

### Problems with this structure

- **Unnecessary coupling**: `Bot.Compat` only needs `Bot.Core` (activity model, conversation client, hosting), but was forced to take a dependency on the entire `Bot.Apps` layer (Teams-specific handlers, routing, streaming, Teams API client).
- **Larger transitive closure**: Any consumer of `Bot.Compat` also pulled in `Bot.Apps` as a transitive dependency, even if they never used Teams-specific features.
- **Breaking change risk**: Changes to `Bot.Apps` could break `Bot.Compat` consumers even when the Compat layer only used Core types.
- **InternalsVisibleTo gap**: `Bot.Core` only exposed internals to `Bot.Apps`, so `Bot.Compat` had to go through Apps to access Core internals.

---

## After: Sibling Architecture

This PR changes `Bot.Compat` to reference `Bot.Core` directly instead of `Bot.Apps`. Both `Apps` and `Compat` are now **independent siblings** that share only the `Core` foundation.

```mermaid
graph BT
    Core["Microsoft.Teams.Bot.Core<br/><i>net8.0 · net10.0</i><br/>Foundation Layer"]
    Apps["Microsoft.Teams.Bot.Apps<br/><i>net8.0 · net10.0</i><br/>Teams Features Layer"]
    Compat["Microsoft.Teams.Bot.Compat<br/><i>net8.0 · net10.0</i><br/>Compatibility Layer"]

    Apps -->|"ProjectReference"| Core
    Compat -->|"ProjectReference"| Core

    style Core fill:#e1f5ff,stroke:#0d6efd
    style Apps fill:#fff4e1,stroke:#fd7e14
    style Compat fill:#ffe1f5,stroke:#d63384
```

---

## Side-by-Side Comparison

```mermaid
graph TB
    subgraph "Before"
        direction BT
        B_Core["Bot.Core"]
        B_Apps["Bot.Apps"]
        B_Compat["Bot.Compat"]
        B_Apps -->|"ProjectReference"| B_Core
        B_Compat -->|"ProjectReference"| B_Apps
    end

    subgraph "After"
        direction BT
        A_Core["Bot.Core"]
        A_Apps["Bot.Apps"]
        A_Compat["Bot.Compat"]
        A_Apps -->|"ProjectReference"| A_Core
        A_Compat -->|"ProjectReference"| A_Core
    end

    style B_Core fill:#e1f5ff
    style B_Apps fill:#fff4e1
    style B_Compat fill:#ffe1f5
    style A_Core fill:#e1f5ff
    style A_Apps fill:#fff4e1
    style A_Compat fill:#ffe1f5
```

| Metric | Before | After |
|--------|--------|-------|
| Dependency depth from Compat | 3 (Compat → Apps → Core) | 2 (Compat → Core) |
| Compat's transitive project refs | 2 (Apps + Core) | 1 (Core) |
| Packages coupled to Bot.Apps | Apps + Compat | Apps only |
| Core InternalsVisibleTo | Apps, Core.UnitTests | Apps, **Compat**, Core.UnitTests |

---

## What Changed

### 1. `Bot.Compat.csproj` — dependency target changed

```diff
  <ItemGroup>
-   <ProjectReference Include="..\Microsoft.Teams.Bot.Apps\Microsoft.Teams.Bot.Apps.csproj" />
+   <ProjectReference Include="..\Microsoft.Teams.Bot.Core\Microsoft.Teams.Bot.Core.csproj" />
  </ItemGroup>
```

### 2. `Bot.Core.csproj` — InternalsVisibleTo added for Compat

```diff
  <ItemGroup>
      <InternalsVisibleTo Include="Microsoft.Teams.Bot.Core.UnitTests" />
      <InternalsVisibleTo Include="Microsoft.Teams.Bot.Apps" />
+     <InternalsVisibleTo Include="Microsoft.Teams.Bot.Compat" />
  </ItemGroup>
```

### 3. Compat source code — rewritten to use Core types directly

Types in `Bot.Compat` (e.g., `CompatActivity`, `CompatTeamsInfo`, `CompatHostingExtensions`) were updated to import from `Microsoft.Teams.Bot.Core` namespaces instead of going through `Microsoft.Teams.Bot.Apps`.

---

## InternalsVisibleTo Relationships

Before, only `Bot.Apps` could access Core internals. Now both sibling packages can.

```mermaid
graph LR
    Core["Bot.Core"]
    Apps["Bot.Apps"]
    Compat["Bot.Compat"]
    CoreTests["Bot.Core.UnitTests"]
    AppsTests["Bot.Apps.UnitTests"]

    Core -.->|"InternalsVisibleTo"| Apps
    Core -.->|"InternalsVisibleTo<br/>(new)"| Compat
    Core -.->|"InternalsVisibleTo"| CoreTests

    Apps -.->|"InternalsVisibleTo"| AppsTests

    style Core fill:#e1f5ff
    style Apps fill:#fff4e1
    style Compat fill:#ffe1f5
    style CoreTests fill:#f0f0f0
    style AppsTests fill:#f0f0f0
```

---

## NuGet Dependencies Per Layer

The external NuGet dependency layout is unchanged — but the transitive impact is different:

```mermaid
graph TD
    subgraph "Bot.Core"
        C1["AspNetCore.Authentication.JwtBearer"]
        C2["AspNetCore.Authentication.OpenIdConnect"]
        C3["System.Security.Cryptography.Pkcs"]
        C4["Microsoft.Identity.Web.UI"]
        C5["Microsoft.Identity.Web.AgentIdentities"]
    end

    subgraph "Bot.Apps"
        A1["(no external NuGet packages)"]
    end

    subgraph "Bot.Compat"
        X1["Microsoft.Bot.Builder.Integration<br/>.AspNet.Core 4.22.3"]
    end

    style C1 fill:#e1f5ff
    style C2 fill:#e1f5ff
    style C3 fill:#e1f5ff
    style C4 fill:#e1f5ff
    style C5 fill:#e1f5ff
    style A1 fill:#fff4e1
    style X1 fill:#ffe1f5
```

**Before**: A `Bot.Compat` consumer transitively received all NuGet packages from Core **plus** the entire `Bot.Apps` assembly.

**After**: A `Bot.Compat` consumer only receives Core's NuGet packages. `Bot.Apps` is no longer in the transitive closure.

---

## Sample Application Dependency Patterns

Samples demonstrate three independent entry points:

```mermaid
graph BT
    Core["Bot.Core"]
    Apps["Bot.Apps"]
    Compat["Bot.Compat"]

    CoreBot["CoreBot<br/><i>net10.0</i>"]
    TeamsBot["TeamsBot<br/><i>net10.0</i>"]
    CompatBot["CompatBot<br/><i>net8.0</i>"]

    CoreBot --> Core
    TeamsBot --> Apps
    CompatBot --> Compat

    Apps --> Core
    Compat --> Core

    style Core fill:#e1f5ff,stroke:#0d6efd
    style Apps fill:#fff4e1,stroke:#fd7e14
    style Compat fill:#ffe1f5,stroke:#d63384
    style CoreBot fill:#f0f0f0
    style TeamsBot fill:#f0f0f0
    style CompatBot fill:#f0f0f0
```

| Entry Point | When to Use |
|-------------|-------------|
| **Bot.Core** directly | Minimal bots needing only core activity handling, middleware, and conversation client |
| **Bot.Apps** | Teams-specific bots with typed handlers, routing, streaming, Teams API client |
| **Bot.Compat** | Migrating existing Bot Framework v4 bots — no longer pulls in Bot.Apps transitively |

---

## Design Rationale

1. **Decoupled Compat from Apps**: `Bot.Compat` only needs Core primitives (activities, conversation client, hosting). Removing the Apps dependency eliminates unnecessary coupling.
2. **Smaller transitive closure**: Consumers of `Bot.Compat` no longer pull in the entire Teams-specific layer (`Bot.Apps`) as a transitive dependency.
3. **Independent evolution**: `Bot.Apps` and `Bot.Compat` can now be versioned and modified independently without risk of cross-impact.
4. **Direct internal access**: Adding `InternalsVisibleTo` for Compat on Core removes the need to route through Apps to access shared infrastructure like `BotHttpClient` and serialization contexts.
5. **Clearer architecture**: The sibling pattern makes the SDK's layering explicit — Core is the shared foundation, Apps adds Teams features, Compat bridges to Bot Framework v4.
