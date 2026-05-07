// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Apps;


/// <summary>
/// Context for a bot turn.
/// </summary>
/// <param name="botApplication">The bot application instance that owns this context.</param>
/// <param name="activity">The incoming activity for this turn.</param>
public class Context<TActivity>(TeamsBotApplication botApplication, TActivity activity) where TActivity : TeamsActivity
{
    /// <summary>
    /// Base bot application.
    /// </summary>
    public TeamsBotApplication TeamsBotApplication { get; } = botApplication;

    /// <summary>
    /// Current activity.
    /// </summary>
    public TActivity Activity { get; } = activity;

    /// <summary>
    /// Gets the application (client) ID configured for this bot.
    /// </summary>
    public string AppId => TeamsBotApplication.AppId;

    private ContextLogger? _log;

    /// <summary>
    /// Gets the logger for this context, providing <c>.Info()</c>, <c>.Error()</c>, <c>.Debug()</c>,
    /// and <c>.Warn()</c> convenience methods that delegate to the underlying <see cref="ILogger"/>.
    /// </summary>
    public ContextLogger Log => _log ??= new ContextLogger(TeamsBotApplication.Logger);

    private ApiClient? _api;

    /// <summary>
    /// Gets the <see cref="ApiClient"/> scoped to the current activity's service URL.
    /// </summary>
    public ApiClient Api => _api ??= TeamsBotApplication.Api.ForServiceUrl(
        Activity.ServiceUrl ?? throw new InvalidOperationException("Activity.ServiceUrl is required to use the Api client."));

    // ==================== Convenience Send/Reply/Typing ====================

    /// <summary>
    /// Sends a text message to the conversation.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Send(string text, CancellationToken cancellationToken = default)
        => SendActivityAsync(text, cancellationToken);

    /// <summary>
    /// Sends an activity to the conversation.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Send(TeamsActivity activity, CancellationToken cancellationToken = default)
        => SendActivityAsync(activity, cancellationToken);

    /// <summary>
    /// Sends a text message as a threaded reply to the current activity. When the inbound activity
    /// has an id, the response auto-quotes it (rendered as a quote bubble above the response in Teams);
    /// otherwise sends without quoting.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Reply(string text, CancellationToken cancellationToken = default)
        => Reply(new MessageActivity(text), cancellationToken);

    /// <summary>
    /// Sends an activity to the conversation. When the inbound activity has an id, the response
    /// auto-quotes it (rendered as a quote bubble above the response in Teams). Otherwise sends
    /// without quoting. To send without quoting unconditionally, use <see cref="Send(TeamsActivity, CancellationToken)"/>.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Reply(TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
#pragma warning disable ExperimentalTeamsQuotedReplies
        if (!string.IsNullOrWhiteSpace(Activity.Id))
        {
            return Quote(Activity.Id, activity, cancellationToken);
        }
#pragma warning restore ExperimentalTeamsQuotedReplies
        return SendActivityAsync(activity, cancellationToken);
    }

    /// <summary>
    /// Sends a typing indicator to the conversation.
    /// </summary>
    /// <param name="text">Reserved for future use; currently ignored.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Typing(string? text = null, CancellationToken cancellationToken = default)
        => SendTypingActivityAsync(cancellationToken);

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="text">The response text, appended to the quoted message placeholder.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<SendActivityResponse?> Quote(string messageId, string text, CancellationToken cancellationToken = default)
        => Quote(messageId, new MessageActivity(text), cancellationToken);

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="activity">The activity to send. For <see cref="MessageActivity"/>, a quote placeholder for messageId is prepended to its text. Other activity types are sent as-is without quoting.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public Task<SendActivityResponse?> Quote(string messageId, TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        if (activity is MessageActivity message)
        {
            message.PrependQuote(messageId);
        }
        return SendActivityAsync(activity, cancellationToken);
    }

    // ==================== Core Send Methods ====================

    /// <summary>
    /// Sends a message activity as a reply.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendActivityAsync(string text, CancellationToken cancellationToken = default)
    {
        TeamsActivity reply = new TeamsActivityBuilder()
            .WithConversationReference(Activity)
            .WithText(text)
            .Build();
        return TeamsBotApplication.SendActivityAsync(reply, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends an activity to the conversation.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendActivityAsync(TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        TeamsActivity reply = new TeamsActivityBuilder(activity)
            .WithConversationReference(Activity)
            .Build();
        return TeamsBotApplication.SendActivityAsync(reply, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a typing activity to the conversation asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendTypingActivityAsync(CancellationToken cancellationToken = default)
    {
        TeamsActivity reply = new TeamsActivityBuilder()
            .WithType(TeamsActivityType.Typing)
            .WithConversationReference(Activity)
            .Build();
        return TeamsBotApplication.SendActivityAsync(reply, cancellationToken: cancellationToken);
    }

    // ==================== OAuth Sign-In ====================

    /// <summary>
    /// Trigger user OAuth sign-in flow for the activity sender.
    /// Attempts silent token acquisition first; if no token is cached, sends an OAuthCard.
    /// </summary>
    /// <param name="options">OAuth options including connection name and card text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The existing user token if found, or null if the sign-in flow was initiated.</returns>
    public Task<string?> SignIn(OAuthOptions? options = null, CancellationToken cancellationToken = default)
    {
        OAuthFlow flow = ResolveOAuthFlow(options?.ConnectionName);
        return flow.SignInAsync(this, options, cancellationToken);
    }

    /// <summary>
    /// Sign the user out, revoking their token from the Bot Framework Token Store.
    /// </summary>
    /// <param name="connectionName">The connection name to sign out from. If null, uses the default registered connection.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task SignOut(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        OAuthFlow flow = ResolveOAuthFlow(connectionName);
        return flow.SignOutAsync(this, cancellationToken);
    }

    /// <summary>
    /// Whether the activity sender has a valid cached token.
    /// When a single OAuthFlow is registered, checks that connection.
    /// When multiple are registered, checks the first one and logs a warning;
    /// prefer <see cref="IsSignedInAsync"/> with an explicit connection name instead.
    /// Returns false if no OAuthFlow is registered.
    /// </summary>
    /// <remarks>
    /// This property blocks the calling thread (sync-over-async) while querying
    /// the Bot Framework Token Service. Under high concurrency this can cause
    /// thread-pool starvation. Prefer <see cref="IsSignedInAsync"/> in new code.
    /// </remarks>
    [Obsolete("Use IsSignedInAsync() instead. This property blocks the calling thread and can cause thread-pool starvation under load.")]
    public bool IsSignedIn
    {
        get
        {
            OAuthFlowRegistry? registry = TeamsBotApplication.OAuthRegistry;
            if (registry is null) return false;

            OAuthFlow? flow = registry.ResolveSingleWithWarning();
            if (flow is null) return false;

            return flow.GetTokenAsync(this).GetAwaiter().GetResult() is not null;
        }
    }

    /// <summary>
    /// Check whether the user has a valid cached token for a given OAuth connection.
    /// </summary>
    /// <param name="connectionName">The connection name to check. If null, uses the single registered connection.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user has a valid token; false otherwise.</returns>
    public Task<bool> IsSignedInAsync(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        OAuthFlow flow = ResolveOAuthFlow(connectionName);
        return flow.IsSignedInAsync(this, cancellationToken);
    }

    /// <summary>
    /// Get the token status for all configured OAuth connections.
    /// Returns every connection registered on the bot, so the developer
    /// never needs to enumerate connection names manually.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of token status results for all configured connections.</returns>
    public Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync(CancellationToken cancellationToken = default)
    {
        OAuthFlowRegistry registry = TeamsBotApplication.OAuthRegistry
            ?? throw new InvalidOperationException("No OAuthFlow registered. Call AddOAuthFlow(connectionName) on the TeamsBotApplication first.");

        // Use any flow -- GetConnectionStatusAsync returns all connections regardless
        OAuthFlow flow = registry.ResolveSingle()
            ?? registry.GetAllFlows().First();

        return flow.GetConnectionStatusAsync(this, cancellationToken);
    }

    private OAuthFlow ResolveOAuthFlow(string? connectionName)
    {
        OAuthFlowRegistry registry = TeamsBotApplication.OAuthRegistry
            ?? throw new InvalidOperationException("No OAuthFlow registered. Call AddOAuthFlow(connectionName) on the TeamsBotApplication first.");

        if (connectionName is not null)
        {
            OAuthFlow? flow = registry.Resolve(connectionName);
            if (flow is not null) return flow;

            string registered = string.Join(", ", registry.GetRegisteredConnectionNames().Select(n => $"'{n}'"));
            throw new InvalidOperationException(
                $"No OAuthFlow registered for connection '{connectionName}'. " +
                $"Registered connections: {(registered.Length > 0 ? registered : "(none)")}.");
        }

        return registry.ResolveSingle()
            ?? throw new InvalidOperationException(
                "Multiple OAuthFlow instances registered. Specify a connection name in OAuthOptions or SignOut(connectionName).");
    }
}
