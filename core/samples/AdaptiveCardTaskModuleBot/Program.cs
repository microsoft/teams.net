// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using AdaptiveCardTaskModuleBot;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.TaskModules;
using Microsoft.Teams.Core.Hosting;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseBotApplication<TeamsBotApplication>();

// ==================== MESSAGE - SEND SIMPLE CARD ====================
bot.OnMessage(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnMessage");

    JsonObject card = Cards.CreateWelcomeCard();

    TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
        .WithAdaptiveCard(card)
        .Build();

    await context.SendAsync(new MessageActivityInput().AddAttachment(attachment), cancellationToken);
});

// ==================== ADAPTIVE CARD ACTION ====================
bot.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnAdaptiveCardAction");
    AdaptiveCardActionValue? value = context.Activity.Value;
    AdaptiveCardAction? action = value?.Action;
    string? verb = action?.Verb;
    Dictionary<string, object>? data = action?.Data;

    Console.WriteLine($"  Verb: {verb}");
    Console.WriteLine($"  Data: {JsonSerializer.Serialize(data)}");

    // Handle file upload request
    if (verb == "requestFileUpload")
    {
        JsonObject fileConsentCard = Cards.CreateFileConsentCard();
        TeamsAttachment fileConsentCardResponse = TeamsAttachment.CreateBuilder()
            .WithContent(fileConsentCard).WithContentType(AttachmentContentTypes.FileConsentCard)
            .WithName("file_consent.json").Build();
        await context.SendAsync(new MessageActivityInput().AddAttachment(fileConsentCardResponse), cancellationToken);

        return AdaptiveCardResponse.CreateMessageResponse("File Consent requested!");
    }

    string? message = data != null && data.TryGetValue("message", out object? msgValue) ? msgValue?.ToString() : null;

    JsonObject adaptiveActionCard = Cards.CreateAdaptiveActionResponseCard(verb, message);
    TeamsAttachment adaptiveActionCardResponse = TeamsAttachment.CreateBuilder().WithAdaptiveCard(adaptiveActionCard).Build();
    await context.SendAsync(new MessageActivityInput().AddAttachment(adaptiveActionCardResponse), cancellationToken);

    return AdaptiveCardResponse.CreateMessageResponse("Action submitted!");
});

// ==================== TASK MODULE - FETCH ====================
bot.OnTaskFetch(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnTaskFetch");
    TeamsAttachment taskModuleCardResponse = TeamsAttachment.CreateBuilder()
        .WithAdaptiveCard(Cards.CreateTaskModuleCard()).Build();
    return TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseTypes.Continue)
        .WithTitle("Task")
        .WithHeight("medium")
        .WithWidth("medium")
        .WithCard(taskModuleCardResponse)
        .Build();

});

// ==================== TASK MODULE - SUBMIT ====================
bot.OnTaskSubmit(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnTaskSubmit");
    return TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseTypes.Message)
        .WithMessage("Done")
        .Build();
});

// ==================== FILE CONSENT ====================
bot.OnFileConsent(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnFileConsent");

    FileConsentValue? value = context.Activity.Value;
    string? action = value?.Action;
    FileUploadInfo? uploadInfo = value?.UploadInfo;
    object? consentContext = value?.Context;

    if (action == "accept")
    {
        Console.WriteLine($"  File accepted!");

        // Upload the file
        string? uploadUrl = uploadInfo?.UploadUrl?.ToString();
        string? fileName = uploadInfo?.Name;
        string? contentUrl = uploadInfo?.ContentUrl?.ToString();
        string? uniqueId = uploadInfo?.UniqueId;

        if (uploadUrl != null && contentUrl != null)
        {
            // Create sample file content
            string fileContent = "This is a sample file uploaded via file consent!";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
            int fileSize = fileBytes.Length;

            using HttpClient httpClient = new();
            using ByteArrayContent content = new(fileBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, fileSize - 1, fileSize);

            try
            {
                HttpResponseMessage uploadResponse = await httpClient.PutAsync(uploadUrl, content, cancellationToken);
                Console.WriteLine($"  Upload Status: {uploadResponse.StatusCode}");

                if (uploadResponse.IsSuccessStatusCode)
                {
                    JsonObject fileInfoContent = Cards.CreateFileInfoCard(uniqueId, uploadInfo?.FileType);

                    TeamsAttachment fileUploadResponse = TeamsAttachment.CreateBuilder()
                        .WithName(fileName)
                        .WithContentType(AttachmentContentTypes.FileInfoCard)
                        .WithContentUrl(contentUrl != null ? new Uri(contentUrl) : null)
                        .WithContent(fileInfoContent).Build();

                    await context.SendAsync(new MessageActivityInput().AddAttachment(fileUploadResponse), cancellationToken);
                }
                else
                {
                    Console.WriteLine($"  File upload failed: {await uploadResponse.Content.ReadAsStringAsync(cancellationToken)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  File upload error: {ex.Message}");
            }
        }
    }
    else if (action == "decline")
    {
        Console.WriteLine($"  File declined!");
        Console.WriteLine($"  Context: {JsonSerializer.Serialize(consentContext)}");
    }

    return AdaptiveCardResponse.CreateBuilder()
        .WithStatusCode(200)
        .Build();
});

webApp.Run();
