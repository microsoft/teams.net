using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

public class ActivitySentEvent : Event
{
    public required IActivity Activity { get; set; }
}

public static partial class AppEventExtensions
{
    public static App OnActivitySent(this App app, Action<ISenderPlugin, ActivitySentEvent> handler)
    {
        return app.OnEvent(EventType.ActivitySent, (plugin, @event) =>
        {
            handler((ISenderPlugin)plugin, (ActivitySentEvent)@event);
        });
    }

    public static App OnActivitySent(this App app, Func<ISenderPlugin, ActivitySentEvent, CancellationToken, Task> handler)
    {
        return app.OnEvent(EventType.ActivitySent, (plugin, @event, token) =>
        {
            return handler((ISenderPlugin)plugin, (ActivitySentEvent)@event, token);
        });
    }
}