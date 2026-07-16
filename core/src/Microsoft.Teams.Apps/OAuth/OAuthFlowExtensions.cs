// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.OAuth;

/// <summary>
/// Extension methods for registering <see cref="OAuthFlow"/> instances on a <see cref="TeamsBotApplication"/>.
/// </summary>
public static class OAuthFlowExtensions
{

    /// <summary>
    /// Register an <see cref="OAuthFlow"/> with an explicit OAuth connection name.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="connectionName">The OAuth connection name configured on the bot.</param>
    /// <returns>The <see cref="OAuthFlow"/> instance for configuring callbacks.</returns>
    public static OAuthFlow AddOAuthFlow(this TeamsBotApplication app, string connectionName)
        => AddOAuthFlow(app, new OAuthOptions { ConnectionName = connectionName });

    /// <summary>
    /// Register an <see cref="OAuthFlow"/> with <see cref="OAuthOptions"/> that configure both the
    /// connection name and the default OAuthCard text shown during sign-in.
    /// Per-call options passed to <see cref="OAuthFlow.SignInAsync{TActivity}(Context{TActivity}, OAuthOptions?, CancellationToken)"/>
    /// override these defaults.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="options">OAuth options. <see cref="OAuthOptions.ConnectionName"/> is required.</param>
    /// <returns>The <see cref="OAuthFlow"/> instance for configuring callbacks.</returns>
    public static OAuthFlow AddOAuthFlow(this TeamsBotApplication app, OAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionName, nameof(options.ConnectionName));

        string connectionName = options.ConnectionName;
        OAuthFlowRegistry registry = GetOrCreateRegistry(app);
        ILogger logger = GetLogger(app);

        OAuthFlow flow = new(app, connectionName, options, logger);
        registry.Register(connectionName, flow);

        return flow;
    }

    private static OAuthFlowRegistry GetOrCreateRegistry(TeamsBotApplication app)
    {
        if (app.OAuthRegistry is not null)
        {
            return app.OAuthRegistry;
        }

        OAuthFlowRegistry registry = new();
        app.OAuthRegistry = registry;

        // Register shared routes once per app
        RegisterRoutes(app, registry);
        return registry;
    }

    private static void RegisterRoutes(TeamsBotApplication app, OAuthFlowRegistry registry)
    {
        // signin/tokenExchange
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Invoke, InvokeNames.SignInTokenExchange),
            Selector = activity => activity.Name == InvokeNames.SignInTokenExchange,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SignInTokenExchangeValue> typedActivity = new(ctx.Activity);
                SignInTokenExchangeValue? exchangeValue = typedActivity.Value;

                if (exchangeValue is null)
                {
                    return new InvokeResponse(400);
                }

                OAuthFlow? flow = registry.Resolve(exchangeValue.ConnectionName);
                if (flow is null)
                {
                    return new InvokeResponse(400);
                }

                return await flow.HandleTokenExchangeAsync(ctx, exchangeValue, cancellationToken).ConfigureAwait(false);
            }
        });

        // signin/failure - Teams client-side SSO failure notification
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Invoke, InvokeNames.SignInFailure),
            Selector = activity => activity.Name == InvokeNames.SignInFailure,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SignInFailureValue> typedActivity = new(ctx.Activity);
                SignInFailureValue failureValue = typedActivity.Value ?? new SignInFailureValue();

                // signin/failure carries no connection name. Since Teams only emits it for
                // silent SSO attempts, ask the registry which connection had a pending SSO sign-in
                // and attribute the failure there — this avoids firing the failure callback on
                // a non-SSO connection (e.g., a GitHub flow) that merely signed in more recently.
                OAuthFlow? target = registry.ResolvePendingSsoFlow(ctx);

                if (target is not null)
                {
                    await target.HandleSignInFailureAsync(ctx, failureValue, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // No SSO-pending connection could be resolved (e.g., no distributed state).
                    // Fall back to notifying every flow rather than dropping the failure.
                    foreach (OAuthFlow flow in registry.GetAllFlows())
                    {
                        await flow.HandleSignInFailureAsync(ctx, failureValue, cancellationToken).ConfigureAwait(false);
                    }
                }

                return new InvokeResponse(200);
            }
        });

        // signin/verifyState
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Invoke, InvokeNames.SignInVerifyState),
            Selector = activity => activity.Name == InvokeNames.SignInVerifyState,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SignInVerifyStateValue> typedActivity = new(ctx.Activity);
                SignInVerifyStateValue? verifyValue = typedActivity.Value;

                if (verifyValue is null)
                {
                    return new InvokeResponse(404);
                }

                // verifyState doesn't carry a connection name.
                // Try the most recently initiated flow first to avoid O(N) token service calls.
                string? userId = ctx.Activity.From?.Id;
                OAuthFlow? mostRecent = null;
                DateTimeOffset mostRecentTs = DateTimeOffset.MinValue;

                if (userId is not null)
                {
                    foreach (OAuthFlow f in registry.GetAllFlows())
                    {
                        DateTimeOffset? ts = f.GetPendingSignInTimestamp(ctx);
                        if (ts is not null && ts.Value > mostRecentTs)
                        {
                            mostRecent = f;
                            mostRecentTs = ts.Value;
                        }
                    }
                }

                if (mostRecent is not null)
                {
                    InvokeResponse response = await mostRecent.HandleVerifyStateAsync(ctx, verifyValue, cancellationToken).ConfigureAwait(false);
                    if (response.Status == 200)
                    {
                        return response;
                    }
                }

                // Fall back to trying all flows (skipping the one we already tried)
                foreach (OAuthFlow flow in registry.GetAllFlows())
                {
                    if (flow == mostRecent)
                    {
                        continue;
                    }

                    InvokeResponse response = await flow.HandleVerifyStateAsync(ctx, verifyValue, cancellationToken).ConfigureAwait(false);
                    if (response.Status == 200)
                    {
                        return response;
                    }
                }

                return new InvokeResponse(404);
            }
        });
    }

    private static ILogger GetLogger(TeamsBotApplication app)
    {
        return app.Logger;
    }
}

/// <summary>
/// Internal registry that maps connection names to <see cref="OAuthFlow"/> instances.
/// Handles multi-connection dispatch for shared invoke routes.
/// </summary>
internal sealed class OAuthFlowRegistry
{
    private readonly Dictionary<string, OAuthFlow> _flows = new(StringComparer.OrdinalIgnoreCase);

    internal void Register(string connectionName, OAuthFlow flow)
    {
        if (!_flows.TryAdd(connectionName, flow))
        {
            throw new InvalidOperationException($"An OAuthFlow is already registered for connection '{connectionName}'.");
        }
    }

    /// <summary>
    /// Resolve the OAuthFlow for a given connection name from a token exchange invoke.
    /// </summary>
    internal OAuthFlow? Resolve(string? connectionName)
    {
        if (connectionName is not null && _flows.TryGetValue(connectionName, out OAuthFlow? flow))
        {
            return flow;
        }

        // If there's exactly one named flow, use it
        if (_flows.Count == 1)
        {
            return _flows.Values.First();
        }

        return null;
    }

    /// <summary>
    /// Returns all registered flows.
    /// </summary>
    internal IEnumerable<OAuthFlow> GetAllFlows() => _flows.Values;

    /// <summary>
    /// Resolve the flow that currently has a pending silent-SSO sign-in for the user in the
    /// given turn, or <c>null</c> if none. When multiple flows have a pending SSO sign-in, the
    /// most recently initiated one is returned.
    /// </summary>
    /// <remarks>
    /// Used to attribute connection-less <c>signin/failure</c> invokes — which Teams only emits
    /// for silent SSO attempts — to the connection that actually offered SSO, instead of guessing
    /// across all flows (which could fire the failure callback on a non-SSO connection).
    /// </remarks>
    internal OAuthFlow? ResolvePendingSsoFlow<TActivity>(Context<TActivity> context) where TActivity : TeamsActivity
    {
        OAuthFlow? resolved = null;
        DateTimeOffset mostRecent = DateTimeOffset.MinValue;
        foreach (OAuthFlow flow in _flows.Values)
        {
            DateTimeOffset? ts = flow.GetPendingSsoSignInTimestamp(context);
            if (ts is not null && ts.Value > mostRecent)
            {
                resolved = flow;
                mostRecent = ts.Value;
            }
        }

        return resolved;
    }

    /// <summary>
    /// Resolve when there's no connection name in the payload (e.g., verifyState).
    /// Returns the single registered flow, or null if zero or multiple flows exist.
    /// </summary>
    internal OAuthFlow? ResolveSingle()
    {
        if (_flows.Count == 1)
        {
            return _flows.Values.First();
        }

        return null;
    }

    /// <summary>
    /// Returns all registered connection names, for use in error messages.
    /// </summary>
    internal IEnumerable<string> GetRegisteredConnectionNames() => _flows.Keys;
}
