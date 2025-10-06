// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Storage;

namespace Microsoft.Teams.Apps;

internal delegate Task ActivitySentHandler(IActivity activity, IContext<IActivity> context);

public partial interface IContext<TActivity> where TActivity : IActivity
{
    /// <summary>
    /// the plugin that received the activity
    /// </summary>
    public ISenderPlugin Sender { get; }

    /// <summary>
    /// the stream instance
    /// </summary>
    public IStreamer Stream { get; }

    /// <summary>
    /// the app id of the bot
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// the tenant id of the request/activity
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// the app logger instance
    /// </summary>
    public ILogger Log { get; set; }

    /// <summary>
    /// the app storage instance
    /// </summary>
    public IStorage<string, object> Storage { get; set; }

    /// <summary>
    /// the api client
    /// </summary>
    public ApiClient Api { get; set; }

    /// <summary>
    /// the inbound activity
    /// </summary>
    public TActivity Activity { get; set; }

    /// <summary>
    /// the inbound activity conversation reference
    /// </summary>
    public ConversationReference Ref { get; set; }

    /// <summary>
    /// The user's access token to the Microsoft Graph API.
    /// </summary>
    public JsonWebToken? UserGraphToken { get; set; }

    /// <summary>
    /// any extra data
    /// </summary>
    public IDictionary<string, object?> Extra { get; set; }

    /// <summary>
    /// the service collection provider
    /// </summary>
    public IServiceProvider Services { get; set; }

    /// <summary>
    /// the cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// destruct the context
    /// </summary>
    /// <param name="log">the ILogger instance</param>
    /// <param name="api">the api client</param>
    /// <param name="activity">the inbound activity</param>
    public void Deconstruct(out ILogger log, out ApiClient api, out TActivity activity);

    /// <summary>
    /// destruct the context
    /// </summary>
    /// <param name="log">the ILogger instance</param>
    /// <param name="api">the api client</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="send">the methods to send activities</param>
    public void Deconstruct(out ILogger log, out ApiClient api, out TActivity activity, out IContext.Client client);

    /// <summary>
    /// destruct the context
    /// </summary>
    /// <param name="appId">the apps id</param>
    /// <param name="log">the ILogger instance</param>
    /// <param name="api">the api client</param>
    /// <param name="activity">the inbound activity</param>
    /// <param name="reference">the inbound conversation reference</param>
    /// <param name="send">the methods to send activities</param>
    public void Deconstruct(out string appId, out ILogger log, out ApiClient api, out TActivity activity, out ConversationReference reference, out IContext.Client client);

    /// <summary>
    /// called to continue the chain of route handlers,
    /// if not called no other handlers in the sequence will be executed
    /// </summary>
    public Task<object?> Next();

    /// <summary>
    /// convert the context to that of another activity type
    /// </summary>
    public IContext<IActivity> ToActivityType();

    /// <summary>
    /// convert the context to that of another activity type
    /// </summary>
    public IContext<TToActivity> ToActivityType<TToActivity>() where TToActivity : IActivity;
}

public partial class Context<TActivity>(ISenderPlugin sender, IStreamer stream) : IContext<TActivity> where TActivity : IActivity
{
    public ISenderPlugin Sender { get; set; } = sender;
    public IStreamer Stream { get; set; } = stream;

    public required string AppId { get; set; }
    public required string TenantId { get; set; }
    public required ILogger Log { get; set; }
    public required IStorage<string, object> Storage { get; set; }
    public required ApiClient Api { get; set; }
    public required TActivity Activity { get; set; }
    public required ConversationReference Ref { get; set; }
    public required JsonWebToken? UserGraphToken { get; set; }
    public IDictionary<string, object?> Extra { get; set; } = new Dictionary<string, object?>();
    public required IServiceProvider Services { get; set; }
    public CancellationToken CancellationToken { get; set; }

    internal Func<IContext<IActivity>, Task<object?>> OnNext { get; set; } = (_) => Task.FromResult<object?>(null);
    internal ActivitySentHandler OnActivitySent { get; set; } = (_, _) => Task.Run(() => { });

    public void Deconstruct(out ILogger log, out ApiClient api, out TActivity activity)
    {
        log = Log;
        api = Api;
        activity = Activity;
    }

    public void Deconstruct(out ILogger log, out ApiClient api, out TActivity activity, out IContext.Client client)
    {
        log = Log;
        api = Api;
        activity = Activity;
        client = new IContext.Client(ToActivityType());
    }

    public void Deconstruct(out string appId, out ILogger log, out ApiClient api, out TActivity activity, out ConversationReference reference, out IContext.Client client)
    {
        appId = AppId;
        log = Log;
        api = Api;
        activity = Activity;
        reference = Ref;
        client = new IContext.Client(ToActivityType());
    }

    public Task<object?> Next() => OnNext(ToActivityType());
    public IContext<IActivity> ToActivityType() => ToActivityType<IActivity>();
    public IContext<TToActivity> ToActivityType<TToActivity>() where TToActivity : IActivity
    {
        return new Context<TToActivity>(Sender, Stream)
        {
            Sender = Sender,
            AppId = AppId,
            TenantId = TenantId,
            Log = Log,
            Storage = Storage,
            Api = Api,
            Activity = (TToActivity)Activity.ToType(typeof(TToActivity), null),
            Ref = Ref,
            UserGraphToken = UserGraphToken,
            IsSignedIn = IsSignedIn,
            ConnectionName = ConnectionName,
            Extra = Extra,
            Services = Services,
            CancellationToken = CancellationToken,
            OnNext = OnNext,
            OnActivitySent = OnActivitySent
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}