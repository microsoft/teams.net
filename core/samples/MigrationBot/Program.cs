// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Teams.Bot.Compat;
using MigrationBot;

// ─────────────────────────────────────────────────────────────────────────────
// MIGRATION BOT — Phased migration from Bot Framework to the Teams SDK
//
// Routing on POST /api/messages:
//   message whose text starts with "bf" → Bot Framework (LegacyEchoBot)
//   everything else                     → Teams SDK     (NewSdkBot)
// ─────────────────────────────────────────────────────────────────────────────

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddCompatAdapter();
builder.Services.AddTransient<IBot, LegacyEchoBot>();
builder.AddNewSdkBot();

WebApplication app = builder.Build();

NewSdkBot newSdkBot = app.Services.GetRequiredService<NewSdkBot>();

app.MapPost("/api/messages", async (
    IBotFrameworkHttpAdapter adapter,
    IBot bot,
    HttpRequest req,
    HttpResponse res,
    CancellationToken ct) =>
{
    // Buffer the body so it can be read once for routing and again by the adapter.
    req.EnableBuffering();

    bool useBotFramework = false;
    try
    {
        using JsonDocument doc = await JsonDocument.ParseAsync(req.Body, cancellationToken: ct);

        string? type = doc.RootElement.TryGetProperty("type", out JsonElement typeProp)
            ? typeProp.GetString() : null;
        string? text = doc.RootElement.TryGetProperty("text", out JsonElement textProp)
            ? textProp.GetString() : null;

        // Message activities whose text starts with "bf" go to the Bot Framework path.
        useBotFramework = string.Equals(type, "message", StringComparison.OrdinalIgnoreCase)
                       && text?.StartsWith("bf", StringComparison.OrdinalIgnoreCase) == true;
    }
    catch { /* malformed body – fall through to Teams SDK */ }

    req.Body.Position = 0;

    if (useBotFramework)
        await adapter.ProcessAsync(req, res, bot, ct);
    else
        await newSdkBot.ProcessAsync(req.HttpContext, ct);

}).RequireAuthorization();

app.Run();
