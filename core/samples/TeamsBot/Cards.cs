// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace TeamsBot;

internal class Cards
{
    public static object ResponseCard(string? feedback) => new
    {
        type = "AdaptiveCard",
        version = "1.4",
        body = new object[]
            {
                    new
                    {
                        type = "TextBlock",
                        text = "Form Submitted Successfully! ✓",
                        weight = "Bolder",
                        size = "Large",
                        color = "Good"
                    },
                    new
                    {
                        type = "TextBlock",
                        text = $"You entered: **{feedback ?? "(empty)"}**",
                        wrap = true
                    }
            }
    };

    public static object ReactionsCard(string? reactionsAdded, string? reactionsRemoved) => new
    {
        type = "AdaptiveCard",
        version = "1.4",
        body = new object[]
            {
                    new
                    {
                        type = "TextBlock",
                        text = "Reaction Received",
                        weight = "Bolder",
                        size = "Medium"
                    },
                    new
                    {
                        type = "TextBlock",
                        text = $"Reactions Added: {reactionsAdded ?? "(empty)"}",
                        wrap = true
                    },
                    new
                    {
                        type = "TextBlock",
                        text = $"Reactions Removed: {reactionsRemoved ?? "(empty)"}",
                        wrap = true
                    }
            }
    };

    public static readonly object FeedbackCardObj = new
    {
        type = "AdaptiveCard",
        version = "1.4",
        body = new object[]
        {
            new
            {
                type = "TextBlock",
                text = "Please provide your feedback:",
                weight = "Bolder",
                size = "Medium"
            },
            new
            {
                type = "Input.Text",
                id = "feedback",
                placeholder = "Enter your feedback here",
                isMultiline = true
            }
        },
        actions = new object[]
        {
            new
            {
                type = "Action.Execute",
                title = "Submit Feedback"
            }
        }
    };
}
