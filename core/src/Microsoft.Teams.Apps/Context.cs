// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Apps.State;
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
    /// Gets the backward-compatible logger for this context, providing <c>.Info()</c>, <c>.Error()</c>,
    /// <c>.Debug()</c>, and <c>.Warn()</c> convenience methods that delegate to the underlying <see cref="ILogger"/>.
    /// </summary>
    [Obsolete("Use a standard Microsoft.Extensions.Logging ILogger obtained via dependency injection instead.")]
    public ContextLogger Log => _log ??= new ContextLogger(TeamsBotApplication.Logger);

    private ApiClient? _api;

    /// <summary>
    /// Gets the <see cref="ApiClient"/> scoped to the current activity's service URL and
    /// the agentic identity derived from the inbound activity's recipient (the bot's own account).
    /// </summary>
    public ApiClient Api => _api ??= TeamsBotApplication.Api.ForActivity(Activity);

    // ==================== Turn State ====================

    private TurnStateContainer? _state;

    /// <summary>
    /// Gets the per-turn state container with <see cref="TurnStateContainer.ConversationState"/>
    /// and <see cref="TurnStateContainer.UserState"/> scopes.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when state management is not configured.</exception>
    public TurnStateContainer State
    {
        get => _state ?? throw new InvalidOperationException(
            "State is not available. Call UseState() during service registration, and if using a custom TeamsBotApplication make sure you pass a TurnStateLoader instance.");
        internal set => _state = value;
    }

    /// <summary>
    /// Returns true if state has been loaded for this turn.
    /// </summary>
    public bool HasState => _state is not null;

    /// <summary>
    /// Creates a copy of this context, preserving state if available.
    /// </summary>
    internal Context<TActivity> CreateDerivedContext()
    {
        Context<TActivity> derived = new(TeamsBotApplication, Activity);
        if (HasState)
        {
            derived.State = State;
        }
        return derived;
    }

    /// <summary>
    /// Creates a new context for a different activity type, preserving state if available.
    /// </summary>
    internal Context<TNew> CreateDerivedContext<TNew>(TNew activity) where TNew : TeamsActivity
    {
        Context<TNew> derived = new(TeamsBotApplication, activity);
        if (HasState)
        {
            derived.State = State;
        }
        return derived;
    }

    // ==================== Convenience Send/Reply/Typing ====================

    /// <summary>
    /// Sends a text message as a threaded reply to the current activity. When the inbound activity
    /// has an id, the response auto-quotes it (rendered as a quote bubble above the response in Teams);
    /// otherwise sends without quoting.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> ReplyAsync(string text, CancellationToken cancellationToken = default)
        => ReplyAsync(MessageActivityInput.CreateBuilder().WithText(text).Build(), cancellationToken);

    /// <summary>
    /// Sends an activity to the conversation. When the inbound activity has an id, the response
    /// auto-quotes it (rendered as a quote bubble above the response in Teams). Otherwise sends
    /// without quoting. To send without quoting unconditionally, use <see cref="Send(TeamsActivity, CancellationToken)"/>.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> ReplyAsync(TeamsActivityInput activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (!string.IsNullOrWhiteSpace(Activity.Id))
        {
            return QuoteAsync(Activity.Id, activity, cancellationToken);
        }

        return SendAsync(activity, cancellationToken);
    }

    /// <summary>
    /// Sends a typing indicator to the conversation.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> TypingAsync(CancellationToken cancellationToken = default)
    {
        string conversationId = Activity.Conversation?.Id
    ?? throw new InvalidOperationException("Activity.Conversation.Id is required to send an activity.");

        TeamsActivityInput typing = new(TeamsActivityTypes.Typing);
        return Api.Conversations.CreateActivityAsync(conversationId, typing, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="text">The response text, appended to the quoted message placeholder.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    public Task<SendActivityResponse?> QuoteAsync(string messageId, string text, CancellationToken cancellationToken = default)
        => QuoteAsync(messageId, MessageActivityInput.CreateBuilder().WithText(text).Build(), cancellationToken);

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="activity">The activity to send. For <see cref="MessageActivity"/>, a quote placeholder for messageId is prepended to its text. Other activity types are sent as-is without quoting.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    public Task<SendActivityResponse?> QuoteAsync(string messageId, TeamsActivityInput activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        if (activity is MessageActivityInput message)
        {
            new MessageActivityInputBuilder(message).PrependQuote(messageId);
        }
        return SendAsync(activity, cancellationToken);
    }

    /// <inheritdoc cref="SendAsync(string, CancellationToken)"/>
    [Obsolete("Use SendActivityAsync instead.")]
    public Task<SendActivityResponse?> Send(string text, CancellationToken cancellationToken = default)
        => SendAsync(text, cancellationToken);

    /// <inheritdoc cref="SendAsync(TeamsActivityInput, CancellationToken)"/>
    [Obsolete("Use SendActivityAsync with a TeamsActivityInput built via MessageActivityInput.CreateBuilder() instead.")]
    public Task<SendActivityResponse?> Send(TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string conversationId = Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Activity.Conversation.Id is required to send an activity.");
#pragma warning disable CS0618 // routing an inbound activity through the obsolete client overload
        return Api.Conversations.Activities.CreateAsync(conversationId, activity, cancellationToken: cancellationToken);
#pragma warning restore CS0618
    }

    /// <inheritdoc cref="ReplyAsync(string, CancellationToken)"/>
    [Obsolete("Use ReplyAsync instead.")]
    public Task<SendActivityResponse?> Reply(string text, CancellationToken cancellationToken = default)
        => ReplyAsync(text, cancellationToken);

    /// <inheritdoc cref="ReplyAsync(TeamsActivityInput, CancellationToken)"/>
    [Obsolete("Use ReplyAsync with a TeamsActivityInput built via MessageActivityInput.CreateBuilder() instead.")]
    public Task<SendActivityResponse?> Reply(TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string conversationId = Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Activity.Conversation.Id is required to send an activity.");
#pragma warning disable CS0618 // routing an inbound activity through the obsolete client overload
        if (!string.IsNullOrWhiteSpace(Activity.Id))
        {
            return Api.Conversations.Activities.ReplyAsync(conversationId, Activity.Id!, activity, cancellationToken: cancellationToken);
        }

        return Api.Conversations.Activities.CreateAsync(conversationId, activity, cancellationToken: cancellationToken);
#pragma warning restore CS0618
    }

    /// <inheritdoc cref="TypingAsync(CancellationToken)"/>
    [Obsolete("Use TypingAsync instead.")]
    public Task<SendActivityResponse?> Typing(CancellationToken cancellationToken = default)
        => TypingAsync(cancellationToken);

    /// <inheritdoc cref="QuoteAsync(string, string, CancellationToken)"/>
    [Obsolete("Use QuoteAsync instead.")]
    public Task<SendActivityResponse?> Quote(string messageId, string text, CancellationToken cancellationToken = default)
        => QuoteAsync(messageId, text, cancellationToken);

    /// <inheritdoc cref="QuoteAsync(string, TeamsActivityInput, CancellationToken)"/>
    [Obsolete("Use QuoteAsync with a TeamsActivityInput built via MessageActivityInput.CreateBuilder() instead.")]
    public Task<SendActivityResponse?> Quote(string messageId, TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string conversationId = Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Activity.Conversation.Id is required to send an activity.");
#pragma warning disable CS0618 // routing an inbound activity through the obsolete client overload
        return Api.Conversations.Activities.ReplyAsync(conversationId, messageId, activity, cancellationToken: cancellationToken);
#pragma warning restore CS0618
    }

    // ==================== Core Send Methods ====================

    /// <summary>
    /// Sends a message activity as a reply.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendAsync(string text, CancellationToken cancellationToken = default)
        => SendAsync(MessageActivityInput.CreateBuilder().WithText(text).Build(), cancellationToken);

    /// <summary>
    /// Sends an activity to the conversation. When the activity carries a recipient marked as targeted
    /// (a recipient with <c>IsTargeted</c> set), the message is sent as a
    /// targeted message visible only to that recipient.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendAsync(TeamsActivityInput activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        string conversationId = Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Activity.Conversation.Id is required to send an activity.");

        bool isTargeted = activity.Recipient?.IsTargeted == true;
        if (isTargeted && Activity.Conversation?.ConversationType == ConversationTypes.Personal)
        {
            throw new InvalidOperationException(
                "Targeted messages are not supported in personal (1:1) chats.");
        }

        // prompt preview support
        if (activity.Type == TeamsActivityTypes.Message
            && Activity.Recipient?.IsTargeted == true
            && Activity.Id is not null)
        {
            TargetedMessageInfoEntityExtensions.AddToActivity(activity, Activity.Id);
        }

        if (!isTargeted)
        {
            return Api.Conversations.CreateActivityAsync(conversationId, activity, cancellationToken: cancellationToken);
        }

#pragma warning disable ExperimentalTeamsTargeted
        return Api.Conversations.CreateTargetedActivityAsync(conversationId, activity, cancellationToken: cancellationToken);
#pragma warning restore ExperimentalTeamsTargeted
    }

    // ==================== OAuth Sign-In ====================

    /// <summary>
    /// Trigger user OAuth sign-in flow for the activity sender.
    /// Attempts silent token acquisition first; if no token is cached, sends an OAuthCard.
    /// </summary>
    /// <param name="options">OAuth options including connection name and card text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The existing user token if found, or null if the sign-in flow was initiated.</returns>
    [Obsolete("Use the OAuthFlow directly: TeamsBotApplication.GetOAuthFlow(connectionName).SignInAsync(context, ...).")]
    public Task<string?> SignInAsync(OAuthOptions? options = null, CancellationToken cancellationToken = default)
    {
        OAuthFlow flow = ResolveOAuthFlow(options?.ConnectionName);
        return flow.SignInAsync(this, options, cancellationToken);
    }

    /// <summary>
    /// Sign the user out, revoking their token from the Bot Framework Token Store.
    /// </summary>
    /// <param name="connectionName">The connection name to sign out from. If null, uses the default registered connection.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    [Obsolete("Use the OAuthFlow directly: TeamsBotApplication.GetOAuthFlow(connectionName).SignOutAsync(context, ...).")]
    public Task SignOutAsync(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        OAuthFlow flow = ResolveOAuthFlow(connectionName);
        return flow.SignOutAsync(this, cancellationToken);
    }

    /// <inheritdoc cref="SignInAsync(OAuthOptions?, CancellationToken)"/>
    [Obsolete("Use the OAuthFlow directly: TeamsBotApplication.GetOAuthFlow(connectionName).SignInAsync(context, ...).")]
    public Task<string?> SignIn(OAuthOptions? options = null, CancellationToken cancellationToken = default)
#pragma warning disable CS0618 // delegates to the obsolete SignInAsync; both are deprecated in favor of the flow.
        => SignInAsync(options, cancellationToken);
#pragma warning restore CS0618

    /// <inheritdoc cref="SignOutAsync(string?, CancellationToken)"/>
    [Obsolete("Use the OAuthFlow directly: TeamsBotApplication.GetOAuthFlow(connectionName).SignOutAsync(context, ...).")]
    public Task SignOut(string? connectionName = null, CancellationToken cancellationToken = default)
#pragma warning disable CS0618 // delegates to the obsolete SignOutAsync; both are deprecated in favor of the flow.
        => SignOutAsync(connectionName, cancellationToken);
#pragma warning restore CS0618


    /// <summary>
    /// Check whether the user has a valid cached token for a given OAuth connection.
    /// </summary>
    /// <param name="connectionName">The connection name to check. If null, uses the single registered connection.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user has a valid token; false otherwise.</returns>
    [Obsolete("Use the OAuthFlow directly: TeamsBotApplication.GetOAuthFlow(connectionName).IsSignedInAsync(context, ...).")]
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
        OAuthFlow flow = registry.GetAllFlows().First();

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
