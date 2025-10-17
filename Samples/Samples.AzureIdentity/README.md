# Azure Identity Sample

This sample demonstrates how to authenticate a Teams bot using **Azure Managed Identity** with the existing `TokenCredentials` class and the **Azure.Identity** SDK. This is a more secure authentication method than traditional client ID and client secret, eliminating the need to store sensitive credentials in your configuration.

## Features

- **User-Assigned Managed Identity**: Authenticate using a specific managed identity with a client ID
- **System-Assigned Managed Identity**: Authenticate using the system-assigned identity of your Azure resource
- **DefaultAzureCredential**: Use Azure Identity's DefaultAzureCredential for local development and production

## What is Managed Identity?

Azure Managed Identity provides Azure services with an automatically managed identity in Microsoft Entra ID (formerly Azure AD). This identity can be used to authenticate to any service that supports Microsoft Entra authentication without storing credentials in your code.

### Benefits:
- **No credentials in code**: Eliminates the need to store client secrets in configuration files
- **Automatic credential rotation**: Azure handles credential management automatically
- **Simplified deployment**: No need to manage and distribute secrets across environments
- **Enhanced security**: Reduces the risk of credential leaks

## Prerequisites

- .NET 9.0 SDK
- Azure subscription
- Azure Bot Service registration
- One of the following:
  - Azure App Service, Azure Functions, or Azure Container Instances with Managed Identity enabled
  - Azure Virtual Machine with Managed Identity enabled
  - Azure Kubernetes Service (AKS) with Workload Identity configured
  - Local development with Azure CLI or Visual Studio signed in (when using DefaultAzureCredential)

## Project Structure

```
Samples.AzureIdentity/
├── Program.cs                          # Main bot logic with Azure Identity configuration
├── Samples.AzureIdentity.csproj       # Project file with SDK dependencies
├── appsettings.json                   # Configuration for managed identity
├── Properties/launchSettings.json     # Launch configuration (port 3978)
└── README.md                         # This file
```

## Setup

### 1. Azure Bot Registration

1. Create an Azure Bot resource in the Azure Portal
2. Configure the messaging endpoint: `https://your-app-url/api/messages`
3. Note the Application (Client) ID

### 2. Enable Managed Identity

#### Option A: System-Assigned Managed Identity

For Azure App Service or Azure Functions:
```bash
# Enable system-assigned managed identity
az webapp identity assign --name <app-name> --resource-group <resource-group>
```

For Azure VM:
```bash
# Enable system-assigned managed identity
az vm identity assign --name <vm-name> --resource-group <resource-group>
```

#### Option B: User-Assigned Managed Identity

1. Create a User-Assigned Managed Identity:
```bash
az identity create --name <identity-name> --resource-group <resource-group>
```

2. Note the Client ID from the output

3. Assign it to your Azure resource:
```bash
# For App Service
az webapp identity assign --name <app-name> --resource-group <resource-group> \
  --identities <identity-resource-id>

# For VM
az vm identity assign --name <vm-name> --resource-group <resource-group> \
  --identities <identity-resource-id>
```

### 3. Grant Permissions to Managed Identity

The managed identity needs permission to authenticate as your bot. Grant the identity the **Bot Service Contributor** role on the bot resource:

```bash
# Get the principal ID of the managed identity
# For system-assigned:
principalId=$(az webapp identity show --name <app-name> --resource-group <resource-group> --query principalId -o tsv)

# For user-assigned:
principalId=$(az identity show --name <identity-name> --resource-group <resource-group> --query principalId -o tsv)

# Grant the role assignment
az role assignment create --role "BotService Contributor" \
  --assignee-object-id $principalId \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.BotService/botServices/<bot-name>
```

Alternatively, you can configure the managed identity's client ID directly in your bot's configuration in the Azure Portal.

### 4. Update Configuration

Update `appsettings.json` based on your authentication method:

#### For System-Assigned Managed Identity:
```json
{
  "AzureIdentity": {
    "BotClientId": "your-bot-application-id",
    "UseDefaultAzureCredential": false,
    "ManagedIdentityClientId": ""
  }
}
```

#### For User-Assigned Managed Identity:
```json
{
  "AzureIdentity": {
    "BotClientId": "your-bot-application-id",
    "UseDefaultAzureCredential": false,
    "ManagedIdentityClientId": "your-managed-identity-client-id"
  }
}
```

#### For DefaultAzureCredential (recommended for local development):
```json
{
  "AzureIdentity": {
    "BotClientId": "your-bot-application-id",
    "UseDefaultAzureCredential": true,
    "ManagedIdentityClientId": ""
  }
}
```

### 5. Local Development Setup

When using `DefaultAzureCredential` for local development, authenticate using one of these methods:

#### Option 1: Azure CLI
```bash
az login
az account set --subscription <subscription-id>
```

#### Option 2: Visual Studio
Sign in to Visual Studio with an Azure account that has access to the bot

#### Option 3: Environment Variables
Set environment variables for a service principal:
```bash
export AZURE_CLIENT_ID="<service-principal-client-id>"
export AZURE_CLIENT_SECRET="<service-principal-client-secret>"
export AZURE_TENANT_ID="<tenant-id>"
```

### 6. Dev Tunnels for Local Testing

To test locally with Teams:

1. Install dev tunnels:
```bash
winget install Microsoft.DevTunnels
```

2. Create and host a tunnel:
```bash
devtunnel create -a
devtunnel host <tunnel-id> -p 3978
```

3. Update your Azure Bot messaging endpoint with the tunnel URL

## Running the Sample

### Locally (Development)

```bash
# Navigate to the project directory
cd Samples/Samples.AzureIdentity

# Run the bot
dotnet run
```

The bot will start on `http://localhost:3978` by default.

### Deploy to Azure

1. Build the project:
```bash
dotnet publish -c Release -o ./publish
```

2. Deploy to your Azure resource (App Service, Functions, etc.)

3. Ensure the managed identity is configured correctly on the Azure resource

## Usage

Once the bot is running and configured in Teams:

1. Send any message to the bot
2. The bot will echo your message back, confirming that it's authenticated using Azure Managed Identity

Example:
```
User: Hello bot!
Bot: You said: 'Hello bot!'

This bot is authenticated using Azure Managed Identity instead of client secret!
```

## How It Works

### Authentication Flow

1. **Credential Creation**: The application creates an instance of `ManagedIdentityCredentials` based on configuration
2. **Token Acquisition**: When the bot needs to authenticate, the `ManagedIdentityCredentials` class uses Azure Identity SDK to acquire a token
3. **Automatic Token Management**: The Azure Identity SDK handles token caching and renewal automatically

### Code Structure

The key authentication setup happens in `Program.cs` using a minimal API style:

```csharp
TokenCredential credential = useDefaultAzureCredential ? new DefaultAzureCredential() :
    !string.IsNullOrEmpty(managedIdentityClientId) ? new ManagedIdentityCredential(managedIdentityClientId) :
    new ManagedIdentityCredential();

var appOptions = new AppOptions
{
    Credentials = new TokenCredentials(botClientId, async (_, scopes) =>
    {
        var scopesToUse = scopes.Length > 0 ? scopes : new[] { "https://api.botframework.com/.default" };
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopesToUse), CancellationToken.None);
        return new TokenResponse { TokenType = "Bearer", AccessToken = token.Token };
    })
};

builder.AddTeams(appOptions);
```

### Using TokenCredentials with Azure.Identity

This sample demonstrates how to use the existing `TokenCredentials` class with the Azure.Identity SDK. The `TokenCredentials` class accepts a `TokenFactory` delegate that allows you to provide custom token acquisition logic:

```csharp
TokenCredential credential = useDefaultAzureCredential ? new DefaultAzureCredential() :
    !string.IsNullOrEmpty(managedIdentityClientId) ? new ManagedIdentityCredential(managedIdentityClientId) :
    new ManagedIdentityCredential();

var appOptions = new AppOptions
{
    Credentials = new TokenCredentials(botClientId, async (_, scopes) =>
    {
        var scopesToUse = scopes.Length > 0 ? scopes : new[] { "https://api.botframework.com/.default" };
        var token = await credential.GetTokenAsync(new TokenRequestContext(scopesToUse), CancellationToken.None);
        return new TokenResponse { TokenType = "Bearer", AccessToken = token.Token };
    })
};
```

The code uses the existing `TokenResponse` class from the SDK to return the token acquired from Azure.Identity.

## Comparison: Client Secret vs Managed Identity

### Traditional Approach (Client Secret)
```json
{
  "Teams": {
    "ClientId": "your-bot-application-id",
    "ClientSecret": "your-bot-client-secret"  // Sensitive!
  }
}
```

### Managed Identity Approach
```json
{
  "AzureIdentity": {
    "BotClientId": "your-bot-application-id",
    "UseDefaultAzureCredential": false,
    "ManagedIdentityClientId": "your-managed-identity-client-id"
  }
}
```

No client secret is stored in your configuration!

**Note:** The `BotClientId` is the Application (Client) ID of your bot registration, which is not a secret and can be safely stored in configuration.

## Troubleshooting

### Issue: "ManagedIdentityCredential authentication unavailable"

**Solution**: Ensure your Azure resource has managed identity enabled and you're running in an Azure environment (or use DefaultAzureCredential for local development).

### Issue: "Authentication failed" when running locally

**Solution**: When using DefaultAzureCredential:
1. Ensure you're signed in with Azure CLI: `az login`
2. Or signed in to Visual Studio with an Azure account
3. Or set environment variables for a service principal

### Issue: "403 Forbidden" when authenticating

**Solution**: Ensure the managed identity has the correct role assignments on the bot resource.

### Issue: Bot receives 401 Unauthorized

**Solution**: 
1. Verify the managed identity client ID (if using user-assigned)
2. Check that the identity has access to the Bot Service
3. Ensure the bot's App ID is correctly configured

## Additional Resources

- [Azure Managed Identity Documentation](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure Identity SDK for .NET](https://learn.microsoft.com/dotnet/api/overview/azure/identity-readme)
- [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [Teams AI SDK Documentation](https://microsoft.github.io/teams-ai)

## Security Best Practices

1. **Never commit credentials**: With managed identity, there are no secrets to commit
2. **Use User-Assigned Managed Identity**: For better control and reusability across resources
3. **Implement proper RBAC**: Grant only the minimum necessary permissions
4. **Rotate regularly**: If you must use service principals locally, rotate credentials regularly
5. **Use DefaultAzureCredential**: For seamless local development and production deployment

## License

This sample is licensed under the MIT License. See the LICENSE file in the repository root for more information.
