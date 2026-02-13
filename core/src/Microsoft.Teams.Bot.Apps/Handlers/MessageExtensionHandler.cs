// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;
/// <summary>
/// Delegate for handling message extension query invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionQueryHandler(Context<InvokeActivity<MessageExtensionQuery>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension submit action invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionSubmitActionHandler(Context<InvokeActivity<MessageExtensionAction>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension fetch task invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionFetchTaskHandler(Context<InvokeActivity<MessageExtensionAction>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension query link invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionQueryLinkHandler(Context<InvokeActivity<AppBasedQueryLink>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension anonymous query link invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionAnonQueryLinkHandler(Context<InvokeActivity<AppBasedQueryLink>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension select item invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionSelectItemHandler(Context<InvokeActivity<JsonElement>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension query setting URL invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionQuerySettingUrlHandler(Context<InvokeActivity<MessageExtensionQuery>> context, CancellationToken cancellationToken = default);

/*
/// <summary>
/// Delegate for handling message extension card button clicked invoke activities.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionCardButtonClickedHandler(Context<InvokeActivity<JsonElement>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling message extension setting invoke activities with.
/// </summary>
public delegate Task<CoreInvokeResponse> MessageExtensionSettingHandler(Context<InvokeActivity<Query>> context, CancellationToken cancellationToken = default);
*/

/// <summary>
/// Extension methods for registering message extension invoke handlers.
/// </summary>
public static class MessageExtensionExtensions
{

    /// <summary>
    /// Registers a handler for message extension query invoke activities.
    /// </summary>
    public static TeamsBotApplication OnQuery(this TeamsBotApplication app, MessageExtensionQueryHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionQuery),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionQuery,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<MessageExtensionQuery> typedActivity = new InvokeActivity<MessageExtensionQuery>(ctx.Activity);
                Context<InvokeActivity<MessageExtensionQuery>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension submit action invoke activities.
    /// </summary>
    public static TeamsBotApplication OnSubmitAction(this TeamsBotApplication app, MessageExtensionSubmitActionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionSubmitAction),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionSubmitAction,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<MessageExtensionAction> typedActivity = new InvokeActivity<MessageExtensionAction>(ctx.Activity);
                Context<InvokeActivity<MessageExtensionAction>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension query link invoke activities.
    /// </summary>
    public static TeamsBotApplication OnQueryLink(this TeamsBotApplication app, MessageExtensionQueryLinkHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionQueryLink),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionQueryLink,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<AppBasedQueryLink> typedActivity = new InvokeActivity<AppBasedQueryLink>(ctx.Activity);
                Context<InvokeActivity<AppBasedQueryLink>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension anonymous query link invoke activities.
    /// </summary>
    public static TeamsBotApplication OnAnonQueryLink(this TeamsBotApplication app, MessageExtensionAnonQueryLinkHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionAnonQueryLink),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionAnonQueryLink,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<AppBasedQueryLink> typedActivity = new InvokeActivity<AppBasedQueryLink>(ctx.Activity);
                Context<InvokeActivity<AppBasedQueryLink>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension fetch task invoke activities.
    /// </summary>
    public static TeamsBotApplication OnFetchTask(this TeamsBotApplication app, MessageExtensionFetchTaskHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionFetchTask),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionFetchTask,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<MessageExtensionAction> typedActivity = new InvokeActivity<MessageExtensionAction>(ctx.Activity);
                Context<InvokeActivity<MessageExtensionAction>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension select item invoke activities.
    /// </summary>
    public static TeamsBotApplication OnSelectItem(this TeamsBotApplication app, MessageExtensionSelectItemHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionSelectItem),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionSelectItem,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<JsonElement> typedActivity = new InvokeActivity<JsonElement>(ctx.Activity);
                Context<InvokeActivity<JsonElement>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension query setting URL invoke activities.
    /// </summary>
    public static TeamsBotApplication OnQuerySettingUrl(this TeamsBotApplication app, MessageExtensionQuerySettingUrlHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionQuerySettingUrl),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionQuerySettingUrl,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<MessageExtensionQuery> typedActivity = new InvokeActivity<MessageExtensionQuery>(ctx.Activity);
                Context<InvokeActivity<MessageExtensionQuery>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }


    /*
    /// <summary>
    /// Registers a handler for message extension card button clicked invoke activities.
    /// </summary>
    public static TeamsBotApplication OnCardButtonClicked(this TeamsBotApplication app, MessageExtensionCardButtonClickedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionCardButtonClicked),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionCardButtonClicked,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                var typedActivity = new InvokeActivity<JsonElement>(ctx.Activity);
                Context<InvokeActivity<JsonElement>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message extension setting invoke activities.
    /// </summary>
    public static TeamsBotApplication OnSetting(this TeamsBotApplication app, MessageExtensionSettingHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageExtensionSetting),
            Selector = activity => activity.Name == InvokeNames.MessageExtensionSetting,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                var typedActivity = new InvokeActivity<Query>(ctx.Activity);
                Context<InvokeActivity<Query>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
    */
}
