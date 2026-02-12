// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using MessageExtensionBot;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Invokes;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

var builder = TeamsBotApplication.CreateBuilder(args);
var bot = builder.Build();

// ==================== MESSAGE EXTENSION QUERY ====================
bot.OnQuery(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnQuery");

    MessageExtensionQuery? query = context.Activity.Value;
    string commandId = query?.CommandId ?? "unknown";
    string searchText = query?.Parameters
        .FirstOrDefault(p => !p.Name.Equals("initialRun"))?
        .Value ?? "default";

    if (searchText.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        MessagingExtensionResponse messageResponse = MessagingExtensionResponse.CreateBuilder()
            .WithType(MessagingExtensionResponseType.Message)
            .WithText("💡 Search for any keyword to see results.")
            .Build();

        return new CoreInvokeResponse(200, messageResponse);
    }

    // Create results with tap actions to trigger OnSelectItem
    var cards = Cards.CreateQueryResultCards(searchText);
    TeamsAttachment[] attachments = cards.Select(card => TeamsAttachment.CreateBuilder().WithContent(card)
        .WithContentType(AttachmentContentType.ThumbnailCard).Build()).ToArray();

    MessagingExtensionResponse response = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachments)
        .Build();

    return new CoreInvokeResponse(200, response);
});

// ==================== MESSAGE EXTENSION SELECT ITEM ====================
bot.OnSelectItem(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnSelectItem");

    var selectedItem = context.Activity.Value;
    var itemData = selectedItem as JsonElement?;
    string? itemId = itemData.Value.TryGetProperty("itemId", out var id) ? id.GetString() : "unknown";
    string? title = itemData.Value.TryGetProperty("title", out var t) ? t.GetString() : "Selected Item";
    string? description = itemData.Value.TryGetProperty("description", out var d) ? d.GetString() : "No description";

    var card = Cards.CreateSelectItemCard(itemId, title, description);
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder().WithAdaptiveCard(card).Build();

    var response = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachment)
        .Build();

    return new CoreInvokeResponse(200, response);
});

// ==================== MESSAGE EXTENSION FETCH TASK ====================
bot.OnFetchTask(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnFetchTask");

    MessageExtensionAction? action = context.Activity.Value;

    var card = Cards.CreateFetchTaskCard(action?.CommandId ?? "unknown");
    var response = TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Continue)
        .WithTitle("Task Module")
        .WithCard(card)
        .Build();

    return new CoreInvokeResponse(200, response);
});

// Helper: Extract title and description from preview card
(string?, string?) GetDataFromPreview(TeamsActivity? preview)
{
    if (preview?.Attachments == null) return (null, null);

    var cardData = JsonSerializer.Deserialize<JsonElement>(
        JsonSerializer.Serialize(preview.Attachments[0].Content));

    if (!cardData.TryGetProperty("body", out var body) || body.ValueKind != JsonValueKind.Array)
        return (null, null);

    var title = body.GetArrayLength() > 0 && body[0].TryGetProperty("text", out var t) ? t.GetString() : null;
    var description = body.GetArrayLength() > 1 && body[1].TryGetProperty("text", out var d) ? d.GetString() : null;

    return (title, description);
}


// ==================== MESSAGE EXTENSION SUBMIT ACTION ====================
bot.OnSubmitAction(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnSubmitAction");

    MessageExtensionAction? action = context.Activity.Value;

    // Handle "edit" - user clicked edit on the preview, show the form again
    if (action?.BotMessagePreviewAction == "edit")
    {
        Console.WriteLine("Handling EDIT action - returning to form");
        var (previewTitle, previewDescription) = GetDataFromPreview(action.BotActivityPreview?.FirstOrDefault());

        var card = Cards.CreateEditFormCard(previewTitle, previewDescription);
        TaskModuleResponse response = TaskModuleResponse.CreateBuilder()
            .WithType(TaskModuleResponseType.Continue)
            .WithTitle("Edit Card")
            .WithCard(card)
            .Build();

        return new CoreInvokeResponse(200, response);
    }

    // Handle "send" - user clicked send on the preview, finalize the card
    //TODO : when I start from the compose box or message, i get an error at this point but seems to be a teams issue ( no activity is sent on clicking send)
    if (action?.BotMessagePreviewAction == "send")
    {
        Console.WriteLine("Handling SEND action - finalizing card");
        var (previewTitle, previewDescription) = GetDataFromPreview(action.BotActivityPreview?.FirstOrDefault());
        Console.WriteLine($"  Title: {previewTitle}, Description: {previewDescription}");

        var card = Cards.CreateSubmitActionCard(previewTitle, previewDescription);
        TeamsAttachment attachment2 = TeamsAttachment.CreateBuilder().WithAdaptiveCard(card).Build();

        MessagingExtensionResponse response = MessagingExtensionResponse.CreateBuilder()
            .WithType(MessagingExtensionResponseType.Result)
            .WithAttachmentLayout(TeamsAttachmentLayout.List)
            .WithAttachments(attachment2)
            .Build();

        return new CoreInvokeResponse(200, response);
    }

    var data = action?.Data as JsonElement?;
    string? title = data !=null && data.Value.TryGetProperty("title", out var t) ? t.GetString() : "Untitled";
    string? description = data != null && data.Value.TryGetProperty("description", out var d) ? d.GetString() : "No description";

    var previewCard = Cards.CreateSubmitActionCard(title, description);
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder().WithAdaptiveCard(previewCard).Build();

    MessagingExtensionResponse previewResponse = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.BotMessagePreview)
        .WithActivityPreview(new MessageActivity([attachment]))
        .Build();

    return new CoreInvokeResponse(200, previewResponse);
});

// ==================== MESSAGE EXTENSION QUERY LINK ====================
bot.OnQueryLink(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnMessageExtensionQueryLink");

    var queryLink = context.Activity.Value;

    var card = Cards.CreateLinkUnfurlCard(queryLink?.Url?.ToString());
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
        .WithContent(card).WithContentType(AttachmentContentType.ThumbnailCard).Build();

    MessagingExtensionResponse response = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachment)
        .Build();

    return new CoreInvokeResponse(200, response);
});

// ==================== MESSAGE EXTENSION ANON QUERY LINK ====================
//TODO : difficult to test, app must be published to catalog
bot.OnAnonQueryLink(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnAnonQueryLink");

    var anonQueryLink = context.Activity.Value;
    if (anonQueryLink != null)
    {
        Console.WriteLine($"  URL: '{anonQueryLink.Url}'");
    }

    var card = Cards.CreateLinkUnfurlCard(anonQueryLink?.Url?.ToString());
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
        .WithContent(card).WithContentType(AttachmentContentType.ThumbnailCard).Build();

    MessagingExtensionResponse response = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachment)
        .Build();

    return new CoreInvokeResponse(200, response);
});


//TODO : i can trigger this, but no response shows up
// ==================== MESSAGE EXTENSION QUERY SETTING URL ====================
bot.OnQuerySettingUrl(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnQuerySettingUrl");

    var query = context.Activity.Value;

    var action = new MessagingExtensionAction
    {
        Type = "openUrl",
        Value = "https://www.microsoft.com",
        Title = "Configure Extension"
    };

    MessagingExtensionResponse response = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.Config)
        .WithSuggestedActions(action)
        .Build();

    return new CoreInvokeResponse(200, response);
});


//TODO : this is deprecated ?
// ==================== MESSAGE EXTENSION CARD BUTTON CLICKED ====================
//bot.OnCardButtonClicked(async (context, cancellationToken) =>
//{
//    Console.WriteLine("✓ OnCardButtonClicked");
//    Console.WriteLine($"  Activity Type: {context.Activity.GetType().Name}");
//
//    return new CoreInvokeResponse(200);
//});

//TODO : only able to get OnQuerySettingUrl activity, how do we get onSetting or OnConfigFetch
/*
// ==================== MESSAGE EXTENSION SETTING ====================
bot.OnSetting(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnSetting");

    var query = context.Activity.Value;
    if (query != null)
    {
        Console.WriteLine($"  Command ID: '{query.CommandId}'");
    }

    var action = new MessagingExtensionAction
    {
        Type = "openUrl",
        Value = "https://microsoft.com",
        Title = "Configure Settings"
    };

    var response = MessagingExtensionResponse.CreateBuilder()
        .WithType(MessagingExtensionResponseType.Config)
        .WithSuggestedActions(action)
        .Build();

    return new CoreInvokeResponse(200, response);
});

// ==================== CONFIG FETCH ====================
bot.OnConfigFetch(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnConfigFetch");

    var card = new
    {
        contentType = AttachmentContentType.AdaptiveCard,
        content = new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = "Extension Settings", size = "large", weight = "bolder" },
                new { type = "TextBlock", text = "Configure your messaging extension settings below:", wrap = true },
                new { type = "Input.Text", id = "apiKey", label = "API Key", placeholder = "Enter your API key" },
                new { type = "Input.Toggle", id = "enableNotifications", label = "Enable Notifications", value = "true" }
            },
            actions = new object[]
            {
                new { type = "Action.Submit", title = "Save Settings" }
            }
        }
    };

    var response = TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Continue)
        .WithTitle("Configure Messaging Extension")
        .WithHeight(TaskModuleSize.Medium)
        .WithWidth(TaskModuleSize.Medium)
        .WithCard(card)
        .Build();

    return new CoreInvokeResponse(200, response);
});

// ==================== CONFIG SUBMIT ====================
bot.OnConfigSubmit(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnConfigSubmit");

    var data = context.Activity.Value;
    Console.WriteLine($"  Config data: {System.Text.Json.JsonSerializer.Serialize(data)}");

    // In a real app, you would save these settings to a database
    // associated with the user/team

    var response = TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Message)
        .WithMessage("Settings saved successfully!")
        .Build();

    return new CoreInvokeResponse(200, response);
});
*/


bot.Run();
