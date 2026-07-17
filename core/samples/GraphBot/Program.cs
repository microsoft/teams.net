// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using GraphOrganization = Microsoft.Graph.Models.Organization;
using GraphUser = Microsoft.Graph.Models.User;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

webAppBuilder.Services.AddTeamsBotApplication();
webAppBuilder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    string tenantId = config["AzureAd:TenantId"]
        ?? throw new InvalidOperationException("AzureAd:TenantId is not configured.");
    string clientId = config["AzureAd:ClientId"]
        ?? throw new InvalidOperationException("AzureAd:ClientId is not configured.");
    string? clientSecret = config["AzureAd:ClientCredentials:0:ClientSecret"];

    TokenCredential credential = string.IsNullOrWhiteSpace(clientSecret)
        ? new ManagedIdentityCredential(clientId)
        : new ClientSecretCredential(tenantId, clientId, clientSecret);

    return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
});

WebApplication webApp = webAppBuilder.Build();
TeamsBotApplication bot = webApp.UseTeamsBotApplication();
GraphServiceClient graph = webApp.Services.GetRequiredService<GraphServiceClient>();

bot.OnMessage("(?i)^help$", async (context, ct) =>
{
    string helpText = """
        **Graph Bot** - App-only Microsoft Graph SDK sample

        Commands:
        - `users` - List top 5 users (app token)
        - `org` - Show tenant organization display name(s)
        - `help` - Show this message
        """;

    await context.SendAsync(
        MessageActivityInput.CreateBuilder()
            .WithText(helpText, TextFormats.Markdown)
            .Build(), ct);
});

bot.OnMessage("(?i)^users$", async (context, ct) =>
{
    try
    {
        var users = await graph.Users.GetAsync(request =>
        {
            request.QueryParameters.Top = 5;
            request.QueryParameters.Select = ["displayName", "mail", "userPrincipalName", "id"];
        }, ct);

        if (users?.Value is null || users.Value.Count == 0)
        {
            await context.SendAsync("No users returned.", ct);
            return;
        }

        List<string> lines = ["**Top users (app token)**"];
        foreach (GraphUser user in users.Value)
        {
            string name = user.DisplayName ?? "(unknown)";
            string email = user.Mail ?? user.UserPrincipalName ?? "(unknown)";
            string id = user.Id ?? "(unknown)";
            lines.Add($"- {name} — {email} (`{id}`)");
        }

        await context.SendAsync(
            MessageActivityInput.CreateBuilder()
                .WithText(string.Join("\n", lines), TextFormats.Markdown)
                .Build(), ct);
    }
    catch (Exception ex)
    {
        await context.SendAsync($"Graph users call failed: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^org$", async (context, ct) =>
{
    try
    {
        var orgs = await graph.Organization.GetAsync(request =>
        {
            request.QueryParameters.Select = ["displayName", "id"];
        }, ct);

        if (orgs?.Value is null || orgs.Value.Count == 0)
        {
            await context.SendAsync("No organization records returned.", ct);
            return;
        }

        List<string> lines = ["**Organization (app token)**"];
        foreach (GraphOrganization org in orgs.Value)
        {
            lines.Add($"- {org.DisplayName ?? "(unknown)"} (`{org.Id ?? "(unknown)"}`)");
        }

        await context.SendAsync(
            MessageActivityInput.CreateBuilder()
                .WithText(string.Join("\n", lines), TextFormats.Markdown)
                .Build(), ct);
    }
    catch (Exception ex)
    {
        await context.SendAsync($"Graph org call failed: {ex.Message}", ct);
    }
});

bot.OnInstall(async (context, ct) =>
{
    await context.SendAsync(
        MessageActivityInput.CreateBuilder()
            .WithText("Welcome to **Graph Bot** (app-only)! Type `help` to see available commands.", TextFormats.Markdown)
            .Build(), ct);
});

webApp.Run();
