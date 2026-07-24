// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Cards;

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

    public static JsonElement CreateSelectItemCard(string? itemId, string? title, string? description)
    {
        AdaptiveCard card = new([
            new TextBlock(title ?? string.Empty)
            {
                Size = TextSize.Large,
                Weight = TextWeight.Bolder
            },
            new TextBlock(description ?? string.Empty)
            {
                Wrap = true
            },
            new FactSet(new List<Fact>
            {
                new("Item ID:", itemId ?? string.Empty)
            })])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static JsonElement CreateFetchTaskCard(string? commandId)
    {
        AdaptiveCard card = new([
            new TextBlock($"Fetch Task for: {commandId}")
            {
                Size = TextSize.Large,
                Weight = TextWeight.Bolder
            },
            new TextInput
            {
                Id = "title",
                Label = "Title",
                Placeholder = "Enter a title"
            },
            new TextInput
            {
                Id = "description",
                Label = "Description",
                Placeholder = "Enter a description",
                IsMultiline = true
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4,
            Actions =
            [
                new SubmitAction
                {
                    Title = "Submit"
                }
            ]
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static JsonElement CreateEditFormCard(string? previewTitle, string? previewDescription)
    {
        AdaptiveCard card = new([
            new TextBlock("Edit Your Card")
            {
                Size = TextSize.Large,
                Weight = TextWeight.Bolder
            },
            new TextInput
            {
                Id = "title",
                Label = "Title",
                Placeholder = "Enter a title",
                Value = previewTitle
            },
            new TextInput
            {
                Id = "description",
                Label = "Description",
                Placeholder = "Enter a description",
                IsMultiline = true,
                Value = previewDescription
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4,
            Actions =
            [
                new SubmitAction
                {
                    Title = "Submit"
                }
            ]
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static JsonElement CreateSubmitActionCard(string? title, string? description)
    {
        AdaptiveCard card = new([
            new TextBlock(title ?? "Untitled")
            {
                Size = TextSize.Large,
                Weight = TextWeight.Bolder
            },
            new TextBlock(description ?? "No description")
            {
                Wrap = true
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static object CreateLinkUnfurlCard(string? url)
    {
        return new { title = $"Link Unfurled: {url}" };
    }
}
