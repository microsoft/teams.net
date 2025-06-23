// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TokenExchange;

/// <summary>
/// State object passed to the bot token service.
/// </summary>
public class State
{
    /// <summary>
    /// The connection name that was used.
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(0)]
    public required string ConnectionName { get; set; }

    /// <summary>
    /// A reference to the conversation.
    /// </summary>
    [JsonPropertyName("conversation")]
    [JsonPropertyOrder(1)]
    public required ConversationReference Conversation { get; set; }

    /// <summary>
    /// A reference to a related parent conversation conversation.
    /// </summary>
    [JsonPropertyName("relatesTo")]
    [JsonPropertyOrder(2)]
    public ConversationReference? RelatesTo { get; set; }

    /// <summary>
    /// The URL of the bot messaging endpoint.
    /// </summary>
    [JsonPropertyName("msAppId")]
    [JsonPropertyOrder(3)]
    public required string MsAppId { get; set; }
}