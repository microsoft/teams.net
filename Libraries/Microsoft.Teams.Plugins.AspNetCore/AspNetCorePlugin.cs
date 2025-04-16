using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Plugins.AspNetCore;

[Plugin(name: "Microsoft.Teams.Plugins.AspNetCore", version: "0.0.0")]
public partial class AspNetCorePlugin : ISenderPlugin, IAspNetCorePlugin
{
    [Dependency]
    public ILogger Logger { get; set; }

    [Dependency]
    public IHttpClient Client { get; set; }

    [Dependency("BotToken", optional: true)]
    public IToken? BotToken { get; set; }

    public event IPlugin.ErrorEventHandler ErrorEvent = (_, _) => Task.Run(() => { });
    public event IPlugin.ActivityEventHandler ActivityEvent = (_, _, _, _) => Task.FromResult(new Response(System.Net.HttpStatusCode.OK));

    private TeamsContext Context => _services.GetRequiredService<TeamsContext>();
    private readonly IServiceProvider _services;

    public AspNetCorePlugin(IServiceProvider provider)
    {
        _services = provider;
    }

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        return builder;
    }

    public Task OnInit(IApp app, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => { });
    }

    public Task OnStart(IApp app, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnStart"));
    }

    public Task OnError(IApp app, IPlugin? plugin, Exception exception, IContext<IActivity>? context, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnError"));
    }

    public Task OnActivity(IApp app, IContext<IActivity> context)
    {
        Context.Activity = context;
        return Task.Run(() => Logger.Debug("OnActivity"));
    }

    public Task OnActivitySent(IApp app, IActivity activity, IContext<IActivity> context)
    {
        return Task.Run(() => Logger.Debug("OnActivitySent"));
    }

    public Task OnActivitySent(IApp app, ISenderPlugin sender, IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnActivitySent"));
    }

    public Task OnActivityResponse(IApp app, Response? response, IContext<IActivity> context)
    {
        Context.Response = response;
        return Task.Run(() => Logger.Debug("OnActivityResponse"));
    }

    public async Task<IActivity> Send(IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return await Send<IActivity>(activity, reference, cancellationToken);
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity, ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var client = new ApiClient(reference.ServiceUrl, Client, cancellationToken);

        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;

        if (activity.Id != null && !activity.IsStreaming)
        {
            await client
                .Conversations
                .Activities
                .UpdateAsync(reference.Conversation.Id, activity.Id, activity);

            return activity;
        }

        var res = await client
            .Conversations
            .Activities
            .CreateAsync(reference.Conversation.Id, activity);

        activity.Id = res?.Id;
        return activity;
    }

    public IStreamer CreateStream(ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return new Stream()
        {
            Send = async activity =>
            {
                var res = await Send(activity, reference, cancellationToken);
                return res;
            }
        };
    }

    public async Task<Response> Do(IToken token, IActivity activity, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await ActivityEvent(this, token, activity, cancellationToken);
            Logger.Debug(res);
            return res;
        }
        catch (Exception err)
        {
            Logger.Error(err);
            await ErrorEvent(this, err);
            return new Response(System.Net.HttpStatusCode.InternalServerError, err.ToString());
        }
    }
}