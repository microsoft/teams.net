// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Apps.OAuth;

/// <summary>
/// Delegate invoked after a successful OAuth token exchange or sign-in verification.
/// </summary>
/// <param name="context">The activity context (invoke context from SSO or verifyState).</param>
/// <param name="tokenResponse">The token result containing the access token and connection name.</param>
/// <param name="cancellationToken">A cancellation token.</param>
public delegate Task SignInCompleteHandler(Context<TeamsActivity> context, GetTokenResult tokenResponse, CancellationToken cancellationToken);

/// <summary>
/// Delegate invoked when an OAuth token exchange or sign-in verification fails.
/// </summary>
/// <param name="context">The activity context.</param>
/// <param name="failure">Optional failure details. Non-null when the failure originates from a Teams client-side
/// <c>signin/failure</c> invoke (contains the structured failure code and message).
/// Null when the failure is a server-side token exchange or verify-state failure.</param>
/// <param name="cancellationToken">A cancellation token.</param>
public delegate Task SignInFailureHandler(Context<TeamsActivity> context, SignInFailureValue? failure, CancellationToken cancellationToken);

/// <summary>
/// Provides a high-level abstraction for Teams Bot SSO authentication.
/// Encapsulates silent token acquisition, SSO token exchange, fallback sign-in, and sign-out.
/// </summary>
public class OAuthFlow
{
    private readonly TeamsBotApplication _app;
    private readonly ILogger _logger;
    private readonly string _connectionName;
    private readonly OAuthOptions _defaultOptions;
    private SignInCompleteHandler? _onSignInComplete;
    private SignInFailureHandler? _onSignInFailure;

    // In-memory fallback for deduplication when turn state is not configured.
    // When UseState() is configured, conversation state is used instead (works across instances).
    private readonly ConcurrentDictionary<string, DateTimeOffset> _processedExchanges = new();

    // In-memory fallback for pending sign-in tracking when turn state is not configured.
    // When UseState() is configured, user state is used instead (works across instances).
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pendingSignIns = new();

    internal OAuthFlow(TeamsBotApplication app, string connectionName, OAuthOptions options, ILogger logger)
    {
        _app = app;
        _connectionName = connectionName;
        _defaultOptions = options;
        _logger = logger;
    }

    /// <summary>
    /// The OAuth connection name.
    /// </summary>
    public string ConnectionName => _connectionName;

    /// <summary>
    /// Register a callback invoked after a successful token exchange (SSO or fallback sign-in).
    /// </summary>
    /// <param name="handler">The handler to invoke on successful sign-in.</param>
    /// <returns>This <see cref="OAuthFlow"/> instance for chaining.</returns>
    public OAuthFlow OnSignInComplete(SignInCompleteHandler handler)
    {
        _onSignInComplete = handler;
        return this;
    }

    /// <summary>
    /// Register a callback invoked when token exchange fails.
    /// </summary>
    /// <param name="handler">The handler to invoke on sign-in failure.</param>
    /// <returns>This <see cref="OAuthFlow"/> instance for chaining.</returns>
    public OAuthFlow OnSignInFailure(SignInFailureHandler handler)
    {
        _onSignInFailure = handler;
        return this;
    }

    /// <summary>
    /// Attempt silent token acquisition from the Bot Framework Token Store.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="context">The current turn context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token string, or null if no token is cached.</returns>
    public async Task<string?> GetTokenAsync<TActivity>(Context<TActivity> context, CancellationToken cancellationToken = default) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(context);
        string userId = GetUserId(context);
        string channelId = GetChannelId(context);

        GetTokenResult? result = await _app.UserTokenClient.GetTokenAsync(userId, _connectionName, channelId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result?.Token;
    }

    /// <summary>
    /// Attempt silent token acquisition; if no token is available, send an OAuthCard to initiate the SSO flow.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="context">The current turn context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The token if already cached, or null if SSO was initiated (the result will arrive via <see cref="OnSignInComplete"/>).</returns>
    public Task<string?> SignInAsync<TActivity>(Context<TActivity> context, CancellationToken cancellationToken = default) where TActivity : TeamsActivity
        => SignInAsync(context, options: null, cancellationToken);

    /// <summary>
    /// Attempt silent token acquisition; if no token is available, send an OAuthCard to initiate the SSO flow.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="context">The current turn context.</param>
    /// <param name="options">OAuth options for customizing the sign-in card text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The token if already cached, or null if SSO was initiated (the result will arrive via <see cref="OnSignInComplete"/>).</returns>
    public async Task<string?> SignInAsync<TActivity>(Context<TActivity> context, OAuthOptions? options, CancellationToken cancellationToken = default) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(context);
        options ??= _defaultOptions;
        string userId = GetUserId(context);
        string channelId = GetChannelId(context);

        // 1. Try silent token acquisition
        GetTokenResult? existingToken = await _app.UserTokenClient.GetTokenAsync(userId, _connectionName, channelId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (existingToken?.Token is not null)
        {
            _logger.LogDebug("Token found in store for connection '{ConnectionName}', user '{UserId}'.", _connectionName, userId);
            return existingToken.Token;
        }

        // 2. No token - get sign-in resource and send OAuthCard
        _logger.LogDebug("No cached token for connection '{ConnectionName}'. Initiating sign-in flow.", _connectionName);

        // Build state with MsAppId so the Token Service returns TokenExchangeResource for SSO
        var tokenExchangeState = new
        {
            ConnectionName = _connectionName,
            Conversation = new
            {
                ActivityId = context.Activity.Id,
                Bot = new { Id = context.Activity.Recipient?.Id },
                ChannelId = channelId,
                Conversation = new { Id = context.Activity.Conversation?.Id },
                ServiceUrl = context.Activity.ServiceUrl?.ToString(),
                User = new { Id = userId }
            },
            MsAppId = _app.AppId
        };
        string state = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(tokenExchangeState));

        GetSignInResourceResult signInResource = await _app.UserTokenClient
            .GetSignInResourceAsync(state, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        OAuthCard oauthCard = new()
        {
            Text = options.OAuthCardText,
            ConnectionName = _connectionName,
            Buttons =
            [
                new SuggestedAction(ActionType.SignIn, options.SignInButtonText, signInResource.SignInLink)
            ],
            TokenExchangeResource = signInResource.TokenExchangeResource,
            TokenPostResource = signInResource.TokenPostResource
        };

        // Serialize to JsonElement so the source-generated JSON context can handle it
        JsonElement oauthCardJson = JsonSerializer.SerializeToElement(oauthCard);

        TeamsAttachment attachment = TeamsAttachment.CreateBuilder()
            .WithContentType(AttachmentContentType.OAuthCard)
            .WithContent(oauthCardJson)
            .Build();

        TeamsActivity oauthActivity = TeamsActivity.CreateBuilder()
            .WithConversationReference(context.Activity)
            .WithRecipient(context.Activity.From, false)
            .WithAttachment(attachment)
            .Build();

        await context.SendActivityAsync(oauthActivity, cancellationToken).ConfigureAwait(false);

        // Track that this user has a pending sign-in for this flow.
        // Use user state when available (distributed); fall back to in-memory otherwise.
        SetPendingSignIn(context, userId);

        return null;
    }

    /// <summary>
    /// Sign the user out, revoking their token from the Bot Framework Token Store.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="context">The current turn context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SignOutAsync<TActivity>(Context<TActivity> context, CancellationToken cancellationToken = default) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(context);
        string userId = GetUserId(context);
        string channelId = GetChannelId(context);

        _logger.LogDebug("Signing out user '{UserId}' from connection '{ConnectionName}'.", userId, _connectionName);
        await _app.UserTokenClient.SignOutUserAsync(userId, _connectionName, channelId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Check whether the user has a valid cached token for this flow's connection.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="context">The current turn context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user has a valid token; false otherwise.</returns>
    public async Task<bool> IsSignedInAsync<TActivity>(Context<TActivity> context, CancellationToken cancellationToken = default) where TActivity : TeamsActivity
    {
        string? token = await GetTokenAsync(context, cancellationToken).ConfigureAwait(false);
        return token is not null;
    }

    /// <summary>
    /// Get the token status for all configured OAuth connections.
    /// This calls GetTokenStatus which returns every connection registered on the bot,
    /// so the developer never needs to enumerate connection names manually.
    /// </summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="context">The current turn context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of token status results for all configured connections.</returns>
    public async Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync<TActivity>(Context<TActivity> context, CancellationToken cancellationToken = default) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(context);
        string userId = GetUserId(context);
        string channelId = GetChannelId(context);

        return await _app.UserTokenClient.GetTokenStatusAsync(userId, channelId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the signin/tokenExchange invoke activity.
    /// </summary>
    internal async Task<InvokeResponse> HandleTokenExchangeAsync(Context<InvokeActivity> context, SignInTokenExchangeValue exchangeValue, CancellationToken cancellationToken)
    {
        string exchangeId = exchangeValue.Id ?? string.Empty;

        // Deduplication: Teams sends duplicate exchanges from multiple endpoints.
        // Use conversation state when available (distributed, works across instances);
        // fall back to in-memory ConcurrentDictionary otherwise.
        if (IsDuplicateExchange(context, exchangeId))
        {
            _logger.LogDebug("Duplicate signin/tokenExchange with Id '{ExchangeId}' - returning 200 no-op.", exchangeId);
            return new InvokeResponse(200);
        }

        string userId = GetUserId(context);
        string channelId = GetChannelId(context);
        string connectionName = exchangeValue.ConnectionName ?? _connectionName;

        try
        {
            GetTokenResult tokenResult = await _app.UserTokenClient
                .ExchangeTokenAsync(userId, connectionName, channelId, exchangeValue.Token, cancellationToken)
                .ConfigureAwait(false);

            if (tokenResult?.Token is not null)
            {
                ClearPendingSignIn(context, userId);
                _logger.LogDebug("Token exchange succeeded for connection '{ConnectionName}', user '{UserId}'.", connectionName, userId);
                if (_onSignInComplete is not null)
                {
                    Context<TeamsActivity> baseContext = new(context.TeamsBotApplication, context.Activity);
                    await _onSignInComplete(baseContext, tokenResult, cancellationToken).ConfigureAwait(false);
                }
                return new InvokeResponse(200);
            }
        }
        catch (HttpRequestException ex)
        {
            ClearPendingSignIn(context, userId);
            _logger.LogWarning(ex, "Token exchange failed for connection '{ConnectionName}', user '{UserId}'.", connectionName, userId);
            return await HandleTokenExchangeFailureAsync(context, exchangeValue, ex.StatusCode, ex.Message, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            ClearPendingSignIn(context, userId);
            _logger.LogWarning(ex, "Token exchange failed for connection '{ConnectionName}', user '{UserId}'.", connectionName, userId);
            return await HandleTokenExchangeFailureAsync(context, exchangeValue, null, ex.Message, cancellationToken).ConfigureAwait(false);
        }

        // Token was null without exception — treat as expected failure
        ClearPendingSignIn(context, userId);
        return await HandleTokenExchangeFailureAsync(context, exchangeValue, null, "Token exchange returned null token.", cancellationToken).ConfigureAwait(false);
    }

    private async Task<InvokeResponse> HandleTokenExchangeFailureAsync(
        Context<InvokeActivity> context,
        SignInTokenExchangeValue exchangeValue,
        System.Net.HttpStatusCode? statusCode,
        string? failureDetail,
        CancellationToken cancellationToken)
    {
        if (_onSignInFailure is not null)
        {
            Context<TeamsActivity> baseContext = new(context.TeamsBotApplication, context.Activity);
            await _onSignInFailure(baseContext, null, cancellationToken).ConfigureAwait(false);
        }

        // For unexpected status codes (e.g., 401 Unauthorized, 403 Forbidden),
        // return the original status code so the caller can distinguish the failure.
        if (statusCode.HasValue
            && statusCode.Value != System.Net.HttpStatusCode.NotFound
            && statusCode.Value != System.Net.HttpStatusCode.BadRequest
            && statusCode.Value != System.Net.HttpStatusCode.PreconditionFailed)
        {
            return new InvokeResponse((int)statusCode.Value);
        }

        // 412 tells Teams to show the sign-in card as fallback.
        // Include a response body with the exchange ID and failure detail for diagnostics.
        return new InvokeResponse(412, new TokenExchangeInvokeResponse
        {
            Id = exchangeValue.Id,
            ConnectionName = exchangeValue.ConnectionName,
            FailureDetail = failureDetail
        });
    }

    /// <summary>
    /// Handles the signin/verifyState invoke activity.
    /// </summary>
    internal async Task<InvokeResponse> HandleVerifyStateAsync(Context<InvokeActivity> context, SignInVerifyStateValue verifyValue, CancellationToken cancellationToken)
    {
        if (verifyValue.State is null)
        {
            _logger.LogWarning(
                "Verify state: state parameter is null for conversation '{ConversationId}', user '{UserId}'.",
                context.Activity.Conversation?.Id,
                context.Activity.From?.Id);
            return new InvokeResponse(404);
        }

        string userId = GetUserId(context);
        string channelId = GetChannelId(context);
        string connectionName = _connectionName;

        try
        {
            GetTokenResult? tokenResult = await _app.UserTokenClient
                .GetTokenAsync(userId, connectionName, channelId, code: verifyValue.State, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (tokenResult?.Token is not null)
            {
                ClearPendingSignIn(context, userId);
                _logger.LogDebug("Verify state succeeded for connection '{ConnectionName}', user '{UserId}'.", connectionName, userId);
                if (_onSignInComplete is not null)
                {
                    Context<TeamsActivity> baseContext = new(context.TeamsBotApplication, context.Activity);
                    await _onSignInComplete(baseContext, tokenResult, cancellationToken).ConfigureAwait(false);
                }
                return new InvokeResponse(200);
            }
        }
        catch (HttpRequestException ex)
        {
            ClearPendingSignIn(context, userId);
            _logger.LogWarning(ex, "Verify state failed for connection '{ConnectionName}', user '{UserId}'.", connectionName, userId);

            if (_onSignInFailure is not null)
            {
                Context<TeamsActivity> baseContext = new(context.TeamsBotApplication, context.Activity);
                await _onSignInFailure(baseContext, null, cancellationToken).ConfigureAwait(false);
            }

            // For unexpected status codes, return the original code
            if (ex.StatusCode.HasValue
                && ex.StatusCode.Value != System.Net.HttpStatusCode.NotFound
                && ex.StatusCode.Value != System.Net.HttpStatusCode.BadRequest
                && ex.StatusCode.Value != System.Net.HttpStatusCode.PreconditionFailed)
            {
                return new InvokeResponse((int)ex.StatusCode.Value);
            }

            // 412 tells Teams to fall back to the sign-in card
            return new InvokeResponse(412);
        }

        // No token returned — the code likely belongs to a different connection.
        // Do NOT fire OnSignInFailure or clear pending state; the verifyState loop
        // in OAuthFlowExtensions will try the next registered flow.
        _logger.LogDebug("Verify state: no token for connection '{ConnectionName}', user '{UserId}'. Code may belong to another connection.", connectionName, userId);
        return new InvokeResponse(412);
    }

    /// <summary>
    /// Whether this flow has a pending sign-in for the user in the given context.
    /// Used to scope <c>signin/failure</c> notifications to flows that initiated a sign-in.
    /// </summary>
    /// <remarks>
    /// When turn state is configured, checks user state (works across instances).
    /// Falls back to in-memory tracking when state is not configured.
    /// Callers should fall back to notifying all flows when no flow reports a pending sign-in
    /// (e.g., multi-instance deployment without distributed state).
    /// </remarks>
    internal bool HasPendingSignIn(Context<InvokeActivity> context)
    {
        string pendingKey = $"oauth:pending:{_connectionName}";
        if (context.HasState && context.State.UserState is not null)
        {
            return context.State.UserState.ContainsKey(pendingKey);
        }

        string userId = context.Activity.From?.Id ?? string.Empty;
        return _pendingSignIns.ContainsKey(userId);
    }

    /// <summary>
    /// Handles the signin/failure invoke activity sent by the Teams client when SSO fails client-side.
    /// </summary>
    internal async Task<InvokeResponse> HandleSignInFailureAsync(Context<InvokeActivity> context, SignInFailureValue failureValue, CancellationToken cancellationToken)
    {
        string? userId = context.Activity.From?.Id;
        if (userId is not null)
        {
            ClearPendingSignIn(context, userId);
        }

        _logger.LogWarning(
            "Sign-in failed for user '{UserId}' in conversation '{ConversationId}': {FailureCode} — {FailureMessage}.{Guidance}",
            userId,
            context.Activity.Conversation?.Id,
            failureValue.Code,
            failureValue.Message,
            string.Equals(failureValue.Code, "resourcematchfailed", StringComparison.OrdinalIgnoreCase)
                ? " Verify that your Entra app registration has 'Expose an API' configured with the correct Application ID URI matching your OAuth connection's Token Exchange URL."
                : string.Empty);

        if (_onSignInFailure is not null)
        {
            Context<TeamsActivity> baseContext = new(context.TeamsBotApplication, context.Activity);
            await _onSignInFailure(baseContext, failureValue, cancellationToken).ConfigureAwait(false);
        }

        return new InvokeResponse(200);
    }

    /// <summary>
    /// Check whether the given exchange ID has already been processed, and mark it as processed if not.
    /// Always uses in-memory ConcurrentDictionary for atomic same-instance dedup (concurrent requests
    /// load separate state snapshots, so state alone cannot catch same-instance races).
    /// Additionally persists to conversation state when available for cross-instance dedup.
    /// </summary>
    private bool IsDuplicateExchange(Context<InvokeActivity> context, string exchangeId)
    {
        // Atomic same-instance check — catches concurrent duplicate requests on this node
        if (!_processedExchanges.TryAdd(exchangeId, DateTimeOffset.UtcNow))
        {
            return true;
        }

        // Cross-instance check via state — catches duplicates routed to different nodes
        string dedupKey = $"oauth:exchange:{exchangeId}";
        if (context.HasState)
        {
            if (context.State.ConversationState.ContainsKey(dedupKey))
            {
                return true;
            }

            context.State.ConversationState.Set(dedupKey, DateTimeOffset.UtcNow);
        }

        CleanupExpiredEntries();
        return false;
    }

    /// <summary>
    /// Record that this user has a pending sign-in for this flow.
    /// Uses user state when available (distributed); falls back to in-memory.
    /// </summary>
    private void SetPendingSignIn<TActivity>(Context<TActivity> context, string userId) where TActivity : TeamsActivity
    {
        string pendingKey = $"oauth:pending:{_connectionName}";
        if (context.HasState && context.State.UserState is not null)
        {
            context.State.UserState.Set(pendingKey, DateTimeOffset.UtcNow);
        }
        else
        {
            _pendingSignIns[userId] = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Clear the pending sign-in tracking for this user/flow.
    /// Clears from both state and in-memory to handle transitions.
    /// </summary>
    private void ClearPendingSignIn<TActivity>(Context<TActivity> context, string userId) where TActivity : TeamsActivity
    {
        string pendingKey = $"oauth:pending:{_connectionName}";
        if (context.HasState && context.State.UserState is not null)
        {
            context.State.UserState.Remove(pendingKey);
        }

        _pendingSignIns.TryRemove(userId, out _);
    }

    private void CleanupExpiredEntries()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        foreach (KeyValuePair<string, DateTimeOffset> kvp in _processedExchanges)
        {
            if (kvp.Value < cutoff)
            {
                _processedExchanges.TryRemove(kvp.Key, out _);
            }
        }
        foreach (KeyValuePair<string, DateTimeOffset> kvp in _pendingSignIns)
        {
            if (kvp.Value < cutoff)
            {
                _pendingSignIns.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string GetUserId<TActivity>(Context<TActivity> context) where TActivity : TeamsActivity
        => context.Activity.From?.Id ?? throw new InvalidOperationException("Activity.From.Id is required for OAuth operations.");

    private static string GetChannelId<TActivity>(Context<TActivity> context) where TActivity : TeamsActivity
        => context.Activity.ChannelId ?? throw new InvalidOperationException("Activity.ChannelId is required for OAuth operations.");

}
