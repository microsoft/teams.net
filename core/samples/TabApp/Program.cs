// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.StaticFiles;
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

// Serve the React tab at /tabs/test (build the web app first: cd Web && npm install && npm run build)
var tabProvider = new PhysicalFileProvider(Path.GetFullPath("./Web/bin"));
var contentTypeProvider = new FileExtensionContentTypeProvider();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = tabProvider,
    RequestPath = "/tabs/test",
    ServeUnknownFileTypes = true
});

app.MapGet("/tabs/test/{*path}", (string path) =>
{
    IFileInfo file = tabProvider.GetFileInfo($"/{path}");
    if (!file.Exists) return Results.NotFound();
    contentTypeProvider.TryGetContentType(file.Name, out var contentType);
    return Results.File(file.CreateReadStream(), contentType ?? "application/octet-stream");
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
}).RequireAuthorization("EntraPolicy");

app.Run();
