// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Bot.Compat;

namespace PABot
{
    internal static class InitCompatAdapter
    {
        public static IServiceCollection AddCustomCompatAdapter(this IServiceCollection services)
        {
            // Use library methods for multi-instance compat adapter registration.
            // Each key corresponds to a configuration section in appsettings.json.
            // The library handles:
            // - Named HttpClients with isolated auth handlers
            // - Keyed services for ConversationClient, UserTokenClient, TeamsApiClient, TeamsBotApplication, CompatBotAdapter
            // - Authorization and MSAL configuration

            services.AddCompatAdapter("RidoABSOne", options =>
            {
                // Use custom auth handler for PA-specific token acquisition
                options.AuthHandlerFactory = CreatePACustomAuthHandler;
            });

            services.AddCompatAdapter("RidoABSTwo", options =>
            {
                // Use custom auth handler for PA-specific token acquisition
                options.AuthHandlerFactory = CreatePACustomAuthHandler;
            });

            return services;
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
                scope);
        }
    }
}
