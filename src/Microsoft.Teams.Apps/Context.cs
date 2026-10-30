// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
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

    private Files.FilesAccessor? _files;

    /// <summary>
    /// Gets the uploaded files attached to the current inbound activity, plus images pasted inline, exposed as lazy <see cref="Files.IncomingFile"/> handles. See <see cref="Files.FilesAccessor"/> for the full contract.
    /// </summary>
    public Files.FilesAccessor Files => _files ??= new Files.FilesAccessor(
        Activity,
        TeamsBotApplication.Logger,
        TeamsBotApplication.FileDownloader,
        // Resolved at fetch time rather than eagerly, so a turn that never touches files pays nothing for it.
        //
        // An Agentic User reads as itself. An app-only token sees what the app may read tenant-wide, a different set from what was shared with the agent, so it would 403 on exactly the files the agent was given.
        FilesCredential.Select(
            Activity.Recipient?.GetAgenticIdentity(),
            TeamsBotApplication.GraphBaseUrl,
            // The activity's tenant, not the app's: a multi-tenant app has to read in the tenant the file lives in.
            ct => TeamsBotApplication.GetAppGraphTokenAsync(Activity.TenantId, ct),
            TeamsBotApplication.GetAgenticGraphTokenAsync),
        // Inline images are read with the turn's Bot API token, not Graph.
        FilesCredential.SelectInlineImage(
            Activity.Recipient?.GetAgenticIdentity(),
            Activity.ServiceUrl,
            TeamsBotApplication.GetBotApiTokenAsync));

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
    /// Sends a text message as a reply to the current activity.
    /// When the inbound activity has an id, the response auto-quotes it (rendered as a quote bubble above the response in Teams).
    /// Otherwise sends without quoting.
    /// </summary>
    /// <remarks>
    /// In a 1:1 chat, sends into the reply chain (thread) when the inbound activity is inside one, and at top level otherwise.
    /// The inbound activity can be a message, a card action, an invoke, a reaction, an edit or a delete.
    /// In a channel, sends into the reply chain of the inbound message.
    /// In a channel, when the service answers HTTP 404, sends to the inbound conversation instead.
    /// Group chats, meetings and other inbound activities are not placed, and the activity is sent to the inbound conversation.
    /// </remarks>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> ReplyAsync(string text, CancellationToken cancellationToken = default)
        => ReplyAsync(new MessageActivityInput().WithText(text), cancellationToken);

    /// <summary>
    /// Sends an activity to the conversation. When the inbound activity has an id, the response
    /// auto-quotes it (rendered as a quote bubble above the response in Teams). Otherwise sends
    /// without quoting. To send without quoting unconditionally, use <see cref="SendAsync(MessageActivityInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// In a 1:1 chat, sends into the reply chain (thread) when the inbound activity is inside one, and at top level otherwise.
    /// The inbound activity can be a message, a card action, an invoke, a reaction, an edit or a delete.
    /// In a channel, sends into the reply chain of the inbound message.
    /// In a channel, when the service answers HTTP 404, sends to the inbound conversation instead.
    /// Group chats, meetings and other inbound activities are not placed, and the activity is sent to the inbound conversation.
    /// </remarks>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> ReplyAsync(MessageActivityInput activity, CancellationToken cancellationToken = default)
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
        => QuoteAsync(messageId, new MessageActivityInput().WithText(text), cancellationToken);

    /// <summary>
    /// Send a message to the conversation with a quoted message reference prepended to the text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="activity">The activity to send. For <see cref="MessageActivity"/>, a quote placeholder for messageId is prepended to its text. Other activity types are sent as-is without quoting.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from sending the activity.</returns>
    public Task<SendActivityResponse?> QuoteAsync(string messageId, MessageActivityInput activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        if (activity is MessageActivityInput message)
        {
            message.PrependQuote(messageId);
        }
        return SendAsync(activity, cancellationToken);
    }

    /// <inheritdoc cref="SendAsync(string, CancellationToken)"/>
    [Obsolete("Use SendActivityAsync instead.")]
    public Task<SendActivityResponse?> Send(string text, CancellationToken cancellationToken = default)
        => SendAsync(text, cancellationToken);

    /// <summary>
    /// Sends the activity to the inbound conversation.
    /// The activity is sent as it is, and is not placed in a reply chain (thread).
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    [Obsolete("Use SendActivityAsync with a TeamsActivityInput built via new MessageActivityInput() instead.")]
    public Task<SendActivityResponse?> Send(MessageActivity activity, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Sends the activity to the inbound conversation without quoting the inbound message.
    /// When the inbound activity has an id, the activity's <c>replyToId</c> is set to it.
    /// The activity is not placed in a reply chain (thread).
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    [Obsolete("Use ReplyAsync with a TeamsActivityInput built via new MessageActivityInput() instead.")]
    public Task<SendActivityResponse?> Reply(MessageActivity activity, CancellationToken cancellationToken = default)
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

    /// <inheritdoc cref="TeamsStreamingWriter.CreateFromContext{TActivity}(Context{TActivity})"/>
    [Obsolete("Use TeamsStreamingWriter.CreateFromContext(context) instead.")]
    public TeamsStreamingWriter Stream()
        => TeamsStreamingWriter.CreateFromContext(this);

    /// <inheritdoc cref="QuoteAsync(string, string, CancellationToken)"/>
    [Obsolete("Use QuoteAsync instead.")]
    public Task<SendActivityResponse?> Quote(string messageId, string text, CancellationToken cancellationToken = default)
        => QuoteAsync(messageId, text, cancellationToken);

    /// <summary>
    /// Sends the activity to the inbound conversation without building a quote.
    /// The activity's <c>replyToId</c> is set to <paramref name="messageId"/>.
    /// The activity is not placed in a reply chain (thread).
    /// </summary>
    /// <param name="messageId">The id the activity's <c>replyToId</c> is set to.</param>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    [Obsolete("Use QuoteAsync with a TeamsActivityInput built via new MessageActivityInput() instead.")]
    public Task<SendActivityResponse?> Quote(string messageId, MessageActivity activity, CancellationToken cancellationToken = default)
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
    /// Sends a text message to the conversation without quoting the inbound message.
    /// </summary>
    /// <remarks>
    /// In a 1:1 chat, sends into the reply chain (thread) when the inbound activity is inside one, and at top level otherwise.
    /// The inbound activity can be a message, a card action, an invoke, a reaction, an edit or a delete.
    /// In a channel, sends into the reply chain of the inbound message.
    /// In a channel, when the service answers HTTP 404, sends to the inbound conversation instead.
    /// Group chats, meetings and other inbound activities are not placed, and the activity is sent to the inbound conversation.
    /// </remarks>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendAsync(string text, CancellationToken cancellationToken = default)
        => SendAsync(new MessageActivityInput().WithText(text), cancellationToken);

    /// <summary>
    /// Sends an activity to the conversation. When the activity carries a recipient marked as targeted
    /// (a recipient with <c>IsTargeted</c> set), the message is sent as a
    /// targeted message visible only to that recipient.
    /// </summary>
    /// <remarks>
    /// In a 1:1 chat, sends into the reply chain (thread) when the inbound activity is inside one, and at top level otherwise.
    /// The inbound activity can be a message, a card action, an invoke, a reaction, an edit or a delete.
    /// In a channel, sends into the reply chain of the inbound message.
    /// In a channel, when the service answers HTTP 404, sends to the inbound conversation instead.
    /// Group chats, meetings and other inbound activities are not placed, and the activity is sent to the inbound conversation.
    /// </remarks>
    /// <param name="activity">The activity to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendAsync(MessageActivityInput activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        string conversationId = Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Activity.Conversation.Id is required to send an activity.");

        bool isTargeted = activity.Recipient?.IsTargeted == true;

        // prompt preview support
        if (activity.Type == TeamsActivityTypes.Message
            && Activity.Recipient?.IsTargeted == true
            && Activity.Id is not null)
        {
            TargetedMessageInfoEntityExtensions.AddToActivity(activity, Activity.Id);
        }

        Task<SendActivityResponse?> SendToInboundConversation() => isTargeted
            ? Api.Conversations.CreateTargetedActivityAsync(conversationId, activity, cancellationToken: cancellationToken)
            : Api.Conversations.CreateActivityAsync(conversationId, activity, cancellationToken: cancellationToken);

#pragma warning disable ExperimentalTeamsThreading // the stable send and reply methods place responses in the reply chain
        if (GetInboundReplyChainPlacement() is { } placement)
        {
            return ReplyChainFallback.SendAsync(
                () => isTargeted
                    ? Api.Conversations.CreateTargetedReplyChainActivityAsync(placement.ChatOrChannelId, placement.ReplyChainId, activity, cancellationToken: cancellationToken)
                    : Api.Conversations.CreateReplyChainActivityAsync(placement.ChatOrChannelId, placement.ReplyChainId, activity, cancellationToken: cancellationToken),
                placement.InChannel ? SendToInboundConversation : null);
        }
#pragma warning restore ExperimentalTeamsThreading

        return SendToInboundConversation();
    }

    /// <summary>
    /// Finds the chat or channel and reply chain a response to the inbound activity is posted to.
    /// In a channel a response to a message always goes to the reply chain of the inbound message.
    /// <c>channelData.replyChain.id</c> is the root of that thread, and it equals the inbound message's own id for a top-level post.
    /// The destination is the channel, <c>channelData.channel.id</c>.
    /// Other inbound activities in a channel are not placed.
    /// In a chat see <see cref="IsInsideThread"/> for which activities are inside a thread.
    /// The destination is the chat, <c>channelData.chat.id</c>.
    /// When the inbound activity is on a top-level message the response is sent as a top-level message.
    /// Group chats and meetings are not placed.
    /// Returns null when an id is missing, so no reply chain (thread) is chosen and the response goes to the inbound conversation.
    /// </summary>
    internal ReplyChainPlacement? GetInboundReplyChainPlacement()
    {
        if (Activity.Conversation?.ConversationType == ConversationTypes.GroupChat)
        {
            return null;
        }

        string? replyChain = Activity.ChannelData?.ReplyChain?.Id;
        if (string.IsNullOrWhiteSpace(replyChain))
        {
            return null;
        }

        if (Activity.Conversation?.ConversationType == ConversationTypes.Channel)
        {
            string? channel = Activity.ChannelData?.Channel?.Id;
            return Activity.Type == TeamsActivityTypes.Message && !string.IsNullOrWhiteSpace(channel)
                ? new ReplyChainPlacement(channel, replyChain, InChannel: true)
                : null;
        }

        string? chat = Activity.ChannelData?.Chat?.Id;
        return !string.IsNullOrWhiteSpace(chat) && IsInsideThread(replyChain)
            ? new ReplyChainPlacement(chat, replyChain, InChannel: false)
            : null;
    }

    /// <summary>
    /// Whether the inbound activity in a chat is inside a thread: <c>channelData.replyChain.id</c> differs from the id of the message the activity acts on.
    /// The message it acts on is:
    /// <c>activity.id</c> for a message, an edit, a delete and a reaction;
    /// <c>replyToId</c> (the card's message) for a message sent by a card button (a message with <c>channelData.source</c>);
    /// <c>value.messagePayload.id</c> for a message action, and <c>replyToId</c> (the message the user acted on, for example a card or a message with feedback buttons) for any other invoke.
    /// An invoke that is not tied to a message (for example a compose-box extension or a <c>task/fetch</c> from a bot command) is inside a thread when <c>channelData.replyChain.id</c> is present.
    /// A <c>replyToId</c> or <c>messagePayload.id</c> that is not a plain number is never inside a thread, and neither are other inbound activities.
    /// A <c>source</c> without a non-empty string <c>name</c> and a <c>replyToId</c> that is null count as absent.
    /// </summary>
    private bool IsInsideThread(string replyChain)
    {
        bool IsOtherPlainId(string? id)
            => !string.IsNullOrEmpty(id) && id != "0" && id != replyChain && id.All(char.IsAsciiDigit);

        switch (Activity.Type)
        {
            case TeamsActivityTypes.Message:
                return string.IsNullOrEmpty(Activity.ChannelData?.Source?.Name) ? Activity.Id != replyChain : IsOtherPlainId(Activity.ReplyToId);
            case TeamsActivityTypes.MessageUpdate:
            case TeamsActivityTypes.MessageDelete:
            case TeamsActivityTypes.MessageReaction:
                return Activity.Id != replyChain;
            case TeamsActivityTypes.Invoke:
                string? messageId = Activity is InvokeActivity { Value: JsonObject value }
                    && value["messagePayload"] is JsonObject payload
                    && payload["id"] is JsonValue payloadId
                    && payloadId.TryGetValue(out string? payloadMessageId)
                    ? payloadMessageId
                    : Activity.ReplyToId;
                return messageId is null || IsOtherPlainId(messageId);
            default:
                return false;
        }
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
    public async Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync(CancellationToken cancellationToken = default)
    {
        OAuthFlowRegistry registry = TeamsBotApplication.OAuthRegistry
            ?? throw new InvalidOperationException("No OAuthFlow registered. Call AddOAuthFlow(connectionName) on the TeamsBotApplication first.");

        // Use any flow to call the GetTokenStatus API (returns all connections)
        OAuthFlow first = registry.GetAllFlows().First();
        IList<GetTokenStatusResult> statuses = await first.GetConnectionStatusAsync(this, cancellationToken).ConfigureAwait(false);

        // GetTokenStatus doesn't reliably reflect tokens obtained via silent SSO token exchange.
        // For any connection that the API reports as not connected, verify by calling GetTokenAsync
        // directly — if a token is actually present, correct the result.
        List<GetTokenStatusResult> results = statuses.ToList();
        foreach (OAuthFlow flow in registry.GetAllFlows())
        {
            GetTokenStatusResult? existing = results.FirstOrDefault(s =>
                string.Equals(s.ConnectionName, flow.ConnectionName, StringComparison.OrdinalIgnoreCase));

            if (existing?.HasToken == true)
                continue;

            string? token = await flow.GetTokenAsync(this, cancellationToken).ConfigureAwait(false);
            if (token is not null)
            {
                if (existing is not null)
                    existing.HasToken = true;
                else
                    results.Add(new GetTokenStatusResult { ConnectionName = flow.ConnectionName, HasToken = true });
            }
        }

        return results;
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
