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
        return MessageExtensionResponse.CreateBuilder()
            .WithType(MessageExtensionResponseType.Message)
            .WithText("💡 Search for any keyword to see results.")
            .Build();
    }

    // Create results with tap actions to trigger OnSelectItem
    var cards = Cards.CreateQueryResultCards(searchText);
    TeamsAttachment[] attachments = [.. cards.Select(card => TeamsAttachment.CreateBuilder().WithContent(card)
        .WithContentType(AttachmentContentType.ThumbnailCard).Build())];

    return MessageExtensionResponse.CreateBuilder()
        .WithType(MessageExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachments)
        .Build();
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

    return MessageExtensionResponse.CreateBuilder()
        .WithType(MessageExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachment)
        .Build();
});

// ==================== MESSAGE EXTENSION FETCH TASK ====================
bot.OnFetchTask(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnFetchTask");

    MessageExtensionAction? action = context.Activity.Value;

    var fetchTaskCard = Cards.CreateFetchTaskCard(action?.CommandId ?? "unknown");
    TeamsAttachment fetchTaskCardResponse = TeamsAttachment.CreateBuilder()
        .WithAdaptiveCard(fetchTaskCard).Build();
    return MessageExtensionActionResponse.CreateBuilder()
            .WithTask(TaskModuleResponse.CreateBuilder()
                .WithType(TaskModuleResponseType.Continue)
                .WithTitle("Task Module")
                .WithCard(fetchTaskCardResponse))
            .Build();
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

        var editFormCard = Cards.CreateEditFormCard(previewTitle, previewDescription);
        TeamsAttachment editFormCardResponse = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(editFormCard).Build();
        return MessageExtensionActionResponse.CreateBuilder()
            .WithTask(TaskModuleResponse.CreateBuilder()
                .WithType(TaskModuleResponseType.Continue)
                .WithTitle("Edit Card")
                .WithCard(editFormCardResponse))
            .Build();
    }

    // Handle "send" - user clicked send on the preview, finalize the card
    //TODO : when I start from the compose box or message, i get an error at this point but seems to be a teams issue ( no activity is sent on clicking send)
    if (action?.BotMessagePreviewAction == "send")
    {
        Console.WriteLine("Handling SEND action - finalizing card");
        var (previewTitle, previewDescription) = GetDataFromPreview(action.BotActivityPreview?.FirstOrDefault());

        var card = Cards.CreateSubmitActionCard(previewTitle, previewDescription);
        TeamsAttachment attachment2 = TeamsAttachment.CreateBuilder().WithAdaptiveCard(card).Build();

        return MessageExtensionActionResponse.CreateBuilder()
            .WithComposeExtension(MessageExtensionResponse.CreateBuilder()
                .WithType(MessageExtensionResponseType.Result)
                .WithAttachmentLayout(TeamsAttachmentLayout.List)
                .WithAttachments(attachment2))
            .Build();
    }


    var data = action?.Data as JsonElement?;
    string? title = data != null && data.Value.TryGetProperty("title", out var t) ? t.GetString() : "Untitled";
    string? description = data != null && data.Value.TryGetProperty("description", out var d) ? d.GetString() : "No description";

    var previewCard = Cards.CreateSubmitActionCard(title, description);
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder().WithAdaptiveCard(previewCard).Build();

    return MessageExtensionActionResponse.CreateBuilder()
            .WithComposeExtension(MessageExtensionResponse.CreateBuilder()
                .WithType(MessageExtensionResponseType.BotMessagePreview)
                .WithActivityPreview(new MessageActivity([attachment]))
                )
            .Build();
});

// ==================== MESSAGE EXTENSION QUERY LINK ====================
bot.OnQueryLink(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnQueryLink");

    MessageExtensionQueryLink? queryLink = context.Activity.Value;

    var card = Cards.CreateLinkUnfurlCard(queryLink?.Url?.ToString());
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
        .WithContent(card).WithContentType(AttachmentContentType.ThumbnailCard).Build();

    return MessageExtensionResponse.CreateBuilder()
        .WithType(MessageExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachment)
        .Build();
});

// ==================== MESSAGE EXTENSION ANON QUERY LINK ====================
//TODO : difficult to test, app must be published to catalog
bot.OnAnonQueryLink(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnAnonQueryLink");

    MessageExtensionQueryLink? anonQueryLink = context.Activity.Value;
    if (anonQueryLink != null)
    {
        Console.WriteLine($"  URL: '{anonQueryLink.Url}'");
    }

    var card = Cards.CreateLinkUnfurlCard(anonQueryLink?.Url?.ToString());
    TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
        .WithContent(card).WithContentType(AttachmentContentType.ThumbnailCard).Build();

    return MessageExtensionResponse.CreateBuilder()
        .WithType(MessageExtensionResponseType.Result)
        .WithAttachmentLayout(TeamsAttachmentLayout.List)
        .WithAttachments(attachment)
        .Build();
});


// ==================== MESSAGE EXTENSION QUERY SETTING URL ====================
bot.OnQuerySettingUrl(async (context, cancellationToken) =>
{
    Console.WriteLine("✓ OnQuerySettingUrl");

    var query = context.Activity.Value;

    var action = new
    {
        Type = "openUrl",
        Value = "https://www.microsoft.com"    
    };

    return MessageExtensionResponse.CreateBuilder()
        .WithType(MessageExtensionResponseType.Config)
        .WithSuggestedActions([action])
        .Build();
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

    return new CoreInvokeResponse<MessageExtensionResponse>(200, response);
});
*/

bot.Run();