// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InstallAttribute() : InstallUpdateAttribute
{
    public override bool Select(IActivity activity)
    {
        if (activity is InstallUpdateActivity installUpdate)
        {
            return installUpdate.Action.IsAdd;
        }

        return false;
    }
}

public static partial class AppActivityExtensions
{
    public static App OnInstall(this App app, Func<IContext<InstallUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<InstallUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is InstallUpdateActivity installUpdate)
                {
                    return installUpdate.Action.IsAdd;
                }

                return false;
            }
        });

        return app;
    }
}