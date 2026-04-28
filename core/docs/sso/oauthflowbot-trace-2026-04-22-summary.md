# 🔐 OAuthFlowBot Trace Summary (Popup Fallback)

**Date**: 2026-04-22 03:12:00 UTC
**Bot**: my-bot-sso (AppID: `e3cb1c84-14e3-419c-b39c-1c06097b55fd`)
**User**: Rido (aadObjectId: `03500558-e554-416c-90c3-a061cdcd012b`)
**Connection**: `teamsgraph` (Azure AD v2, no SSO — popup fallback)
**Platform**: 🌐 Web (Teams)
**SDK Version**: `0.0.1-alpha-0107-g1c503584a7`
**Result**: ✅ SUCCESS (login graph + my ad user + logout graph)

> **Key difference from SsoBot**: This connection does not have `tokenExchangeResource` (SSO not configured).
> Login completes via **popup sign-in** + `signin/verifyState` instead of silent `signin/tokenExchange`.

### 🆔 Identity Reference

| Identity | MRI / Value |
|----------|-------------|
| User MRI | `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` |
| User AAD ObjectId | `03500558-e554-416c-90c3-a061cdcd012b` |
| Bot MRI | `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` |
| Bot AppId | `e3cb1c84-14e3-419c-b39c-1c06097b55fd` |
| Tenant Id | `3f3d1cea-7a18-41af-872b-cfbbd5140984` |
| Conversation Id | `a:1xH4HncZ6lyZnMVYp9rTKoRyS44qDCikYZ1u-Q0VNmZqyceL6nKfe5ZKG9CqOi2WuXNDJyLBAaDgVChKMxKFPlAZ5bsy0_8RhvPYYi5ZJJKCiia_SEd_e8WJVlSHOIM3Z` |
| Service URL | `https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/` |

---

## 🔑 Login Flow (Popup Fallback — no SSO)

### Step 1 — User sends "login graph" message

📥 **INCOMING** `POST http://localhost:3978/api/messages`
- **Activity**:
  - `type`: `message`
  - `id`: `1776827520098`
  - `channelId`: `msteams`
  - `text`: `"login graph"`
  - `textFormat`: `plain`
  - `timestamp`: `2026-04-22T03:12:00.1176725Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.name`: `Rido`
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `recipient.name`: `my-bot-sso`
  - `conversation.id`: `a:1xH4HncZ6ly...OIM3Z`
  - `conversation.conversationType`: `personal`
  - `conversation.tenantId`: `3f3d1cea-7a18-41af-872b-cfbbd5140984`
  - `serviceUrl`: `https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/`
  - `entities[0]`: `{ locale: "en-US", country: "US", platform: "Web", timezone: "America/Los_Angeles", type: "clientInfo" }`
  - `MSCV`: `4+YxTIufBEq78SeAVHsSdQ.1.1.1.485196365.1.1`
- 🛡️ JWT validated (AzureAd scheme)
- 🔀 Route: `message/(?i)^login graph$`

### Step 2 — Silent token check (no cached token)

📤 **OUTGOING** `GET https://token.botframework.com/api/usertoken/GetToken`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `teamsgraph`
  - `channelId`: `msteams`
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL AcquireTokenForClient (source: IdentityProvider) — first token from AAD
- ❌ **Response**: `404` — no cached user token

### Step 3 — Get sign-in resource

📤 **OUTGOING** `GET https://token.botframework.com/api/botsignin/GetSignInResource`
- **Query Parameters**:
  - `state`: base64-encoded JSON:
    ```json
    {
      "ConnectionName": "teamsgraph",
      "Conversation": {
        "ActivityId": "1776827520098",
        "Bot": { "Id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd" },
        "ChannelId": "msteams",
        "Conversation": { "Id": "a:1xH4HncZ6ly...OIM3Z" },
        "ServiceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
        "User": { "Id": "29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ" }
      },
      "MsAppId": "e3cb1c84-14e3-419c-b39c-1c06097b55fd"
    }
    ```
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL from cache
- ✅ **Response**: `200` — returns signInLink + tokenPostResource, **⚠️ NO tokenExchangeResource** (SSO not configured)

### Step 4 — Send OAuthCard to user (popup only, no SSO)

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/1776827520098`
- **Auth**: 🔑 MSAL from cache
- **Request Body**:
  ```json
  {
    "from": {
      "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd",
      "name": "my-bot-sso"
    },
    "recipient": {
      "id": "29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ",
      "name": "Rido",
      "aadObjectId": "03500558-e554-416c-90c3-a061cdcd012b"
    },
    "conversation": {
      "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984",
      "conversationType": "personal",
      "id": "a:1xH4HncZ6ly...OIM3Z"
    },
    "attachments": [{
      "contentType": "application/vnd.microsoft.card.oauth",
      "content": {
        "text": "Please Sign In",
        "connectionName": "teamsgraph",
        "buttons": [{
          "type": "signin",
          "title": "Sign In",
          "value": "https://token.botframework.com/api/oauth/signin?signin=02706e367e884e8ea4e86472cbd71932"
        }],
        "tokenPostResource": {
          "SasUrl": "https://token.botframework.com/api/sas/postToken?expiry=1776827583&id=key2&state=02706e367e884e8ea4e86472cbd71932&hmac=..."
        }
      }
    }],
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "1776827520098"
  }
  ```
  > **Note**: `tokenExchangeResource` is **omitted** (not sent as null). This is the fix applied in this run — previously it was serialized as `"tokenExchangeResource": null` which caused Teams to reject with `BadRequest`.
- ✅ **Response**: `200`

🏁 **HTTP Response to Teams**: `200`

### Step 5 — User completes popup sign-in, Teams sends signin/verifyState

📥 **INCOMING** `POST http://localhost:3978/api/messages`
- **Activity**:
  - `type`: `invoke`
  - `name`: `signin/verifyState`
  - `id`: `f:7d1e8ec2-5897-396e-aa7b-f579ad2fac9f`
  - `channelId`: `msteams`
  - `timestamp`: `2026-04-22T03:12:09.445Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.name`: `Rido`
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `recipient.name`: `my-bot-sso`
  - `conversation.id`: `a:1xH4HncZ6ly...OIM3Z`
  - `conversation.conversationType`: `personal`
  - `conversation.tenantId`: `3f3d1cea-7a18-41af-872b-cfbbd5140984`
  - `serviceUrl`: `https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/`
  - `replyToId`: `1776827524158`
  - `channelData.source.name`: `message`
  - `channelData.legacy.replyToId`: `1:1m2Cdy7qBU0p3417d81g04kt7MXJrjQC-X21CRiZVWzk`
  - `value`: `{ "state": "745254" }` *(verification code from popup)*
  - `MSCV`: `FvSbzMYrUE+OReNyK+4lyg.1.3`
- 🛡️ JWT validated (AzureAd scheme)
- 🔀 Route: `invoke/signin/verifyState`

### Step 6 — Verify state and get token

📤 **OUTGOING** `GET https://token.botframework.com/api/usertoken/GetToken`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `teamsgraph`
  - `channelId`: `msteams`
  - `code`: `745254` *(verification code from verifyState)*
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL from cache
- ✅ **Response**: `200` — user token returned

### Step 7 — 🎉 OnSignInComplete fires, bot sends confirmation

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/.../v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/f:7d1e8ec2-5897-396e-aa7b-f579ad2fac9f`
- **Auth**: 🔑 MSAL from cache
- **Request Body**:
  ```json
  {
    "from": { "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd", "name": "my-bot-sso" },
    "conversation": { "tenantId": "3f3d1cea-...", "conversationType": "personal", "id": "a:1xH4HncZ6ly...OIM3Z" },
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "f:7d1e8ec2-5897-396e-aa7b-f579ad2fac9f",
    "text": "Connected to Microsoft Graph (teamsgraph)!",
    "textFormat": "plain"
  }
  ```
- ✅ **Response**: `201 Created`

🏁 **Invoke Response**: `200` (body: null)

---

## 👤 "my ad user" Flow (token cached)

### Step 8 — User sends "my ad user" message

📥 **INCOMING** `POST http://localhost:3978/api/messages`
- **Activity**:
  - `type`: `message`
  - `id`: `1776827541160`
  - `text`: `"my ad user"`
  - `textFormat`: `plain`
  - `timestamp`: `2026-04-22T03:12:21.179708Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `attachments[0]`: `{ contentType: "text/html", content: "<div><span style=\"font-size:inherit\">my ad user</span></div>" }`
  - `MSCV`: `eTD3PgxXhEiK0mFFc7QunQ.1.1.1.485974193.1.1`
- 🔀 Route: `message/(?i)^my ad user`

### Step 9 — Silent token check (token exists)

📤 **OUTGOING** `GET https://token.botframework.com/api/usertoken/GetToken`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `teamsgraph`
  - `channelId`: `msteams`
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL from cache
- ✅ **Response**: `200` — cached user token returned

### Step 10 — Call Graph API with token

📤 **OUTGOING** `GET https://graph.microsoft.com/v1.0/me`
- **Auth**: `Authorization: Bearer {user_token}`
- ✅ **Response**: `200` — `{ displayName: "Rido", mail: "rido@teamssdk.onmicrosoft.com", id: "03500558-e554-416c-90c3-a061cdcd012b" }`

### Step 11 — Send profile result

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/.../v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/1776827541160`
- **Auth**: 🔑 MSAL from cache
- **Request Body**:
  ```json
  {
    "from": { "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd", "name": "my-bot-sso" },
    "conversation": { "tenantId": "3f3d1cea-...", "conversationType": "personal", "id": "a:1xH4HncZ6ly...OIM3Z" },
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "1776827541160",
    "text": "Your Azure AD user :\n```json\n{\"displayName\":\"Rido\",\"givenName\":\"Rido\",\"jobTitle\":\"Not an architect\",\"mail\":\"rido@teamssdk.onmicrosoft.com\",...}\n```",
    "textFormat": "plain"
  }
  ```
- ✅ **Response**: `201 Created`

---

## 🚪 Logout Flow

### Step 12 — User sends "logout graph" message

📥 **INCOMING** `POST http://localhost:3978/api/messages`
- **Activity**:
  - `type`: `message`
  - `id`: `1776827548949`
  - `text`: `"logout graph"`
  - `textFormat`: `plain`
  - `timestamp`: `2026-04-22T03:12:28.9762671Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `MSCV`: `ymRk2x/XZ0CFG0QGeNHrOg.1.1.1.486335532.1.1`
- 🔀 Route: `message/(?i)^logout graph$`

### Step 13 — Sign out user

📤 **OUTGOING** `DELETE https://token.botframework.com/api/usertoken/SignOut`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `teamsgraph`
  - `channelId`: `msteams`
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL from cache
- ✅ **Response**: `200` — token revoked

### Step 14 — Send confirmation

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/.../v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/1776827548949`
- **Auth**: 🔑 MSAL from cache
- **Request Body**:
  ```json
  {
    "from": { "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd", "name": "my-bot-sso" },
    "conversation": { "tenantId": "3f3d1cea-...", "conversationType": "personal", "id": "a:1xH4HncZ6ly...OIM3Z" },
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "1776827548949",
    "text": "Signed out from Graph.",
    "textFormat": "plain"
  }
  ```
- ✅ **Response**: `201 Created`

---

## 📊 Request Summary Table

| # | Direction | Method | Endpoint | Status | Purpose |
|---|-----------|--------|----------|--------|---------|
| 1 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | 💬 "login graph" message |
| 2 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/usertoken/GetToken` | ❌ 404 | 🔍 Silent token check (miss) |
| 3 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/botsignin/GetSignInResource` | ✅ 200 | 🔗 Get sign-in resource (no tokenExchangeResource) |
| 4 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 200 | 🃏 Send OAuthCard (popup only, no SSO) |
| 5 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | 🔄 signin/verifyState invoke (code=745254) |
| 6 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/usertoken/GetToken` | ✅ 200 | 🔐 Verify state + get token (code=745254) |
| 7 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 201 | 🎉 "Connected to Microsoft Graph!" |
| 8 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | 💬 "my ad user" message |
| 9 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/usertoken/GetToken` | ✅ 200 | 🔍 Silent token check (hit) |
| 10 | 📤 ⬆️ OUT | GET | `graph.microsoft.com/v1.0/me` | ✅ 200 | 👤 Graph API call |
| 11 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 201 | 📄 Profile response |
| 12 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | 💬 "logout graph" message |
| 13 | 📤 ⬆️ OUT | DELETE | `token.botframework.com/api/usertoken/SignOut` | ✅ 200 | 🚪 Revoke token |
| 14 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 201 | 💬 "Signed out from Graph." |

## 🆔 User MRI Usage Across Requests

| Request | Where User MRI appears | Format |
|---------|----------------------|--------|
| Step 1 (incoming message) | `activity.from.id` | `29:1cgsv1oFLAoTflZ-...` |
| Step 2 (GetToken) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |
| Step 3 (GetSignInResource) | `state.Conversation.User.Id` (base64 JSON) | `29:1cgsv1oFLAoTflZ-...` |
| Step 4 (Send OAuthCard) | `recipient.id` (reply to user) | `29:1cgsv1oFLAoTflZ-...` |
| Step 5 (verifyState invoke) | `activity.from.id` | `29:1cgsv1oFLAoTflZ-...` |
| Step 6 (GetToken + code) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |
| Step 9 (GetToken cached) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |
| Step 13 (SignOut) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |

> **Note**: The User MRI (`29:...`) is the Teams-specific identifier. It is used as `userid` in all Token Bot Service calls (GetToken, SignOut) and appears in `from.id` on incoming activities and `recipient.id` on outgoing replies. The AAD ObjectId (`03500558-...`) appears separately in `from.aadObjectId` and in the outgoing `recipient.aadObjectId`.

---

## 🔑 vs SsoBot: Key Differences

| Aspect | SsoBot (`sso` connection) | OAuthFlowBot (`teamsgraph` connection) |
|--------|--------------------------|----------------------------------------|
| SSO support | ✅ `tokenExchangeResource` present | ❌ `tokenExchangeResource` omitted |
| Sign-in invoke | `signin/tokenExchange` (silent) | `signin/verifyState` (popup + code) |
| Token acquisition | `POST /api/usertoken/exchange` with SSO JWT | `GET /api/usertoken/GetToken` with `code` param |
| User interaction | None (fully silent) | Popup window + consent |
| OAuthFlow API | Context API (`context.SignIn()`) | Instance API (`graphAuth.SignInAsync(context)`) |
| verifyState value | N/A | `{ "state": "745254" }` |
| tokenExchange value | `{ id, connectionName, token }` | N/A |

## 🐛 Bug Fixed During This Run

**Issue**: `OAuthCard` serialized `"tokenExchangeResource": null` explicitly in JSON. Teams rejected this with `BadRequest: {"error":{"code":"ServiceError","message":"Unknown"}}`.

**Fix**: Added `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to `TokenExchangeResource` and `TokenPostResource` properties in `OAuthCard.cs`. When null, these properties are now omitted from the JSON instead of being sent as explicit nulls.

**File**: `src/Microsoft.Teams.Bot.Apps/Schema/OAuthCard.cs`
