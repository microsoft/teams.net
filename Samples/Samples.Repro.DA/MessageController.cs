using System.Diagnostics;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace Samples.Repro.DA;

#pragma warning disable CS0618 // TeamsController is marked obsolete in favor of Minimal APIs
[TeamsController]
public class MessageController
{
    [Message]
    public Task OnMessage(IContext<MessageActivity> context)
    {
        // NOTE: not calling Send/Typing — outbound auth is not configured in this repro.
        if (context.Extra.TryGetValue(TimingConstants.EndpointStopwatchKey, out var swObj)
            && swObj is Stopwatch sw)
        {
            context.Log.Info($"OnMessage: handler reached at {sw.ElapsedMilliseconds}ms");
        }

        context.Log.Info($"OnMessage: '{context.Activity.Text}'");
        return Task.CompletedTask;
    }

    [InstallUpdate]
    public Task OnInstall(IContext<InstallUpdateActivity> context)
    {
        context.Log.Info($"OnInstall: action={context.Activity.Action}");
        return Task.CompletedTask;
    }
}
#pragma warning restore CS0618
