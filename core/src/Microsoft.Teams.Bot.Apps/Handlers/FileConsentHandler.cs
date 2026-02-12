// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Invokes;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling file consent invoke activities
/// </summary>
public delegate Task<CoreInvokeResponse> FileConsentValueHandler(Context<InvokeActivity<FileConsentValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering file consent invoke handlers.
/// </summary>
public static class FileConsentExtensions
{

    /// <summary>
    /// Registers a handler for file consent invoke activities.
    /// </summary>
    public static TeamsBotApplication OnFileConsent(this TeamsBotApplication app, FileConsentValueHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.FileConsent),
            Selector = activity => activity.Name == InvokeNames.FileConsent,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<FileConsentValue> typedActivity = new InvokeActivity<FileConsentValue>(ctx.Activity);
                Context<InvokeActivity<FileConsentValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
