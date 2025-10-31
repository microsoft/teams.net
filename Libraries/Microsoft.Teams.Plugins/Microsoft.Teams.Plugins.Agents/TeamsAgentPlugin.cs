using Microsoft.Agents.Builder;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Plugins.Agents.Models;

namespace Microsoft.Teams.Plugins.Agents;

[Plugin]
public partial class TeamsAgentPlugin : AgentExtension, ISenderPlugin
{
    [Dependency]
    public ILogger Logger { get; set; }
    public TeamsAgentPluginOptions Options { get; }
    public event EventFunction Events;

    public TeamsAgentPlugin(TeamsAgentPluginOptions options) : base()
    {
        ChannelId = Microsoft.Agents.Core.Models.Channels.Msteams;
        Options = options;
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnInit");
        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnStart");
        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnError");
        return Task.CompletedTask;
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivity");
        return Task.CompletedTask;
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivitySent");
        return Task.CompletedTask;
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivityResponse");
        return Task.CompletedTask;
    }

    public IStreamer CreateStream(Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return new Stream(Options.Context);
    }

    public async Task<IActivity> Send(IActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        activity.ServiceUrl = reference.ServiceUrl;
        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;
        activity.ChannelId = reference.ChannelId;

        var res = await Options.Context.SendActivityAsync(activity.ToAgentEntity(), cancellationToken);
        activity.Id = res.Id;
        return activity;
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        activity.ServiceUrl = reference.ServiceUrl;
        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;
        activity.ChannelId = reference.ChannelId;

        var res = await Options.Context.SendActivityAsync(activity.ToAgentEntity(), cancellationToken);
        activity.Id = res.Id;
        return activity;
    }

    public async Task<Response> Do(ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var @out = await Events(
                this,
                EventType.Activity,
                @event,
                cancellationToken
            );

            var res = (Response?)@out ?? throw new Exception("expected activity response");
            Logger.Debug(res);
            return res;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await Events(
                this,
                EventType.Error,
                new ErrorEvent() { Exception = ex },
                cancellationToken
            );

            return new Response(System.Net.HttpStatusCode.InternalServerError, ex.ToString());
        }
    }
}