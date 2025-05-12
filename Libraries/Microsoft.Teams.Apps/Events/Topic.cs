using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

internal class Topic : List<Func<IPlugin, Event, CancellationToken, Task<object?>>>
{
    public async Task<object?> Emit(IPlugin plugin, Event? @event = null, CancellationToken cancellationToken = default)
    {
        object? result = null;
        @event ??= [];

        foreach (var fn in this)
        {
            var res = await fn(plugin, @event, cancellationToken);
            result ??= res;
        }

        return result;
    }
}