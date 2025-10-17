using Azure.Core;
using Azure.Identity;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Samples.AzureIdentity;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddTransient<Controller>();

        // Configure authentication using Azure Managed Identity
        var botClientId = builder.Configuration["AzureIdentity:BotClientId"] ?? "";
        var managedIdentityClientId = builder.Configuration["AzureIdentity:ManagedIdentityClientId"];
        var useDefaultAzureCredential = builder.Configuration.GetValue<bool>("AzureIdentity:UseDefaultAzureCredential");

        var appOptions = new AppOptions();

        // Create the appropriate TokenCredential based on configuration
        TokenCredential credential;

        if (useDefaultAzureCredential)
        {
            // Use DefaultAzureCredential which tries multiple authentication methods
            // in the following order:
            // 1. Environment credentials
            // 2. Workload Identity (for AKS)
            // 3. Managed Identity
            // 4. Visual Studio credentials
            // 5. Azure CLI credentials
            // 6. Azure PowerShell credentials
            credential = new DefaultAzureCredential();
        }
        else if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            // Use User-Assigned Managed Identity
            credential = new ManagedIdentityCredential(managedIdentityClientId);
        }
        else
        {
            // Use System-Assigned Managed Identity
            credential = new ManagedIdentityCredential();
        }

        // Create TokenCredentials using the Azure Identity credential
        // The TokenFactory delegate acquires tokens using the Azure.Identity SDK
        appOptions.Credentials = new TokenCredentials(
            botClientId,
            async (tenantId, scopes) =>
            {
                var scopesToUse = scopes.Length > 0 ? scopes : new[] { "https://api.botframework.com/.default" };
                var tokenRequestContext = new TokenRequestContext(scopesToUse);
                var accessToken = await credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

                return new AzureIdentityTokenResponse(accessToken);
            });

        builder.AddTeams(appOptions).AddTeamsDevTools();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseTeams();
        app.Run();
    }

    [TeamsController]
    public class Controller
    {
        [Activity]
        public async Task OnActivity(IContext<Activity> context, [Context] IContext.Next next)
        {
            context.Log.Info($"Bot App ID: {context.AppId}");
            await next();
        }

        [Message]
        public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("Message received!");
            await client.Typing();

            var response = $"You said: '{activity.Text}'\n\n" +
                          "This bot is authenticated using Azure Managed Identity instead of client secret!";

            await client.Send(response);
        }
    }

    /// <summary>
    /// Helper class to adapt Azure.Identity AccessToken to ITokenResponse
    /// </summary>
    private class AzureIdentityTokenResponse : ITokenResponse
    {
        public string TokenType => "Bearer";
        public int? ExpiresIn { get; }
        public string AccessToken { get; }

        public AzureIdentityTokenResponse(AccessToken accessToken)
        {
            AccessToken = accessToken.Token;
            ExpiresIn = (int)(accessToken.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds;
        }
    }
}