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

    public static readonly object TaskModuleLauncherCard = new JsonObject
    {
        ["type"] = "AdaptiveCard",
        ["version"] = "1.4",
        ["body"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = "Task Module Demo",
                ["weight"] = "Bolder",
                ["size"] = "Medium"
            },
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = "Click the button below to open a task module dialog.",
                ["wrap"] = true
            }
        },
        ["actions"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Action.Submit",
                ["title"] = "Open Task Module",
                ["data"] = new JsonObject
                {
                    ["msteams"] = new JsonObject
                    {
                        ["type"] = "task/fetch"
                    }
                }
            }
        }
    };

    public static readonly object TaskModuleFormCard = new JsonObject
    {
        ["type"] = "AdaptiveCard",
        ["version"] = "1.4",
        ["body"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = "Enter your details:",
                ["weight"] = "Bolder",
                ["size"] = "Medium"
            },
            new JsonObject
            {
                ["type"] = "Input.Text",
                ["id"] = "userName",
                ["label"] = "Name",
                ["placeholder"] = "Enter your name"
            },
            new JsonObject
            {
                ["type"] = "Input.Text",
                ["id"] = "userComment",
                ["label"] = "Comment",
                ["placeholder"] = "Enter a comment",
                ["isMultiline"] = true
            }
        },
        ["actions"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Action.Submit",
                ["title"] = "Submit",
                ["data"] = new JsonObject
                {
                    ["msteams"] = new JsonObject
                    {
                        ["type"] = "task/submit"
                    }
                }
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
