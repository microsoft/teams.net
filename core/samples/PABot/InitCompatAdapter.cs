// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Compat;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace PABot
{
    internal static class InitCompatAdapter
    {
        private const string DefaultScope = "https://api.botframework.com/.default";

        public static IServiceCollection AddCustomCompatAdapter(this IServiceCollection services)
        {
            ILogger logger = GetOrCreateLogger(services);
            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            // Register shared services (only once)
            services
                .AddHttpClient()
                .AddTokenAcquisition(true)
                .AddInMemoryTokenCaches()
                .AddAgentIdentities();

            // Register each compat adapter instance by configuration section name
            services.AddCompatAdapterInstance(logger, configuration, "RidoABSOne");
            services.AddCompatAdapterInstance(logger, configuration, "RidoABSTwo");

            return services;
        }

        /// <summary>
        /// Registers all services needed for a single CompatAdapter instance.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="logger">Logger for authorization setup.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="configSectionName">The configuration section name (used as the keyed service key).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCompatAdapterInstance(
            this IServiceCollection services,
            ILogger logger,
            IConfiguration configuration,
            string configSectionName)
        {
            var configSection = configuration.GetSection(configSectionName);
            var scope = configSection.GetValue<string>("Scope") ?? DefaultScope;

            // Configure authorization and MSAL options
            services.AddAuthorization(logger, configSectionName);
            services.Configure<MicrosoftIdentityApplicationOptions>(configSectionName, configSection!);

            // Register named HttpClients with the auth handler
            RegisterHttpClients(services, configSectionName, scope);

            // Register keyed service instances
            RegisterKeyedServices(services, configSectionName);

            return services;
        }

        private static void RegisterHttpClients(IServiceCollection services, string keyName, string scope)
        {
            // ConversationClient
            services.AddHttpClient($"{keyName}_ConversationClient")
                .AddHttpMessageHandler(sp => CreateAuthHandler(sp, keyName, scope));

            // UserTokenClient
            services.AddHttpClient($"{keyName}_UserTokenClient")
                .AddHttpMessageHandler(sp => CreateAuthHandler(sp, keyName, scope));

            // TeamsApiClient
            services.AddHttpClient($"{keyName}_TeamsApiClient")
                .AddHttpMessageHandler(sp => CreateAuthHandler(sp, keyName, scope));
        }

        private static PACustomAuthHandler CreateAuthHandler(IServiceProvider sp, string keyName, string scope)
        {
            return new PACustomAuthHandler(
                keyName,
                sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                scope);
        }

        private static void RegisterKeyedServices(IServiceCollection services, string keyName)
        {
            // ConversationClient
            services.AddKeyedSingleton<ConversationClient>(keyName, (sp, key) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{keyName}_ConversationClient");
                return new ConversationClient(httpClient, sp.GetRequiredService<ILogger<ConversationClient>>());
            });

            // UserTokenClient
            services.AddKeyedSingleton<UserTokenClient>(keyName, (sp, key) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{keyName}_UserTokenClient");
                return new UserTokenClient(
                    httpClient,
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<ILogger<UserTokenClient>>());
            });

            // TeamsApiClient
            services.AddKeyedSingleton<TeamsApiClient>(keyName, (sp, key) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{keyName}_TeamsApiClient");
                return new TeamsApiClient(httpClient, sp.GetRequiredService<ILogger<TeamsApiClient>>());
            });

            // TeamsBotApplication
            services.AddKeyedSingleton<TeamsBotApplication>(keyName, (sp, key) =>
            {
                return new TeamsBotApplication(
                    sp.GetRequiredKeyedService<ConversationClient>(keyName),
                    sp.GetRequiredKeyedService<UserTokenClient>(keyName),
                    sp.GetRequiredKeyedService<TeamsApiClient>(keyName),
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<BotApplication>>(),
                    keyName);
            });

            // CompatBotAdapter
            services.AddKeyedSingleton<CompatBotAdapter>(keyName, (sp, key) =>
            {
                return new CompatBotAdapter(
                    sp,
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<CompatBotAdapter>>(),
                    key.ToString()!);
            });
        }

        private static ILogger GetOrCreateLogger(IServiceCollection services)
        {
            var loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
            var loggerFactory = loggerFactoryDescriptor?.ImplementationInstance as ILoggerFactory;

            return loggerFactory?.CreateLogger<BotApplication>()
                ?? (ILogger)Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }
    }
}
