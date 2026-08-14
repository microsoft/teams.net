// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.M365Extensions;

/// <summary>
/// Extension methods for registering the Teams SDK services using the Microsoft 365 Agents SDK's
/// authentication (<c>IConnections</c>) instead of a separate AzureAd config section.
/// </summary>
public static class TeamsSdkExtensions
{
    private const string HttpClientName = "TeamsBot";

    /// <summary>
    /// One-call setup for embedding the Teams SDK in a Microsoft 365 Agents SDK app. Registers:
    /// <list type="bullet">
    /// <item>the <see cref="TeamsBotApplication"/> subclass <typeparamref name="T"/> and its
    /// dependencies (<see cref="ApiClient"/>, <see cref="ConversationClient"/>,
    /// <see cref="UserTokenClient"/>) on a named <see cref="HttpClient"/> whose outbound
    /// requests are authenticated by <see cref="AgentSdkAuthHandler"/> (Agents SDK auth);</item>
    /// <item><see cref="TeamsSdkMiddleware"/> on the <c>CloudAdapter</c> pipeline so Teams turns
    /// are routed to the Teams SDK and everything else falls through to the Agents SDK.</item>
    /// <item>an optional bypass that can force specific Teams activities to fall through
    /// to the Agents SDK instead of routing into the Teams SDK.</item>
    /// </list>
    /// This is the only call needed — see the sample's <c>Program.cs</c>.
    /// </summary>
    /// <typeparam name="T">A <see cref="TeamsBotApplication"/> subclass.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="shouldBypassTeams">
    /// Optional extra predicate evaluated only for Teams-channel activities. Return
    /// <see langword="true"/> to bypass Teams routing and force the turn to
    /// fall through to the Agents SDK even when the Teams SDK has a matching route.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTeamsSdk<T>(
        this IServiceCollection services,
        Func<ITurnContext, bool>? shouldBypassTeams = null)
        where T : TeamsBotApplication
    {
        // TeamsBotApplication depends on IHttpContextAccessor for its own request-scoped behavior.
        services.AddHttpContextAccessor();

        // DelegatingHandler that acquires outbound Bot Framework tokens via the
        // Agents SDK connection manager and the ambient Agents SDK turn context.
        services.AddTransient<AgentSdkAuthHandler>();

        // Named HttpClient with the auth handler in its pipeline.
        services.AddHttpClient(HttpClientName)
            .AddHttpMessageHandler<AgentSdkAuthHandler>();

        services.AddSingleton<ConversationClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new ConversationClient(httpClient, sp.GetRequiredService<ILogger<ConversationClient>>());
        });

        services.AddSingleton<UserTokenClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new UserTokenClient(
                httpClient,
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<UserTokenClient>>());
        });

        services.AddSingleton<ApiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new ApiClient(
                httpClient,
                sp.GetRequiredService<ConversationClient>(),
                sp.GetRequiredService<UserTokenClient>(),
                sp.GetRequiredService<ILogger<ApiClient>>());
        });

        services.AddSingleton<T>(sp => ActivatorUtilities.CreateInstance<T>(sp));

        // Expose the bot under its base type so TeamsSdkMiddleware — which
        // depends on TeamsBotApplication rather than any concrete subclass — resolves
        // the same singleton instance.
        services.AddSingleton<TeamsBotApplication>(sp => sp.GetRequiredService<T>());

        // Install the routing middleware: register the bridge as an Agents SDK
        // IMiddleware, plus the IMiddleware[] the CloudAdapter consumes (DI doesn't
        // resolve array types). The array factory resolves lazily, so it still
        // includes any other IMiddleware the app registers. Fully qualified because
        // Microsoft.AspNetCore.Http also defines an IMiddleware.
        services.AddSingleton<Microsoft.Agents.Builder.IMiddleware>(sp =>
            new TeamsSdkMiddleware(
                sp.GetRequiredService<TeamsBotApplication>(),
                sp.GetRequiredService<ILogger<TeamsSdkMiddleware>>(),
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp,
                shouldBypassTeams));
        services.AddSingleton<Microsoft.Agents.Builder.IMiddleware[]>(
            sp => sp.GetServices<Microsoft.Agents.Builder.IMiddleware>().ToArray());

        return services;
    }
}
