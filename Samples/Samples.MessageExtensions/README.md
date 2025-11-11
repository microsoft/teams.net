# Message Extensions Sample

This sample demonstrates how to implement message extensions (also known as messaging extensions) in Microsoft Teams using the Teams AI SDK for .NET. Message extensions allow users to interact with your bot directly from the compose area or from search in Teams.

## Features

This sample includes comprehensive message extension functionality:

- **Search-based message extensions** - Query and return results
- **Action-based message extensions** - Create cards from user input
- **Link unfurling** - Automatically unfurl links in conversations
- **Settings configuration** - Configure message extension settings
- **Task modules** - Display interactive forms
- **Message actions** - Get details from existing messages

## Prerequisites

- .NET 9.0 or later
- Azure Bot Service registration
- Dev tunnels or ngrok for local development
- Microsoft Teams (desktop or web client)
- Microsoft 365 tenant with Teams enabled

## Project Structure

```
Samples.MessageExtensions/
├── Program.cs                          # Main bot logic and message extension handlers
├── Samples.MessageExtensions.csproj   # Project file with SDK dependencies
├── appsettings.json                   # Bot credentials configuration
├── Properties/launchSettings.json     # Launch configuration (port 3978)
└── README.md                          # This file
```

## Setup

### 1. Azure Bot Registration

1. Navigate to the [Azure Portal](https://portal.azure.com)
2. Create an **Azure Bot** resource:
   - Click "Create a resource"
   - Search for "Azure Bot"
   - Click "Create"
3. Configure the bot:
   - **Bot handle**: Choose a unique name
   - **Subscription**: Select your subscription
   - **Resource group**: Create new or select existing
   - **Pricing tier**: Choose appropriate tier (F0 for free)
   - **Microsoft App ID**: Create new Microsoft App ID
4. After creation, go to **Configuration**:
   - Note the **Microsoft App ID** (Client ID)
   - Click "Manage" next to Microsoft App ID
   - In the app registration, create a new **Client Secret** under "Certificates & secrets"
   - Copy the secret value immediately (it won't be shown again)
5. Set the **Messaging endpoint**: `https://your-tunnel-url/api/messages` (update after setting up dev tunnel)

### 2. Update Configuration

Update `appsettings.json` with your bot credentials:

```json
{
  "Teams": {
    "ClientId": "your-microsoft-app-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### 3. Local Development Setup

#### Option A: Using Dev Tunnels (Recommended)

1. Install dev tunnels:
   ```bash
   winget install Microsoft.DevTunnels
   ```

2. Login to dev tunnels:
   ```bash
   devtunnel user login
   ```

3. Create a tunnel:
   ```bash
   devtunnel create --allow-anonymous
   ```

4. Start the tunnel (port 3978):
   ```bash
   devtunnel port create -p 3978
   devtunnel host
   ```

5. Copy the tunnel URL (e.g., `https://abc123.devtunnels.ms`)

#### Option B: Using ngrok

1. Download and install [ngrok](https://ngrok.com/download)

2. Start ngrok tunnel:
   ```bash
   ngrok http 3978
   ```

3. Copy the HTTPS forwarding URL (e.g., `https://abc123.ngrok.io`)

### 4. Update Azure Bot Messaging Endpoint

1. Go back to your Azure Bot resource in the Azure Portal
2. Navigate to **Configuration**
3. Update **Messaging endpoint** to: `https://your-tunnel-url/api/messages`
4. Click **Apply**

### 5. Configure Teams App Manifest

Create a Teams app manifest to register your message extension. Create a folder called `AppPackage` with the following files:

#### manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.17/MicrosoftTeams.schema.json",
  "manifestVersion": "1.17",
  "version": "1.0.0",
  "id": "YOUR-APP-ID-GUID",
  "packageName": "com.example.messageextensions",
  "developer": {
    "name": "Your Company",
    "websiteUrl": "https://example.com",
    "privacyUrl": "https://example.com/privacy",
    "termsOfUseUrl": "https://example.com/terms"
  },
  "name": {
    "short": "Message Extensions Sample",
    "full": "Message Extensions Sample for Teams"
  },
  "description": {
    "short": "Sample message extensions bot",
    "full": "A comprehensive sample demonstrating message extensions functionality in Microsoft Teams"
  },
  "icons": {
    "outline": "outline.png",
    "color": "color.png"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "YOUR-MICROSOFT-APP-ID",
      "scopes": [
        "personal",
        "team",
        "groupchat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "composeExtensions": [
    {
      "botId": "YOUR-MICROSOFT-APP-ID",
      "commands": [
        {
          "id": "searchQuery",
          "type": "query",
          "title": "Search",
          "description": "Search for items",
          "initialRun": false,
          "parameters": [
            {
              "name": "searchQuery",
              "title": "Search",
              "description": "Enter search terms",
              "inputType": "text"
            }
          ]
        },
        {
          "id": "createCard",
          "type": "action",
          "title": "Create Card",
          "description": "Create a custom card",
          "context": ["compose", "commandBox"],
          "parameters": [
            {
              "name": "title",
              "title": "Title",
              "description": "Card title",
              "inputType": "text"
            },
            {
              "name": "description",
              "title": "Description",
              "description": "Card description",
              "inputType": "textarea"
            }
          ]
        },
        {
          "id": "getMessageDetails",
          "type": "action",
          "title": "Get Message Details",
          "description": "Get details from a message",
          "context": ["message"]
        }
      ],
      "messageHandlers": [
        {
          "type": "link",
          "value": {
            "domains": [
              "*.example.com"
            ]
          }
        }
      ]
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "*.botframework.com",
    "YOUR-TUNNEL-DOMAIN"
  ]
}
```

#### Icon Requirements

You'll need two icon files in the same folder:

- **color.png**: 192x192 pixels, full color icon
- **outline.png**: 32x32 pixels, transparent outline icon

You can create simple icons or use placeholder images for testing.

#### Creating the App Package

1. Create icons (or download sample icons)
2. Update the manifest:
   - Replace `YOUR-APP-ID-GUID` with a new GUID (you can generate one at [guidgen.com](https://www.guidgen.com/))
   - Replace `YOUR-MICROSOFT-APP-ID` with your Azure Bot's Microsoft App ID (same as ClientId)
   - Replace `YOUR-TUNNEL-DOMAIN` with your tunnel domain (e.g., `abc123.devtunnels.ms`)
   - Update company and URL information
3. Zip the three files (manifest.json, color.png, outline.png) into a package named `MessageExtensions.zip`

### 6. Install the App in Teams

1. Open Microsoft Teams
2. Click on **Apps** in the left sidebar
3. Click **Manage your apps** (bottom left)
4. Click **Upload an app** → **Upload a custom app**
5. Select your `MessageExtensions.zip` file
6. Click **Add** to install the app

## Running the Sample

```bash
# Navigate to the project directory
cd Samples/Samples.MessageExtensions

# Run the bot
dotnet run
```

The bot will start on `http://localhost:3978` by default.

## Using the Message Extension

### Search Command

1. In any chat or channel in Teams, click the **+** icon in the compose area
2. Find and select your **Message Extensions Sample** app
3. Type a search query in the search box
4. The bot will return 5 adaptive card results based on your query
5. Click on a result to insert it into the conversation

### Create Card Action

1. Click the **...** (more options) button in the compose area
2. Select **Message Extensions Sample**
3. Choose **Create Card**
4. Fill in the title and description
5. Submit to create and insert the card

### Get Message Details (Message Action)

1. Hover over any message in a conversation
2. Click the **...** (more actions) menu
3. Select **More actions** → **Get Message Details**
4. The bot will display the message details in a card

### Link Unfurling

1. Configure the domains you want to unfurl in the manifest
2. Paste a link from those domains in a conversation
3. The bot will automatically unfurl it with a rich preview card

## Message Extension Handlers

The sample implements the following handlers:

- **`[MessageExtension.Query]`** (Line 70) - Handles search queries
- **`[MessageExtension.SubmitAction]`** (Line 99) - Handles action submissions (create card, message actions)
- **`[MessageExtension.QueryLink]`** (Line 127) - Handles link unfurling
- **`[MessageExtension.SelectItem]`** (Line 146) - Handles item selection from search results
- **`[MessageExtension.QuerySettingsUrl]`** (Line 160) - Returns settings configuration URL
- **`[MessageExtension.FetchTask]`** (Line 178) - Returns task module for actions
- **`[MessageExtension.Setting]`** (Line 191) - Handles settings updates

## Key Features Demonstrated

### AttachmentLayout Types

The sample uses the new `MessageExtensions.AttachmentLayout` type which supports:

```csharp
// List layout (default)
AttachmentLayout = MessageExtensions.AttachmentLayout.List

// Grid layout (new in this version)
AttachmentLayout = MessageExtensions.AttachmentLayout.Grid
```

### Adaptive Cards

Search results return adaptive cards with:
- Title and description
- Preview cards for search results
- Interactive elements

### Task Modules

Action-based commands display task modules for data collection.

### Error Handling

Comprehensive error handling with user-friendly error messages.

## Dependencies

The project references the following Teams AI SDK libraries:

- `Microsoft.Teams.Apps` - Core Teams bot functionality
- `Microsoft.Teams.Api` - Teams API models and clients
- `Microsoft.Teams.Common` - Common utilities and logging
- `Microsoft.Teams.Cards` - Adaptive Cards support
- `Microsoft.Teams.Extensions.Hosting` - ASP.NET Core integration
- `Microsoft.Teams.Plugins.AspNetCore` - ASP.NET Core plugin support
- `Microsoft.Teams.Plugins.AspNetCore.DevTools` - Development tools

## Troubleshooting

### Bot Not Responding

- Verify the messaging endpoint in Azure Bot is correct and includes `https://`
- Ensure dev tunnel or ngrok is running
- Check that the bot is running locally (`dotnet run`)
- Review console logs for errors

### Message Extension Not Appearing

- Verify the manifest is correctly configured
- Check that `botId` in `composeExtensions` matches your Microsoft App ID
- Ensure the app is installed in Teams
- Try reinstalling the app package

### Authentication Errors

- Verify `ClientId` and `ClientSecret` in `appsettings.json` are correct
- Ensure the Client Secret hasn't expired
- Check Azure Bot configuration

### Commands Not Working

- Review the `commandId` values in the manifest match those in the code
- Check console logs for handler execution
- Verify the command types (`query` vs `action`) are correct

### Settings Not Loading

- Ensure the settings page HTML is being served correctly
- Verify the `/settings` endpoint is accessible
- Check browser console for errors

## Additional Resources

- [Microsoft Teams Message Extensions Documentation](https://learn.microsoft.com/microsoftteams/platform/messaging-extensions/what-are-messaging-extensions)
- [Teams App Manifest Schema](https://learn.microsoft.com/microsoftteams/platform/resources/schema/manifest-schema)
- [Adaptive Cards Designer](https://adaptivecards.io/designer/)
- [Teams AI SDK Documentation](https://microsoft.github.io/teams-ai)

## Support

For issues and questions:
- Check the [Teams AI SDK GitHub repository](https://github.com/microsoft/teams-ai)
- Review [Microsoft Teams Platform documentation](https://learn.microsoft.com/microsoftteams/platform/)
