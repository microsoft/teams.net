using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType Typing = new("typing");
    public bool IsTyping => Typing.Equals(Value);
}

public class TypingActivity : Activity
{
    [JsonPropertyName("text")]
    [JsonPropertyOrder(31)]
    public string? Text { get; set; }

    public TypingActivity() : base(ActivityType.Typing)
    {
    }

    public TypingActivity(string text) : base(ActivityType.Typing)
    {
        Text = text;
    }

    public TypingActivity AddStreamUpdate(int sequence = 1)
    {
        ChannelData ??= new();
        ChannelData.StreamId ??= Id;
        ChannelData.StreamType ??= StreamType.Streaming;
        ChannelData.StreamSequence ??= sequence;

        AddEntity(new StreamInfoEntity()
        {
            StreamId = Id,
            StreamType = ChannelData.StreamType,
            StreamSequence = ChannelData.StreamSequence
        });

        return this;
    }
}