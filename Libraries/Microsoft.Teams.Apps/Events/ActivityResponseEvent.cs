using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

public class ActivityResponseEvent : Event
{
    public required Response Response { get; set; }
}

public static partial class AppEventExtensions
{
    public static App OnActivityResponse(this App app, Action<ISenderPlugin, ActivityResponseEvent> handler)
    {
        return app.OnEvent(EventType.ActivityResponse, (plugin, @event) =>
        {
            handler((ISenderPlugin)plugin, (ActivityResponseEvent)@event);
        });
    }

    public static App OnActivityResponse(this App app, Func<ISenderPlugin, ActivityResponseEvent, CancellationToken, Task> handler)
    {
        return app.OnEvent(EventType.ActivityResponse, (plugin, @event, token) =>
        {
            return handler((ISenderPlugin)plugin, (ActivityResponseEvent)@event, token);
        });
    }
}