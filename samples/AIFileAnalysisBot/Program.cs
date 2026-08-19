// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using AIFileAnalysisBot;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Files;

// Two kinds of code live in this sample, labeled throughout:
//
// - FILE RECEIVE is the Teams SDK file API itself. This is the part worth copying into your own app.
// - SAMPLE GUARDRAIL is this sample deciding what it is willing to forward to a model. Those limits are arbitrary
//   product choices, not SDK requirements, and your app should pick its own.
WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();

// SAMPLE GUARDRAIL: the file API needs no model, so the sample stays usable without Azure OpenAI settings. Without
// them it answers every file with the metadata card instead of analyzing it, which keeps download, content type,
// scope, and source demonstrable with no model subscription.
string? endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
string? apiKey = builder.Configuration["AzureOpenAI:ApiKey"];
string? deployment = builder.Configuration["AzureOpenAI:Deployment"];
bool aiConfigured = !string.IsNullOrWhiteSpace(endpoint)
    && !string.IsNullOrWhiteSpace(apiKey)
    && !string.IsNullOrWhiteSpace(deployment);

if (aiConfigured)
{
    builder.Services.AddSingleton<IChatClient>(_ =>
        new AzureOpenAIClient(new Uri(endpoint!), new ApiKeyCredential(apiKey!))
            .GetChatClient(deployment!)
            .AsIChatClient());
    builder.Services.AddSingleton<AnalysisRunner>();
}

WebApplication webApp = builder.Build();
TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();
AnalysisRunner? runner = webApp.Services.GetService<AnalysisRunner>();
ILogger logger = webApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AIFileAnalysisBot");

const string NoModelNote =
    "I downloaded this file, but no model is configured for this sample, so I did not analyze it. "
    + "Set the AzureOpenAI values in appsettings to enable analysis.";

if (runner is null)
{
    logger.LogWarning(
        "Azure OpenAI is not configured, so files will be reported but not analyzed. Set AzureOpenAI:Endpoint, "
        + "AzureOpenAI:ApiKey, and AzureOpenAI:Deployment to enable analysis.");
}

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    await context.TypingAsync(cancellationToken);

    // FILE RECEIVE: the files attached to this activity.
    IList<IncomingFile> attached = await context.Files.ListAsync(cancellationToken);
    if (attached.Count == 0)
    {
        await context.SendAsync(
            runner is not null
                ? "Attach one or more files. I analyze text files and images, and describe anything else I cannot read."
                : "Attach one or more files. No model is configured, so I will report what I received without analyzing it.",
            cancellationToken);
        return;
    }

    List<AnalyzableFile> analyzable = [];

    foreach (IncomingFile file in attached)
    {
        DownloadedFile downloaded;
        try
        {
            // FILE RECEIVE: download once. Every read below uses this in-memory copy rather than refetching through the
            // short-lived Teams download URL.
            downloaded = await file.DownloadAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not download {FileName}", file.Name);
            await context.SendAsync($"I could not download {file.Name}.", cancellationToken);
            continue;
        }

        // SAMPLE GUARDRAIL: the SDK hands over every attached file regardless of type. This sample is what narrows
        // that to the formats it will send on.
        if (runner is null)
        {
            await context.SendAsync(
                new MessageActivityInput().AddAdaptiveCardAttachment(
                    FileCard.Unsupported(file, downloaded, NoModelNote)),
                cancellationToken);
            continue;
        }

        FileKind kind = FileContext.Classify(downloaded, file.Extension);

        if (kind == FileKind.Unsupported)
        {
            await context.SendAsync(
                new MessageActivityInput().AddAdaptiveCardAttachment(FileCard.Unsupported(file, downloaded)),
                cancellationToken);
            continue;
        }

        analyzable.Add(new AnalyzableFile(downloaded, kind));
    }

    if (analyzable.Count == 0)
    {
        return;
    }

    // SAMPLE GUARDRAIL: applies this sample's size and count caps and reports anything it dropped or truncated.
    AnalysisRequest analysis = FileContext.Prepare(context.Activity.TextWithoutMentions ?? string.Empty, analyzable);

    foreach (string warning in analysis.Warnings)
    {
        await context.SendAsync(warning, cancellationToken);
    }

    if (analysis.FileCount == 0)
    {
        return;
    }

    if (runner is null)
    {
        // Not reachable: with no model configured every file already took the metadata-card path above, so nothing
        // reaches this point. The check is here to satisfy nullable analysis.
        return;
    }

    await runner.RunAsync(analysis, TeamsStreamingWriter.CreateFromContext(context), cancellationToken);
});

webApp.Run();
