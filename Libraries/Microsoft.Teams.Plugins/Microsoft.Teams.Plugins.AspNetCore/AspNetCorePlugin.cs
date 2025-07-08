// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Plugins.AspNetCore;

[Plugin]
public partial class AspNetCorePlugin : ISenderPlugin, IAspNetCorePlugin
{
    [Dependency]
    public ILogger Logger { get; set; }

    [Dependency]
    public IHttpClient Client { get; set; }

    [Dependency("BotToken", optional: true)]
    public IToken? BotToken { get; set; }

    public event EventFunction Events;

    private TeamsContext Context => _services.CreateScope().ServiceProvider.GetRequiredService<TeamsContext>();
    private readonly IServiceProvider _services;

    public AspNetCorePlugin(IServiceProvider provider)
    {
        _services = provider;
    }

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        return builder;
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
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

    public Task<IActivity> Send(IActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Send<IActivity>(activity, reference, cancellationToken);
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var client = new ApiClient(reference.ServiceUrl, Client, cancellationToken);

        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;

        if (activity.Id is not null && !activity.IsStreaming)
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

    public IStreamer CreateStream(Api.ConversationReference reference, CancellationToken cancellationToken = default)
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

    public async Task<Response> Do(IToken token, IActivity activity, IDictionary<string, object>? contextExtra = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var @out = await Events(
                this,
                "activity",
                new ActivityEvent()
                {
                    Token = token,
                    Activity = activity,
                    ContextExtra = contextExtra
                },
                cancellationToken
            );

            var res = (Response?)@out;

            if (res is null)
            {
                throw new Exception("expected activity response");
            }

            Logger.Debug(res);
            return res;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await Events(
                this,
                "error",
                new ErrorEvent() { Exception = ex },
                cancellationToken
            );

            return new Response(System.Net.HttpStatusCode.InternalServerError, ex.ToString());
        }
    }
}