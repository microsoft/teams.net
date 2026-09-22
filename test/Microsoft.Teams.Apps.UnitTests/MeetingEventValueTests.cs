// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.Teams.Apps.Meetings;

namespace Microsoft.Teams.Apps.UnitTests;

public class MeetingEventValueTests
{
    // Captured from a live meeting held inside a channel.
    // A channel meeting has no join link, so the platform sends JoinUrl as null on both the start and the end event.
    private const string ChannelMeetingStartValue = """
    {
        "MeetingType": "",
        "Title": "Meeting in \"General\" ",
        "Id": "MCMxOTpPOThDcWI2UHJIVExzMUB0aHJlYWQudGFjdjIjMTc4OTY2NjYwODA4MA==",
        "JoinUrl": null,
        "StartTime": "2026-09-17T17:10:02.000000Z"
    }
    """;

    private const string ChannelMeetingEndValue = """
    {
        "MeetingType": "",
        "Title": "Meeting in \"General\" ",
        "Id": "MCMxOTpPOThDcWI2UHJIVExzMUB0aHJlYWQudGFjdjIjMTc4OTY2NjYwODA4MA==",
        "JoinUrl": null,
        "StartTime": null,
        "EndTime": "2026-09-17T17:40:13.081877Z"
    }
    """;

    [Fact]
    public void MeetingStartValue_DeserializesChannelMeetingWithNullJoinUrl()
    {
        MeetingStartValue? value = JsonSerializer.Deserialize<MeetingStartValue>(ChannelMeetingStartValue);

        Assert.NotNull(value);
        Assert.Null(value.JoinUrl);
        Assert.Equal("Meeting in \"General\" ", value.Title);
        Assert.Equal("MCMxOTpPOThDcWI2UHJIVExzMUB0aHJlYWQudGFjdjIjMTc4OTY2NjYwODA4MA==", value.Id);
    }

    [Fact]
    public void MeetingEndValue_DeserializesChannelMeetingWithNullJoinUrl()
    {
        MeetingEndValue? value = JsonSerializer.Deserialize<MeetingEndValue>(ChannelMeetingEndValue);

        Assert.NotNull(value);
        Assert.Null(value.JoinUrl);
        Assert.Equal("Meeting in \"General\" ", value.Title);
        Assert.Equal("MCMxOTpPOThDcWI2UHJIVExzMUB0aHJlYWQudGFjdjIjMTc4OTY2NjYwODA4MA==", value.Id);
    }

    [Fact]
    public void MeetingStartValue_DeserializesJoinUrlWhenPresent()
    {
        const string scheduledMeetingValue = """
        {
            "MeetingType": "Scheduled",
            "Title": "Weekly sync",
            "Id": "meeting-id",
            "JoinUrl": "https://teams.microsoft.com/l/meetup-join/19%3ameeting_id%40thread.v2/0",
            "StartTime": "2026-09-17T17:10:02.000000Z"
        }
        """;

        MeetingStartValue? value = JsonSerializer.Deserialize<MeetingStartValue>(scheduledMeetingValue);

        Assert.NotNull(value);
        Assert.NotNull(value.JoinUrl);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/19%3ameeting_id%40thread.v2/0", value.JoinUrl.ToString());
    }

    // Nullable reference types are erased at runtime, so System.Text.Json assigns null to a non-nullable
    // property without complaint and the deserialization tests above pass whether or not the annotation is present.
    // The annotation is what tells callers to null-check, so it is asserted directly.
    [Theory]
    [InlineData(typeof(MeetingStartValue))]
    [InlineData(typeof(MeetingEndValue))]
    public void JoinUrl_IsAnnotatedNullable(Type valueType)
    {
        var property = valueType.GetProperty(nameof(MeetingStartValue.JoinUrl));
        Assert.NotNull(property);

        var nullability = new NullabilityInfoContext().Create(property);

        Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
    }
}
