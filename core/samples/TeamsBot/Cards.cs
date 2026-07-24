// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Cards;

namespace TeamsBot;

internal class Cards
{
    public static JsonElement ResponseCard(string? feedback)
    {
        AdaptiveCard card = new([
            new TextBlock("Form Submitted Successfully! ✓")
            {
                Weight = TextWeight.Bolder,
                Size = TextSize.Large,
                Wrap = true
            },
            new TextBlock($"You entered: **{feedback ?? "(empty)"}**")
            {
                Wrap = true
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static readonly JsonElement FeedbackCardObj = CreateFeedbackCard();

    private static JsonElement CreateFeedbackCard()
    {
        AdaptiveCard card = new([
            new TextBlock("Please provide your feedback:")
            {
                Weight = TextWeight.Bolder,
                Size = TextSize.Medium,
                Wrap = true
            },
            new TextInput
            {
                Id = "feedback",
                Placeholder = "Enter your feedback here",
                IsMultiline = true
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4,
            Actions =
            [
                new ExecuteAction
                {
                    Title = "Submit Feedback"
                }
            ]
        };

        return JsonSerializer.SerializeToElement(card);
    }
}
