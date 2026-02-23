// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace PABot
{
    internal static class InitCompatAdapter
    {
        private const string DefaultScope = "https://api.botframework.com/.default";

        public static IServiceCollection AddTeamsBotApplications(this IServiceCollection services)
        {
            // Register shared services (needed once for all adapters)
            services.AddHttpClient();
            services.AddTokenAcquisition(true);
            services.AddInMemoryTokenCaches();
            services.AddAgentIdentities();
            services.AddHttpContextAccessor();

            // Register each keyed adapter instance
            RegisterKeyedTeamsBotApplication(services, "AdapterOne");
            RegisterKeyedTeamsBotApplication(services, "AdapterTwo");

            return services;
        }

        private static void RegisterKeyedTeamsBotApplication(IServiceCollection services, string keyName)
        {
            // Get configuration for this key
            var configSection = services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetSection(keyName);

            // Configure authorization and authentication for this key
            // This sets up JWT bearer authentication and authorization policies
            services.AddAuthorization(null, keyName);

            // Configure MSAL options for this key
            services.Configure<MicrosoftIdentityApplicationOptions>(keyName, configSection);

            // Register named HttpClients with custom auth handlers
            services.AddHttpClient($"{keyName}_ConversationClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, keyName, DefaultScope));

            services.AddHttpClient($"{keyName}_UserTokenClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, keyName, DefaultScope));

            services.AddHttpClient($"{keyName}_TeamsApiClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, keyName, DefaultScope));

            // Register keyed ConversationClient
            services.AddKeyedSingleton<ConversationClient>(keyName, (sp, key) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{keyName}_ConversationClient");
                return new ConversationClient(httpClient, sp.GetRequiredService<ILogger<ConversationClient>>());
            });

            // Register keyed UserTokenClient
            services.AddKeyedSingleton<UserTokenClient>(keyName, (sp, key) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{keyName}_UserTokenClient");
                return new UserTokenClient(
                    httpClient,
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<ILogger<UserTokenClient>>());
            });

            // Register keyed TeamsApiClient
            services.AddKeyedSingleton<TeamsApiClient>(keyName, (sp, key) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{keyName}_TeamsApiClient");
                return new TeamsApiClient(httpClient, sp.GetRequiredService<ILogger<TeamsApiClient>>());
            });

            services.AddKeyedSingleton<BotApplicationOptions>(keyName, (sp, _) =>
            {
                return new BotApplicationOptions()
                {
                    AppId = configSection["ClientId"] ?? string.Empty
                };
            });

            // Register keyed TeamsBotApplication
            services.AddKeyedSingleton(keyName, (sp, key) =>
            {
                return new TeamsBotApplication(
                    sp.GetRequiredKeyedService<ConversationClient>(keyName),
                    sp.GetRequiredKeyedService<UserTokenClient>(keyName),
                    sp.GetRequiredKeyedService<TeamsApiClient>(keyName),
                    sp.GetRequiredKeyedService<BotApplicationOptions>(keyName),
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<TeamsBotApplication>>()
                );
            });
        }

        private static DelegatingHandler CreatePACustomAuthHandler(
            IServiceProvider sp,
            string keyName,
            string scope)
        {
            return new PACustomAuthHandler(
                keyName,
                sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                scope,
                sp.GetService<IOptions<ManagedIdentityOptions>>());
        }
    }
}
