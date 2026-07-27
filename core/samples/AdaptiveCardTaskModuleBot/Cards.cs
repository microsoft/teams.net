// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace AdaptiveCardTaskModuleBot;

public static class Cards
{
    public static JsonElement CreateWelcomeCard()
    {
        AdaptiveCard card = new([
            new TextBlock("Welcome to InvokesBot!")
            {
                Size = TextSize.Large,
                Weight = TextWeight.Bolder
            },
            new TextBlock("Click the buttons below to test different invoke handlers:")])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4,
            Actions =
            [
                new ExecuteAction
                {
                    Title = "Test Adaptive Card Action",
                    Verb = "testAction"
                },
                new SubmitAction
                {
                    Title = "Open Task Module",
                    Data = new Union<string, SubmitActionData>(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?>
                        {
                            ["msteams"] = new
                            {
                                type = "task/fetch"
                            }
                        }
                    })
                },
                new ExecuteAction
                {
                    Title = "Request File Upload",
                    Verb = "requestFileUpload"
                }
            ]
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static JsonObject CreateFileConsentCard()
    {
        return new JsonObject
        {
            ["description"] = "This is a sample file to demonstrate file consent",
            ["sizeInBytes"] = 1024,
            ["acceptContext"] = new JsonObject
            {
                ["fileId"] = "123456"
            },
            ["declineContext"] = new JsonObject
            {
                ["fileId"] = "123456"
            }
        };
    }

    public static JsonElement CreateAdaptiveActionResponseCard(string? verb, string? message)
    {
        AdaptiveCard card = new([
            new TextBlock($"Action '{verb}' executed")
            {
                Weight = TextWeight.Bolder
            },
            new TextBlock($"Message: {message}")
            {
                Wrap = true
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4
        };

        return JsonSerializer.SerializeToElement(card);
    }

    public static JsonElement CreateTaskModuleCard()
    {
        AdaptiveCard card = new([
            new TextBlock("Task Module")])
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

    public static JsonObject CreateFileInfoCard(string? uniqueId, string? fileType)
    {
        return new JsonObject
        {
            ["uniqueId"] = uniqueId,
            ["fileType"] = fileType
        };
    }
}
