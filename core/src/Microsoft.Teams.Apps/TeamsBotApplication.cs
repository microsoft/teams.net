// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Auth;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;

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

    /// <param name="conversationClient">The conversation client for sending and managing activities.</param>
    /// <param name="userTokenClient">The user token client for OAuth operations.</param>
    /// <param name="teamsApiClient">The Teams API client for Teams-specific operations.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for reading invoke responses.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Options containing the application (client) ID, used for logging and diagnostics. Defaults to an empty instance if not provided.</param>
    /// <param name="teamsOptions">Teams-specific options including OAuth flow configuration. Defaults to an empty instance if not provided.</param>
    public TeamsBotApplication(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        ApiClient teamsApiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TeamsBotApplication> logger,
        BotApplicationOptions? options = null,
        TeamsBotApplicationOptions? teamsOptions = null)
        : base(conversationClient, userTokenClient, logger, options)
    {
        _teamsApiClient = teamsApiClient;
        Api = teamsApiClient;
        Logger = logger;
        Router = new Router(logger);

        // Auto-register OAuth flows from DI options
        if (teamsOptions is not null)
        {
            foreach (var descriptor in teamsOptions.OAuthFlows)
            {
                this.AddOAuthFlow(descriptor.Options);
            }
        }
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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Send(string conversationId, string text, Uri? serviceUrl = null, CancellationToken cancellationToken = default)
    {
        Uri resolvedUrl = serviceUrl ?? _lastServiceUrl
            ?? throw new InvalidOperationException("No service URL available. Either pass a serviceUrl parameter or ensure the bot has received at least one activity.");

        TeamsActivity activity = new TeamsActivityBuilder()
            .WithType(TeamsActivityType.Message)
            .WithServiceUrl(resolvedUrl)
            .WithChannelId("msteams")
            .WithConversation(new Core.Schema.Conversation { Id = conversationId })
            .WithText(text)
            .Build();

        return SendActivityAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a text message proactively as a threaded reply.
    /// Constructs a threaded conversation ID from the conversation ID and message ID.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="messageId">The thread root message ID.</param>
    /// <param name="text">The text to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the send operation.</returns>
    public Task<SendActivityResponse?> Reply(string conversationId, string messageId, string text, CancellationToken cancellationToken = default)
    {
        string threadedConversationId = $"{conversationId};messageid={messageId}";
        return Send(threadedConversationId, text, cancellationToken: cancellationToken);
    }
}
