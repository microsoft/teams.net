// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.DevTools.Models;

namespace Microsoft.Teams.Bot.DevTools.Events;

/// <summary>
/// Event emitted over WebSocket when a client connects, carrying app metadata.
/// </summary>
public class MetaDataEvent : IDevToolsEvent
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public Guid Id { get; }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public string Type { get; }

    /// <inheritdoc/>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(2)]
    public object? Body { get; }

    /// <inheritdoc/>
    [JsonPropertyName("sentAt")]
    [JsonPropertyOrder(3)]
    public DateTime SentAt { get; }

    /// <summary>
    /// Creates a new metadata event.
    /// </summary>
    public MetaDataEvent(DevToolsMetaData body)
    {
        Id = Guid.NewGuid();
        Type = "metadata";
        Body = body;
        SentAt = DateTime.Now;
    }
}
