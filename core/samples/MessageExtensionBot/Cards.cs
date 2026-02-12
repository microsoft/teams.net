// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MessageExtensionBot;

public static class Cards
{
    public static object[] CreateQueryResultCards(string searchText)
    {
        return new[]
        {
            new
            {
                title = $"Result 1: {searchText}",
                text = "Click to see full details",
                tap = new
                {
                    type = "invoke",
                    value = new
                    {
                        itemId = "item-1",
                        title = $"Full details for Result 1: {searchText}",
                        description = "This is the expanded content"
                    }
                }
            },
            new
            {
                title = $"Result 2: {searchText}",
                text = "Click to see full details",
                tap = new
                {
                    type = "invoke",
                    value = new
                    {
                        itemId = "item-2",
                        title = $"Full details for Result 2: {searchText}",
                        description = "This is more expanded content"
                    }
                }
            }
        };
    }

    public static object CreateSelectItemCard(string? itemId, string? title, string? description)
    {
        return new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = title, size = "large", weight = "bolder" },
                new { type = "TextBlock", text = description, wrap = true },
                new { type = "FactSet", facts = new[]
                    {
                        new { title = "Item ID:", value = itemId }
                    }
                }
            }
        };
    }

    public static object CreateFetchTaskCard(string? commandId)
    {
        return new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = $"Fetch Task for: {commandId}", size = "large", weight = "bolder" },
                new { type = "Input.Text", id = "title", label = "Title", placeholder = "Enter a title" },
                new { type = "Input.Text", id = "description", label = "Description", placeholder = "Enter a description", isMultiline = true }
            },
            actions = new object[]
            {
                new { type = "Action.Submit", title = "Submit" }
            }
        };
    }

    public static object CreateEditFormCard(string? previewTitle, string? previewDescription)
    {
        return new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = "Edit Your Card", size = "large", weight = "bolder" },
                new { type = "Input.Text", id = "title", label = "Title", placeholder = "Enter a title", value = previewTitle },
                new { type = "Input.Text", id = "description", label = "Description", placeholder = "Enter a description", isMultiline = true, value = previewDescription }
            },
            actions = new object[] { new { type = "Action.Submit", title = "Submit" } }
        };
    }

    public static object CreateSubmitActionCard(string? title, string? description)
    {
        return new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
                {
                    new { type = "TextBlock", text = title ?? "Untitled", size = "large", weight = "bolder", color = "accent" },
                    new { type = "TextBlock", text = description ?? "No description", wrap = true }
                }
        };
    }

    public static object CreateLinkUnfurlCard(string? url)
    {
        return new { title = $"Link Unfurled: {url}" };
    }
}
