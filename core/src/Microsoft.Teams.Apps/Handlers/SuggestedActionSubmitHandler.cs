
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling <c>suggestedActions/submit</c> invoke activities.
/// Sent when the user clicks a suggested action of type <c>Action.Submit</c>.
/// The structured payload authored on the suggested action is delivered via <see cref="InvokeActivity.Value"/>.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
[System.Diagnostics.CodeAnalysis.Experimental("ExperimentalTeamsSuggestedAction")]
public delegate Task<InvokeResponse> SuggestedActionSubmitHandler(Context<InvokeActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering suggested action submit invoke handlers.
/// </summary>
[System.Diagnostics.CodeAnalysis.Experimental("ExperimentalTeamsSuggestedAction")]
public static class SuggestedActionSubmitExtensions
{
    /// <summary>
    /// Registers a handler for <c>suggestedActions/submit</c> invoke activities.
    /// Triggered when the user clicks a suggested action of type <c>Action.Submit</c>.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnSuggestedActionSubmit(this TeamsBotApplication app, SuggestedActionSubmitHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.SuggestedActionSubmit),
            Selector = activity => activity.Name == InvokeNames.SuggestedActionSubmit,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                return await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
