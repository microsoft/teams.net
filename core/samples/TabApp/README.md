# TabApp

A sample demonstrating a React/Vite tab served by the bot, with server functions and client-side Graph calls.

| Feature | How it works |
|---|---|
| **Static tab** | Bot serves `Web/bin` via `app.WithTab("test", "./Web/bin")` at `/tabs/test` |
| **Teams Context** | Reads the raw Teams context via the Teams JS SDK |
| **Post to Chat** | Tab calls `POST /functions/post-to-chat` → bot sends a proactive message |
| **Who Am I** | Acquires a Graph token via MSAL and calls `GET /me` |
| **Toggle Presence** | Acquires a Graph token with `Presence.ReadWrite` and calls `POST /me/presence/setUserPreferredPresence` |

---

## Azure App Registration

### 1. Application ID URI

Under **Expose an API → Application ID URI**, set it to:

```
api://{YOUR_CLIENT_ID}
```

Then add a scope named `access_as_user` and pre-authorize the Teams client IDs:

| Client ID | App |
|---|---|
| `1fec8e78-bce4-4aaf-ab1b-5451cc387264` | Teams desktop / mobile |
| `5e3ce6c0-2b1f-4285-8d4b-75ee78787346` | Teams web |

### 2. Redirect URI

Under **Authentication → Add a platform → Single-page application**, add:

```
https://{YOUR_DOMAIN}/tabs/test
brk-multihub://{YOUR_DOMAIN}
```

### 3. API permissions

Under **API permissions → Add a permission → Microsoft Graph → Delegated**:

| Permission | Required for |
|---|---|
| `User.Read` | Who Am I |
| `Presence.ReadWrite` | Toggle Presence |

---

## Manifest

**`webApplicationInfo`** — required for SSO (`authentication.getAuthToken()` and MSAL silent auth):

```json
"webApplicationInfo": {
  "id": "{YOUR_CLIENT_ID}",
  "resource": "api://{YOUR_CLIENT_ID}"
}
```

**`staticTabs`**:

```json
"staticTabs": [
  {
    "entityId": "tab",
    "name": "Tab",
    "contentUrl": "https://{YOUR_DOMAIN}/tabs/test",
    "websiteUrl": "https://{YOUR_DOMAIN}/tabs/test",
    "scopes": ["personal"]
  }
]
```

---

## Configuration

**`launchSettings.json`** (or environment variables):

```json
"AzureAD__TenantId": "{YOUR_TENANT_ID}",
"AzureAD__ClientId": "{YOUR_CLIENT_ID}",
"AzureAD__ClientCredentials__0__SourceType": "ClientSecret",
"AzureAd__ClientCredentials__0__ClientSecret": "{YOUR_CLIENT_SECRET}"
```

**`Web/.env.local`**:

```
VITE_CLIENT_ID={YOUR_CLIENT_ID}
```

---

## Build & Run

```bash
# Build the React app
cd Web && npm install && npm run build

# Run the bot
dotnet run
```
