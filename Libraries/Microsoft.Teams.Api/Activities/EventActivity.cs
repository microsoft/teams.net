// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType Event = new("event");
    public bool IsEvent => Event.Equals(Value);
}

public interface IEventActivity
{
    /// <summary>
    /// The name of the operation associated with an invoke or event activity.
    /// </summary>
    public Events.Name Name { get; set; }
}

[JsonConverter(typeof(JsonConverter))]
public partial class EventActivity(Events.Name name) : Activity(ActivityType.Event), IEventActivity
{
    /// <summary>
    /// The name of the operation associated with an invoke or event activity.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(31)]
    public Events.Name Name { get; set; } = name;

    public override string GetPath()
    {
        return string.Join(".", ["Activity", Type.ToPrettyString(), Name.ToPrettyString()]);
    }

    public Events.MeetingStartActivity ToMeetingStart() => (Events.MeetingStartActivity)this;
    public Events.MeetingEndActivity ToMeetingEnd() => (Events.MeetingEndActivity)this;
    public Events.MeetingParticipantJoinActivity ToMeetingParticipantJoin() => (Events.MeetingParticipantJoinActivity)this;
    public Events.MeetingParticipantLeaveActivity ToMeetingParticipantLeave() => (Events.MeetingParticipantLeaveActivity)this;
    public Events.ReadReceiptActivity ToReadReceipt() => (Events.ReadReceiptActivity)this;

    public override object ToType(Type type, IFormatProvider? provider)
    {
        if (type == Events.Name.ReadReceipt.ToType()) return ToReadReceipt();
        if (type == Events.Name.MeetingStart.ToType()) return ToMeetingStart();
        if (type == Events.Name.MeetingEnd.ToType()) return ToMeetingEnd();
        if (type == Events.Name.MeetingParticipantJoin.ToType()) return ToMeetingParticipantJoin();
        if (type == Events.Name.MeetingParticipantLeave.ToType()) return ToMeetingParticipantLeave();
        return this;
    }
}