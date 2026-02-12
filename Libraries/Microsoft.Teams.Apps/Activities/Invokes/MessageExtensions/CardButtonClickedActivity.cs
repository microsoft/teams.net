// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class CardButtonClickedAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.CardButtonClicked, typeof(MessageExtensions.CardButtonClickedActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.CardButtonClickedActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnCardButtonClicked(this App app, Func<IContext<MessageExtensions.CardButtonClickedActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.CardButtonClicked]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.CardButtonClickedActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.CardButtonClickedActivity
        });

        return app;
    }

    public static App OnCardButtonClicked(this App app, Func<IContext<MessageExtensions.CardButtonClickedActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.CardButtonClicked]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.CardButtonClickedActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is MessageExtensions.CardButtonClickedActivity
        });

        return app;
    }
}