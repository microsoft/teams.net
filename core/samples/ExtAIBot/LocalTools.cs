// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Cards;

namespace ExtAIBot;

// Provides local AIFunction definitions that the model can call during a turn.
static class LocalTools
{
    // Returns a fresh AIFunction each turn; pendingCards is a per-turn accumulator
    // captured by closure.
    public static AIFunction CreateWelcomeCardTool(IList<object> pendingCards) =>
        AIFunctionFactory.Create(
            ([Description("Greeting message for the user, e.g. 'Hello, Alex!'")] string greeting) =>
            {
                pendingCards.Add(BuildWelcomeCard(greeting));
                return "Card attached.";
            },
            "send_welcome_card",
            "Attach a welcome Adaptive Card that shows the bot's capabilities.");

    private static AdaptiveCard BuildWelcomeCard(string greeting) =>
        new AdaptiveCard(
            new TextBlock($"{greeting} Here are some things I can do:")
                .WithSize(TextSize.Large)
                .WithWeight(TextWeight.Bolder)
                .WithWrap(true),
            new FactSet(
                new Fact("Memory",    "Per-conversation context across turns"),
                new Fact("Streaming", "Token-by-token replies as the model generates them"),
                new Fact("Tools",     "Local functions + remote MCP servers"),
                new Fact("Docs",      "Microsoft Learn search with inline citations"),
                new Fact("Feedback",  "Thumbs up / down with a follow-up form")));
}
