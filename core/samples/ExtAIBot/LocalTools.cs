// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace ExtAIBot;

// Provides local AIFunction definitions that the model can call during a turn.
static class LocalTools
{
    // Returns a fresh AIFunction each turn; pendingCards is a per-turn accumulator
    // captured by closure.

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public static AIFunction CreateClarificationCardTool(IList<object> pendingCards) =>
        AIFunctionFactory.Create(
            ([Description("The clarification question to ask the user.")] string question,
             [Description("2–4 candidate interpretations the user can pick between.")] string[] options) =>
            {
                pendingCards.Add(BuildClarificationCard(question, options));
                return "Clarification card attached.";
            },
            "request_clarification",
            "Show an Adaptive Card asking the user to clarify their request. " +
            "The card IS the entire response — do not emit any text alongside " +
            "or after calling this tool. " +
            "Use only when the user's message is genuinely ambiguous and you cannot answer " +
            "without knowing which of several interpretations they meant. The user picks " +
            "one option and submits; their choice arrives as the next user turn.");

    private static JsonElement BuildClarificationCard(string question, string[] options)
    {
        SubmitActionData submitData = new();
        submitData.NonSchemaProperties["actionName"] = "clarification";

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
                new SubmitAction()
                    .WithTitle("Submit")
                    .WithData(new Union<string, SubmitActionData>(submitData))
                    .WithAssociatedInputs(AssociatedInputs.Auto));

        return JsonSerializer.SerializeToElement(card, SerializerOptions);
    }
}
