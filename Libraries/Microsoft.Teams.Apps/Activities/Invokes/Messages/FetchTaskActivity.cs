// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Message
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class FetchTaskAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Messages.FetchTask, typeof(Messages.FetchTaskActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Messages.FetchTaskActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    /// <summary>
    /// Registers a handler for <c>message/fetchTask</c> activities.
    /// The bot should return a task module response containing the dialog to show the user.
    /// </summary>
    public static App OnMessageFetchTask(this App app, Func<IContext<Messages.FetchTaskActivity>, CancellationToken, Task<Api.TaskModules.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Messages.FetchTask]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Messages.FetchTaskActivity>(), context.CancellationToken),
            Selector = activity => activity is Messages.FetchTaskActivity
        });

        return app;
    }
}
