// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();

WebApplication webApp = webAppBuilder.Build();
TeamsBotApplication bot = webApp.UseTeamsBotApplication();

bot.OnMessage(async (context, cancellationToken) =>
{
    string text = context.Activity.Text?.Trim() ?? string.Empty;

    if (text.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        string helpText = """
            **Common Handlers Bot**

            - `help` - Show this message

            **Handlers**
            - `OnMessage`
            - `OnMessageUpdate`
            - `OnMessageDelete`
            - `OnMessageReaction`
            - `OnMembersAdded`
            - `OnMembersRemoved`
            - `OnInstall`
            - `OnUnInstall`
            """;

        await context.SendAsync(
            new MessageActivityInput()
                .WithText(helpText, TextFormats.Markdown)
                ,
            cancellationToken);
        return;
    }

    await context.SendAsync("Send `help` to see the common handlers in this sample.", cancellationToken);
});

bot.OnMessageUpdate(async (context, cancellationToken) =>
{
    string updatedText = context.Activity.Text ?? "<no text>";
    await context.SendAsync($"I saw that you updated your message to: `{updatedText}`", cancellationToken);
});

bot.OnMessageDelete(async (context, cancellationToken) =>
{
    await context.SendAsync("I saw that message you deleted", cancellationToken);
});

bot.OnMessageReaction(async (context, cancellationToken) =>
{
    string reactionsAdded = string.Join(", ", context.Activity.ReactionsAdded?.Select(r => r.Type) ?? []);
    string reactionsRemoved = string.Join(", ", context.Activity.ReactionsRemoved?.Select(r => r.Type) ?? []);

    await context.SendAsync(
        $"Reactions added: {reactionsAdded}; reactions removed: {reactionsRemoved}",
        cancellationToken);
});

bot.OnMembersAdded(async (context, cancellationToken) =>
{
    string memberNames = string.Join(", ", context.Activity.MembersAdded?.Select(m => m.Name ?? m.Id) ?? []);
    await context.SendAsync($"Welcome! Members added: {memberNames}", cancellationToken);
});

bot.OnMembersRemoved(async (context, cancellationToken) =>
{
    string memberNames = string.Join(", ", context.Activity.MembersRemoved?.Select(m => m.Name ?? m.Id) ?? []);
    await context.SendAsync($"Goodbye! Members removed: {memberNames}", cancellationToken);
});

bot.OnInstall(async (context, cancellationToken) =>
{
    await context.SendAsync("Thanks for installing the common handlers bot!", cancellationToken);
});

bot.OnUnInstall((context, cancellationToken) =>
{
    return Task.CompletedTask;
});

webApp.Run();
