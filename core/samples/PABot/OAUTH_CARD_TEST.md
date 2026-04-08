# OAuth Card NullReferenceException Test Scenario

## Purpose
Reproduce the NullReferenceException that occurs when sending OAuth SSO cards via `SendToConversationAsync` when APX returns HTTP 202 Accepted with an empty response body.

## Background
**Issue**: When APX returns HTTP 202 Accepted with empty body (for ephemeral OAuth SSO activities), the BotHttpClient returns null, causing a NullReferenceException in CompatConversations.SendToConversationWithHttpMessagesAsync line 324 when trying to access `response.Id`.

**Root Cause Flow**:
1. OAuth SSO card sent via `MessageFactory.Attachment()`
2. APX treats it as ephemeral → returns 202 Accepted with NO body
3. `BotHttpClient.DeserializeResponseAsync` sees empty body → returns `null`
4. `ConversationClient.SendActivityAsync` has `!` operator → returns `null`
5. `CompatConversations` accesses `response.Id` → **NullReferenceException**

## Test Scenario Added

### Location
`Bots/SsoBot.cs` - `TestOAuthCardSendScenario()` method

### What It Does
Mimics the exact scenario from ProjectAgentBot.SendOAuthCardForSSO and OAuthPrompt:
1. Gets sign-in resource from token service via `UserTokenAccess.GetSignInResourceAsync()`:
   - Returns `SignInLink`
   - Returns `TokenExchangeResource` (with URI and ID)
   - Returns `TokenPostResource`
2. Creates proper **SSO OAuth card** with:
   - `ConnectionName` (from config)
   - `TokenExchangeResource` (from token service)
   - `TokenPostResource` (from token service)
   - Sign-in button with `SignInLink` from token service
3. Creates activity using `MessageFactory.Attachment(oAuthSsoCard.ToAttachment())`
4. Sets `Recipient` and `Conversation` properties
5. Calls `connectorClient.Conversations.SendToConversationAsync((Activity)activity, cancellationToken)`
6. Catches and reports `NullReferenceException` if it occurs

### Code Path
```
SsoBot.TestOAuthCardSendScenario()
  → connectorClient.Conversations.SendToConversationAsync()
    → ConversationsExtensions (Bot Framework SDK)
      → CompatConversations.SendToConversationWithHttpMessagesAsync()
        → ConversationClient.SendActivityAsync()
          → BotHttpClient.SendAsync<SendActivityResponse>()
            → APX BotFrontEndRole ConversationsController.SendToConversationActivity()
```

## How to Run

### 1. Start PABot
```bash
cd C:\Users\kavinsingh\source\repos\teams.net\core\samples\PABot
dotnet run
```

### 2. Configure Bot
Update `appsettings.json` with your OAuth connection name:
```json
{
  "ConnectionName": "YourSsoConnectionName"
}
```

**Note**: The `SignInLink` and `TokenExchangeResource` are automatically retrieved from the Bot Framework Token Service via `UserTokenAccess.GetSignInResourceAsync()`. You do NOT need to configure them manually.

Ensure PABot is also configured with:
- Valid bot credentials (MsalBot/MsalAgent sections)
- OAuth connection configured in Azure Bot Service with the ConnectionName
- Proper APX endpoint configuration
- CompatBotAdapter registered (should already be set up via `AddTeamsBotApplications()`)

### 3. Trigger Test
In Teams or Bot Framework Emulator, send message to bot:
```
test oauth card
```

### 4. Expected Results

**If bug reproduces**:
```
Testing OAuth card send scenario...
❌ NullReferenceException caught! This is the bug we're investigating.
Message: Object reference not set to an instance of an object.
StackTrace: at Microsoft.Teams.Bot.Compat.CompatConversations.SendToConversationWithHttpMessagesAsync(...)
```

**If bug is fixed**:
```
Testing OAuth card send scenario...
✅ SUCCESS! Response ID: <some-id-or-NULL>
```

## Investigation Results

### HTTP 202 Accepted Scenarios
APX returns 202 Accepted with empty body when:
1. OAuth SSO cards (treated as ephemeral activities)
2. `ConversationService.CreateActivity` returns null
3. Multiple resources or no resources created

### HTTP 499 Scenarios
Client Closed Request - connection timeout or cancellation before response completed

## Related Files

### Source Code
- `core/src/Microsoft.Teams.Bot.Compat/CompatConversations.cs` (line 324)
- `core/src/Microsoft.Teams.Bot.Core/ConversationClient.cs` (line 80 - null-forgiving operator)
- `core/src/Microsoft.Teams.Bot.Core/Http/BotHttpClient.cs` (line 206-208 - returns null for empty body)

### APX Code
- `async_messaging_botapiservice/BotFrontEnd.Library/Controllers/ConversationsController.cs` (lines 377-380)
- `async_messaging_botapiservice/Library/Services/ConversationService.cs` (lines 638-642, 665-668)

### Investigation Document
`core/docs/debugging/null-reference-investigation-2026-04-08.md`

## Next Steps

1. **Run this test** to confirm the NullReferenceException reproduces
2. **Check logs** for actual HTTP status code (202 or 499)
3. **Implement fix** based on findings:
   - Remove null-forgiving operator in ConversationClient
   - Add proper null handling in CompatConversations
   - Throw meaningful exceptions for empty response bodies

## Notes

- This test requires real APX connectivity
- The bug only occurs when APX returns 202/499 with empty body
- May not reproduce in all environments (depends on APX configuration/flighting)
- Check APX logs to see what `ConversationService.CreateActivity` returned
