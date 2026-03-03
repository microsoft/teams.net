// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Teams.Bot.Core.Hosting;
using TabApp;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddAuthorization(logger: null);
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


app.MapPost("/functions/post-to-chat", async (
    PostToChatBody body,
    HttpContext httpCtx,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("post-to-chat called by user {UserId}", httpCtx.User.FindFirst("oid")?.Value);
    return Results.Json(new PostToChatResult(Ok: true));
}).RequireAuthorization("EntraPolicy");

app.Run();
