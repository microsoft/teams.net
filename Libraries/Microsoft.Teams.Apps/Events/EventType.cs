using Microsoft.Teams.Common;

namespace Microsoft.Teams.Apps.Events;

public class EventType(string value) : StringEnum(value)
{
    public static readonly EventType Start = new("start");
    public bool IsStart => Start.Equals(Value);

    public static readonly EventType Error = new("error");
    public bool IsError => Error.Equals(Value);

    public static readonly EventType Activity = new("activity");
    public bool IsActivity => Activity.Equals(Value);

    public static readonly EventType ActivitySent = new("activity.sent");
    public bool IsActivitySent => ActivitySent.Equals(Value);

    public static readonly EventType ActivityResponse = new("activity.response");
    public bool IsActivityResponse => ActivityResponse.Equals(Value);

    public bool IsBuiltIn => IsStart || IsError || IsActivity || IsActivitySent || IsActivityResponse;
}