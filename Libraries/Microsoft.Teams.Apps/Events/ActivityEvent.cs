using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Events;

public class ActivityEvent : Event
{
    public required IToken Token { get; set; }
    public required IActivity Activity { get; set; }
}