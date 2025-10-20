using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddTransient<Controller>();

var botClientId = builder.Configuration["AzureIdentity:BotClientId"] ?? "";
var managedIdentityClientId = builder.Configuration["AzureIdentity:ManagedIdentityClientId"];

var managedIdentityId = string.IsNullOrEmpty(managedIdentityClientId)
    ? ManagedIdentityId.SystemAssigned
    : ManagedIdentityId.WithUserAssignedClientId(managedIdentityClientId);

var msalApp = ManagedIdentityApplicationBuilder.Create(managedIdentityId).Build();

var appOptions = new AppOptions
{
    Credentials = new TokenCredentials(botClientId, async (_, scopes) =>
    {
        var scopesToUse = scopes.Length > 0 ? scopes : new[] { "https://api.botframework.com/.default" };
        var result = await msalApp.AcquireTokenForManagedIdentity(scopesToUse[0]).ExecuteAsync();
        return new TokenResponse { TokenType = "Bearer", AccessToken = result.AccessToken };
    })
};

builder.AddTeams(appOptions).AddTeamsDevTools();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseTeams();
app.Run();

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
    public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
    {
        await client.Typing();
        await client.Send($"You said: '{activity.Text}'\n\nThis bot is authenticated using Azure Managed Identity!");
    }
}