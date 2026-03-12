// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace TeamsBot;

internal class Cards
{
    public static object ResponseCard(string? feedback) => new JsonObject
    {
        ["type"] = "AdaptiveCard",
        ["version"] = "1.4",
        ["body"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = "Form Submitted Successfully! ✓",
                ["weight"] = "Bolder",
                ["size"] = "Large",
                ["color"] = "Good"
            },
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = $"You entered: **{feedback ?? "(empty)"}**",
                ["wrap"] = true
            }
        }
    };

    public static object ReactionsCard(string? reactionsAdded, string? reactionsRemoved) => new JsonObject
    {
        ["type"] = "AdaptiveCard",
        ["version"] = "1.4",
        ["body"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = "Reaction Received",
                ["weight"] = "Bolder",
                ["size"] = "Medium"
            },
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = $"Reactions Added: {reactionsAdded ?? "(empty)"}",
                ["wrap"] = true
            },
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = $"Reactions Removed: {reactionsRemoved ?? "(empty)"}",
                ["wrap"] = true
            }
        }
    };

    public static readonly object FeedbackCardObj = new JsonObject
    {
        ["type"] = "AdaptiveCard",
        ["version"] = "1.4",
        ["body"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = "Please provide your feedback:",
                ["weight"] = "Bolder",
                ["size"] = "Medium"
            },
            new JsonObject
            {
                ["type"] = "Input.Text",
                ["id"] = "feedback",
                ["placeholder"] = "Enter your feedback here",
                ["isMultiline"] = true
            }
        },
        ["actions"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Action.Execute",
                ["title"] = "Submit Feedback"
            }
        }
    };
}
