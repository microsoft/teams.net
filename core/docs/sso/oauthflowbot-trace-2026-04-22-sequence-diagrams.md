# 🔐 OAuthFlowBot — Sequence Diagrams (Popup Fallback)

Trace from 2026-04-22 03:12 UTC. Connection `teamsgraph` (Azure AD v2, no SSO configured).
Sign-in completes via **popup window** + `signin/verifyState` — no silent SSO.

---

## 🔑 Login Flow (Popup Sign-In)

```mermaid
sequenceDiagram
    actor User as 👤 Rido
    participant Teams as 🟣 Teams
    participant Bot as 🤖 Bot
    participant MSAL as 🔑 MSAL
    participant AAD as 🔵 Azure AD
    participant TBS as 🟠 Token Service
    participant BFC as 🔷 Bot Framework

    User->>Teams: Types "login graph"
    Teams->>Bot: 📥 POST /api/messages<br/>type=message, text="login graph"
    Note over Bot: 🛡️ JWT validated
    Note over Bot: 🔀 Route: message/^login graph$

    rect rgb(240, 248, 255)
        Note over Bot,TBS: Step 1 — Silent token check (miss)
        Bot->>MSAL: AcquireTokenForClient
        MSAL->>AAD: POST /oauth2/v2.0/token
        AAD-->>MSAL: 🔑 App token
        Bot->>TBS: 📤 GET /api/usertoken/GetToken<br/>connectionName=teamsgraph
        TBS-->>Bot: ❌ 404 No cached token
    end

    rect rgb(255, 248, 240)
        Note over Bot,TBS: Step 2 — Get sign-in resource
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 GET /api/botsignin/GetSignInResource<br/>state={MsAppId, ConnectionName=teamsgraph}
        TBS-->>Bot: ✅ 200 signInLink + tokenPostResource<br/>⚠️ No tokenExchangeResource (no SSO)
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 3 — Send OAuthCard (popup only)
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>🃏 OAuthCard (no tokenExchangeResource)<br/>buttons: [Sign In → popup link]
        BFC-->>Bot: ✅ 200
    end

    Bot-->>Teams: ✅ 200
    Teams->>User: Shows Sign In button

    rect rgb(255, 250, 230)
        Note over User,AAD: Step 4 — User signs in via popup
        User->>Teams: Clicks "Sign In" button
        Teams->>AAD: Opens popup → AAD login
        AAD-->>Teams: Auth code / consent
        Teams->>TBS: Posts token via SasUrl
    end

    rect rgb(245, 240, 255)
        Note over Teams,Bot: Step 5 — Teams sends verifyState invoke
        Teams->>Bot: 📥 POST /api/messages<br/>type=invoke, name=signin/verifyState<br/>value={ state: "745254" }
        Note over Bot: 🛡️ JWT validated
        Note over Bot: 🔀 Route: invoke/signin/verifyState
    end

    rect rgb(255, 245, 245)
        Note over Bot,TBS: Step 6 — Verify state and get token
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 GET /api/usertoken/GetToken<br/>connectionName=teamsgraph&code=745254
        TBS-->>Bot: ✅ 200 User token returned
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 7 — 🎉 OnSignInComplete
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>"Connected to Microsoft Graph (teamsgraph)!"
        BFC-->>Bot: ✅ 201
    end

    Bot-->>Teams: ✅ 200 invoke response
    Teams->>User: "Connected to Microsoft Graph!"
```

---

## 👤 "my ad user" Flow (token cached)

```mermaid
sequenceDiagram
    actor User as 👤 Rido
    participant Teams as 🟣 Teams
    participant Bot as 🤖 Bot
    participant MSAL as 🔑 MSAL
    participant TBS as 🟠 Token Service
    participant Graph as 📊 Graph
    participant BFC as 🔷 Bot Framework

    User->>Teams: Types "my ad user"
    Teams->>Bot: 📥 POST /api/messages<br/>type=message, text="my ad user"
    Note over Bot: 🔀 Route: message/^my ad user

    rect rgb(240, 248, 255)
        Note over Bot,TBS: Step 1 — Silent token check (hit)
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 GET /api/usertoken/GetToken<br/>connectionName=teamsgraph
        TBS-->>Bot: ✅ 200 Cached user token
    end

    rect rgb(245, 240, 255)
        Note over Bot,Graph: Step 2 — Call Graph API
        Bot->>Graph: 📤 GET /v1.0/me<br/>🔑 Bearer {user_token}
        Graph-->>Bot: ✅ 200 {displayName:"Rido", mail:"rido@teamssdk..."}
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 3 — Send profile to user
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>📄 Graph /me JSON
        BFC-->>Bot: ✅ 201
    end

    Bot-->>Teams: ✅ 200
    Teams->>User: Shows AD user JSON
```

---

## 🚪 Logout Flow

```mermaid
sequenceDiagram
    actor User as 👤 Rido
    participant Teams as 🟣 Teams
    participant Bot as 🤖 Bot
    participant MSAL as 🔑 MSAL
    participant TBS as 🟠 Token Service
    participant BFC as 🔷 Bot Framework

    User->>Teams: Types "logout graph"
    Teams->>Bot: 📥 POST /api/messages<br/>type=message, text="logout graph"
    Note over Bot: 🔀 Route: message/^logout graph$

    rect rgb(255, 240, 240)
        Note over Bot,TBS: Step 1 — Revoke user token
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 DELETE /api/usertoken/SignOut<br/>connectionName=teamsgraph
        TBS-->>Bot: ✅ 200 Token revoked
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 2 — Send confirmation
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>"Signed out from Graph."
        BFC-->>Bot: ✅ 201
    end

    Bot-->>Teams: ✅ 200
    Teams->>User: "Signed out from Graph."
```
