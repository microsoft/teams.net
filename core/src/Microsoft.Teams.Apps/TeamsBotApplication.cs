// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Teams-specific bot application.
/// </summary>
public class TeamsBotApplication : BotApplication
{
    private readonly Api.Clients.ApiClient _teamsApiClient;
    private Uri? _lastServiceUrl;

    /// <summary>
    /// Gets the logger instance for this application, used by <see cref="Context{TActivity}.Log"/>.
    /// </summary>
    internal ILogger Logger { get; }

    /// <summary>
    /// Gets the router for dispatching Teams activities to registered routes.
    /// </summary>
    internal Router Router { get; }

    /// <summary>
    /// Gets the registry of OAuthFlow instances. Set by AddOAuthFlow.
    /// </summary>
    internal OAuthFlowRegistry? OAuthRegistry { get; set; }

    /// <summary>
    /// Gets a registered <see cref="OAuthFlow"/> by connection name.
    /// Use this to attach callbacks (<see cref="OAuthFlow.OnSignInComplete"/>, <see cref="OAuthFlow.OnSignInFailure"/>)
    /// to flows that were configured via <see cref="TeamsBotApplicationOptions.AddOAuthFlow"/>.
    /// </summary>
    /// <param name="connectionName">The OAuth connection name.</param>
    /// <returns>The <see cref="OAuthFlow"/> instance.</returns>
    /// <exception cref="InvalidOperationException">No flow is registered for the given connection name.</exception>
    public OAuthFlow GetOAuthFlow(string connectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        OAuthFlow? flow = OAuthRegistry?.Resolve(connectionName);
        if (flow is null)
        {
            IEnumerable<string> registered = OAuthRegistry?.GetRegisteredConnectionNames() ?? [];
            throw new InvalidOperationException(
                $"No OAuthFlow registered for connection '{connectionName}'. " +
                $"Registered connections: [{string.Join(", ", registered)}].");
        }

        return flow;
    }

    /// <summary>
    /// Gets the client used to interact with the Teams API service.
    /// </summary>
    public ApiClient TeamsApiClient => _teamsApiClient;
    /// <summary>
    /// Gets the hierarchical API facade for Teams operations.
    /// </summary>
    /// <remarks>
    /// This property provides a structured API for accessing Teams operations through a hierarchy:
    /// <list type="bullet">
    /// <item><c>Api.Conversations.Activities</c> - Activity operations (send, update, delete)</item>
    /// <item><c>Api.Conversations.Members</c> - Member operations (get, delete)</item>
    /// <item><c>Api.Users.Token</c> - User token operations (OAuth SSO, sign-in resources)</item>
    /// <item><c>Api.Teams</c> - Team operations (get details, channels)</item>
    /// <item><c>Api.Meetings</c> - Meeting operations (get info, participant, notifications)</item>
    /// <item><c>Api.Batch</c> - Batch messaging operations</item>
    /// </list>
    /// </remarks>
    public ApiClient Api { get; }

    /// <summary>
    /// Initializes a new instance using a bundled <see cref="TeamsBotApplicationDependencies"/>.
    /// Designed for subclassing: derived types declare a single-parameter constructor that
    /// forwards to this overload.
    /// </summary>
    /// <param name="dependencies">The bundled dependencies. Cannot be null.</param>
    /// <example>
    /// <code>
    /// public class MyBot : TeamsBotApplication
    /// {
    ///     public MyBot(TeamsBotApplicationDependencies deps) : base(deps)
    ///     {
    ///         this.OnMessage(async (ctx, ct) =>
    ///             await ctx.SendActivityAsync("Hello!", ct));
    ///     }
    /// }
    /// </code>
    /// </example>
    public TeamsBotApplication(TeamsBotApplicationDependencies dependencies)
        : base(
            (dependencies ?? throw new ArgumentNullException(nameof(dependencies))).ConversationClient,
            dependencies.UserTokenClient,
            dependencies.Logger,
            dependencies.Options)
    {
        _teamsApiClient = dependencies.TeamsApiClient;
        Api = dependencies.TeamsApiClient;
        Logger = dependencies.Logger;
        Router = new Router(dependencies.Logger);

        if (dependencies.TeamsOptions is not null)
        {
            foreach (TeamsBotApplicationOptions.OAuthFlowDescriptor descriptor in dependencies.TeamsOptions.OAuthFlows)
            {
                this.AddOAuthFlow(descriptor.Options);
            }
        }

        IHttpContextAccessor httpContextAccessor = dependencies.HttpContextAccessor;
        ILogger<TeamsBotApplication> logger = dependencies.Logger;
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogDebug("OnActivity invoked for activity: Id={Id}", activity.Id);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);

            // Cache the service URL for proactive messaging
            if (teamsActivity.ServiceUrl is not null)
            {
                _lastServiceUrl = teamsActivity.ServiceUrl;
            }

            Context<TeamsActivity> defaultContext = new(this, teamsActivity);

            if (teamsActivity.Type != TeamsActivityType.Invoke)
            {
                await Router.DispatchAsync(defaultContext, cancellationToken).ConfigureAwait(false);
            }
            else // invokes
            {
                InvokeResponse invokeResponse = await Router.DispatchWithReturnAsync(defaultContext, cancellationToken).ConfigureAwait(false);
                HttpContext? httpContext = httpContextAccessor.HttpContext;
                if (httpContext is not null && invokeResponse is not null)
                {
                    httpContext.Response.StatusCode = invokeResponse.Status;
                    logger.LogDebug("Sending invoke response with status {Status}", invokeResponse.Status);
                    logger.LogTrace("Sending invoke response with status {Status} and Body {Body}", invokeResponse.Status, invokeResponse.Body);
                    if (invokeResponse.Body is not null)
                        await httpContext.Response.WriteAsJsonAsync(invokeResponse.Body, cancellationToken).ConfigureAwait(false);
                }
            }
        };
    }

    // ==================== Proactive Messaging ====================

    /// <summary>
    /// Sends a text message proactively to a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to send to. For channel threads, include <c>;messageid=</c>.</param>
    /// <param name="text">The text to send.</param>
    /// <param name="serviceUrl">The service URL. If null, uses the last-seen service URL from an incoming activity.</param>
    /// <param name="agenticIdentity">The agentic identity for user-delegated token acquisition. Extract from the inbound activity's <c>Recipient</c> via <see cref="ConversationAccount.GetAgenticIdentity"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> SendAsync(string conversationId, string text, Uri? serviceUrl = null, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        Uri resolvedUrl = serviceUrl ?? _lastServiceUrl
            ?? throw new InvalidOperationException("No service URL available. Either pass a serviceUrl parameter or ensure the bot has received at least one activity.");

        TeamsActivityBuilder builder = new TeamsActivityBuilder()
            .WithType(TeamsActivityType.Message)
            .WithServiceUrl(resolvedUrl)
            .WithChannelId("msteams")
            .WithConversation(new Conversation { Id = conversationId })
            .WithText(text);

        if (agenticIdentity is not null)
        {
            builder.WithFrom(new ConversationAccount
            {
                AgenticAppId = agenticIdentity.AgenticAppId,
                AgenticUserId = agenticIdentity.AgenticUserId,
                AgenticAppBlueprintId = agenticIdentity.AgenticAppBlueprintId,
            });
        }

        TeamsActivity activity = builder.Build();

        return SendActivityAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a text message proactively as a threaded reply.
    /// Constructs a threaded conversation ID from the conversation ID and message ID.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="messageId">The thread root message ID.</param>
    /// <param name="text">The text to send.</param>
    /// <param name="agenticIdentity">The agentic identity for user-delegated token acquisition. Extract from the inbound activity's <c>Recipient</c> via <see cref="ConversationAccount.GetAgenticIdentity"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> ReplyAsync(string conversationId, string messageId, string text, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        string threadedConversationId = ConversationExtensions.ToThreadedConversationId(conversationId, messageId);
        return SendAsync(threadedConversationId, text, agenticIdentity: agenticIdentity, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="SendAsync(string, string, Uri?, AgenticIdentity?, CancellationToken)"/>
    public Task<SendActivityResponse?> Send(string conversationId, string text, Uri? serviceUrl = null, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
        => SendAsync(conversationId, text, serviceUrl, agenticIdentity, cancellationToken);

    /// <inheritdoc cref="ReplyAsync(string, string, string, AgenticIdentity?, CancellationToken)"/>
    public Task<SendActivityResponse?> Reply(string conversationId, string messageId, string text, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
        => ReplyAsync(conversationId, messageId, text, agenticIdentity, cancellationToken);
}
