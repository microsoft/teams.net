// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Bot.Apps.Schema;

namespace AllInvokesBot;

public static class Cards
{
    public static JsonObject CreateWelcomeCard()
    {
        return new JsonObject
        {
            ["type"] = "AdaptiveCard",
            ["version"] = "1.4",
            ["body"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = "Welcome to InvokesBot!",
                    ["size"] = "Large",
                    ["weight"] = "Bolder"
                },
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = "Click the buttons below to test different invoke handlers:"
                }
            },
            ["actions"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Action.Execute",
                    ["id"] = "1234",
                    ["title"] = "Test Adaptive Card Action",
                    ["verb"] = "testAction",
                    ["data"] = new JsonObject
                    {
                        ["message"] = "Button clicked!"
                    }
                },
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
                },
                new JsonObject
                {
                    ["type"] = "Action.Execute",
                    ["title"] = "Request File Upload",
                    ["verb"] = "requestFileUpload"
                }
            }
        };
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

    public static JsonObject CreateAdaptiveActionResponseCard(string? verb, string? message)
    {
        return new JsonObject
        {
            ["type"] = "AdaptiveCard",
            ["version"] = "1.4",
            ["body"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = $"Action '{verb}' executed",
                    ["weight"] = "Bolder"
                },
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = $"Message: {message}",
                    ["wrap"] = true
                }
            }
        };
    }

    public static JsonObject CreateTaskModuleCard()
    {
        return new JsonObject
        {
            ["type"] = "AdaptiveCard",
            ["version"] = "1.4",
            ["body"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = "Task Module"
                }
            },
            ["actions"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Action.Submit",
                    ["title"] = "Submit"
                }
            }
        };
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
