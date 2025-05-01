using System.Text.Json.Serialization;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Events;

public class ActivityEvent : IEvent
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

    [JsonPropertyName("chat")]
    [JsonPropertyOrder(3)]
    public Conversation Chat { get; set; }

    [JsonPropertyName("error")]
    [JsonPropertyOrder(4)]
    public object? Error { get; set; }

    [JsonPropertyName("sentAt")]
    [JsonPropertyOrder(5)]
    public DateTime SentAt { get; }

    public ActivityEvent(string type, IActivity body, Conversation chat)
    {
        Id = Guid.NewGuid();
        Type = $"activity.{type}";
        Body = body;
        Chat = chat;
        SentAt = DateTime.Now;
    }

    public static ActivityEvent Received(IActivity body, Conversation chat)
    {
        return new("received", body, chat);
    }

    public static ActivityEvent Sent(IActivity body, Conversation chat)
    {
        return new("sent", body, chat);
    }

    public static ActivityEvent Err(IActivity body, Conversation chat, object error)
    {
        return new("error", body, chat) { Error = error };
    }
}