// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Message
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SubmitActionAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Messages.SubmitAction, typeof(Messages.SubmitActionActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Messages.SubmitActionActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnSubmitAction(this App app, Func<IContext<Messages.SubmitActionActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Messages.SubmitActionActivity>()),
            Selector = activity => activity is Messages.SubmitActionActivity
        });

        return app;
    }
}