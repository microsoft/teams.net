// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable ExperimentalTeamsTargeted       // WithRecipient(targeted) is experimental.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.TaskModules;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace M365ExtensionsBot;

/// <summary>
/// Teams SDK routes that own matching Teams turns before the Agents SDK sees them.
/// </summary>
public class MyTeamsBot : TeamsBotApplication
{
    public MyTeamsBot(ApiClient api, IHttpContextAccessor accessor, ILogger<MyTeamsBot> logger, TeamsBotApplicationOptions? options = null)
        : base(api, accessor, logger, options)
    {
        this.OnMessage("help", async (context, ct) =>
        {
            var attachment = TeamsAttachment.CreateBuilder()
                .WithAdaptiveCard(HelpCard())
                .Build();

            await context.SendAsync(new MessageActivityInput().AddAttachment(attachment), ct);
        });

        this.OnMessage("react", async (context, ct) =>
        {
            var response = await context.SendAsync("React to this message! I'll add thumbs-up and remove it.", ct);
            if (response?.Id is null)
            {
                return;
            }

            string conversationId = context.Activity.Conversation!.Id;
            await Task.Delay(2000, ct);
            await context.Api.Conversations.AddReactionAsync(conversationId, response.Id, ReactionTypes.Like, cancellationToken: ct);
            await Task.Delay(2000, ct);
            await context.Api.Conversations.DeleteReactionAsync(conversationId, response.Id, ReactionTypes.Like, cancellationToken: ct);
        });

        this.OnMessage("quote", async (context, ct) =>
        {
            await context.ReplyAsync("Quoting your message!", ct);
        });

        this.OnMessage("targeted", async (context, ct) =>
        {
            var sender = context.Activity.From;
            var targeted = new MessageActivityInput()
                .WithText("👁️ This message is only visible to you.")
                .WithRecipient(new TeamsChannelAccount { Id = sender!.Id, Name = sender.Name }, isTargeted: true);
            await context.SendAsync(targeted, ct);
        });

        this.OnMessage("task", async (context, ct) =>
        {
            var attachment = TeamsAttachment.CreateBuilder()
                .WithAdaptiveCard(TaskLauncherCard())
                .Build();

            await context.SendAsync(new MessageActivityInput().AddAttachment(attachment), ct);
        });

        this.OnTaskFetch(async (context, ct) =>
        {
            var attachment = TeamsAttachment.CreateBuilder()
                .WithAdaptiveCard(TaskFormCard())
                .Build();

            return TaskModuleResponse.CreateBuilder()
                .WithType(TaskModuleResponseTypes.Continue)
                .WithTitle("Sample Task Module")
                .WithCard(attachment)
                .WithHeight(TaskModuleSizes.Medium)
                .WithWidth(TaskModuleSizes.Medium)
                .Build();
        });

        this.OnTaskSubmit(async (context, ct) =>
        {
            await context.SendAsync($"[Teams SDK] Task module submitted. Data: {context.Activity.Value?.Data}", ct);
            return TaskModuleResponse.CreateBuilder()
                .WithType(TaskModuleResponseTypes.Message)
                .WithMessage("Done.")
                .Build();
        });

        this.OnMessageReaction(async (context, ct) =>
        {
            var added = context.Activity.ReactionsAdded?.Select(reaction => reaction.Type?.ToString() ?? string.Empty).ToArray() ?? [];
            var removed = context.Activity.ReactionsRemoved?.Select(reaction => reaction.Type?.ToString() ?? string.Empty).ToArray() ?? [];
            await context.SendAsync($"[Teams SDK] Reactions: added=[{string.Join(", ", added)}] removed=[{string.Join(", ", removed)}]", ct);
        });
    }

    private static AdaptiveCard HelpCard()
        => new(
            new TextBlock("Teams SDK Feature Showcase")
                .WithWeight(TextWeight.Bolder)
                .WithSize(TextSize.Large)
                .WithWrap(true),
            new TextBlock("Teams SDK handlers (MyTeamsBot)")
                .WithWeight(TextWeight.Bolder)
                .WithSpacing(Spacing.Medium),
            new FactSet(
                new Fact("help", "This command list"),
                new Fact("react", "Bot adds/removes emoji reactions"),
                new Fact("quote", "Bot quotes your message"),
                new Fact("targeted", "Ephemeral message visible only to sender"),
                new Fact("task", "Task module fetch/submit flow")),
            new TextBlock("Agents SDK fallthrough handlers (MyAgent)")
                .WithWeight(TextWeight.Bolder)
                .WithSpacing(Spacing.Medium),
            new FactSet(
                new Fact("help", "Plain-text help on non-Teams channels"),
                new Fact("channel", "Report the channel and how it was routed"),
                new Fact("whoami", "Graph profile via OAuth connection graphuser"),
                new Fact("mail", "Recent mail via OAuth connection graphmail"),
                new Fact("signout", "Clear both OAuth handler caches"),
                new Fact("agents sdk react", "Reach Teams reactions API from the Agent SDK"),
                new Fact("agents sdk proactive", "Send via Teams SDK API client from the Agent SDK"),
                new Fact("anything else", "Echo via the Agent SDK")));

    private static AdaptiveCard TaskLauncherCard()
        => new AdaptiveCard(
            new TextBlock("Task module demo")
                .WithWeight(TextWeight.Bolder)
                .WithSize(TextSize.Medium),
            new TextBlock("Press the button to open a task module.")
                .WithWrap(true))
            .WithActions(
                new SubmitAction()
                    .WithTitle("Open task module")
                    .WithData(new Union<string, SubmitActionData>(
                        new SubmitActionData().WithMsteams(new { type = "task/fetch" }))));

    private static AdaptiveCard TaskFormCard()
        => new AdaptiveCard(
            new TextBlock("Task Module Form")
                .WithWeight(TextWeight.Bolder)
                .WithSize(TextSize.Medium),
            new TextInput()
                .WithId("note")
                .WithPlaceholder("Type here...")
                .WithLabel("Your response"))
            .WithActions(new SubmitAction().WithTitle("Submit"));
}
