using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

internal class Topic : List<Func<IPlugin, Event, CancellationToken, Task<object?>>>
{
    public async Task<object?> Emit(IPlugin plugin, Event? @event = null, CancellationToken cancellationToken = default)
    {
        object? res = null;
        @event ??= [];

        foreach (var fn in this)
        {
            res ??= await fn(plugin, @event, cancellationToken);
        }

        return res;
    }
}