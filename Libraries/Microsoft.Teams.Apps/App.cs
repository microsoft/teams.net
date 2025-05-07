using System.Reflection;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Storage;

namespace Microsoft.Teams.Apps;

public partial interface IApp
{
    public ILogger Logger { get; }
    public IStorage<string, object> Storage { get; }
    public ApiClient Api { get; }
    public IHttpClient Client { get; }
    public IHttpCredentials? Credentials { get; }
    public IToken? BotToken { get; }
    public IToken? GraphToken { get; }

    /// <summary>
    /// start the app
    /// </summary>
    public Task Start(CancellationToken cancellationToken = default);

    /// <summary>
    /// send an activity to the conversation
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    public Task<T> Send<T>(string conversationId, T activity, string? serviceUrl = null, CancellationToken cancellationToken = default) where T : IActivity;

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    public Task<MessageActivity> Send(string conversationId, string text, string? serviceUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    public Task<MessageActivity> Send(string conversationId, Cards.AdaptiveCard card, string? serviceUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    public Task<Response> Process(ISenderPlugin sender, IToken token, IActivity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <exception cref="Exception"></exception>
    public Task<Response> Process(string sender, IToken token, IActivity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <exception cref="Exception"></exception>
    public Task<Response> Process<TPlugin>(IToken token, IActivity activity, CancellationToken cancellationToken = default) where TPlugin : ISenderPlugin;
}

public partial class App : IApp
{
    public static IAppBuilder Builder(IAppOptions? options = null) => new AppBuilder(options);

    /// <summary>
    /// the apps id
    /// </summary>
    public string? Id => BotToken?.AppId ?? GraphToken?.AppId;

    /// <summary>
    /// the apps name
    /// </summary>
    public string? Name => BotToken?.AppDisplayName ?? GraphToken?.AppDisplayName;

    public ILogger Logger { get; }
    public IStorage<string, object> Storage { get; }
    public ApiClient Api { get; }
    public IHttpClient Client { get; }
    public IHttpCredentials? Credentials { get; }
    public IToken? BotToken { get; internal set; }
    public IToken? GraphToken { get; internal set; }

    internal IContainer Container { get; set; }
    internal string UserAgent
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            version ??= "0.0.0";
            return $"teams.net[apps]/{version}";
        }
    }

    public App(IAppOptions? options = null)
    {
        Logger = options?.Logger ?? new ConsoleLogger();
        Storage = options?.Storage ?? new LocalStorage<object>();
        Client = options?.Client ?? options?.ClientFactory?.CreateClient() ?? new Common.Http.HttpClient();
        Client.Options.TokenFactory = () => BotToken;
        Client.Options.AddUserAgent(UserAgent);
        Credentials = options?.Credentials;
        Api = new ApiClient("https://smba.trafficmanager.net/teams", Client);
        Plugins = options?.Plugins ?? [];
        ErrorEvent = (_, sender, exception, context) => OnErrorEvent(sender, exception, context);
        StartEvent = (_, _) => OnStartEvent();
        ActivityEvent = (_, _) => Task.Run(() => { });
        ActivitySentEvent = (_, activity, context) => OnActivitySentEvent(activity, context);
        ActivityResponseEvent = (_, res, context) => OnActivityResponseEvent(res, context);

        Container = new Container();
        Container.Register(Logger);
        Container.Register(Storage);
        Container.Register(Client);
        Container.Register(Api);
        Container.Register<IHttpCredentials>(new FactoryProvider(() => Credentials));
        Container.Register("AppId", new FactoryProvider(() => Id));
        Container.Register("AppName", new FactoryProvider(() => Name));
        Container.Register("BotToken", new FactoryProvider(() => BotToken));
        Container.Register("GraphToken", new FactoryProvider(() => GraphToken));

        OnTokenExchange(OnTokenExchangeActivity);
        OnVerifyState(OnVerifyStateActivity);
    }

    /// <summary>
    /// start the app
    /// </summary>
    public async Task Start(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var plugin in Plugins)
            {
                Inject(plugin);
            }

            if (Credentials is not null)
            {
                var botToken = await Api.Bots.Token.GetAsync(Credentials);
                var graphToken = await Api.Bots.Token.GetGraphAsync(Credentials);

                BotToken = new JsonWebToken(botToken.AccessToken);
                GraphToken = new JsonWebToken(graphToken.AccessToken);
            }

            Logger.Debug(Id);
            Logger.Debug(Name);

            foreach (var plugin in Plugins)
            {
                await plugin.OnInit(this, cancellationToken);
            }

            foreach (var plugin in Plugins)
            {
                await plugin.OnStart(this, cancellationToken);
            }

            await StartEvent(this, Logger);
        }
        catch (Exception err)
        {
            await ErrorEvent(this, null, err, null);
        }
    }

    /// <summary>
    /// send an activity to the conversation
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    public async Task<T> Send<T>(string conversationId, T activity, string? serviceUrl = null, CancellationToken cancellationToken = default) where T : IActivity
    {
        if (Id is null || Name is null)
        {
            throw new InvalidOperationException("app not started");
        }

        var reference = new ConversationReference()
        {
            ChannelId = ChannelId.MsTeams,
            ServiceUrl = serviceUrl ?? Api.ServiceUrl,
            Bot = new()
            {
                Id = Id,
                Name = Name,
                Role = Role.Bot
            },
            Conversation = new()
            {
                Id = conversationId,
                Type = ConversationType.Personal
            }
        };

        var sender = Plugins.Where(plugin => plugin is ISenderPlugin).Select(plugin => plugin as ISenderPlugin).First();

        if (sender is null)
        {
            throw new Exception("no plugin that can send activities was found");
        }

        var res = await sender.Send(activity, reference, cancellationToken);
        await OnActivitySentEvent(sender, res, reference, cancellationToken).ConfigureAwait(false);
        return res;
    }

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    public async Task<MessageActivity> Send(string conversationId, string text, string? serviceUrl = null, CancellationToken cancellationToken = default)
    {
        return await Send(conversationId, new MessageActivity(text), serviceUrl, cancellationToken);
    }

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    public async Task<MessageActivity> Send(string conversationId, Cards.AdaptiveCard card, string? serviceUrl = null, CancellationToken cancellationToken = default)
    {
        return await Send(conversationId, new MessageActivity().AddAttachment(card), serviceUrl, cancellationToken);
    }

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    public async Task<Response> Process(ISenderPlugin sender, IToken token, IActivity activity, CancellationToken cancellationToken = default)
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
            return userToken is null ? default : new Azure.Core.AccessToken(userToken.ToString(), userToken.Token.ValidTo);
        });

        object? data = null;
        var i = -1;
        async Task<object?> Next(IContext<IActivity> context)
        {
            i++;
            if (i == routes.Count) return data;
            var res = await routes[i].Invoke(context);

            if (res is not null)
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
            IsSignedIn = userToken is not null,
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

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <exception cref="Exception"></exception>
    public Task<Response> Process(string sender, IToken token, IActivity activity, CancellationToken cancellationToken = default)
    {
        var plugin = ((ISenderPlugin?)GetPlugin(sender)) ?? throw new Exception($"sender plugin '{sender}' not found");
        return Process(plugin, token, activity, cancellationToken);
    }

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <exception cref="Exception"></exception>
    public Task<Response> Process<TPlugin>(IToken token, IActivity activity, CancellationToken cancellationToken = default) where TPlugin : ISenderPlugin
    {
        var plugin = GetPlugin<TPlugin>() ?? throw new Exception($"sender plugin '{typeof(TPlugin).Name}' not found");
        return Process(plugin, token, activity, cancellationToken);
    }
}