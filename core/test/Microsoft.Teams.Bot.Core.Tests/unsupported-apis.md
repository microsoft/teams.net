# Unsupported Bot Framework APIs in Teams

APIs that return errors when called against the Teams Bot Framework service (`smba.trafficmanager.net`). These are documented in the Bot Framework v3 REST API but appear unsupported or behave differently in Teams.

**Service URL tested:** `https://smba.trafficmanager.net/teams/`
**Date tested:** 2026-04-15
**SDK:** Microsoft.Teams.Bot.Core (v0.0.1-alpha)

---

## 1. GET /v3/conversations — MethodNotAllowed

**Endpoint:** `GET {serviceUrl}/v3/conversations`
**Expected behavior:** Returns a list of conversations the bot is part of.
**Actual response:**
```
HTTP 405 Method Not Allowed
{"message":"The requested resource does not support http method 'GET'."}
```
**Question:** Is this endpoint supported in Teams? If not, what is the recommended alternative for listing conversations?

---

## 2. DELETE /v3/conversations/{conversationId}/members/{memberId} — MethodNotAllowed

**Endpoint:** `DELETE {serviceUrl}/v3/conversations/{conversationId}/members/{memberId}`
**Expected behavior:** Removes a member from a conversation.
**Actual response:**
```
HTTP 405 Method Not Allowed
{"message":"The requested resource does not support http method 'DELETE'."}
```
**Context:** Tested with a channel conversation ID (`19:...@thread.tacv2`) and a valid pairwise MRI member ID.
**Question:** Is member removal supported via BF API in Teams? Is there a different endpoint or method?

---

## 3. POST /v3/conversations/{conversationId}/history — BadRequest

**Endpoint:** `POST {serviceUrl}/v3/conversations/{conversationId}/history`
**Expected behavior:** Sends a transcript of historical activities to a conversation.
**Actual response:**
```
HTTP 400 Bad Request
{"error":{"code":"BadArgument","message":"Unknown activity type"}}
```
**Context:** Sent a transcript containing activities with `"type": "message"`. The activities are `CoreActivity` instances serialized via `System.Text.Json`. The `type` field serializes as the enum string value.
**Question:**
- Is the `SendConversationHistory` endpoint supported in Teams?
- If so, what activity type values are accepted? Is there a specific serialization format required?
- Could the issue be that additional required fields (e.g., `timestamp`, `from`) are missing from the transcript activities?

---

## 4. POST /v3/conversations/{conversationId}/attachments — NotFound

**Endpoint:** `POST {serviceUrl}/v3/conversations/{conversationId}/attachments`
**Expected behavior:** Uploads an attachment to a conversation's blob storage.
**Request body:**
```json
{
  "type": "text/plain",
  "name": "test-attachment.txt",
  "originalBase64": "<base64-encoded-content>"
}
```
**Actual response:**
```
HTTP 404 Not Found
```
**Question:** Is the attachment upload endpoint available in Teams? If not, what is the recommended way to share files via the bot API?

---

## 5. POST /v3/conversations (group creation) — BadRequest

**Endpoint:** `POST {serviceUrl}/v3/conversations`
**Request body:**
```json
{
  "isGroup": true,
  "members": [
    { "id": "29:<pairwise-mri-1>" },
    { "id": "29:<pairwise-mri-2>" }
  ],
  "tenantId": "<tenant-id>"
}
```
**Expected behavior:** Creates a group conversation with the specified members.
**Actual response:**
```
HTTP 400 Bad Request
{"error":{"code":"BadSyntax","message":"Incorrect conversation creation parameters"}}
```
**Context:** 1:1 conversation creation (`isGroup: false`, single member) works. Group creation with `isGroup: true` fails. Also fails with `topicName` set.
**Question:** Is group conversation creation supported via BF API in Teams? What parameters are required?

---

## 6. POST /v3/conversations (with topicName) — BadRequest

**Endpoint:** `POST {serviceUrl}/v3/conversations`
**Request body:**
```json
{
  "isGroup": true,
  "topicName": "Test Conversation - 2026-04-15T20:00:00",
  "members": [{ "id": "29:<pairwise-mri>" }],
  "tenantId": "<tenant-id>"
}
```
**Actual response:**
```
HTTP 400 Bad Request
{"error":{"code":"BadSyntax","message":"Incorrect conversation creation parameters"}}
```
**Question:** Same as above — are `isGroup` and `topicName` supported?

---

## Summary

| Endpoint | Method | Status | Error |
|---|---|---|---|
| `/v3/conversations` | GET | 405 | MethodNotAllowed |
| `/v3/conversations/{id}/members/{memberId}` | DELETE | 405 | MethodNotAllowed |
| `/v3/conversations/{id}/history` | POST | 400 | Unknown activity type |
| `/v3/conversations/{id}/attachments` | POST | 404 | NotFound |
| `/v3/conversations` (isGroup: true) | POST | 400 | Incorrect conversation creation parameters |
| `/v3/conversations` (topicName) | POST | 400 | Incorrect conversation creation parameters |
