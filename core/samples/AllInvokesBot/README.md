# AllInvokesBot Testing Guide

A sample bot demonstrating Teams invoke handlers.

## Setup

1. Configure bot credentials in `appsettings.json` or environment variables
2. Run the bot: `dotnet run`
3. Upload `manifest.json` to Teams

## Testing Handlers

### OnMessage
**Manifest:** `bots` section with appropriate `scopes` (personal, team, groupChat)

1. Send any message to the bot in 1:1 chat
2. Verify welcome card with action buttons appears

### OnAdaptiveCardAction
**Manifest:** No specific requirement (triggered by adaptive card actions)

1. After receiving the welcome card
2. Click any action button on the card
3. Verify action response card appears
4. Console logs will show the verb and data

**File Upload Flow:**
1. Click "Request File Upload" button
2. Verify file consent card appears

### OnFileConsent
**Manifest:** `bots.supportsFiles: true`
**Azure:** Delegated permission `Files.ReadWrite.All` required in Azure app registration

1. After requesting file upload (see above)
2. Click Accept or Decline on the file consent card
3. If Accept - verify file uploads and file info card appears
4. If Decline - verify console logs the decline action

### OnTaskFetch
**Manifest:** No specific requirement (triggered by task module actions)

1. Click "Open Task Module" button on the welcome card
2. Verify task module dialog opens with input form

### OnTaskSubmit
**Manifest:** No specific requirement (works with OnTaskFetch)

1. Open task module (see OnTaskFetch)
2. Fill in the form
3. Click submit
4. Verify "Done" message appears
