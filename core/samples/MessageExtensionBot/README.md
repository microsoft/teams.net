# MessageExtensionBot Testing Guide

A sample bot demonstrating Teams message extension handlers.

## Setup

1. Configure bot credentials in `appsettings.json` or environment variables
2. Run the bot: `dotnet run`
3. Upload `manifest.json` to Teams

## Testing Handlers

### OnQuery (Search)
**Manifest:** `composeExtensions.commands` with `type: "query"`

1. Open message compose box
2. Select the message extension
3. Type a search term
4. Verify results display in list format
5. Type "help" to test message response

### OnSelectItem
**Manifest:** No specific requirement (works with OnQuery results)

1. After running a search (OnQuery)
2. Click on any search result
3. Verify adaptive card preview appears

### OnFetchTask (Action - Task Module)
**Manifest:** `composeExtensions.commands` with `type: "action"` and `fetchTask: true`

1. Click the message extension action button (createAction)
2. Verify task module opens with input form

### OnSubmitAction (Action Submit)
**Manifest:** No specific requirement (works with OnFetchTask)

1. Fill form in task module
2. Click submit
3. Verify preview card appears with Edit/Send buttons
4. Click Edit - verify form reopens with values
5. Click Send - verify final card posts to conversation -- Currently this only works when we start from commandbox.

### OnQueryLink (Link Unfurling)
**Manifest:** `composeExtensions.messageHandlers` with `type: "link"` and `domains`

1. Paste a URL in compose box that matches the unfurl domain in manifest (*.example.com)
2. Verify card unfurls automatically

### OnQuerySettingUrl (Settings)
**Manifest:** `composeExtensions.canUpdateConfiguration: true`

1. Right-click message extension icon
2. Select Settings
3. Verify settings URL opens (microsoft.com)
