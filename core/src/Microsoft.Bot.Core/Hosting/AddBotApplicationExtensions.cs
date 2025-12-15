// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Bot.Core.Hosting;

/// <summary>
/// Provides extension methods for registering bot application clients and related authentication services with the
/// dependency injection container.
/// </summary>
/// <remarks>This class is intended to be used during application startup to configure HTTP clients, token
/// acquisition, and agent identity services required for bot-to-bot communication. The configuration section specified
/// by the Azure Active Directory (AAD) configuration name is used to bind authentication options. Typically, these
/// methods are called in the application's service configuration pipeline.</remarks>
public static class AddBotApplicationExtensions
{

    /// <summary>
    /// Configures the application to handle bot messages at the specified route and returns the registered bot
    /// application instance.
    /// </summary>
    /// <remarks>This method adds authentication and authorization middleware to the request pipeline and maps
    /// a POST endpoint for bot messages. The endpoint requires authorization. Ensure that the bot application is
    /// registered in the service container before calling this method.</remarks>
    /// <typeparam name="TApp">The type of the bot application to use. Must inherit from BotApplication.</typeparam>
    /// <param name="builder">The application builder used to configure the request pipeline.</param>
    /// <param name="routePath">The route path at which to listen for incoming bot messages. Defaults to "api/messages".</param>
    /// <returns>The registered bot application instance of type TApp.</returns>
    /// <exception cref="ApplicationException">Thrown if the bot application of type TApp is not registered in the application's service container.</exception>
    public static TApp UseBotApplication<TApp>(
       this IApplicationBuilder builder,
       string routePath = "api/messages")
           where TApp : BotApplication
    {
        ArgumentNullException.ThrowIfNull(builder);
        WebApplication? webApp = builder as WebApplication;
        TApp app = builder.ApplicationServices.GetService<TApp>() ?? throw new InvalidOperationException("Application not registered");

        webApp?.MapPost(routePath, async (HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            CoreActivity resp = await app.ProcessAsync(httpContext, cancellationToken).ConfigureAwait(false);
            return resp.Id;
        });

        return app;
    }

    /// <summary>
    /// Adds a bot application to the service collection.
    /// </summary>
    /// <typeparam name="TApp"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services) where TApp : BotApplication
    {
        services.AddAuthorization();
        services.AddBotApplicationClients();
        services.AddSingleton<TApp>();
        return services;
    }

    /// <summary>
    /// Adds and configures Bot Framework application clients and related authentication services to the specified
    /// service collection.
    /// </summary>
    /// <remarks>This method registers HTTP clients, token acquisition, in-memory token caching, and agent
    /// identity services required for Bot Framework integration. It also configures authentication options using the
    /// specified Azure AD configuration section. The method should be called during application startup as part of
    /// service configuration.</remarks>
    /// <param name="services">The service collection to which the Bot Framework clients and authentication services will be added. Must not be
    /// null.</param>
    /// <param name="aadConfigSectionName">The name of the configuration section containing Azure Active Directory settings. Defaults to "AzureAd" if not
    /// specified.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddBotApplicationClients(this IServiceCollection services, string aadConfigSectionName = "AzureAd")
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        services
            .AddHttpClient()
            .AddTokenAcquisition(true)
            .AddInMemoryTokenCaches()
            .AddAgentIdentities();

        services.Configure<MicrosoftIdentityApplicationOptions>(aadConfigSectionName, configuration.GetSection(aadConfigSectionName));

        string agentScope = configuration[$"{aadConfigSectionName}:Scope"] ?? "https://api.botframework.com/.default";

        if (configuration.GetSection(aadConfigSectionName).Get<MicrosoftIdentityApplicationOptions>() is null)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("AddBotApplicationExtensions")
                .LogWarning("No configuration found for section {AadConfigSectionName}. BotAuthenticationHandler will not be configured.", aadConfigSectionName);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

            services.AddHttpClient<ConversationClient>(ConversationClient.ConversationHttpClientName);

        }
        else
        {
            services.AddHttpClient<ConversationClient>(ConversationClient.ConversationHttpClientName)
                .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    agentScope,
                    aadConfigSectionName));
        }

        return services;
    }
}
