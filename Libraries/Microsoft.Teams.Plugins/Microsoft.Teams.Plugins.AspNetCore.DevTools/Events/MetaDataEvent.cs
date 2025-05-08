using System.Text.Json.Serialization;

using Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Events;

public class MetaDataEvent : IEvent
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public Guid Id { get; }

    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public string Type { get; }

    [JsonPropertyName("body")]
    [JsonPropertyOrder(2)]
    public object? Body { get; }

    [JsonPropertyName("sentAt")]
    [JsonPropertyOrder(3)]
    public DateTime SentAt { get; }

    public MetaDataEvent(MetaData body)
    {
        Id = Guid.NewGuid();
        Type = "metadata";
        Body = body;
        SentAt = DateTime.Now;
    }
}