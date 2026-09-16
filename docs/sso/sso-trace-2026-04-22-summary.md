# 🔐 SsoBot Trace Summary (Silent SSO)

**Date**: 2026-04-22 02:45:26 UTC
**Bot**: my-bot-sso (AppID: `e3cb1c84-14e3-419c-b39c-1c06097b55fd`)
**User**: Rido (aadObjectId: `03500558-e554-416c-90c3-a061cdcd012b`)
**Connection**: `sso`
**Platform**: 🌐 Web (Teams)
**SDK Version**: `0.0.1-alpha-0107-g1c503584a7`
**Result**: ✅ SUCCESS (login + profile + logout)

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

## 🔑 Login Flow

### Step 1 — User sends "login" message

📥 **INCOMING** `POST http://localhost:3978/api/messages` (1017 bytes)
- **Request Headers**: `Content-Type: application/json;+charset=utf-8`
- **Activity**:
  - `type`: `message`
  - `id`: `1776825925953`
  - `channelId`: `msteams`
  - `text`: `"login"`
  - `textFormat`: `plain`
  - `timestamp`: `2026-04-22T02:45:26.0070993Z`
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
  - `MSCV`: `V3M44DfUokajTFBIXtrInA.1.1.1.422967360.1.1`
- 🛡️ JWT validated (AzureAd scheme)
- 🔀 Route: `message/(?i)^login$`

### Step 2 — Silent token check (no cached token)

📤 **OUTGOING** `GET https://token.botframework.com/api/usertoken/GetToken`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `sso`
  - `channelId`: `msteams`
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL AcquireTokenForClient (source: IdentityProvider, ⏱️ 535ms) — first token from AAD
- ❌ **Response**: `404` (⏱️ 568ms) — no cached user token

### Step 3 — Get sign-in resource

📤 **OUTGOING** `GET https://token.botframework.com/api/botsignin/GetSignInResource`
- **Query Parameters**:
  - `state`: base64-encoded JSON:
    ```json
    {
      "ConnectionName": "sso",
      "Conversation": {
        "ActivityId": "1776825925953",
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
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- ✅ **Response**: `200` (⏱️ 286ms) — returns signInLink, tokenExchangeResource, tokenPostResource

### Step 4 — Send OAuthCard to user

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/1776825925953?isTargetedActivity=true`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
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
      "isTargeted": true,
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
        "connectionName": "sso",
        "buttons": [{
          "type": "signin",
          "title": "Sign In",
          "value": "https://token.botframework.com/api/oauth/signin?signin=893cf4ca0d6943fca7754c614f20451c"
        }],
        "tokenExchangeResource": {
          "Id": "fc67c7b5-d0d4-494c-a0e9-3a7ddec999f0",
          "ProviderId": "30dd229c-58e3-4a48-bdfd-91ec48eb906c",
          "Uri": "api://botid-e3cb1c84-14e3-419c-b39c-1c06097b55fd"
        },
        "tokenPostResource": {
          "SasUrl": "https://token.botframework.com/api/sas/postToken?expiry=1776825989&id=key1&state=893cf4ca0d6943fca7754c614f20451c&hmac=..."
        }
      }
    }],
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "1776825925953"
  }
  ```
- ✅ **Response**: `202 Accepted` (⏱️ 631ms)

🏁 **HTTP Response to Teams**: `200` (total ⏱️ 3034ms)

### Step 5 — Teams sends signin/tokenExchange invoke

📥 **INCOMING** `POST http://localhost:3978/api/messages` (2731 bytes)
- **Activity**:
  - `type`: `invoke`
  - `name`: `signin/tokenExchange`
  - `id`: `f:9b40df9c-b27c-55a0-7b42-0d2033f7d213`
  - `channelId`: `msteams`
  - `timestamp`: `2026-04-22T02:45:29.991Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.name`: `Rido`
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `recipient.name`: `my-bot-sso`
  - `conversation.id`: `a:1xH4HncZ6ly...OIM3Z`
  - `conversation.conversationType`: `personal`
  - `conversation.tenantId`: `3f3d1cea-7a18-41af-872b-cfbbd5140984`
  - `serviceUrl`: `https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/`
  - `channelData.source.name`: `message`
  - `value`:
    - `id`: `fc67c7b5-d0d4-494c-a0e9-3a7ddec999f0` *(matches tokenExchangeResource.Id from OAuthCard)*
    - `connectionName`: `sso`
    - `token`: SSO JWT (`aud=e3cb1c84...`, `iss=login.microsoftonline.com`, `name=Rido`, `scp=access_as_user`, `preferred_username=rido@teamssdk.onmicrosoft.com`)
  - `MSCV`: `M1mwQ79zSkClUOfTm5O0ew.1.2.1.423058522.1.1.0.1.1.0.1.3`
- 🛡️ JWT validated (AzureAd scheme)
- 🔀 Route: `invoke/signin/tokenExchange`

### Step 6 — Exchange SSO token for user token

📤 **OUTGOING** `POST https://token.botframework.com/api/usertoken/exchange`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `sso`
  - `channelId`: `msteams`
- **Request Body**: `{ "token": "<SSO JWT>" }`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- ✅ **Response**: `200` (⏱️ 903ms) — user token returned

### Step 7 — 🎉 OnSignInComplete fires, bot sends confirmation

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/f:9b40df9c-b27c-55a0-7b42-0d2033f7d213`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- **Request Body**:
  ```json
  {
    "from": { "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd", "name": "my-bot-sso" },
    "conversation": { "tenantId": "3f3d1cea-...", "conversationType": "personal", "id": "a:1xH4HncZ6ly...OIM3Z" },
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "f:9b40df9c-b27c-55a0-7b42-0d2033f7d213",
    "text": "You're now signed in! Try `profile` or `calendar`.",
    "textFormat": "plain"
  }
  ```
- ✅ **Response**: `201 Created` (⏱️ 366ms)

🏁 **Invoke Response**: `200` (body: null)
🏁 **HTTP Response to Teams**: `200` (total ⏱️ 1308ms)

---

## 👤 Profile Flow (token cached)

### Step 8 — User sends "profile" message

📥 **INCOMING** `POST http://localhost:3978/api/messages` (1019 bytes)
- **Activity**:
  - `type`: `message`
  - `id`: `1776825937933`
  - `text`: `"profile"`
  - `timestamp`: `2026-04-22T02:45:37.9548075Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `MSCV`: `wqMomZDl5k2Mdw7S3YUAsQ.1.1.1.423403741.1.1`
- 🔀 Route: `message/(?i)^profile$`

### Step 9 — Silent token check (token exists)

📤 **OUTGOING** `GET https://token.botframework.com/api/usertoken/GetToken`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `sso`
  - `channelId`: `msteams`
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- ✅ **Response**: `200` (⏱️ 214ms) — cached user token returned

### Step 10 — Call Graph API with token

📤 **OUTGOING** `GET https://graph.microsoft.com/v1.0/me`
- **Auth**: `Authorization: Bearer {user_token}`
- ✅ **Response**: `200` — `{ displayName: "Rido", mail: "rido@teamssdk.onmicrosoft.com", id: "03500558-e554-416c-90c3-a061cdcd012b" }`

### Step 11 — Send profile result

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/.../v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/1776825937933`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- **Request Body**:
  ```json
  {
    "from": { "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd", "name": "my-bot-sso" },
    "conversation": { "tenantId": "3f3d1cea-...", "conversationType": "personal", "id": "a:1xH4HncZ6ly...OIM3Z" },
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "1776825937933",
    "text": "```json\n{\"@odata.context\":\"...\",\"displayName\":\"Rido\",\"givenName\":\"Rido\",\"jobTitle\":\"Not an architect\",\"mail\":\"rido@teamssdk.onmicrosoft.com\",...}\n```",
    "textFormat": "plain"
  }
  ```
- ✅ **Response**: `201 Created` (⏱️ 283ms)

🏁 **HTTP Response to Teams**: `200` (total ⏱️ 664ms)

---

## 🚪 Logout Flow

### Step 12 — User sends "logout" message

📥 **INCOMING** `POST http://localhost:3978/api/messages` (1018 bytes)
- **Activity**:
  - `type`: `message`
  - `id`: `1776825945288`
  - `text`: `"logout"`
  - `timestamp`: `2026-04-22T02:45:45.3792484Z`
  - `from.id`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `from.aadObjectId`: `03500558-e554-416c-90c3-a061cdcd012b`
  - `recipient.id`: `28:e3cb1c84-14e3-419c-b39c-1c06097b55fd` *(Bot MRI)*
  - `MSCV`: `xflMC1y26keiHnFL8vvL7g.1.1.1.423642628.1.1`
- 🔀 Route: `message/(?i)^logout$`

### Step 13 — Sign out user

📤 **OUTGOING** `DELETE https://token.botframework.com/api/usertoken/SignOut`
- **Query Parameters**:
  - `userid`: `29:1cgsv1oFLAoTflZ-AxCZ_erWK6f4AqDSzZGpJOS7FuyfB8gn-g9bWmVM8usvvrv2e0atWV6wxZOQCn-xntjVrrQ` *(User MRI)*
  - `connectionName`: `sso`
  - `channelId`: `msteams`
- **Request Body**: `(null)`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- ✅ **Response**: `200` (⏱️ 313ms) — token revoked

### Step 14 — Send confirmation

📤 **OUTGOING** `POST https://smba.trafficmanager.net/amer/.../v3/conversations/a%3A1xH4HncZ6ly...OIM3Z/activities/1776825945288`
- **Auth**: 🔑 MSAL from cache (⏱️ 0ms)
- **Request Body**:
  ```json
  {
    "from": { "id": "28:e3cb1c84-14e3-419c-b39c-1c06097b55fd", "name": "my-bot-sso" },
    "conversation": { "tenantId": "3f3d1cea-...", "conversationType": "personal", "id": "a:1xH4HncZ6ly...OIM3Z" },
    "type": "message",
    "channelId": "msteams",
    "serviceUrl": "https://smba.trafficmanager.net/amer/3f3d1cea-7a18-41af-872b-cfbbd5140984/",
    "replyToId": "1776825945288",
    "text": "Signed out.",
    "textFormat": "plain"
  }
  ```
- ✅ **Response**: `201 Created` (⏱️ 339ms)

🏁 **HTTP Response to Teams**: `200` (total ⏱️ 662ms)

---

## 📊 Request Summary Table

| # | Direction | Method | Endpoint | Status | Latency | Purpose |
|---|-----------|--------|----------|--------|---------|---------|
| 1 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | ⏱️ 3034ms | 💬 "login" message |
| 2 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/usertoken/GetToken` | ❌ 404 | ⏱️ 568ms | 🔍 Silent token check (miss) |
| 3 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/botsignin/GetSignInResource` | ✅ 200 | ⏱️ 286ms | 🔗 Get sign-in resource |
| 4 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 202 | ⏱️ 631ms | 🃏 Send OAuthCard |
| 5 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | ⏱️ 1308ms | 🔄 signin/tokenExchange invoke |
| 6 | 📤 ⬆️ OUT | POST | `token.botframework.com/api/usertoken/exchange` | ✅ 200 | ⏱️ 903ms | 🔐 SSO token exchange |
| 7 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 201 | ⏱️ 366ms | 🎉 "Signed in!" confirmation |
| 8 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | ⏱️ 664ms | 💬 "profile" message |
| 9 | 📤 ⬆️ OUT | GET | `token.botframework.com/api/usertoken/GetToken` | ✅ 200 | ⏱️ 214ms | 🔍 Silent token check (hit) |
| 10 | 📤 ⬆️ OUT | GET | `graph.microsoft.com/v1.0/me` | ✅ 200 | - | 👤 Graph API call |
| 11 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 201 | ⏱️ 283ms | 📄 Profile response |
| 12 | 📥 ⬇️ IN | POST | `/api/messages` | ✅ 200 | ⏱️ 662ms | 💬 "logout" message |
| 13 | 📤 ⬆️ OUT | DELETE | `token.botframework.com/api/usertoken/SignOut` | ✅ 200 | ⏱️ 313ms | 🚪 Revoke token |
| 14 | 📤 ⬆️ OUT | POST | `smba.trafficmanager.net/.../activities` | ✅ 201 | ⏱️ 339ms | 💬 "Signed out." confirmation |

## 🆔 User MRI Usage Across Requests

| Request | Where User MRI appears | Format |
|---------|----------------------|--------|
| Step 1 (incoming message) | `activity.from.id` | `29:1cgsv1oFLAoTflZ-...` |
| Step 2 (GetToken) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |
| Step 3 (GetSignInResource) | `state.Conversation.User.Id` (base64 JSON) | `29:1cgsv1oFLAoTflZ-...` |
| Step 4 (Send OAuthCard) | `recipient.id` (reply to user) | `29:1cgsv1oFLAoTflZ-...` |
| Step 5 (tokenExchange invoke) | `activity.from.id` | `29:1cgsv1oFLAoTflZ-...` |
| Step 6 (Exchange token) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |
| Step 9 (GetToken cached) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |
| Step 13 (SignOut) | `?userid=` query param | URL-encoded: `29%3A1cgsv1oFLAoTflZ-...` |

> **Note**: The User MRI (`29:...`) is the Teams-specific identifier. It is used as `userid` in all Token Bot Service calls (GetToken, Exchange, SignOut) and appears in `from.id` on incoming activities and `recipient.id` on outgoing replies. The AAD ObjectId (`03500558-...`) appears separately in `from.aadObjectId` and in the outgoing `recipient.aadObjectId`.

## 🔑 MSAL Token Acquisitions

| # | Time | Source | Duration | Scope |
|---|------|--------|----------|-------|
| 1 | 02:45:27Z | 🌐 IdentityProvider | ⏱️ 535ms | `api.botframework.com/.default` |
| 2-14 | 02:45:28-46Z | 💾 Cache | ⏱️ 0ms | `api.botframework.com/.default` |

First acquisition hit AAD (instance discovery + token POST). All subsequent acquisitions served from in-memory MSAL cache.
