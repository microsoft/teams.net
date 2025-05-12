using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

public class ErrorEvent : Event
{
    public required Exception Exception { get; set; }
    public IContext<IActivity>? Context { get; set; }
}

public static partial class AppEventExtensions
{
    public static App OnError(this App app, Action<IPlugin, ErrorEvent> handler)
    {
        return app.OnEvent(EventType.Error, handler);
    }

    public static App OnError(this App app, Func<IPlugin, ErrorEvent, CancellationToken, Task> handler)
    {
        return app.OnEvent(EventType.Error, handler);
    }
}