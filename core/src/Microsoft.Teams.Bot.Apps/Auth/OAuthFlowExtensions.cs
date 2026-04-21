// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps.Auth;

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
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        OAuthFlowRegistry registry = GetOrCreateRegistry(app);
        ILogger logger = GetLogger(app);

        OAuthFlow flow = new(app, connectionName, logger);
        registry.Register(connectionName, flow);

        return flow;
    }

    /// <summary>
    /// Register an <see cref="OAuthFlow"/> that auto-discovers the connection name
    /// via GetTokenStatus on first use. Use this when only one OAuth connection is configured.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <returns>The <see cref="OAuthFlow"/> instance for configuring callbacks.</returns>
    public static OAuthFlow AddOAuthFlow(this TeamsBotApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        OAuthFlowRegistry registry = GetOrCreateRegistry(app);
        ILogger logger = GetLogger(app);

        OAuthFlow flow = new(app, connectionName: null, logger);
        registry.RegisterAutoDiscover(flow);

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
        // Magic code handler: intercepts numeric messages (4-8 digits) that may be OAuth magic codes
        // from the fallback sign-in flow (non-AAD providers like GitHub).
        // Registered as a message route so it runs alongside other matching message handlers.
        app.Router.Register(new Route<MessageActivity>
        {
            Name = "message/oauth/magicCode",
            Selector = msg => IsMagicCode(msg.Text),
            Handler = async (ctx, cancellationToken) =>
            {
                string code = ctx.Activity.Text!.Trim();
                string userId = ctx.Activity.From?.Id ?? throw new InvalidOperationException("Activity.From.Id is required.");
                string channelId = ctx.Activity.ChannelId ?? throw new InvalidOperationException("Activity.ChannelId is required.");

                // Try each registered flow to see which one can redeem the code
                foreach (OAuthFlow flow in registry.GetAllFlows())
                {
                    string? connectionName = flow.ConnectionName;
                    if (connectionName is null) continue;

                    GetTokenResult? tokenResult = await app.UserTokenClient
                        .GetTokenAsync(userId, connectionName, channelId, code: code, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (tokenResult?.Token is not null)
                    {
                        await flow.HandleMagicCodeRedeemAsync(ctx, tokenResult, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }
            }
        });

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
                    return new InvokeResponse(400);
                }

                // verifyState doesn't carry a connection name, so try each registered flow
                foreach (OAuthFlow flow in registry.GetAllFlows())
                {
                    if (flow.ConnectionName is null) continue;
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

    private static bool IsMagicCode([NotNullWhen(true)] string? text)
    {
        string? trimmed = text?.Trim();
        return trimmed is not null && trimmed.Length is >= 4 and <= 8 && trimmed.All(char.IsAsciiDigit);
    }
}

/// <summary>
/// Internal registry that maps connection names to <see cref="OAuthFlow"/> instances.
/// Handles multi-connection dispatch for shared invoke routes.
/// </summary>
internal sealed class OAuthFlowRegistry
{
    private readonly Dictionary<string, OAuthFlow> _flows = new(StringComparer.OrdinalIgnoreCase);
    private OAuthFlow? _autoDiscoverFlow;

    internal void Register(string connectionName, OAuthFlow flow)
    {
        if (!_flows.TryAdd(connectionName, flow))
        {
            throw new InvalidOperationException($"An OAuthFlow is already registered for connection '{connectionName}'.");
        }
    }

    internal void RegisterAutoDiscover(OAuthFlow flow)
    {
        if (_autoDiscoverFlow is not null)
        {
            throw new InvalidOperationException("Only one auto-discover OAuthFlow can be registered. Specify connection names explicitly for multiple connections.");
        }
        _autoDiscoverFlow = flow;
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

        // If there's an auto-discover flow, use it
        if (_autoDiscoverFlow is not null)
        {
            return _autoDiscoverFlow;
        }

        // If there's exactly one named flow, use it
        if (_flows.Count == 1)
        {
            return _flows.Values.First();
        }

        return null;
    }

    /// <summary>
    /// Returns all registered flows (both named and auto-discover).
    /// </summary>
    internal IEnumerable<OAuthFlow> GetAllFlows()
    {
        foreach (OAuthFlow flow in _flows.Values)
        {
            yield return flow;
        }
        if (_autoDiscoverFlow is not null)
        {
            yield return _autoDiscoverFlow;
        }
    }

    /// <summary>
    /// Resolve when there's no connection name in the payload (e.g., verifyState).
    /// </summary>
    internal OAuthFlow? ResolveSingle()
    {
        if (_autoDiscoverFlow is not null)
        {
            return _autoDiscoverFlow;
        }

        if (_flows.Count == 1)
        {
            return _flows.Values.First();
        }

        // Multiple flows and no way to disambiguate
        return null;
    }

    /// <summary>
    /// Like <see cref="ResolveSingle"/> but when multiple flows are registered,
    /// returns the first one and logs a warning instead of returning null.
    /// Used by <c>Context.IsSignedIn</c> for backwards compatibility.
    /// </summary>
    internal OAuthFlow? ResolveSingleWithWarning()
    {
        OAuthFlow? single = ResolveSingle();
        if (single is not null)
        {
            return single;
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
}
