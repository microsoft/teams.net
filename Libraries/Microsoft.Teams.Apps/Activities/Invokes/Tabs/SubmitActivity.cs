// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Tab
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tabs.Submit, typeof(Tabs.SubmitActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tabs.SubmitActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTabSubmit(this App app, Func<IContext<Tabs.SubmitActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tabs.SubmitActivity>());
                return null;
            },
            Selector = activity => activity is Tabs.SubmitActivity
        });

        return app;
    }

    public static App OnTabSubmit(this App app, Func<IContext<Tabs.SubmitActivity>, Task<Response<Api.Tabs.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<Tabs.SubmitActivity>()),
            Selector = activity => activity is Tabs.SubmitActivity
        });

        return app;
    }

    public static App OnTabSubmit(this App app, Func<IContext<Tabs.SubmitActivity>, Task<Api.Tabs.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<Tabs.SubmitActivity>()),
            Selector = activity => activity is Tabs.SubmitActivity
        });

        return app;
    }
}