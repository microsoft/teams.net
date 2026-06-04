// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Cards;

namespace ExtAIBot;

// Provides local AIFunction definitions that the model can call during a turn.
internal static class LocalTools
{
    // Returns a fresh AIFunction each turn; pendingCards is a per-turn accumulator
    // captured by closure.

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public static AIFunction CreateClarificationCardTool(IList<object> pendingCards, ILogger logger) =>
        AIFunctionFactory.Create(
            ([Description("The clarification question to ask the user.")] string question,
             [Description("2–4 candidate interpretations the user can pick between.")] string[] options) =>
            {
                logger.LogInformation("[tool] request_clarification(question={Question}, options=[{Options}])",
                    question, string.Join(", ", options));
                pendingCards.Add(BuildClarificationCard(question, options));
                return "Clarification card attached.";
            },
            "request_clarification",
            "Show an Adaptive Card asking the user to clarify their request when needed. " +
            "The user picks one option and submits; their choice arrives as the next user turn.");

    private static JsonElement BuildClarificationCard(string question, string[] options)
    {
        AdaptiveCard card = new AdaptiveCard(
            new TextBlock(question)
                .WithSize(TextSize.Medium)
                .WithWeight(TextWeight.Bolder)
                .WithWrap(true),
            new ChoiceSetInput([.. options.Select(o => new Choice { Title = o, Value = o })])
                .WithId("clarificationChoice")
                .WithIsRequired(true)
                .WithErrorMessage("Please pick one option."))
            .WithVersion(Microsoft.Teams.Cards.Version.Version1_6)
            .WithActions(
                new ExecuteAction()
                    .WithTitle("Submit")
                    .WithVerb("clarification")
                    .WithAssociatedInputs(AssociatedInputs.Auto));

        return JsonSerializer.SerializeToElement(card, SerializerOptions);
    }
}
