# NullReferenceException Investigation - CompatConversations.SendToConversationWithHttpMessagesAsync

**Date:** 2026-04-08
**Issue:** NullReferenceException in `CompatConversations.SendToConversationWithHttpMessagesAsync`
**Error Message:** "Object reference not set to an instance of an object"

## Context

Exception occurs when ProjectAgentBot.cs calls `SendToConversationAsync` to send an OAuth SSO card:

```csharp
IMessageActivity activity = MessageFactory.Attachment(oAuthSSOAdaptiveCard.ToAttachment());
activity.Recipient = userChannel;
activity.Conversation = new ConversationAccount { Id = conversationId };
await connectorClient.Conversations.SendToConversationAsync((Activity)activity, cancellationToken);
```

## Exception Details

```
OuterType: System.NullReferenceException
OuterAssembly: System.Private.CoreLib, Version=8.0.0.0
OuterMethodSource: Microsoft.Teams.Bot.Compat
Stack Trace:
  at Microsoft.Teams.Bot.Compat.CompatConversations.SendToConversationWithHttpMessagesAsync(String conversationId, Activity activity, Dictionary`2 customHeaders, CancellationToken cancellationToken)
  at Microsoft.Bot.Connector.ConversationsExtensions.SendToConversationAsync(...)
  at ProjectAgentBot.SendOAuthCardForSSO(...) line 1263
```

## Investigation Steps

### Phase 1: Root Cause Hypothesis - FromCompatActivity Returns Null

**Hypothesis:** `activity.FromCompatActivity()` returns null, causing subsequent null dereference.

**Investigation:**
- Examined `FromCompatActivity()` method - serializes activity to JSON then calls `CoreActivity.FromJsonString()`
- Found `FromJsonString()` uses null-forgiving operator `!` which masks potential null return from `JsonSerializer.Deserialize()`

**Tests Created:**
1. `FromCompatActivity_MessageFactoryAttachment_ReturnsNonNull` - ✅ PASSED
2. `FromCompatActivity_MinimalActivity_ReturnsNonNull` - ✅ PASSED
3. `FromCompatActivity_ActivityWithOnlyAttachment_ReturnsNonNull` - ✅ PASSED
4. `FromCompatActivity_ExactProjectAgentScenario_ConversationNotNull` - ✅ PASSED

**Result:** `FromCompatActivity()` does NOT return null in test scenarios.

### Phase 2: ServiceUrl Null Check

**Hypothesis:** If `ServiceUrl` is null, `ConversationClient.SendActivityAsync()` validation would fail.

**Investigation:**
- `ConversationClient.SendActivityAsync()` line 49: `ArgumentNullException.ThrowIfNull(activity.ServiceUrl)`
- This throws `ArgumentNullException`, NOT `NullReferenceException`

**Test Created:**
- `SendToConversationWithHttpMessagesAsync_WithNullServiceUrl_ThrowsArgumentNullException` - ✅ PASSED
  - Confirms throws `ArgumentNullException` with parameter "act.ServiceUrl"

**Result:** Null ServiceUrl causes `ArgumentNullException`, not `NullReferenceException`.

### Phase 3: Conversation Property Investigation

**Hypothesis:** Issue with `Conversation` object initialization using primary constructor and object initializer.

**Investigation:**
- `Conversation` class uses primary constructor: `public class Conversation(string id = "")`
- Line 318: `coreActivity.Conversation ??= new Microsoft.Teams.Bot.Core.Schema.Conversation { Id = conversationId };`

**Tests Created:**
- Tested null conversationId - ✅ PASSED (Conversation.Id correctly set to null)
- Tested Conversation property access - ✅ PASSED (no NullReferenceException)

**Result:** Conversation initialization works correctly even with null conversationId.

### Phase 4: GetAgenticIdentity Investigation

**Hypothesis:** `ConversationClient.SendActivityAsync` line 79 calls `activity.From?.GetAgenticIdentity()`, which might throw if `Properties` is null.

**Investigation:**
- `GetAgenticIdentity()` accesses `Properties.TryGetValue(...)` without null check
- If `Properties` is null → NullReferenceException

**Test Created:**
- `FromCompatActivity_PreservesFromAccountProperties` - ✅ PASSED
  - Properties correctly initialized after conversion
  - `GetAgenticIdentity()` doesn't throw

**Result:** Properties dictionary properly initialized during deserialization.

### Phase 5: Missing From Property

**Hypothesis:** `MessageFactory.Attachment()` doesn't set `From` property, leading to null `coreActivity.From`.

**Investigation:**
- ProjectAgentBot doesn't set `From` on the activity
- Line 79 uses null-conditional operator: `activity.From?.GetAgenticIdentity()` (safe)

**Test Created:**
- `FromCompatActivity_WithoutFromProperty_DoesNotCrash` - ✅ PASSED
  - Confirmed `coreActivity.From` is null when not set
  - This is handled safely with `?.` operator

**Result:** Null `From` is handled correctly.

## What We've Ruled Out

1. ✅ `FromCompatActivity()` returning null
2. ✅ `coreActivity.Conversation` being null after conversion
3. ✅ Null `conversationId`
4. ✅ Null `ServiceUrl` (would throw ArgumentNullException, not NullReferenceException)
5. ✅ `ConvertHeaders` with null input
6. ✅ Null `coreActivity.From` (handled with `?.` operator)
7. ✅ Null `coreActivity.From.Properties` (properly initialized)

## Most Likely Causes (Untested)

### 1. _client (ConversationClient) is Null
Despite confidence that it's set, if `_client` is null, line 320 would throw NullReferenceException:
```csharp
SendActivityResponse response = await _client.SendActivityAsync(...);
```

### 2. OAuth Card Attachment Data Issue
Something specific in the OAuth card's attachment content causes serialization/deserialization to fail in a way that creates a null reference.

### 3. SetAppIdOnChannelData Side Effect
```csharp
this.botUtilities.SetAppIdOnChannelData(activity, turnContext);
```
This might modify the activity in a way that breaks subsequent serialization.

### 4. .NET Version Difference ❌ **RULED OUT**
- **Production:** .NET 8.0 (from stack trace: `System.Private.CoreLib, Version=8.0.0.0`)
- **Tests:** Originally .NET 10.0, retested on .NET 8.0
- **Result:** All 47 tests pass on .NET 8.0 - no behavioral difference found
- Primary constructor and JSON serialization work identically on both versions

## ROOT CAUSE IDENTIFIED ✅

**Issue:** `ConversationClient.SendActivityAsync` can return `null` despite non-nullable return type due to null-forgiving operator `!` on line 80 of `ConversationClient.cs`:

```csharp
return (await _botHttpClient.SendAsync<SendActivityResponse>(
    HttpMethod.Post, url, body,
    CreateRequestOptions(...), cancellationToken
).ConfigureAwait(false))!;  // ← Null-forgiving operator masks potential null
```

When `SendActivityAsync` returns `null`, `CompatConversations.SendToConversationWithHttpMessagesAsync` line 324 throws `NullReferenceException`:

```csharp
ResourceResponse resourceResponse = new()
{
    Id = response.Id  // ← NullReferenceException if response is null!
};
```

### When BotHttpClient.SendAsync Returns Null

From `BotHttpClient.HandleResponseAsync` analysis:

1. **HTTP 404 Response** (line 179-182 of BotHttpClient.cs):
   ```csharp
   if (response.StatusCode == HttpStatusCode.NotFound && options.ReturnNullOnNotFound)
   {
       return default;  // Returns null
   }
   ```

2. **Empty/Invalid Response Body** (line 206-208 of BotHttpClient.cs):
   ```csharp
   if (string.IsNullOrWhiteSpace(responseString) || responseString.Length <= 2)
   {
       return default;  // Returns null
   }
   ```

### Test Confirmation

✅ **Successfully reproduced NullReferenceException** with test:
- `SendToConversationWithHttpMessagesAsync_WhenSendActivityReturnsNull_ThrowsNullReferenceException`
- Confirms that null response from `SendActivityAsync` causes exact exception seen in production

## Recommended Next Steps

### 1. APX Response Analysis ✅ **CONFIRMED**

**Production logs show APX returned HTTP 499** (Client Closed Request)

**APX (BotFrontEndRole) Response Behavior:**

From `ConversationsController.SendToConversationActivity` (lines 377-380):
```csharp
resource = resources?.Count == 1 ? resources.First() : null;
success = resource != null
    ? Request.CreateResponse(HttpStatusCode.Created, resource, ...)  // 201 with Resource body
    : Request.CreateResponse(HttpStatusCode.Accepted);  // 202 with NO body
```

**When APX returns empty body:**
- HTTP 202 Accepted when `resources` is null/empty/multiple
- HTTP 499 (client closed connection before completion)
- Any response with empty/minimal body (≤ 2 characters)

**Why BotHttpClient returns null:**

`BotHttpClient.HandleResponseAsync` → `DeserializeResponseAsync` (line 206-208):
```csharp
if (string.IsNullOrWhiteSpace(responseString) || responseString.Length <= 2)
{
    return default;  // Returns null for empty responses!
}
```

**Complete Flow:**
1. ProjectAgent sends OAuth card via `SendToConversationAsync`
2. Request goes to APX `/v3/conversations/{id}/activities`
3. APX's `ConversationService.CreateActivity` returns null/empty `resources`
4. APX returns HTTP 202 or 499 with **empty body**
5. BotHttpClient sees empty body → returns `null`
6. ConversationClient has `!` operator → returns `null` despite non-nullable type
7. CompatConversations accesses `response.Id` → **NullReferenceException**

### 2. Fix CompatConversations.SendToConversationWithHttpMessagesAsync

Add null check after receiving response:

```csharp
SendActivityResponse response = await _client.SendActivityAsync(coreActivity, convertedHeaders, cancellationToken).ConfigureAwait(false);

// Add this null check
if (response == null)
{
    throw new InvalidOperationException(
        $"SendActivityAsync returned null. " +
        $"ConversationId: {conversationId}, " +
        $"ActivityType: {activity.Type}, " +
        $"ServiceUrl: {ServiceUrl}");
}

ResourceResponse resourceResponse = new()
{
    Id = response.Id
};
```

### 3. Fix ConversationClient.SendActivityAsync

Remove the null-forgiving operator and add proper null handling:

```csharp
// Line 75-80: Change from
return (await _botHttpClient.SendAsync<SendActivityResponse>(
    HttpMethod.Post, url, body,
    CreateRequestOptions(activity.From?.GetAgenticIdentity(), "sending activity", customHeaders),
    cancellationToken).ConfigureAwait(false))!;  // ← Remove !

// To:
SendActivityResponse? response = await _botHttpClient.SendAsync<SendActivityResponse>(
    HttpMethod.Post, url, body,
    CreateRequestOptions(activity.From?.GetAgenticIdentity(), "sending activity", customHeaders),
    cancellationToken).ConfigureAwait(false);

if (response == null)
{
    throw new HttpRequestException(
        $"SendActivityAsync received null response from {url}. " +
        $"This may indicate an empty response body or 404 status.");
}

return response;
```

### 4. Review BotRequestOptions

Check if `ReturnNullOnNotFound` is being set to `true` unintentionally in `CreateRequestOptions` method.

## Summary

- **Root Cause:** APX returns null/empty response → BotHttpClient returns null → CompatConversations crashes
- **Fix Location:** CompatConversations.cs line 320-324
- **Prevention:** Remove null-forgiving operator from ConversationClient.cs line 80
- **Investigation:** Check APX service for HTTP 404 or empty response bodies

### Immediate Actions Required:

1. **Add Diagnostic Logging** ⚠️ **CRITICAL**
   Add temporary logging to capture exact null reference location:
   ```csharp
   public async Task<HttpOperationResponse<ResourceResponse>> SendToConversationWithHttpMessagesAsync(...)
   {
       try
       {
           logger.LogInformation("[1] Entered method");
           logger.LogInformation($"[2] activity is null: {activity == null}");
           logger.LogInformation($"[3] _client is null: {_client == null}");
           logger.LogInformation($"[4] conversationId is null: {conversationId == null}");

           Dictionary<string, string>? convertedHeaders = ConvertHeaders(customHeaders);
           logger.LogInformation("[5] ConvertHeaders completed");

           CoreActivity coreActivity = activity.FromCompatActivity();
           logger.LogInformation($"[6] FromCompatActivity completed, result is null: {coreActivity == null}");
           logger.LogInformation($"[7] ServiceUrl: {ServiceUrl ?? "NULL"}");

           // ... rest of method with logging after each line
       }
       catch (NullReferenceException ex)
       {
           logger.LogError($"NullRef caught. Last logged step indicates failure point.");
           throw;
       }
   }
   ```

2. **Capture Production Activity JSON**
   Before calling SendToConversationAsync, serialize and log the activity:
   ```csharp
   var activityJson = JsonSerializer.Serialize(activity);
   logger.LogInformation($"Activity JSON: {activityJson}");
   ```

3. **Verify ConnectorClient State**
   Check if connectorClient and its Conversations property are properly initialized:
   ```csharp
   logger.LogInformation($"connectorClient is null: {connectorClient == null}");
   logger.LogInformation($"connectorClient.Conversations is null: {connectorClient?.Conversations == null}");
   if (connectorClient is CompatBotAdapter adapter)
   {
       logger.LogInformation($"CompatConversations._client is null: {adapter._client == null}");
   }
   ```

4. **Inspect SetAppIdOnChannelData**
   Log activity state before and after the SetAppIdOnChannelData call:
   ```csharp
   logger.LogInformation($"Activity before SetAppIdOnChannelData: {JsonSerializer.Serialize(activity)}");
   this.botUtilities.SetAppIdOnChannelData(activity, turnContext);
   logger.LogInformation($"Activity after SetAppIdOnChannelData: {JsonSerializer.Serialize(activity)}");
   ```

## Code Locations

- **CompatConversations.cs:** `C:\Users\kavinsingh\SOURCE\REPOS\teams.net\core\src\Microsoft.Teams.Bot.Compat\CompatConversations.cs`
  - Method: `SendToConversationWithHttpMessagesAsync` (lines 305-332)
  - Problematic line likely: 309, 312, 318, or 320

- **ProjectAgentBot.cs:** `C:\Users\kavinsingh\source\repos\Teams-Graph\Src\TeamsBotService\GroupCopilotBot\Partners\ProjectAgent\Bots\ProjectAgentBot.cs`
  - Calling code: line 1263

- **ConversationClient.cs:** `C:\Users\kavinsingh\SOURCE\REPOS\teams.net\core\src\Microsoft.Teams.Bot.Core\ConversationClient.cs`
  - Method: `SendActivityAsync` (lines 44-81)
  - Potential issue: line 73 `activity.ToJson()` or line 79 `activity.From?.GetAgenticIdentity()`

## Test Files

- **CompatActivityTests.cs** - Activity conversion tests
- **CompatConversationsTests.cs** - SendToConversation scenario tests

All tests pass on .NET 10.0 - issue may be .NET 8 specific.
