using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Events;

public class ErrorEvent : Event
{
    public required Exception Exception { get; set; }
    public IContext<IActivity>? Context { get; set; }
}