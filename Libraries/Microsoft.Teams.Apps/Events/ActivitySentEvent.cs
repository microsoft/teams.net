using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Events;

public class ActivitySentEvent : Event
{
    public required IActivity Activity { get; set; }
}