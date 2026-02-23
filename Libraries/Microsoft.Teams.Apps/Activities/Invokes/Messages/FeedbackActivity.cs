// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Message
{
    /// <summary>
    /// Attribute for handling message feedback activities
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class FeedbackAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Messages.SubmitAction, typeof(Messages.SubmitActionActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Messages.SubmitActionActivity>();
        public override bool Select(IActivity activity)
        {
            return activity is Messages.SubmitActionActivity submitAction &&
                submitAction.Value?.ActionName == "feedback";
        }
    }
}

public static partial class AppInvokeActivityExtensions
{
    /// <summary>
    /// Registers a handler for message feedback activities
    /// </summary>
    public static App OnFeedback(this App app, Func<IContext<Messages.SubmitActionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Messages.SubmitAction, "feedback"]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Messages.SubmitActionActivity>());
                return null;
            },
            Selector = activity => activity is Messages.SubmitActionActivity submitAction && submitAction.Value?.ActionName == "feedback"
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message feedback activities with cancellation token support
    /// </summary>
    public static App OnFeedback(this App app, Func<IContext<Messages.SubmitActionActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Messages.SubmitAction, "feedback"]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Messages.SubmitActionActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is Messages.SubmitActionActivity submitAction && submitAction.Value?.ActionName == "feedback"
        });

        return app;
    }
}