﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Storage;

namespace Microsoft.Teams.Apps;

public partial class App
{
    public static AppBuilder Builder(AppOptions? options = null) => new(options);

    /// <summary>
    /// the apps id
    /// </summary>
    public string? Id => BotToken?.AppId ?? GraphToken?.AppId;

    /// <summary>
    /// the apps name
    /// </summary>
    public string? Name => BotToken?.AppDisplayName ?? GraphToken?.AppDisplayName;

    public Status? Status { get; internal set; }
    public ILogger Logger { get; }
    public IStorage<string, object> Storage { get; }
    public ApiClient Api { get; }
    public IHttpClient Client { get; }
    public IHttpCredentials? Credentials { get; }
    public IToken? BotToken { get; internal set; }
    public IToken? GraphToken { get; internal set; }
    public OAuthSettings OAuth { get; internal set; }

    internal IServiceProvider? Provider { get; set; }
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

    public App(AppOptions? options = null)
    {
        Logger = options?.Logger ?? new ConsoleLogger();
        Storage = options?.Storage ?? new LocalStorage<object>();
        Client = options?.Client ?? options?.ClientFactory?.CreateClient() ?? new Common.Http.HttpClient();
        Client.Options.TokenFactory = () => BotToken;
        Client.Options.AddUserAgent(UserAgent);
        Credentials = options?.Credentials;
        Api = new ApiClient("https://smba.trafficmanager.net/teams/", Client);
        Plugins = options?.Plugins ?? [];
        OAuth = options?.OAuth ?? new OAuthSettings();
        Provider = options?.Provider;

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

        this.OnTokenExchange(OnTokenExchangeActivity);
        this.OnVerifyState(OnVerifyStateActivity);
        this.OnError(OnErrorEvent);
        this.OnActivitySent(OnActivitySentEvent);
        this.OnActivityResponse(OnActivityResponseEvent);

        Events.On(EventType.Activity, (plugin, @event, token) =>
        {
            return OnActivityEvent((ISenderPlugin)plugin, (ActivityEvent)@event, token);
        });

        Status = Apps.Status.Ready;
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

            Status = Apps.Status.Started;
        }
        catch (Exception ex)
        {
            Status = Apps.Status.Stopped;
            await Events.Emit(
                null!,
                EventType.Error,
                new ErrorEvent() { Exception = ex }
            );
        }
    }

    /// <summary>
    /// send an activity to the conversation
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    public async Task<T> Send<T>(string conversationId, T activity, ConversationType? conversationType, string? serviceUrl = null, CancellationToken cancellationToken = default) where T : IActivity
    {
        if (Id is null)
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
                Type = conversationType ?? ConversationType.Personal
            }
        };

        var sender = Plugins.Where(plugin => plugin is ISenderPlugin).Select(plugin => plugin as ISenderPlugin).First();

        if (sender is null)
        {
            throw new Exception("no plugin that can send activities was found");
        }

        var res = await sender.Send(activity, reference, cancellationToken);

        await Events.Emit(
            sender,
            EventType.ActivitySent,
            new ActivitySentEvent() { Activity = res },
            cancellationToken
        );

        return res;
    }

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    public async Task<MessageActivity> Send(string conversationId, string text, ConversationType? conversationType, string? serviceUrl = null, CancellationToken cancellationToken = default)
    {
        return await Send(conversationId, new MessageActivity(text), conversationType, serviceUrl, cancellationToken);
    }

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    public async Task<MessageActivity> Send(string conversationId, Cards.AdaptiveCard card, ConversationType? conversationType, string? serviceUrl = null, CancellationToken cancellationToken = default)
    {
        return await Send(conversationId, new MessageActivity().AddAttachment(card), conversationType, serviceUrl, cancellationToken);
    }

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    public async Task<Response> Process(ISenderPlugin sender, IToken token, IActivity activity, IDictionary<string, object?>? extra = null, CancellationToken cancellationToken = default)
    {
        return await Process(sender, new()
        {
            Token = token,
            Activity = activity,
            Extra = extra
        }, cancellationToken);
    }

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <exception cref="Exception"></exception>
    public Task<Response> Process(string sender, IToken token, IActivity activity, IDictionary<string, object?>? extra = null, CancellationToken cancellationToken = default)
    {
        var plugin = ((ISenderPlugin?)GetPlugin(sender)) ?? throw new Exception($"sender plugin '{sender}' not found");
        return Process(plugin, token, activity, extra, cancellationToken);
    }

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="token">the request token</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <exception cref="Exception"></exception>
    public Task<Response> Process<TPlugin>(IToken token, IActivity activity, IDictionary<string, object?>? extra = null, CancellationToken cancellationToken = default) where TPlugin : ISenderPlugin
    {
        var plugin = GetPlugin<TPlugin>() ?? throw new Exception($"sender plugin '{typeof(TPlugin).Name}' not found");
        return Process(plugin, token, activity, extra, cancellationToken);
    }

    /// <summary>
    /// process an activity
    /// </summary>
    /// <param name="sender">the plugin to use</param>
    /// <param name="@event">the activity event</param>
    /// <param name="cancellationToken">the cancellation token</param>
    private async Task<Response> Process(ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;
        var routes = Router.Select(@event.Activity);
        JsonWebToken? userToken = null;

        var api = new ApiClient(Api);

        try
        {
            var tokenResponse = await api.Users.Token.GetAsync(new()
            {
                UserId = @event.Activity.From.Id,
                ChannelId = @event.Activity.ChannelId,
                ConnectionName = OAuth.DefaultConnectionName
            });

            userToken = new JsonWebToken(tokenResponse);
        }
        catch { }

        var path = @event.Activity.GetPath();
        Logger.Debug(path);

        var reference = new ConversationReference()
        {
            ServiceUrl = @event.Activity.ServiceUrl ?? @event.Token.ServiceUrl,
            ChannelId = @event.Activity.ChannelId,
            Bot = @event.Activity.Recipient,
            User = @event.Activity.From,
            Locale = @event.Activity.Locale,
            Conversation = @event.Activity.Conversation,
        };

        object? data = null;
        var i = -1;
        async Task<object?> Next(IContext<IActivity> context)
        {
            if (i + 1 == routes.Count) return data;

            i++;
            var res = await routes[i].Invoke(context);

            if (res is not null)
                data = res;

            return res;
        }

        var stream = sender.CreateStream(reference, cancellationToken);
        var context = new Context<IActivity>(sender, stream)
        {
            AppId = @event.Token.AppId ?? Id ?? string.Empty,
            Log = Logger.Child(path),
            Storage = Storage,
            Api = api,
            Activity = @event.Activity,
            Ref = reference,
            IsSignedIn = userToken is not null,
            OnNext = Next,
            Extra = @event.Extra ?? new Dictionary<string, object?>(),
            UserGraphToken = userToken,
            CancellationToken = cancellationToken,
            ConnectionName = OAuth.DefaultConnectionName,
            OnActivitySent = async (activity, context) =>
            {
                await Events.Emit(
                    context.Sender,
                    EventType.ActivitySent,
                    new ActivitySentEvent() { Activity = activity },
                    context.CancellationToken
                );
            }
        };

        stream.OnChunk += async activity =>
        {
            await Events.Emit(
                sender,
                EventType.ActivitySent,
                new ActivitySentEvent() { Activity = activity },
                cancellationToken
            );
        };

        try
        {
            if (@event.Services is not null)
            {
                var accessor = (IContext.Accessor?)@event.Services.GetService(typeof(IContext.Accessor));

                if (accessor is not null)
                {
                    accessor.Value = context;
                }
            }

            foreach (var plugin in Plugins)
            {
                await plugin.OnActivity(this, sender, @event, cancellationToken);
            }

            var res = await Next(context);
            await stream.Close();

            var response = res is Response value
                ? value
                : new Response(System.Net.HttpStatusCode.OK, res);

            response.Meta.Routes = i + 1;
            response.Meta.Elapse = (DateTime.UtcNow - start).Milliseconds;

            await Events.Emit(
                sender,
                EventType.ActivityResponse,
                new ActivityResponseEvent() { Response = response },
                cancellationToken
            );

            return response;
        }
        catch (Exception ex)
        {
            await Events.Emit(
                sender,
                EventType.Error,
                new ErrorEvent() { Exception = ex },
                cancellationToken
            );

            return new Response(System.Net.HttpStatusCode.InternalServerError);
        }
    }
}