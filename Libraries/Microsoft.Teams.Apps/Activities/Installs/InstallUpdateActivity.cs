// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InstallUpdateAttribute : ActivityAttribute
{
    public InstallUpdateAttribute() : base(ActivityType.InstallUpdate, typeof(InstallUpdateActivity))
    {

    }

    public InstallUpdateAttribute(InstallUpdateAction action) : base(string.Join("/", [ActivityType.InstallUpdate, action]), typeof(InstallUpdateActivity))
    {

    }

    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<InstallUpdateActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnInstallUpdate(this App app, Func<IContext<InstallUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.InstallUpdate,
            Handler = async context =>
            {
                await handler(context.ToActivityType<InstallUpdateActivity>());
                return null;
            },
            Selector = activity => activity is InstallUpdateActivity
        });

        return app;
    }
}