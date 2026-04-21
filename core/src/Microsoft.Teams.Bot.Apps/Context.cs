// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Api.Clients;
using Microsoft.Teams.Bot.Apps.Auth;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps;


/// <summary>
/// Context for a bot turn.
/// </summary>
/// <param name="botApplication"></param>
/// <param name="activity"></param>
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

    private ApiClient? _api;

    /// <summary>
    /// Gets the <see cref="ApiClient"/> scoped to the current activity's service URL.
    /// </summary>
    public ApiClient Api => _api ??= TeamsBotApplication.Api.ForServiceUrl(
        Activity.ServiceUrl ?? throw new InvalidOperationException("Activity.ServiceUrl is required to use the Api client."));

    /// <summary>
    /// Sends a message activity as a reply.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendActivityAsync(string text, CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            new TeamsActivityBuilder()
                .WithConversationReference(Activity)
                .WithText(text)
                .Build(), cancellationToken);

    /// <summary>
    /// Sends Activity
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendActivityAsync(TeamsActivity activity, CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            new TeamsActivityBuilder(activity)
                .WithConversationReference(Activity)
                .Build(), cancellationToken);


    /// <summary>
    /// Sends a typing activity to the conversation asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SendActivityResponse?> SendTypingActivityAsync(CancellationToken cancellationToken = default)
        => TeamsBotApplication.SendActivityAsync(
            new TeamsActivityBuilder()
                .WithType(TeamsActivityType.Typing)
                .WithConversationReference(Activity)
                .Build(), cancellationToken);

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
            ?? throw new InvalidOperationException("No OAuthFlow registered. Call AddOAuthFlow() on the TeamsBotApplication first.");

        // Use any flow -- GetConnectionStatusAsync returns all connections regardless
        OAuthFlow flow = registry.ResolveSingle()
            ?? registry.GetAllFlows().First();

        return flow.GetConnectionStatusAsync(this, cancellationToken);
    }

    private OAuthFlow ResolveOAuthFlow(string? connectionName)
    {
        OAuthFlowRegistry registry = TeamsBotApplication.OAuthRegistry
            ?? throw new InvalidOperationException("No OAuthFlow registered. Call AddOAuthFlow() on the TeamsBotApplication first.");

        if (connectionName is not null)
        {
            return registry.Resolve(connectionName)
                ?? throw new InvalidOperationException($"No OAuthFlow registered for connection '{connectionName}'.");
        }

        return registry.ResolveSingle()
            ?? throw new InvalidOperationException("Multiple OAuthFlow instances registered. Specify a connection name.");
    }
}
