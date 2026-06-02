// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Web;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;
using TabApp;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddBotAuthorization();
builder.Services.AddConversationClient();
WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ==================== TABS ====================

FileExtensionContentTypeProvider contentTypes = new();
app.MapGet("/tabs/test/{*path}", (string? path) =>
{
    string root = Path.Combine(Directory.GetCurrentDirectory(), "Web", "bin");
    string full = Path.Combine(root, path ?? "index.html");
    contentTypes.TryGetContentType(full, out string? ct);
    return Results.File(File.OpenRead(full), ct ?? "text/html");
});

// ==================== SERVER FUNCTIONS ====================

app.MapPost("/functions/post-to-chat", async (
    PostToChatBody body,
    HttpContext httpCtx,
    ConversationClient conversations,
    IConfiguration config,
    IMemoryCache cache,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("post-to-chat called");

    Uri serviceUrl = new("https://smba.trafficmanager.net/teams");
    string conversationId;

    if (body.ChatId is not null)
    {
        // group chat or 1:1 chat tab — chat ID is the conversation ID
        conversationId = body.ChatId;
    }
    else if (body.ChannelId is not null)
    {
        // channel tab — post to the channel directly
        conversationId = body.ChannelId;
    }
    else
    {
        // personal tab — create or reuse a 1:1 conversation
        string userId = httpCtx.User.GetObjectId() ?? throw new InvalidOperationException("User object ID claim not found.");

        if (!cache.TryGetValue($"conv:{userId}", out string? cached))
        {
            string botId = config["AzureAd:ClientId"] ?? throw new InvalidOperationException("Bot client ID not configured.");
            string tenantId = httpCtx.User.GetTenantId() ?? throw new InvalidOperationException("Tenant ID claim not found.");

            CreateConversationResponse res = await conversations.CreateConversationAsync(new ConversationParameters
            {
                IsGroup = false,
                TenantId = tenantId,
                Members = [new TeamsConversationAccount { Id = userId }]
            }, serviceUrl, cancellationToken: ct);

            cached = res.Id ?? throw new InvalidOperationException("CreateConversation returned no ID.");
            cache.Set($"conv:{userId}", cached);
        }

        conversationId = cached!;
    }

    TeamsActivity activity = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("Hello from the tab!")
        .WithServiceUrl(serviceUrl)
        .WithConversation(new TeamsConversation { Id = conversationId! })
        .Build();
    await conversations.SendActivityAsync(activity, cancellationToken: ct);

    return Results.Json(new PostToChatResult(Ok: true));
}).RequireAuthorization();

app.Run();
