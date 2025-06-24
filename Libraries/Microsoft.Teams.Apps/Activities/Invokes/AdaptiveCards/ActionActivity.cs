// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.AdaptiveCards;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class AdaptiveCard
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ActionAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.AdaptiveCards.Action, typeof(AdaptiveCards.ActionActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<AdaptiveCards.ActionActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnAdaptiveCardAction(this App app, Func<IContext<AdaptiveCards.ActionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<AdaptiveCards.ActionActivity>());
                return null;
            },
            Selector = activity => activity is AdaptiveCards.ActionActivity
        });

        return app;
    }

    public static App OnAdaptiveCardAction(this App app, Func<IContext<AdaptiveCards.ActionActivity>, Task<Response<ActionResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<AdaptiveCards.ActionActivity>()),
            Selector = activity => activity is AdaptiveCards.ActionActivity
        });

        return app;
    }

    public static App OnAdaptiveCardAction(this App app, Func<IContext<AdaptiveCards.ActionActivity>, Task<ActionResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<AdaptiveCards.ActionActivity>()),
            Selector = activity => activity is AdaptiveCards.ActionActivity
        });

        return app;
    }
}