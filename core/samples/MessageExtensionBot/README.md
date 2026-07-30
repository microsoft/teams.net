# MessageExtensionBot

A sample bot demonstrating Teams message extension handlers.

## Prerequisites

- Bot registered and installed in Teams.
- Manifest package created from the inline manifest plus `color.png` / `outline.png`.

## Manifest Setup

```json
{
  "bots": [
    {
      "botId": "YOUR_BOT_ID",
      "scopes": [
        "personal",
        "team",
        "groupChat"
      ],
      "isNotificationOnly": false,
      "supportsCalling": false,
      "supportsVideo": false,
      "supportsFiles": false
    }
  ],
  "composeExtensions": [
    {
      "botId": "YOUR_BOT_ID",
      "canUpdateConfiguration": true,
      "commands": [
        {
          "id": "searchQuery",
          "type": "query",
          "title": "searchQuery",
          "description": "Enter search text",
          "initialRun": true,
          "fetchTask": false,
          "context": [
            "commandBox",
            "compose",
            "message"
          ],
          "parameters": [
            {
              "name": "searchText",
              "title": "searchText",
              "description": "Enter search text",
              "inputType": "text"
            }
          ]
        },
        {
          "id": "createAction",
          "type": "action",
          "title": "createAction",
          "description": "Create a new item",
          "initialRun": true,
          "fetchTask": true,
          "context": [
            "commandBox",
            "compose",
            "message"
          ],
          "parameters": [
            {
              "name": "createAction",
              "title": "createAction",
              "description": "Create a new item",
              "inputType": "text"
            }
          ]
        }
      ],
      "messageHandlers": [
        {
          "type": "link",
          "value": {
            "domains": [
              "*.example.com",
              "*.microsoft.com"
            ],
            "supportsAnonymizedPayloads": true
          }
        }
      ]
    }
  ],
   "validDomains": [
    "*.botframework.com",
    "xxx.devtunnels.ms"
  ]
}
```

## What it shows

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


### OnCardButtonClicked (Card Button)
**Manifest:** No specific requirement (works with any message extension result card that has `Action` buttons)

1. Click the **View Details** button on the adaptive card
2. Verify `OnCardButtonClicked` fires — link opens)


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
5. Click Send - verify final card posts to conversation — currently only works when started from the command box

### OnQueryLink (Link Unfurling)
**Manifest:** `composeExtensions.messageHandlers` with `type: "link"` and `domains`

1. Paste a URL in compose box that matches the unfurl domain in manifest (*.example.com)
2. Verify card unfurls automatically

### OnQuerySettingUrl + OnSettings (Settings)
**Manifest:** `composeExtensions.canUpdateConfiguration: true` (must be at the top level of the compose extension, not inside a command)

> **Important:** Add your bot's domain to `validDomains` in the manifest, otherwise Teams will block the settings page from loading in the iframe:
> ```json
> "validDomains": [
>   "YOUR_DEVTUNNEL_SOMAIN"
> ]
> ```

1. Right-click the message extension icon in the compose box
2. Select **Settings** — Teams calls `OnQuerySettingUrl` which returns the settings page URL
3. A popup opens at `{BOT_ENDPOINT}/tabs/settings`
4. Select an option and click **Save Settings** — Teams calls `OnSettings` with `Value.State` set to the submitted value
5. If the user dismisses the dialog, `Value.State` will be `"CancelledByUser"`

## Running the Sample

~~~bash
dotnet run --project samples/MessageExtensionBot/MessageExtensionBot.csproj
~~~
