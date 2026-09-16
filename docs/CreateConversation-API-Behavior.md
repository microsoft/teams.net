# CreateConversation API Behavior

Technical reference documenting the exact behavior of the Teams Bot Framework `POST /v3/conversations` endpoint, based on integration test results captured on 2026-04-17.

## Endpoint

```
POST {serviceUrl}/v3/conversations
Content-Type: application/json; charset=utf-8
Authorization: Bearer {token}
```

Service URL: `https://smba.trafficmanager.net/teams/`

## Supported Conversation Types

The endpoint supports exactly **two** conversation creation patterns:

1. **1:1 Personal Chat** (proactive messaging to a single user)
2. **Channel Thread** (new thread in an existing Teams channel)

**Group chat creation is NOT supported** — every variation returns `400 BadSyntax`.

---

## 1:1 Personal Chat

### Minimal (working)

```http
POST https://smba.trafficmanager.net/teams/v3/conversations

{
  "isGroup": false,
  "members": [
    {
      "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg"
    }
  ],
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 201 Created
Server: Microsoft-HTTPAPI/2.0
MS-CV: p82ptW4x80GRgeZm9NkMlQ.0
Content-Type: application/json; charset=utf-8
Content-Length: 140

{
  "id": "a:1p0iicaJlVi-_KIYKDDvLi4c2pMZMc8B0bPauUJq9pZ6IHPzMOrXbWS4g7Wktn1hwl8J3FecCj4cn33DInsp7AGj8mSSb23S5cQJTjU_CXlYs-eph-CchluBdnSKVFm40"
}
```

**Notes:**
- `isGroup` must be `false`
- `members` must contain exactly 1 member
- Member ID must be in MRI format (`29:...`), not pairwise bot framework ID (`29:guid`)
- `tenantId` is required
- Response `id` starts with `a:` prefix (personal chat conversation ID)
- Calling with the same member returns the same conversation ID (idempotent)

### With bot specified (working)

```http
{
  "isGroup": false,
  "bot": {
    "id": "28:3738fe3d-bca2-479d-8e45-1660de89ee41"
  },
  "members": [
    {
      "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg"
    }
  ],
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 201 Created
MS-CV: 2yFg6cTgzUeVB8q/F9pHkA.0
```

**Notes:**
- `bot.id` uses `28:{appId}` format
- Bot field is optional for 1:1 — the API infers the bot from the auth token
- Same response as without bot

### With initial activity (working)

```http
{
  "isGroup": false,
  "members": [
    {
      "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg"
    }
  ],
  "activity": {
    "type": "message",
    "text": "[Diagnostic] 1:1 with initial activity"
  },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 201 Created
MS-CV: PB7kLrArfE6r21I3q5gMRA.0

{
  "id": "a:1p0iicaJlVi-_KIYKDDvLi4c2pMZMc8B0bPauUJq9pZ6IHPzMOrXbWS4g7Wktn1hwl8J3FecCj4cn33DInsp7AGj8mSSb23S5cQJTjU_CXlYs-eph-CchluBdnSKVFm40"
}
```

**Notes:**
- The activity is sent as the first message in the conversation
- Response does NOT include `activityId` (unlike channel threads)
- If the conversation already exists, the activity is still sent

---

## Channel Thread

### With activity (working)

```http
{
  "isGroup": true,
  "activity": {
    "type": "message",
    "text": "[Diagnostic] channel thread"
  },
  "channelData": {
    "channel": {
      "id": "19:LydFnezGKSkhYoiLNP6kZ8AuXQr36EDAkvG9CNJSPKc1@thread.tacv2"
    }
  },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 201 Created
Server: Microsoft-HTTPAPI/2.0
MS-CV: 7hdK6FlaqE+BjXxKvUCQUg.0
Content-Type: application/json; charset=utf-8
Content-Length: 122

{
  "id": "19:LydFnezGKSkhYoiLNP6kZ8AuXQr36EDAkvG9CNJSPKc1@thread.tacv2;messageid=1776390257332",
  "activityId": "1776390257332"
}
```

**Notes:**
- `isGroup` must be `true`
- `channelData.channel.id` must reference a valid channel
- `activity` is **required** — the thread root message
- Response `id` is `{channelId};messageid={messageId}` (the thread conversation ID)
- Response includes `activityId` (the thread root message ID, used for replies)
- `members` is NOT required (thread is visible to all channel members)

### With members and activity (working)

```http
{
  "isGroup": true,
  "members": [
    {
      "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg"
    }
  ],
  "activity": {
    "type": "message",
    "text": "[Diagnostic] channel thread with members"
  },
  "channelData": {
    "channel": {
      "id": "19:LydFnezGKSkhYoiLNP6kZ8AuXQr36EDAkvG9CNJSPKc1@thread.tacv2"
    }
  },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 201 Created
MS-CV: +YAgno/+yUqSpHSvnRVcOQ.0

{
  "id": "19:LydFnezGKSkhYoiLNP6kZ8AuXQr36EDAkvG9CNJSPKc1@thread.tacv2;messageid=1776390250598",
  "activityId": "1776390250598"
}
```

**Notes:**
- Adding `members` to a channel thread request does not cause an error
- The members field appears to be ignored (thread visibility is determined by channel membership)

### Without activity (FAILS)

```http
{
  "isGroup": true,
  "channelData": {
    "channel": {
      "id": "19:LydFnezGKSkhYoiLNP6kZ8AuXQr36EDAkvG9CNJSPKc1@thread.tacv2"
    }
  },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
Server: Microsoft-HTTPAPI/2.0
MS-CV: 1juAJRUj5ki4igxWv3Y8EQ.0
Content-Type: application/json; charset=utf-8
Content-Length: 85

{
  "error": {
    "code": "BadSyntax",
    "message": "Incorrect conversation creation parameters"
  }
}
```

**Conclusion:** `activity` is mandatory for channel thread creation. You cannot create an empty thread.

---

## Group Chat (NOT SUPPORTED)

All of the following variations return the same `400 BadSyntax` error. The `MS-CV` header is included for each to enable service-side log correlation.

### 2 members, no bot, no channelData

```http
{
  "isGroup": true,
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" },
    { "id": "29:100DQ6CrcJc9p_L654DvdNtwAazXhnxkoNAedgV0ZAgalPOz0oy7RmLG0VKCPhdia_w0lJJLUp0QEw6ogU7zyWg" }
  ],
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: /qe9JFWupEGpNA9S/vIXaQ.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

### 2 members, with bot

```http
{
  "isGroup": true,
  "bot": { "id": "28:3738fe3d-bca2-479d-8e45-1660de89ee41" },
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" },
    { "id": "29:100DQ6CrcJc9p_L654DvdNtwAazXhnxkoNAedgV0ZAgalPOz0oy7RmLG0VKCPhdia_w0lJJLUp0QEw6ogU7zyWg" }
  ],
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: Sm3G0wjzV0y2EKXpeJu7jQ.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

### 2 members, bot, channelData.tenant

```http
{
  "isGroup": true,
  "bot": { "id": "28:3738fe3d-bca2-479d-8e45-1660de89ee41" },
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" },
    { "id": "29:100DQ6CrcJc9p_L654DvdNtwAazXhnxkoNAedgV0ZAgalPOz0oy7RmLG0VKCPhdia_w0lJJLUp0QEw6ogU7zyWg" }
  ],
  "channelData": { "tenant": { "id": "3f3d1cea-7a18-41af-872b-cfbbd5140984" } },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: lmTBZpEylUeAiMTGSPnpVQ.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

### 2 members, bot, topic, activity, channelData (all fields)

```http
{
  "isGroup": true,
  "bot": { "id": "28:3738fe3d-bca2-479d-8e45-1660de89ee41" },
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" },
    { "id": "29:100DQ6CrcJc9p_L654DvdNtwAazXhnxkoNAedgV0ZAgalPOz0oy7RmLG0VKCPhdia_w0lJJLUp0QEw6ogU7zyWg" }
  ],
  "topicName": "Diagnostic group test",
  "activity": { "type": "message", "text": "group chat init" },
  "channelData": { "tenant": { "id": "3f3d1cea-7a18-41af-872b-cfbbd5140984" } },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: zkVf7eA6BEytpPgWI8KH9Q.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

### 1 member, isGroup=true

```http
{
  "isGroup": true,
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" }
  ],
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: yP2h6kv4iUG08nVbdcQJ0g.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

### 1 member, bot, channelData.tenant

```http
{
  "isGroup": true,
  "bot": { "id": "28:3738fe3d-bca2-479d-8e45-1660de89ee41" },
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" }
  ],
  "channelData": { "tenant": { "id": "3f3d1cea-7a18-41af-872b-cfbbd5140984" } },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: 10bRBNHyxk+eigCT8saVDg.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

### 3 members, bot, channelData.tenant

```http
{
  "isGroup": true,
  "bot": { "id": "28:3738fe3d-bca2-479d-8e45-1660de89ee41" },
  "members": [
    { "id": "29:1aK9mYhSZ3egG5Ve2UaOoEjrOppWz-gl7AQmsXeW-4XS1et5FiZ3_V45othuWHgfY0Ytv82M6WnH8lRI8gLMeHg" },
    { "id": "29:100DQ6CrcJc9p_L654DvdNtwAazXhnxkoNAedgV0ZAgalPOz0oy7RmLG0VKCPhdia_w0lJJLUp0QEw6ogU7zyWg" },
    { "id": "29:1wh0NxivaCTCGl7pmILex0arFbszG6RaKMMOXImiDOCu3-T1qzkGdsmA_AfFpawkDaQl0kfvVy9RkVWQNGl30-w" }
  ],
  "channelData": { "tenant": { "id": "3f3d1cea-7a18-41af-872b-cfbbd5140984" } },
  "tenantId": "3f3d1cea-7a18-41af-872b-cfbbd5140984"
}
```

```http
HTTP/1.1 400 Bad Request
MS-CV: gfacBHOnI0CQ+n6Nxim+1w.0

{ "error": { "code": "BadSyntax", "message": "Incorrect conversation creation parameters" } }
```

---

## Summary Table

| Scenario | `isGroup` | `channelData.channel.id` | `activity` | `members` | HTTP | Result |
|---|---|---|---|---|---|---|
| 1:1 personal chat | `false` | — | optional | 1 (required) | **201** | Conversation created |
| 1:1 with bot | `false` | — | optional | 1 (required) | **201** | Conversation created |
| 1:1 with initial activity | `false` | — | message | 1 (required) | **201** | Conversation + message |
| Channel thread | `true` | required | **required** | optional | **201** | Thread created |
| Channel thread + members | `true` | required | **required** | ignored | **201** | Thread created |
| Channel thread, no activity | `true` | required | — | — | **400** | BadSyntax |
| Group: any member count | `true` | — | any | 1-3 | **400** | BadSyntax |
| Group: with bot | `true` | — | any | 1-3 | **400** | BadSyntax |
| Group: all fields | `true` | — | message | 2 | **400** | BadSyntax |

## Response Headers

Common response headers across all requests:

| Header | Description |
|---|---|
| `Server` | Always `Microsoft-HTTPAPI/2.0` |
| `MS-CV` | Correlation vector for service-side log tracing |
| `Content-Type` | Always `application/json; charset=utf-8` |
| `Date` | Server-side timestamp |
| `Content-Length` | Response body size |

The `MS-CV` header is the key diagnostic value — it can be used to correlate with Teams service-side logs for deeper investigation of `BadSyntax` failures.

## Key Observations

1. **Member ID format matters.** The API requires MRI-format IDs (`29:1aK9...`), not the pairwise bot framework IDs stored in `TEST_USER_ID` env vars (`29:guid`). MRI IDs can be obtained from `GET /v3/conversations/{id}/members`.

2. **1:1 conversations are idempotent.** Calling CreateConversation with the same member always returns the same conversation ID (`a:...` prefix).

3. **Channel threads require an activity.** You cannot create an empty thread — the initial message IS the thread.

4. **Group chat creation is a platform limitation.** The `POST /v3/conversations` endpoint does not support creating multi-user group chats. The error is always `BadSyntax: Incorrect conversation creation parameters` regardless of parameter combinations. This applies to the Teams channel (msteams) specifically — other Bot Framework channels may behave differently.

5. **`tenantId` is required** for all Teams conversation creation. Omitting it causes auth failures.

6. **`bot` field is optional.** The API infers the bot identity from the bearer token for 1:1 chats.
