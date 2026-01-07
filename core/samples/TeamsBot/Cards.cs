// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace TeamsBot;

internal class Cards
{
    public static object FeedbackCardObj = new
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
