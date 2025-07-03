// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Events;

[JsonConverter(typeof(JsonConverter<Name>))]
public partial class Name(string value) : StringEnum(value)
{
    public Type ToType()
    {
        if (IsReadReceipt) return typeof(ReadReceiptActivity);
        if (IsMeetingStart) return typeof(MeetingStartActivity);
        if (IsMeetingEnd) return typeof(MeetingEndActivity);
        if (IsMeetingParticipantJoin) return typeof(MeetingParticipantJoinActivity);
        if (IsMeetingParticipantLeave) return typeof(MeetingParticipantLeaveActivity);
        return typeof(EventActivity);
    }

    public string ToPrettyString()
    {
        var value = ToString();
        return $"{value.First().ToString().ToUpper()}{value.AsSpan(1).ToString()}";
    }
}