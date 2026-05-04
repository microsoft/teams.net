// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace McpServer;

public static class Cards
{
    // Action.Execute fires an `adaptiveCard/action` invoke that lands in
    // OnAdaptiveCardAction; the `verb` field tells that handler which kind of
    // card click this is (here, "approval_response"). Action.Submit would
    // route elsewhere and wouldn't reach the same handler.
    public static JsonObject ApprovalCard(string approvalId, string title, string description) => new()
    {
        ["type"] = "AdaptiveCard",
        ["version"] = "1.4",
        ["body"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = title,
                ["weight"] = "Bolder",
                ["size"] = "Large",
                ["wrap"] = true
            },
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = description,
                ["wrap"] = true
            }
        },
        ["actions"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Action.Execute",
                ["title"] = "Approve",
                ["verb"] = "approval_response",
                ["data"] = new JsonObject
                {
                    ["approval_id"] = approvalId,
                    ["decision"] = "approved"
                }
            },
            new JsonObject
            {
                ["type"] = "Action.Execute",
                ["title"] = "Reject",
                ["verb"] = "approval_response",
                ["data"] = new JsonObject
                {
                    ["approval_id"] = approvalId,
                    ["decision"] = "rejected"
                }
            }
        }
    };
}
