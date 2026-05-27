// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace McpServer;

public static class Cards
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static JsonElement AskCard(string requestId, string question)
    {
        AdaptiveCard card = new(
            new TextBlock(question) { Weight = TextWeight.Bolder, Size = TextSize.Medium, Wrap = true },
            new TextInput
            {
                Id = "reply",
                Placeholder = "Type your reply...",
                IsMultiline = true,
                IsRequired = true,
                ErrorMessage = "Please enter a reply."
            })
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4,
            Actions = new List<Microsoft.Teams.Cards.Action>
            {
                new ExecuteAction
                {
                    Title = "Send",
                    Verb = "ask_reply",
                    AssociatedInputs = AssociatedInputs.Auto,
                    Data = new Union<string, SubmitActionData>(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?>
                        {
                            ["request_id"] = requestId
                        }
                    })
                }
            }
        };
        return JsonSerializer.SerializeToElement(card, SerializerOptions);
    }

    public static JsonElement ApprovalCard(string approvalId, string title, string description)
    {
        AdaptiveCard card = new(
            new TextBlock(title) { Weight = TextWeight.Bolder, Size = TextSize.Large, Wrap = true },
            new TextBlock(description) { Wrap = true })
        {
            Version = Microsoft.Teams.Cards.Version.Version1_4,
            Actions = new List<Microsoft.Teams.Cards.Action>
            {
                new ExecuteAction
                {
                    Title = "Approve",
                    Verb = "approval_response",
                    Data = new Union<string, SubmitActionData>(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?>
                        {
                            ["approval_id"] = approvalId,
                            ["decision"] = ApprovalStatus.Approved
                        }
                    })
                },
                new ExecuteAction
                {
                    Title = "Reject",
                    Verb = "approval_response",
                    Data = new Union<string, SubmitActionData>(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?>
                        {
                            ["approval_id"] = approvalId,
                            ["decision"] = ApprovalStatus.Rejected
                        }
                    })
                }
            }
        };
        return JsonSerializer.SerializeToElement(card, SerializerOptions);
    }
}
