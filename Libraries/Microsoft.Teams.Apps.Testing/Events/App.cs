using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Testing.Events;

public class TestMessageEvent : Event
{
    public required string Message { get; set; }
}

public static partial class AppExtensions
{
    public static App OnTestMessage(this App app, Action<TestPlugin, TestMessageEvent> handler)
    {
        return app.OnEvent("test.message", (plugin, @event) =>
        {
            handler((TestPlugin)plugin, (TestMessageEvent)@event);
        });
    }
}