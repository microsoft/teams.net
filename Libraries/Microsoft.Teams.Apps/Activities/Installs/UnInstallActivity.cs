// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class UnInstallAttribute() : InstallUpdateAttribute
{
    public override bool Select(IActivity activity)
    {
        if (activity is InstallUpdateActivity installUpdate)
        {
            return installUpdate.Action.IsRemove;
        }

        return false;
    }
}

public static partial class AppActivityExtensions
{
    public static App OnUnInstall(this App app, Func<IContext<InstallUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.InstallUpdate, InstallUpdateAction.Remove]),
            Handler = async context =>
            {
                await handler(context.ToActivityType<InstallUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is InstallUpdateActivity installUpdate)
                {
                    return installUpdate.Action.IsRemove;
                }

                return false;
            }
        });

        return app;
    }
}