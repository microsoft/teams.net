// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

#pragma warning disable ExperimentalTeamsSuggestedAction

public static partial class AppInvokeActivityExtensions
{
    [Experimental("ExperimentalTeamsSuggestedAction")]
    public static App OnSuggestedActionSubmit(this App app, Func<IContext<SuggestedActionSubmitActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SuggestedActionSubmit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SuggestedActionSubmitActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is SuggestedActionSubmitActivity
        });

        return app;
    }

    [Experimental("ExperimentalTeamsSuggestedAction")]
    public static App OnSuggestedActionSubmit(this App app, Func<IContext<SuggestedActionSubmitActivity>, CancellationToken, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SuggestedActionSubmit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SuggestedActionSubmitActivity>(), context.CancellationToken),
            Selector = activity => activity is SuggestedActionSubmitActivity
        });

        return app;
    }
}

#pragma warning restore ExperimentalTeamsSuggestedAction
