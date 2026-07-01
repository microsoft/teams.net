// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.HtmlWidget;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling <c>htmlwidget/calltool</c> invoke activities.
/// Sent when a widget calls a tool on the bot via the MCP Apps protocol.
/// </summary>
/// <param name="context">The context for the invoke activity with a strongly-typed <see cref="CallToolRequest"/> value.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
[Experimental("ExperimentalTeamsHtmlWidget")]
public delegate Task<InvokeResponse> HtmlWidgetCallToolHandler(Context<InvokeActivity<CallToolRequest>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering HTML widget call tool invoke handlers.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public static class HtmlWidgetCallToolExtensions
{
    /// <summary>
    /// Registers a handler for <c>htmlwidget/calltool</c> invoke activities.
    /// Triggered when a widget calls a tool on the bot.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnWidgetCallTool(this TeamsBotApplication app, HtmlWidgetCallToolHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Invoke, InvokeNames.HtmlWidgetCallTool),
            Selector = activity => activity.Name == InvokeNames.HtmlWidgetCallTool,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<CallToolRequest> typedActivity = new(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
