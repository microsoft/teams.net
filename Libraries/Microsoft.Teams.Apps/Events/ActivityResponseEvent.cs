namespace Microsoft.Teams.Apps.Events;

public class ActivityResponseEvent : Event
{
    public required Response Response { get; set; }
}