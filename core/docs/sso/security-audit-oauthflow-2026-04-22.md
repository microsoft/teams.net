# Security Audit: OAuthFlow Token Retrieval Attack Surface

**Date:** 2026-04-22
**Scope:** OAuthFlow design, implementation, live traffic trace, Azure Bot Service + Entra ID configuration
**Bot App ID:** `e3cb1c84-14e3-419c-b39c-1c06097b55fd` ("my-bot-sso")
**Tenant:** `3f3d1cea-7a18-41af-872b-cfbbd5140984`

---

## Executive Summary

The Bot Framework Token Service (`token.botframework.com`) acts as a **centralized token vault** for all user tokens acquired through OAuth connections. **Any caller that can authenticate as the bot** (i.e., possesses the bot's `AppId` + client secret) can retrieve any user's cached token by calling a single unauthenticated-beyond-app-identity API. The only inputs needed are:

- The bot's credentials (AppId + secret)
- A user's Teams MRI (semi-public, visible to anyone in the same org/conversation)
- The connection name (a short string like `"teamsgraph"`)

This is **by design** in the Bot Framework Token Service protocol. The mitigation is entirely dependent on protecting the bot's client secret.

---

## Detailed Attack Reconstruction

### What the trace shows

From the live traffic trace (`oauthflowbot-trace-2026-04-22-raw.log`), the token retrieval call is:

```
GET https://token.botframework.com/api/usertoken/GetToken
    ?userid=29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ
    &connectionName=teamsgraph
    &channelId=msteams
Authorization: Bearer <app-only-token for https://api.botframework.com/.default>
```

The authorization token is an **app-only** token (no user context), acquired by the bot using its own credentials:
```
aud: https://api.botframework.com
appid: e3cb1c84-14e3-419c-b39c-1c06097b55fd
idtyp: app
```

### The attack (step by step)

An attacker with access to the bot's client secret can reproduce this outside the bot:

1. **Acquire app-only token:**
   ```bash
   curl -X POST https://login.microsoftonline.com/3f3d1cea-7a18-41af-872b-cfbbd5140984/oauth2/v2.0/token \
     -d "client_id=e3cb1c84-14e3-419c-b39c-1c06097b55fd" \
     -d "client_secret=<stolen-secret>" \
     -d "scope=https://api.botframework.com/.default" \
     -d "grant_type=client_credentials"
   ```

2. **Retrieve any user's token:**
   ```bash
   curl "https://token.botframework.com/api/usertoken/GetToken?\
   userid=29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ\
   &connectionName=teamsgraph\
   &channelId=msteams" \
     -H "Authorization: Bearer <token-from-step-1>"
   ```

3. **Use the returned delegated token** to call Microsoft Graph, GitHub, etc. as the victim user.

### What tokens are at risk

| Connection | Provider App | Scopes | Impact |
|---|---|---|---|
| `teamsgraph` | `9f43e2fb-2cbd-4303-aaf4-c6d209dc2666` ("RidoGraphExperiment") | `ChannelMessage.Read.All TeamMember.Read.All` | **Read ALL channel messages and team members as the user** |
| `sso` | `e3cb1c84-14e3-419c-b39c-1c06097b55fd` (bot itself) | `User.Read Calendars.Read` | Read user profile and calendar |
| `gh` | `Ov23ligyZwD5j1u41P81` (GitHub OAuth App) | `repo pr` | **Full access to user's GitHub repositories** |
| `sso-bad` | Unknown | Unknown | (likely a test connection) |

### How easy is it to get the inputs?

| Input | Difficulty | How |
|---|---|---|
| Bot AppId | **Trivial** | Visible in every activity (`recipient.id`), in the OAuthCard, in the base64 state, in bot manifests |
| Client secret | **Medium** | Stored in: app settings, Key Vault, CI/CD pipelines, developer machines, `.env` files. Hint: `a-t`, expires 2028-04-20 |
| User MRI | **Low** | Visible to any user in the same conversation. Format: `29:<base64>`. Enumerable via Graph API with `TeamMember.Read.All` |
| Connection name | **Low** | Visible in OAuthCard payload (`connectionName: "teamsgraph"`), guessable, or enumerable if you have the bot's Azure subscription access |

---

## Configuration Findings

### Finding 1: CRITICAL — Overprivileged OAuth Connection Scopes

The `teamsgraph` connection requests `ChannelMessage.Read.All` and `TeamMember.Read.All`. These are high-privilege delegated permissions that grant access far beyond what the sample bot uses (it only calls `/me`).

If a user's token is stolen via the attack above, the attacker gets these broad permissions for free.

**Recommendation:** Apply least-privilege. The sample only needs `User.Read`. Remove `ChannelMessage.Read.All` and `TeamMember.Read.All` from the connection scopes.

### Finding 2: HIGH — `teamsgraph` Uses a Separate App Registration

The `teamsgraph` connection's `clientId` is `9f43e2fb-2cbd-4303-aaf4-c6d209dc2666` ("RidoGraphExperiment") — a **different** app registration than the bot itself. This means:

- The Token Service performs OBO (on-behalf-of) using this separate app's credentials
- This separate app has its own client secrets (hints: `2PX` expiring 2026-10-17, `Fta` expiring 2027-04-17)
- Two sets of credentials must be protected, doubling the attack surface
- The `RidoGraphExperiment` app's credentials are stored in the Token Service, not in the bot's code, but if the bot credentials are compromised, the stored tokens (already exchanged) are directly accessible

**Recommendation:** Use the bot's own app ID for the OAuth connection where possible (as the `sso` connection already does). This reduces the number of credential sets to protect.

### Finding 3: HIGH — `signInAudience` vs `msaAppType` Mismatch

| Setting | Value |
|---|---|
| Entra App `signInAudience` | `AzureADMultipleOrgs` (any Entra tenant) |
| Bot Service `msaAppType` | `SingleTenant` |

The Entra app accepts tokens from **any** Azure AD tenant, but the Bot Service is configured as single-tenant. This mismatch means:

- An attacker from a different tenant could acquire an app-only token against `https://api.botframework.com/.default` using a service principal in their own tenant (if the app is registered as multi-org)
- The Bot Framework Token Service may or may not enforce tenant isolation on the `GetToken` API

**Recommendation:** Align the Entra app `signInAudience` to `AzureADMyOrg` (single tenant) to match the Bot Service configuration. This restricts token acquisition to the bot's home tenant.

### Finding 4: MEDIUM — `appRoleAssignmentRequired: false`

The bot's service principal does not require role assignment. Combined with `AzureADMultipleOrgs`, any user in any tenant can authenticate. While this is typical for bots (they need to accept tokens from the Bot Framework), it should be reviewed.

### Finding 5: MEDIUM — Dev Tunnel Endpoint in Production Bot Registration

The messaging endpoint is:
```
https://klljrqz0-3978.usw2.devtunnels.ms/api/messages
```

This is a dev tunnel URL. If this bot registration is also used for testing with real user tokens, those tokens are cached in the Token Service and retrievable even after the dev tunnel is shut down. The tokens persist until they expire or the user signs out.

### Finding 6: MEDIUM — GitHub Connection Has `repo` Scope

The `gh` connection grants `repo` scope — full read/write access to all repositories. A stolen GitHub token would allow an attacker to read private code, push malicious commits, or exfiltrate proprietary source code.

**Recommendation:** Use fine-grained GitHub permissions or the minimum scope needed.

---

## What the SDK Can and Cannot Do

### Cannot fix (Bot Framework Token Service design)

The core issue — that `GetToken` only requires bot identity + userId — is a **Token Service protocol property**. The Token Service treats the bot as a trusted party for all its users. This is analogous to how a web app's backend can use its OAuth client credentials to access stored refresh tokens.

The SDK cannot add additional authorization to the Token Service API.

### Can mitigate

| Mitigation | Where | Status |
|---|---|---|
| **Document the threat model** | Design doc, SDK docs | Not done |
| **Warn about credential protection** | Sample README, getting-started guide | Not done |
| **Log token retrieval attempts** | OAuthFlow.cs `GetTokenAsync` | Partially done (debug-level) |
| **Support Managed Identity** | BotConfig.cs | Supported (eliminates client secret) |
| **Support Federated Identity** | BotConfig.cs | Supported (eliminates client secret) |
| **Reduce default log verbosity** | BotAuthenticationHandler.cs | Not done (full claims at Trace) |

---

## Recommendations (Priority Order)

### 1. Eliminate the client secret (P0)

The **single most effective mitigation** is to remove the client secret entirely:

- **Managed Identity**: If the bot runs on Azure (App Service, Container Apps), use system-assigned managed identity. No secret to steal.
- **Federated Identity Credentials**: For non-Azure hosts or CI/CD, use workload identity federation. No secret stored.

The bot's `BotConfig` already supports both (`Credential.ManagedIdentity`, `Credential.FederatedIdentity`). The sample should demonstrate this.

### 2. Fix the `signInAudience` mismatch (P0)

```bash
az ad app update --id e3cb1c84-14e3-419c-b39c-1c06097b55fd \
    --sign-in-audience AzureADMyOrg
```

This ensures only the home tenant (`3f3d1cea-...`) can acquire tokens for this app.

### 3. Apply least-privilege scopes to OAuth connections (P1)

For `teamsgraph`: change scopes from `ChannelMessage.Read.All TeamMember.Read.All` to `User.Read` (what the sample actually uses).

For `gh`: change from `repo pr` to `read:user` if only profile info is needed.

### 4. Consolidate to a single app registration (P1)

Use the bot's own app ID (`e3cb1c84-...`) for the `teamsgraph` OAuth connection instead of the separate `RidoGraphExperiment` app. This halves the credential surface.

### 5. Document the Token Service threat model (P1)

Add to the OAuthFlow design doc:

> **Security Note:** The Bot Framework Token Service stores user tokens on behalf of the bot. Any entity that can authenticate as the bot (via AppId + credential) can retrieve any user's cached token by calling the Token Service API with the user's ID and connection name. Protect the bot's credentials with the same rigor as a database connection string. Prefer Managed Identity or Federated Identity Credentials over client secrets.

### 6. Rotate the existing client secret (P1)

The current secret (hint: `a-t`, created 2026-04-20, expires 2028-04-20) has a **2-year lifetime** — far too long. Rotate immediately and set a shorter expiry (90 days max) as a bridge while migrating to Managed Identity.

### 7. Clean up the dev tunnel endpoint (P2)

If this bot registration was used with real users during development, their tokens may still be cached. Either:
- Sign out all users via the Token Service API
- Delete and recreate the bot registration for production use

---

## Appendix: Token Service API Surface (Attack-Relevant)

All endpoints authenticated with bot's app-only token for `https://api.botframework.com/.default`:

| Endpoint | Method | What it does |
|---|---|---|
| `/api/usertoken/GetToken?userid=X&connectionName=Y&channelId=Z` | GET | **Returns the user's cached access token** |
| `/api/usertoken/GetToken?userid=X&connectionName=Y&channelId=Z&code=C` | GET | Exchanges a verify-state code for a token |
| `/api/usertoken/SignOut?userid=X&connectionName=Y&channelId=Z` | DELETE | Revokes a user's cached token |
| `/api/usertoken/GetTokenStatus?userid=X&channelId=Z` | GET | Lists all connections and whether tokens exist |
| `/api/usertoken/exchange?userid=X&connectionName=Y&channelId=Z` | POST | Exchanges an SSO token for an access token |
| `/api/botsignin/GetSignInResource?state=X` | GET | Returns sign-in URL + TokenExchangeResource |

Every one of these is callable by anyone with the bot's credentials. The `GetTokenStatus` endpoint even lets an attacker enumerate which connections a user has tokens for without knowing the connection names.
