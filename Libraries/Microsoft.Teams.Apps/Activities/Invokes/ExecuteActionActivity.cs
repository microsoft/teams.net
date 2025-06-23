// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class ExecuteActionAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.ExecuteAction, typeof(ExecuteActionActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ExecuteActionActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnExecuteAction(this App app, Func<IContext<ExecuteActionActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<ExecuteActionActivity>()),
            Selector = activity => activity is ExecuteActionActivity
        });

        return app;
    }
}