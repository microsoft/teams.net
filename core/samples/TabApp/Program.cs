// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.FileProviders;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using TabApp;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddAuthorization(logger: null);
builder.Services.AddConversationClient();
WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ==================== TABS ====================

// Serve the React build folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Web", "build")),
    RequestPath = "/tabs/test"
});

// Fallback to index.html for SPA routing
app.MapFallback("/tabs/test/{*path}", () =>
{
    var file = Path.Combine(Directory.GetCurrentDirectory(), "Web", "build", "index.html");
    return Results.File(File.OpenRead(file), "text/html");
});
// ==================== SERVER FUNCTIONS ====================

app.MapPost("/functions/post-to-chat", async (
    PostToChatBody body,
    HttpContext httpCtx,
    ConversationClient conversations,
    IConfiguration config,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("post-to-chat called");

    var serviceUrl = new Uri("https://smba.trafficmanager.net/teams");
    var conversationId = body.ConversationId;

    if (conversationId is null)
    {
        var botId = config["CLIENT_ID"] ?? config["MicrosoftAppId"] ?? config["AzureAd:ClientId"]
            ?? throw new InvalidOperationException("Bot client ID not configured.");
        var userId = httpCtx.User.FindFirst("oid")?.Value
            ?? httpCtx.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? throw new InvalidOperationException("User OID claim missing.");
        var tenantId = httpCtx.User.FindFirst("tid")?.Value
            ?? httpCtx.User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        var res = await conversations.CreateConversationAsync(new ConversationParameters
        {
            IsGroup = false,
            TenantId = tenantId,
            Bot = new ConversationAccount { Id = botId },
            Members = [new ConversationAccount { Id = userId }]
        }, serviceUrl, cancellationToken: ct);

        conversationId = res.Id ?? throw new InvalidOperationException("CreateConversation returned no ID.");
    }

    MessageActivity activity = new(body.Message);
    activity.ServiceUrl = serviceUrl;
    activity.Conversation.Id = conversationId;
    await conversations.SendActivityAsync(activity, cancellationToken: ct);

    return Results.Json(new PostToChatResult(Ok: true));
}).RequireAuthorization();

app.Run();
