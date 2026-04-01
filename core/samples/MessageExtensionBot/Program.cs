// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using MessageExtensionBot;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers.MessageExtension;
using Microsoft.Teams.Bot.Apps.Handlers.TaskModules;
using Microsoft.Teams.Bot.Apps.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

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
    object[] cards = Cards.CreateQueryResultCards(searchText);
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

    JsonElement selectedItem = context.Activity.Value;
    JsonElement? itemData = selectedItem;
    string? itemId = itemData.Value.TryGetProperty("itemId", out JsonElement id) ? id.GetString() : "unknown";
    string? title = itemData.Value.TryGetProperty("title", out JsonElement t) ? t.GetString() : "Selected Item";
    string? description = itemData.Value.TryGetProperty("description", out JsonElement d) ? d.GetString() : "No description";

    object card = Cards.CreateSelectItemCard(itemId, title, description);
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

    object fetchTaskCard = Cards.CreateFetchTaskCard(action?.CommandId ?? "unknown");
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
static (string?, string?) GetDataFromPreview(TeamsActivity? preview)
{
    if (preview?.Attachments == null) return (null, null);

    JsonElement cardData = JsonSerializer.Deserialize<JsonElement>(
        JsonSerializer.Serialize(preview.Attachments[0].Content));

    if (!cardData.TryGetProperty("body", out JsonElement body) || body.ValueKind != JsonValueKind.Array)
        return (null, null);

    string? title = body.GetArrayLength() > 0 && body[0].TryGetProperty("text", out JsonElement t) ? t.GetString() : null;
    string? description = body.GetArrayLength() > 1 && body[1].TryGetProperty("text", out JsonElement d) ? d.GetString() : null;

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
        (string? previewTitle, string? previewDescription) = GetDataFromPreview(action.BotActivityPreview?.FirstOrDefault());

        object editFormCard = Cards.CreateEditFormCard(previewTitle, previewDescription);
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
        (string? previewTitle, string? previewDescription) = GetDataFromPreview(action.BotActivityPreview?.FirstOrDefault());

        object card = Cards.CreateSubmitActionCard(previewTitle, previewDescription);
        TeamsAttachment attachment2 = TeamsAttachment.CreateBuilder().WithAdaptiveCard(card).Build();

        return MessageExtensionActionResponse.CreateBuilder()
            .WithComposeExtension(MessageExtensionResponse.CreateBuilder()
                .WithType(MessageExtensionResponseType.Result)
                .WithAttachmentLayout(TeamsAttachmentLayout.List)
                .WithAttachments(attachment2))
            .Build();
    }


    JsonElement? data = action?.Data as JsonElement?;
    string? title = data != null && data.Value.TryGetProperty("title", out JsonElement t) ? t.GetString() : "Untitled";
    string? description = data != null && data.Value.TryGetProperty("description", out JsonElement d) ? d.GetString() : "No description";

    object previewCard = Cards.CreateSubmitActionCard(title, description);
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

    object card = Cards.CreateLinkUnfurlCard(queryLink?.Url?.ToString());
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

    object card = Cards.CreateLinkUnfurlCard(anonQueryLink?.Url?.ToString());
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

    MessageExtensionQuery? query = context.Activity.Value;

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

webApp.Run();
