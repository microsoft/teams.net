// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace A2ABot;

static class Cards
{
    public static JsonElement AskCardElement(string from, string question, string qid) =>
        JsonSerializer.SerializeToElement(new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = $"**Peer question from {from}:**", wrap = true },
                new { type = "TextBlock", text = question, wrap = true, spacing = "Small" },
                new
                {
                    type = "Input.Text",
                    id = "answer",
                    placeholder = "Type your answer here...",
                    isMultiline = true,
                    spacing = "Medium"
                }
            },
            actions = new[]
            {
                new
                {
                    type = "Action.Execute",
                    title = "Send Reply",
                    verb = "a2a-reply",
                    data = new { qid }
                }
            }
        });

    public static JsonElement ReplyCardElement(string from, string question, string answer) =>
        JsonSerializer.SerializeToElement(new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = $"**Reply from {from}:**", wrap = true },
                new { type = "TextBlock", text = $"Your question: {question}", wrap = true, isSubtle = true, size = "Small", spacing = "Small" },
                new { type = "TextBlock", text = answer, wrap = true, spacing = "Small" }
            }
        });

    public static JsonElement AttachmentElement(JsonElement cardContent) =>
        JsonSerializer.SerializeToElement(new[]
        {
            new { contentType = "application/vnd.microsoft.card.adaptive", content = cardContent }
        });
}
