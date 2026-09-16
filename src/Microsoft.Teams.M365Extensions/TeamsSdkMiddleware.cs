// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Core.Schema;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IMiddleware = Microsoft.Agents.Builder.IMiddleware;

namespace Microsoft.Teams.M365Extensions;

/// <summary>
/// Middleware that intercepts incoming activities and routes Teams-channel
/// traffic to a <see cref="Microsoft.Teams.Apps.TeamsBotApplication"/> instead of letting it
/// continue through the Microsoft 365 Agents SDK pipeline.
/// </summary>
/// <remarks>
/// Register this as a singleton <see cref="IMiddleware"/> in DI.  Because the
/// <see cref="Microsoft.Agents.Hosting.AspNetCore.CloudAdapter"/> constructor
/// takes <c>IMiddleware[]</c> (and .NET DI does not auto-resolve array types),
/// you must also register <c>IMiddleware[]</c> explicitly — see the sample's <c>Program.cs</c>.
/// </remarks>
public class TeamsSdkMiddleware : IMiddleware
{
    /// <summary>
    /// The Agents SDK <see cref="ITurnContext"/> for the current turn.
    /// Uses <see cref="AsyncLocal{T}"/> so it flows within the same async context
    /// regardless of which thread the turn executes on (non-invoke activities are
    /// processed on a background thread where HttpContext is unavailable).
    /// </summary>
    internal static ITurnContext? CurrentTurnContext => _currentTurnContext.Value;

    /// <summary>
    /// True for Teams turns, including Teams sub-channels such as <c>msteams:COPILOT</c>.
    /// </summary>
    /// <param name="activity">The activity to inspect.</param>
    /// <returns><see langword="true"/> when the activity is on the Teams channel.</returns>
    public static bool IsTeamsChannel(IActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        string? channelId = activity.ChannelId?.ToString();
        if (string.IsNullOrWhiteSpace(channelId))
        {
            return false;
        }

        int separatorIndex = channelId.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            channelId = channelId[..separatorIndex];
        }

        return string.Equals(channelId, Channels.Msteams.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static readonly AsyncLocal<ITurnContext?> _currentTurnContext = new();

    private readonly TeamsBotApplication _teamsBot;
    private readonly ILogger<TeamsSdkMiddleware> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<ITurnContext, bool>? _shouldBypassTeams;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsSdkMiddleware"/> class.
    /// </summary>
    /// <param name="teamsBot">The Teams SDK application matched Teams turns are routed to.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="httpContextAccessor">Accessor used to expose a synthetic request to Teams SDK handlers.</param>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="shouldBypassTeams">
    /// Optional predicate evaluated only for Teams-channel activities. Return
    /// <see langword="true"/> to bypass Teams routing and force the turn to fall through
    /// to the Agents SDK even when the Teams SDK has a matching route.
    /// </param>
    public TeamsSdkMiddleware(
        TeamsBotApplication teamsBot,
        ILogger<TeamsSdkMiddleware> logger,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        Func<ITurnContext, bool>? shouldBypassTeams = null)
    {
        _teamsBot = teamsBot ?? throw new ArgumentNullException(nameof(teamsBot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _shouldBypassTeams = shouldBypassTeams;
    }

    /// <inheritdoc/>
    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(next);

        if (IsTeamsChannel(turnContext.Activity))
        {
            // Bridge: serialize the Agents SDK IActivity to JSON, then deserialize
            // into the Teams SDK activity model.  Both SDKs implement the same
            // Activity Protocol wire format, so the conversion is lossless.
            string activityJson = ProtocolJsonSerializer.ToJson(turnContext.Activity);

            if (_shouldBypassTeams is not null && _shouldBypassTeams(turnContext))
            {
                _logger.LogDebug(
                    "TeamsSdkMiddleware: custom Teams bypass routed activity {ActivityId} to Agents SDK",
                    turnContext.Activity.Id);
                await next(cancellationToken).ConfigureAwait(false);
                return;
            }

            // HasMatchingRoute calls TeamsActivity.FromActivity which mutates the
            // CoreActivity (Extract removes entries from Properties). Deserialize a
            // separate copy for the match check so the real activity stays intact.
            CoreActivity routeCheckActivity = CoreActivity.FromJsonString(activityJson);

            // Only route to Teams SDK if a registered handler matches this activity.
            // Unmatched activities fall through to the Agents SDK pipeline.
            if (_teamsBot.HasMatchingRoute(routeCheckActivity))
            {
                _logger.LogDebug("TeamsSdkMiddleware: routing msteams activity {ActivityId} to Teams SDK", turnContext.Activity.Id);

                // Make the Agents SDK turn context available to Teams SDK handlers.
                // Non-invoke activities are processed on a background thread where
                // the original ASP.NET HttpContext is unavailable, so we synthesize
                // a minimal HttpContext from the current turn before calling
                // TeamsBotApplication.ProcessAsync.
                ITurnContext? previousTurnContext = _currentTurnContext.Value;
                HttpContext? previousHttpContext = _httpContextAccessor.HttpContext;
                _currentTurnContext.Value = turnContext;

                // When the turn runs on a background thread there is no ambient
                // HttpContext, so the synthetic context would otherwise fall back to
                // the root service provider. Resolving scoped services from the root
                // provider throws, so create a dedicated DI scope for the turn and
                // dispose it once ProcessAsync completes. When a real request context
                // is present we reuse its already-scoped provider instead.
                IServiceScope? turnScope = previousHttpContext is null ? _serviceProvider.CreateScope() : null;
                DefaultHttpContext syntheticContext = CreateSyntheticHttpContext(turnContext, activityJson, previousHttpContext, turnScope);
                _httpContextAccessor.HttpContext = syntheticContext;

                try
                {
                    await _teamsBot.ProcessAsync(syntheticContext, cancellationToken).ConfigureAwait(false);

                    if (turnContext.Activity.Type == ActivityTypes.Invoke)
                    {
                        Activity invokeResponseActivity = await CreateInvokeResponseActivityAsync(syntheticContext.Response, cancellationToken).ConfigureAwait(false);
                        await turnContext.SendActivityAsync(invokeResponseActivity, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _httpContextAccessor.HttpContext = previousHttpContext;
                    _currentTurnContext.Value = previousTurnContext;
                    await syntheticContext.Request.Body.DisposeAsync().ConfigureAwait(false);
                    await syntheticContext.Response.Body.DisposeAsync().ConfigureAwait(false);
                    turnScope?.Dispose();
                }

                // Short-circuit: do NOT call next() — Teams SDK handled this activity.
                return;
            }

            _logger.LogDebug("TeamsSdkMiddleware: no matching Teams SDK route for activity {ActivityId}, falling through to Agents SDK", turnContext.Activity.Id);
        }

        // Non-Teams channels (or unmatched Teams activities) continue to the Agents SDK pipeline.
        await next(cancellationToken).ConfigureAwait(false);
    }

    private DefaultHttpContext CreateSyntheticHttpContext(ITurnContext turnContext, string activityJson, HttpContext? previousHttpContext, IServiceScope? turnScope)
    {
        byte[] requestBody = Encoding.UTF8.GetBytes(activityJson);
        var syntheticContext = new DefaultHttpContext
        {
            RequestServices = previousHttpContext?.RequestServices ?? turnScope?.ServiceProvider ?? _serviceProvider,
            TraceIdentifier = previousHttpContext?.TraceIdentifier ?? turnContext.Activity.RequestId ?? Guid.NewGuid().ToString(),
            User = turnContext.Identity is ClaimsIdentity identity
                ? new ClaimsPrincipal(identity)
                : previousHttpContext?.User
                    ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        syntheticContext.Request.Method = HttpMethods.Post;
        syntheticContext.Request.ContentType = "application/json";
        syntheticContext.Request.ContentLength = requestBody.Length;
        syntheticContext.Request.Body = new MemoryStream(requestBody);
        syntheticContext.Response.Body = new MemoryStream();

        if (previousHttpContext is not null)
        {
            syntheticContext.Request.Scheme = previousHttpContext.Request.Scheme;
            syntheticContext.Request.Host = previousHttpContext.Request.Host;
            syntheticContext.Request.PathBase = previousHttpContext.Request.PathBase;
            syntheticContext.Request.Path = previousHttpContext.Request.Path;
            syntheticContext.Request.QueryString = previousHttpContext.Request.QueryString;
            syntheticContext.Request.Protocol = previousHttpContext.Request.Protocol;

            if (previousHttpContext.Request.Headers.TryGetValue("MS-CV", out var correlationVector))
            {
                syntheticContext.Request.Headers["MS-CV"] = correlationVector.ToString();
            }
        }

        return syntheticContext;
    }

    private static async Task<Activity> CreateInvokeResponseActivityAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        object? body = null;

        if (response.Body.CanSeek)
        {
            response.Body.Position = 0;
        }

        if (response.Body is not null && response.Body.Length > 0)
        {
            using JsonDocument responseJson = await JsonDocument.ParseAsync(response.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
            body = responseJson.RootElement.Clone();
        }

        return (Activity)Activity.CreateInvokeResponseActivity(body, response.StatusCode);
    }
}
