using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Apps;

public partial interface IApp
{
    public IApp OnError(ErrorEventHandler handler);
    public IApp OnStart(StartEventHandler handler);
    public IApp OnActivity(ActivityEventHandler handler);
    public IApp OnActivitySent(ActivitySentEventHandler handler);
    public IApp OnActivityResponse(ActivityResponseEventHandler handler);

    public delegate Task StartEventHandler(IApp app, ILogger logger);
    public delegate Task ErrorEventHandler(IApp app, IPlugin? plugin, Exception exception, IContext<IActivity>? context);
    public delegate Task ActivityEventHandler(IApp app, IContext<IActivity> context);
    public delegate Task ActivitySentEventHandler(IApp app, IActivity activity, IContext<IActivity> context);
    public delegate Task ActivityResponseEventHandler(IApp app, Response? response, IContext<IActivity> context);
}

public partial class App
{
    protected event IApp.ErrorEventHandler ErrorEvent;
    protected event IApp.StartEventHandler StartEvent;
    protected event IApp.ActivityEventHandler ActivityEvent;
    protected event IApp.ActivitySentEventHandler ActivitySentEvent;
    protected event IApp.ActivityResponseEventHandler ActivityResponseEvent;

    public IApp OnError(IApp.ErrorEventHandler handler)
    {
        ErrorEvent += handler;
        return this;
    }

    public IApp OnStart(IApp.StartEventHandler handler)
    {
        StartEvent += handler;
        return this;
    }

    public IApp OnActivity(IApp.ActivityEventHandler handler)
    {
        ActivityEvent += handler;
        return this;
    }

    public IApp OnActivitySent(IApp.ActivitySentEventHandler handler)
    {
        ActivitySentEvent += handler;
        return this;
    }

    public IApp OnActivityResponse(IApp.ActivityResponseEventHandler handler)
    {
        ActivityResponseEvent += handler;
        return this;
    }

    protected async Task OnErrorEvent(IPlugin? sender, Exception exception, IContext<IActivity>? context)
    {
        var cancellationToken = context?.CancellationToken ?? default;
        Logger.Error(exception);

        if (exception is HttpException ex)
        {
            Logger.Error(ex.Request?.RequestUri?.ToString());

            if (ex.Request?.Content != null)
            {
                var content = await ex.Request.Content.ReadAsStringAsync();
                Logger.Error(content);
            }
        }

        foreach (var plugin in Plugins)
        {
            if (sender != null && sender.Equals(plugin)) continue;
            await plugin.OnError(this, sender, exception, context, cancellationToken);
        }
    }

    protected Task OnStartEvent()
    {
        return Task.Run(() => Logger.Info("started"));
    }

    protected async Task OnActivitySentEvent(IActivity activity, IContext<IActivity> context)
    {
        Logger.Debug(activity);

        foreach (var plugin in Plugins)
        {
            await plugin.OnActivitySent(this, activity, context);
        }
    }

    protected async Task OnActivitySentEvent(ISenderPlugin sender, IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        Logger.Debug(activity);

        foreach (var plugin in Plugins)
        {
            await plugin.OnActivitySent(this, sender, activity, reference, cancellationToken);
        }
    }

    protected async Task OnActivityResponseEvent(Response? response, IContext<IActivity> context)
    {
        Logger.Debug(response);

        foreach (var plugin in Plugins)
        {
            await plugin.OnActivityResponse(this, response, context);
        }
    }

    protected async Task<Response> OnActivityEvent(ISenderPlugin sender, IToken token, IActivity activity, CancellationToken cancellationToken = default)
    {
        var routes = Router.Select(activity);
        JsonWebToken? userToken = null;

        var api = new ApiClient(Api);

        try
        {
            var tokenResponse = await api.Users.Token.GetAsync(new()
            {
                UserId = activity.From.Id,
                ChannelId = activity.ChannelId,
                ConnectionName = "graph"
            });

            userToken = new JsonWebToken(tokenResponse);
        }
        catch { }

        var path = activity.GetPath();
        Logger.Debug(path);

        var reference = new ConversationReference()
        {
            ServiceUrl = activity.ServiceUrl ?? token.ServiceUrl,
            ChannelId = activity.ChannelId,
            Bot = activity.Recipient,
            User = activity.From,
            Locale = activity.Locale,
            Conversation = activity.Conversation,
        };

        var userGraphTokenProvider = Azure.Core.DelegatedTokenCredential.Create((context, _) =>
        {
            return userToken == null ? default : new Azure.Core.AccessToken(userToken.ToString(), userToken.Token.ValidTo);
        });

        object? data = null;
        var i = -1;
        async Task<object?> Next(IContext<IActivity> context)
        {
            i++;
            if (i == routes.Count) return data;
            var res = await routes[i].Invoke(context);

            if (res != null)
                data = res;

            return res;
        }

        var stream = sender.CreateStream(reference, cancellationToken);
        var context = new Context<IActivity>(sender, stream)
        {
            AppId = token.AppId ?? Id ?? string.Empty,
            Log = Logger.Child(path),
            Storage = Storage,
            Api = api,
            Activity = activity,
            Ref = reference,
            IsSignedIn = userToken != null,
            OnNext = Next,
            UserGraph = new Graph.GraphServiceClient(userGraphTokenProvider),
            CancellationToken = cancellationToken,
            OnActivitySent = (activity, context) => ActivitySentEvent(this, activity, context)
        };

        stream.OnChunk += activity => ActivitySentEvent(this, activity, context);

        try
        {
            await ActivityEvent(this, context);

            foreach (var plugin in Plugins)
            {
                await plugin.OnActivity(this, context);
            }

            var res = await Next(context);
            await stream.Close();

            var response = res is Response value ? value : new Response(System.Net.HttpStatusCode.OK, res);
            await ActivityResponseEvent(this, response, context);
            return response;
        }
        catch (Exception err)
        {
            await ErrorEvent(this, sender, err, context);
            return new Response(System.Net.HttpStatusCode.InternalServerError);
        }
    }
}