// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Auth;

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
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.SignInTokenExchange),
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
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.SignInFailure),
            Selector = activity => activity.Name == InvokeNames.SignInFailure,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SignInFailureValue> typedActivity = new(ctx.Activity);
                SignInFailureValue failureValue = typedActivity.Value ?? new SignInFailureValue();
                string? userId = ctx.Activity.From?.Id;

                // signin/failure doesn't carry a connection name.
                // Scope to flows that have an active sign-in for this user;
                // fall back to all flows if none report a pending sign-in
                // (e.g., multi-instance deployment where the OAuthCard was sent by another node).
                IEnumerable<OAuthFlow> allFlows = registry.GetAllFlows();
                List<OAuthFlow> activeFlows = userId is not null
                    ? allFlows.Where(f => f.HasPendingSignIn(userId)).ToList()
                    : [];
                IEnumerable<OAuthFlow> targetFlows = activeFlows.Count > 0 ? activeFlows : allFlows;

                foreach (OAuthFlow flow in targetFlows)
                {
                    await flow.HandleSignInFailureAsync(ctx, failureValue, cancellationToken).ConfigureAwait(false);
                }

                return new InvokeResponse(200);
            }
        });

        // signin/verifyState
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.SignInVerifyState),
            Selector = activity => activity.Name == InvokeNames.SignInVerifyState,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SignInVerifyStateValue> typedActivity = new(ctx.Activity);
                SignInVerifyStateValue? verifyValue = typedActivity.Value;

                if (verifyValue is null)
                {
                    return new InvokeResponse(404);
                }

                // verifyState doesn't carry a connection name, so try each registered flow
                foreach (OAuthFlow flow in registry.GetAllFlows())
                {
                    InvokeResponse response = await flow.HandleVerifyStateAsync(ctx, verifyValue, cancellationToken).ConfigureAwait(false);
                    if (response.Status == 200)
                    {
                        return response;
                    }
                }

                return new InvokeResponse(400);
            }
        });
    }

    private static NullLogger GetLogger(TeamsBotApplication app)
    {
        _ = app; // Reserved for future use (e.g., resolving ILoggerFactory from DI)
        return NullLogger.Instance;
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
    /// Like <see cref="ResolveSingle"/> but when multiple flows are registered,
    /// returns the first one and logs a warning instead of returning null.
    /// Used by <c>Context.IsSignedIn</c> for backwards compatibility.
    /// </summary>
    internal OAuthFlow? ResolveSingleWithWarning()
    {
        if (_flows.Count == 1)
        {
            return _flows.Values.First();
        }

        if (_flows.Count > 1)
        {
            OAuthFlow first = _flows.Values.First();
            System.Diagnostics.Trace.TraceWarning(
                $"IsSignedIn: multiple OAuthFlow connections registered. " +
                $"Checking '{first.ConnectionName}' only. Use IsSignedInAsync(connectionName) for explicit control.");
            return first;
        }

        return null;
    }

    /// <summary>
    /// Returns all registered connection names, for use in error messages.
    /// </summary>
    internal IEnumerable<string> GetRegisteredConnectionNames() => _flows.Keys;
}
