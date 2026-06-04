// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Diagnostics;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Hosting;

/// <summary>
/// HTTP message handler that automatically acquires and attaches authentication tokens
/// for Bot Framework API calls. Supports both app-only and agentic (user-delegated) token acquisition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BotAuthenticationHandler"/> class.
/// </remarks>
/// <param name="authorizationHeaderProvider">The authorization header provider for acquiring tokens.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="authenticationOptionsName">The name of the MSAL configuration options to use for token acquisition. Defaults to "AzureAd".</param>
/// <param name="managedIdentityOptions">Optional managed identity options monitor. When the named entry matching <paramref name="authenticationOptionsName"/> has a non-empty <c>UserAssignedClientId</c>, tokens are acquired via the IMDS endpoint as the configured managed identity instead of via the app-credentials flow.</param>
internal sealed class BotAuthenticationHandler(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    ILogger<BotAuthenticationHandler> logger,
    string? authenticationOptionsName = null,
    IOptionsMonitor<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
{
    private const string AgenticScope = "https://botapi.skype.com/.default";
    private const string BotAppScope = "https://api.botframework.com/.default";
    private static readonly TimeSpan _agenticPrincipalSlidingExpiry = TimeSpan.FromHours(1);

    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
    private readonly ILogger<BotAuthenticationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOptionsMonitor<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;

    // Cache ClaimsPrincipal per agentic identity (bounded + sliding expiry) so MSAL can reuse
    // the account ID populated after the first ROPC call for silent token acquisition on subsequent calls.
    private readonly MemoryCache _agenticPrincipalCache = new(new MemoryCacheOptions { SizeLimit = 10_000 });
    // Per-key semaphores to prevent concurrent requests from mutating the same ClaimsPrincipal simultaneously.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _agenticLocks = new();
    private static readonly Action<ILogger, string, Exception?> _logAgenticToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(2), "Acquiring agentic token for AgenticAppId {AgenticAppId}");
    private static readonly Action<ILogger, string, Exception?> _logAppOnlyToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Acquiring app-only token for scope: {Scope}");
    private static readonly Action<ILogger, string, Exception?> _logTokenClaims =
        LoggerMessage.Define<string>(LogLevel.Trace, new(4), "Acquired token claims:{Claims}");
    private static readonly Action<ILogger, string, Exception?> _logInvalidAgenticUserId =
        LoggerMessage.Define<string>(LogLevel.Warning, new(5), "Invalid AgenticUserId '{AgenticUserId}'; falling back to app-only token.");
    private static readonly Action<ILogger, Exception?> _logTokenParseFailure =
        LoggerMessage.Define(LogLevel.Warning, new(6), "Failed to parse JWT token for trace logging.");

    /// <summary>
    /// Key used to store the agentic identity in HttpRequestMessage options.
    /// </summary>
    public static readonly HttpRequestOptionsKey<AgenticIdentity?> AgenticIdentityKey = new("AgenticIdentity");

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Options.TryGetValue(AgenticIdentityKey, out AgenticIdentity? agenticIdentity);

        string token = await GetAuthorizationHeaderAsync(agenticIdentity, cancellationToken).ConfigureAwait(false);

        string tokenValue = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..]
            : token;

        LogTokenClaims(tokenValue);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an authorization header for Bot Framework API calls.
    /// Supports both app-only and agentic (user-delegated) token acquisition.
    /// </summary>
    /// <param name="agenticIdentity">Optional agentic identity for user-delegated token acquisition. If not provided, acquires an app-only token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization header value.</returns>
    private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken cancellationToken)
    {
        string optionsName = authenticationOptionsName ?? BotConfig.DefaultSectionName;
        using Activity? span = Telemetry.Source.StartActivity(Telemetry.Spans.AuthOutbound, ActivityKind.Client);


        try
        {
            AuthorizationHeaderProviderOptions options = new()
            {
                AcquireTokenOptions = new AcquireTokenOptions()
                {
                    AuthenticationOptionsName = optionsName,
                }
            };

            // Conditionally apply ManagedIdentity configuration if registered
            if (_managedIdentityOptions is not null)
            {
                ManagedIdentityOptions miOptions = _managedIdentityOptions.Get(optionsName);

                if (!string.IsNullOrEmpty(miOptions.UserAssignedClientId))
                {
                    _logger.InferringUserAssignedManagedIdentity(miOptions.UserAssignedClientId);
                    options.AcquireTokenOptions.ManagedIdentity = miOptions;
                    span?.SetTag(Telemetry.Tags.AuthFlow, "managed_identity");
                }
            }

            if (agenticIdentity is not null &&
                !string.IsNullOrEmpty(agenticIdentity.AgenticAppId) &&
                !string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
            {
                span?.SetTag(Telemetry.Tags.AuthScope, AgenticScope);
                _logAgenticToken(_logger, agenticIdentity.AgenticAppId, null);

                if (!Guid.TryParse(agenticIdentity.AgenticUserId, out Guid agenticUserGuid))
                {
                    _logInvalidAgenticUserId(_logger, agenticIdentity.AgenticUserId, null);
                }
                else
                {
                    span?.SetTag(Telemetry.Tags.AuthFlow, "agentic");
                    options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, agenticUserGuid);

                    // Reuse a cached ClaimsPrincipal so MSAL's silent flow can look up
                    // the account ID populated after the first ROPC token exchange.
                    // Without this, ClaimsPrincipal is null on every call and MSAL
                    // always falls through to a network round-trip to Entra.
                    // A per-key semaphore serialises concurrent requests for the same identity
                    // to prevent unsynchronised mutation of the shared ClaimsPrincipal.
                    string cacheKey = GetAgenticCacheKey(agenticIdentity.AgenticAppId, agenticUserGuid);
                    SemaphoreSlim semaphore = _agenticLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        if (!_agenticPrincipalCache.TryGetValue(cacheKey, out ClaimsPrincipal? principal) || principal is null)
                        {
                            principal = new ClaimsPrincipal();
                            _agenticPrincipalCache.Set(cacheKey, principal, new MemoryCacheEntryOptions
                            {
                                SlidingExpiration = _agenticPrincipalSlidingExpiry,
                                Size = 1,
                                PostEvictionCallbacks =
                                {
                                    new PostEvictionCallbackRegistration
                                    {
                                        EvictionCallback = (key, _, _, _) =>
                                        {
                                            if (key is string k)
                                            {
                                                _agenticLocks.TryRemove(k, out _);
                                            }
                                        }
                                        },
                                    }
                                },
                            });
                        }

                        string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync([AgenticScope], options, principal, cancellationToken).ConfigureAwait(false);
                        return token;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }
            span?.SetTag(Telemetry.Tags.AuthScope, BotAppScope);
            _logAppOnlyToken(_logger, BotAppScope, null);
            // Don't overwrite a more specific flow (managed_identity) already set above.
            if (span is not null && !span.TagObjects.Any(t => t.Key == Telemetry.Tags.AuthFlow))
            {
                span.SetTag(Telemetry.Tags.AuthFlow, "app_only");
            }
            string appToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(BotAppScope, options, cancellationToken).ConfigureAwait(false);


            return appToken;
        }
        catch (Exception ex)
        {
            span.RecordException(ex);
            throw;
        }
    }

    private static string GetAgenticCacheKey(string agenticAppId, Guid agenticUserGuid) =>
        $"{agenticAppId}:{agenticUserGuid:D}";

    private void LogTokenClaims(string token)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }


        try
        {
            JwtSecurityToken jwtToken = new(token);
            string claims = Environment.NewLine + string.Join(Environment.NewLine, jwtToken.Claims.Select(c => $"  {c.Type}: {c.Value}"));
            _logTokenClaims(_logger, claims, null);
        }
        catch (ArgumentException ex)
        {
            _logTokenParseFailure(_logger, ex);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Snapshot and clear the lock dictionary before disposing the cache so that
            // post-eviction callbacks (which call TryRemove on _agenticLocks) cannot
            // race with or double-dispose the semaphores we are about to dispose here.
            SemaphoreSlim[] semaphores = [.. _agenticLocks.Values];
            _agenticLocks.Clear();
            _agenticPrincipalCache.Dispose();
            foreach (SemaphoreSlim s in semaphores)
            {
                s.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}
