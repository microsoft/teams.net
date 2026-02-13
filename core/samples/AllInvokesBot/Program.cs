// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using AllInvokesBot;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;

var builder = TeamsBotApplication.CreateBuilder(args);
var bot = builder.Build();

// ==================== MESSAGE - SEND SIMPLE CARD ====================
bot.OnMessage(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnMessage");

    var card = Cards.CreateWelcomeCard();

    TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
        .WithAdaptiveCard(card)
        .Build();

    await context.SendActivityAsync(new MessageActivity([attachment]), cancellationToken);
});

// ==================== ADAPTIVE CARD ACTION ====================
bot.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnAdaptiveCardAction");
    var value = context.Activity.Value;
    var action = value?.Action;
    string? verb = action?.Verb;
    var data = action?.Data;

    Console.WriteLine($"  Verb: {verb}");
    Console.WriteLine($"  Data: {JsonSerializer.Serialize(data)}");

    // Handle file upload request
    if (verb == "requestFileUpload")
    {
        var fileConsentCard = Cards.CreateFileConsentCard();
        TeamsAttachment fileConsentCardResponse = TeamsAttachment.CreateBuilder()
            .WithContent(fileConsentCard).WithContentType(AttachmentContentType.FileConsentCard)
            .WithName("file_consent.json").Build();
        await context.SendActivityAsync(new MessageActivity([fileConsentCardResponse]), cancellationToken);

        return new CoreInvokeResponse(200, AdaptiveCardInvokeResponse.CreateMessageResponse("File consent request sent!"));
    }

    string? message = data != null && data.TryGetValue("message", out var msgValue) ? msgValue?.ToString() : null;

    var adaptiveActionCard = Cards.CreateAdaptiveActionResponseCard(verb, message);
    TeamsAttachment adaptiveActionCardResponse = TeamsAttachment.CreateBuilder().WithAdaptiveCard(adaptiveActionCard).Build();
    await context.SendActivityAsync(new MessageActivity([adaptiveActionCardResponse]), cancellationToken);

    return new CoreInvokeResponse(200, AdaptiveCardInvokeResponse.CreateMessageResponse("Action submitted!"));
});

// ==================== TASK MODULE - FETCH ====================
bot.OnTaskFetch(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnTaskFetch");
    TaskModuleResponse response = TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Continue)
        .WithTitle("Task")
        .WithHeight("medium")
        .WithWidth("medium")
        .WithCard(Cards.CreateTaskModuleCard())
        .Build();

    return new CoreInvokeResponse(200, response);

});

// ==================== TASK MODULE - SUBMIT ====================
bot.OnTaskSubmit(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnTaskSubmit");
    var response = TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Message)
        .WithMessage("Done")
        .Build();

    return new CoreInvokeResponse(200, response);
});

// ==================== FILE CONSENT ====================
bot.OnFileConsent(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnFileConsent");

    var value = context.Activity.Value;
    string? action = value?.Action;
    var uploadInfo = value?.UploadInfo;
    var consentContext = value?.Context;

    if (action == "accept")
    {
        Console.WriteLine($"  File accepted!");

        // Upload the file
        string? uploadUrl = uploadInfo?.UploadUrl?.ToString();
        string? fileName = uploadInfo?.Name;
        string? contentUrl = uploadInfo?.ContentUrl?.ToString();
        string? uniqueId = uploadInfo?.UniqueId;

        if (uploadUrl!=null && contentUrl != null)
        {
            // Create sample file content
            string fileContent = "This is a sample file uploaded via file consent!";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
            int fileSize = fileBytes.Length;

            using var httpClient = new HttpClient();
            using var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, fileSize - 1, fileSize);

            try
            {
                var uploadResponse = await httpClient.PutAsync(uploadUrl, content, cancellationToken);
                Console.WriteLine($"  Upload Status: {uploadResponse.StatusCode}");

                if (uploadResponse.IsSuccessStatusCode)
                {
                    var fileInfoContent = Cards.CreateFileInfoCard(uniqueId, uploadInfo?.FileType);

                    TeamsAttachment fileUploadResponse = TeamsAttachment.CreateBuilder()
                        .WithName(fileName)
                        .WithContentType(AttachmentContentType.FileInfoCard)
                        .WithContentUrl(contentUrl != null ? new Uri(contentUrl) : null)
                        .WithContent(fileInfoContent).Build();

                    await context.SendActivityAsync(new MessageActivity([fileUploadResponse]), cancellationToken);
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

    return new CoreInvokeResponse(200, AdaptiveCardInvokeResponse.CreateBuilder()
        .WithStatusCode(200)
        .Build());
});

/*
// ==================== EXECUTE ACTION ====================
bot.OnExecuteAction(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnExecuteAction");

    var responseBody = new JsonObject
    {
        ["status"] = "completed"
    };

    return new CoreInvokeResponse(200, responseBody);
});

// ==================== HANDOFF ====================
bot.OnHandoff(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnHandoff");
    return new CoreInvokeResponse(200);
});

// ==================== SEARCH ====================
bot.OnSearch(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnSearch");

    var responseBody = new JsonObject
    {
        ["results"] = new JsonArray
        {
            new JsonObject
            {
                ["id"] = "1",
                ["title"] = "Result"
            }
        }
    };

    return new CoreInvokeResponse(200, responseBody);
});

// ==================== MESSAGE SUBMIT ACTION ====================
bot.OnMessageSubmitAction(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnMessageSubmitAction");

    var data = context.Activity.Value;
    Console.WriteLine($"  Data: {System.Text.Json.JsonSerializer.Serialize(data)}");

    // Extract data fields
    var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
        System.Text.Json.JsonSerializer.Serialize(data));

    string? action = jsonData.TryGetProperty("action", out var a) ? a.GetString() : "unknown";
    string? value = jsonData.TryGetProperty("value", out var v) ? v.GetString() : "no value";

    Console.WriteLine($"  Action: {action}");
    Console.WriteLine($"  Value: {value}");

    var responseBody = new JsonObject
    {
        ["statusCode"] = 200,
        ["type"] = "application/vnd.microsoft.activity.message",
        ["value"] = $"Message action '{action}' submitted! Value: {value}"
    };

    return new CoreInvokeResponse(200, responseBody);
});
*/

bot.Run();
