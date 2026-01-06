// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace CompatBot;

internal class Cards
{
    public static object FeedbackCardJson = new
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
