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

string endpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
string apiKey = builder.Configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
string deployment = builder.Configuration["AzureOpenAI:Deployment"] ?? throw new InvalidOperationException("AzureOpenAI:Deployment is required.");

builder.Services.AddSingleton<IChatClient>(_ =>
    new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
        .GetChatClient(deployment)
        .AsIChatClient());
builder.Services.AddSingleton<AnalysisRunner>();

WebApplication webApp = builder.Build();
TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();
AnalysisRunner runner = webApp.Services.GetRequiredService<AnalysisRunner>();
ILogger logger = webApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AIFileAnalysisBot");

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    await context.TypingAsync(cancellationToken);

    // FILE RECEIVE: the files attached to this activity.
    IList<IncomingFile> attached = await context.Files.ListAsync(cancellationToken);
    if (attached.Count == 0)
    {
        await context.SendAsync(
            "Attach one or more files. I analyze text files and images, and describe anything else I cannot read.",
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

    await runner.RunAsync(analysis, TeamsStreamingWriter.CreateFromContext(context), cancellationToken);
});

webApp.Run();
