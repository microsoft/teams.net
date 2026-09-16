# 🔐 SsoBot — Sequence Diagrams (Silent SSO)

Trace from 2026-04-22 02:45 UTC. Connection `sso` (Azure AD v2 with SSO).
Sign-in completes via silent `signin/tokenExchange` — no popup needed.

---

## 🔑 Login Flow

```mermaid
sequenceDiagram
    actor User as 👤 Rido
    participant Teams as 🟣 Teams
    participant Bot as 🤖 Bot
    participant MSAL as 🔑 MSAL
    participant AAD as 🔵 Azure AD
    participant TBS as 🟠 Token Service
    participant BFC as 🔷 Bot Framework

    User->>Teams: Types "login"
    Teams->>Bot: 📥 POST /api/messages<br/>type=message, text="login"
    Note over Bot: 🛡️ JWT validated
    Note over Bot: 🔀 Route: message/^login$

    rect rgb(240, 248, 255)
        Note over Bot,TBS: Step 1 — Silent token check (miss)
        Bot->>MSAL: AcquireTokenForClient
        MSAL->>AAD: GET /common/discovery/instance
        AAD-->>MSAL: Instance metadata
        MSAL->>AAD: POST /oauth2/v2.0/token
        AAD-->>MSAL: 🔑 App token (⏱️ 535ms)
        Bot->>TBS: 📤 GET /api/usertoken/GetToken<br/>connectionName=sso
        TBS-->>Bot: ❌ 404 No cached token
    end

    rect rgb(255, 248, 240)
        Note over Bot,TBS: Step 2 — Get sign-in resource
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 GET /api/botsignin/GetSignInResource<br/>state={MsAppId, ConnectionName, Conversation}
        TBS-->>Bot: ✅ 200 signInLink + tokenExchangeResource
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 3 — Send OAuthCard (with SSO)
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>🃏 OAuthCard<br/>tokenExchangeResource.Uri=api://botid-...
        BFC-->>Bot: ✅ 202 Accepted (⏱️ 631ms)
    end

    Bot-->>Teams: ✅ 200 (⏱️ 3034ms total)
    Teams->>User: Shows OAuthCard / triggers silent SSO

    rect rgb(245, 240, 255)
        Note over Teams,Bot: Step 4 — Teams sends SSO token
        Teams->>Bot: 📥 POST /api/messages<br/>type=invoke, name=signin/tokenExchange<br/>token=SSO JWT (scp=access_as_user)
        Note over Bot: 🛡️ JWT validated
        Note over Bot: 🔀 Route: invoke/signin/tokenExchange
    end

    rect rgb(255, 245, 245)
        Note over Bot,TBS: Step 5 — Exchange SSO token
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 POST /api/usertoken/exchange<br/>connectionName=sso, token=SSO JWT
        TBS-->>Bot: ✅ 200 User token (⏱️ 903ms)
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 6 — 🎉 OnSignInComplete
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>"You're now signed in!"
        BFC-->>Bot: ✅ 201 (⏱️ 366ms)
    end

    Bot-->>Teams: ✅ 200 invoke response (⏱️ 1308ms total)
    Teams->>User: "You're now signed in!"
```

---

## 👤 Profile Flow (token cached)

```mermaid
sequenceDiagram
    actor User as 👤 Rido
    participant Teams as 🟣 Teams
    participant Bot as 🤖 Bot
    participant MSAL as 🔑 MSAL
    participant TBS as 🟠 Token Service
    participant Graph as 📊 Graph
    participant BFC as 🔷 Bot Framework

    User->>Teams: Types "profile"
    Teams->>Bot: 📥 POST /api/messages<br/>type=message, text="profile"
    Note over Bot: 🔀 Route: message/^profile$

    rect rgb(240, 248, 255)
        Note over Bot,TBS: Step 1 — Silent token check (hit)
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 GET /api/usertoken/GetToken<br/>connectionName=sso
        TBS-->>Bot: ✅ 200 Cached user token (⏱️ 214ms)
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
        BFC-->>Bot: ✅ 201 (⏱️ 283ms)
    end

    Bot-->>Teams: ✅ 200 (⏱️ 664ms total)
    Teams->>User: Shows profile JSON
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

    User->>Teams: Types "logout"
    Teams->>Bot: 📥 POST /api/messages<br/>type=message, text="logout"
    Note over Bot: 🔀 Route: message/^logout$

    rect rgb(255, 240, 240)
        Note over Bot,TBS: Step 1 — Revoke user token
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>TBS: 📤 DELETE /api/usertoken/SignOut<br/>connectionName=sso
        TBS-->>Bot: ✅ 200 Token revoked (⏱️ 313ms)
    end

    rect rgb(240, 255, 240)
        Note over Bot,BFC: Step 2 — Send confirmation
        Bot->>MSAL: AcquireTokenForClient
        MSAL-->>Bot: 💾 Cached
        Bot->>BFC: 📤 POST /v3/.../activities<br/>"Signed out."
        BFC-->>Bot: ✅ 201 (⏱️ 339ms)
    end

    Bot-->>Teams: ✅ 200 (⏱️ 662ms total)
    Teams->>User: "Signed out."
```
