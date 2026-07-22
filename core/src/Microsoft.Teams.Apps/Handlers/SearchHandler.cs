// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling 'application/search' invoke activities, sent by Adaptive Card
/// dynamic typeahead 'Input.ChoiceSet' inputs.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
public delegate Task<InvokeResponse> SearchHandler(Context<InvokeActivity<SearchValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering 'application/search' invoke handlers.
/// </summary>
public static class SearchExtensions
{
    /// <summary>
    /// Registers a handler for 'application/search' invoke activities. The handler should return
    /// an <see cref="InvokeResponse"/> whose body is a <see cref="SearchResponse"/>.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously a catch-all invoke handler could be registered alongside specific invoke handlers. This combination now throws at registration time.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnSearch(this TeamsBotApplication app, SearchHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Invoke, InvokeNames.Search),
            Selector = activity => activity.Name == InvokeNames.Search,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SearchValue> typedActivity = new(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
