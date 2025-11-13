# Graph Sample

This sample demonstrates how to implement OAuth authentication and Microsoft Graph integration in a Teams bot using the Teams SDK for .NET.

## Features

- OAuth authentication with Microsoft Graph
- User sign-in/sign-out functionality
- Access to Microsoft Graph API (user profile information)
- Custom sign-in UI with configurable text
- Token display after successful authentication

## Prerequisites

- .NET 9.0
- Azure Bot Service registration
- Microsoft Graph OAuth connection configured in Azure Bot Service
- Dev tunnels or ngrok for local development

## Project Structure

```
Samples.Graph/
├── Program.cs                          # Main bot logic and OAuth handlers
├── Samples.Graph.csproj               # Project file with SDK dependencies
├── appsettings.json                   # Bot credentials configuration
├── Properties/launchSettings.json     # Launch configuration (port 3978)
└── README.md                         # This file
```

## Setup

### 1. Azure Bot Registration

1. Create an Azure Bot resource in the Azure Portal
2. Configure the messaging endpoint: `https://your-tunnel-url/api/messages`
3. Note the Application (Client) ID and create a Client Secret

### 2. OAuth Connection Setup

1. In your Azure Bot resource, go to **Configuration** → **OAuth Connection Settings**
2. Add new OAuth connection:
   - **Name**: `graph` (must match the code)
   - **Service Provider**: `Generic Oauth 2` or `Azure Active Directory v2`
   - **Client ID**: Your bot's Application (Client) ID
   - **Client Secret**: Your bot's client secret
   - **Authorization URL**: `https://login.microsoftonline.com/common/oauth2/v2.0/authorize`
   - **Token URL**: `https://login.microsoftonline.com/common/oauth2/v2.0/token`
   - **Refresh URL**: `https://login.microsoftonline.com/common/oauth2/v2.0/token`
   - **Scopes**: `User.Read`

### 3. Update Configuration

Update `appsettings.json` with your bot credentials:

```json
{
  "Teams": {
    "ClientId": "your-bot-application-id",
    "ClientSecret": "your-bot-client-secret"
  }
}
```

### 4. Regional Bot Configuration (Optional)

If you're deploying a regional bot, you need to configure several files to use regional endpoints. This example uses West Europe, but follow the equivalent for other locations.

**Step 1: Update Azure Bot Configuration**

In your `azurebot.bicep` file, replace all `global` occurrences with `westeurope`.

**Step 2: Update Manifest**

In `manifest.json`, in `validDomains`, replace `*.botframework.com` with `europe.token.botframework.com`.

**Step 3: Update AAD Manifest**

In `aad.manifest.json`, replace `https://token.botframework.com/.auth/web/redirect` with `https://europe.token.botframework.com/.auth/web/redirect`.

**Step 4: Update Program.cs**

Update `Program.cs` to include `ApiClientOptions`:

```csharp
var appBuilder = App.Builder()
    .AddLogger(new ConsoleLogger(level: Microsoft.Teams.Common.Logging.LogLevel.Debug))
    .AddOAuth("graph")
    .AddOptions(new AppOptions
    {
        ApiClientOptions = new ApiClientOptions("https://europe.token.botframework.com")
    });
```

**Alternative: Using Environment Variable**

Instead of hard-coding the OAuth URL in `Program.cs`, you can set the `OAUTH_URL` environment variable:

```bash
# Windows (Command Prompt)
set OAUTH_URL=https://europe.token.botframework.com
dotnet run

# Windows (PowerShell)
$env:OAUTH_URL="https://europe.token.botframework.com"
dotnet run

# Linux/macOS
export OAUTH_URL=https://europe.token.botframework.com
dotnet run
```

**Available Regional Endpoints:**
- **Default (Global)**: `https://token.botframework.com`
- **Europe**: `https://europe.token.botframework.com`

**Note**: When using a regional bot, ensure your Azure Bot resource is also configured for the same region.

### 5. Local Development Setup

1. Install dev tunnels: `winget install Microsoft.DevTunnels`
2. Create tunnel: `devtunnel create -a`
3. Host tunnel: `devtunnel host <tunnel-id> -p 3978`
4. Update Azure Bot messaging endpoint with the tunnel URL

## Running the Sample

```bash
# Navigate to the project directory
cd Samples/Samples.Graph

# Run the bot
dotnet run
```

The bot will start on `http://localhost:3978` by default.

## Usage

### Authentication Flow

1. **Initial Message**: Send any message to the bot to trigger sign-in
2. **Sign-in Card**: Bot presents OAuth sign-in card with custom text
3. **Authentication**: Complete OAuth flow with Microsoft Graph
4. **Success Response**: Bot displays user's display name and access token

### Commands

- **Any message**: Triggers sign-in flow if not authenticated, shows user info if authenticated
- **`/signout`**: Signs out the current user and clears authentication

### Expected Responses

- **Not signed in**: "Sign in to your account" OAuth card
- **Already signed in**: "user 'DisplayName' is already signed in!"
- **After sign-in**: "user \"DisplayName\" signed in. Here's the token: [token]"
- **Sign-out**: "you have been signed out!"

## Key Components

- **OAuth Integration**: Configured with `.AddOAuth("graph")` (Program.cs:13)
- **Sign-in Handler**: Main message handler with `SignInOptions` (Program.cs:32-48)
- **Sign-out Handler**: Dedicated `/signout` command handler (Program.cs:20-30)
- **Sign-in Event**: Handles successful authentication and token display (Program.cs:50-57)
- **Graph API Access**: Uses `context.UserGraph.Me.GetAsync()` for user profile (Program.cs:46, 55)

## Dependencies

The project references the following Teams SDK libraries:

- `Microsoft.Teams.Apps` - Core Teams bot functionality
- `Microsoft.Teams.Api` - Teams API models and clients
- `Microsoft.Teams.Common` - Common utilities and logging
- `Microsoft.Teams.Cards` - Adaptive Cards support
- `Microsoft.Teams.Extensions.Hosting` - ASP.NET Core integration
- `Microsoft.Teams.Plugins.AspNetCore` - ASP.NET Core plugin support

## Troubleshooting

- **Authentication fails**: Verify OAuth connection name matches "graph" in code
- **Bot not reachable**: Ensure dev tunnel is running and messaging endpoint is correct
- **Permission errors**: Check Azure Bot and App Registration have correct permissions
- **Token issues**: Verify OAuth scopes include `User.Read` for Microsoft Graph access
