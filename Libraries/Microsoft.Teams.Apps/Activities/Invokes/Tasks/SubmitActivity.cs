// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TaskSubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tasks.Submit, typeof(Tasks.SubmitActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tasks.SubmitActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Tasks.SubmitActivity>()),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }
}