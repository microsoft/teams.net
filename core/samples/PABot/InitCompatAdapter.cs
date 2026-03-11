// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace PABot
{
    internal static class InitCompatAdapter
    {
        private const string DefaultScope = "https://api.botframework.com/.default";

        /// <summary>
        /// Configuration values for a bot adapter.
        /// </summary>
        private sealed record AdapterConfig
        {
            public required string KeyName { get; init; }
            public required IConfigurationSection ConfigSection { get; init; }
            public required string ClientId { get; init; }
            public required string TenantId { get; init; }
            public string? AgenticClientId { get; init; }
            public string? AgenticTenantId { get; init; }
            public required string BotScope { get; init; }
            public string? AgenticScope { get; init; }
        }

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
            // Read configuration for this adapter
            AdapterConfig config = ReadAdapterConfig(services, keyName);

            // Set up token validation (authentication schemes and authorization policy)
            ConfigureTokenValidation(services, config);

            // Register MSAL options for token acquisition
            ConfigureMsalOptions(services, config);

            // Register the routed token acquisition service
            RegisterRoutedTokenService(services, config);

            // Register HTTP clients with auth handlers
            RegisterHttpClients(services, config);

            // Register Bot Framework clients
            RegisterBotClients(services, config);
        }

        private static AdapterConfig ReadAdapterConfig(IServiceCollection services, string keyName)
        {
            IConfigurationSection configSection = services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection(keyName);

            return new AdapterConfig
            {
                KeyName = keyName,
                ConfigSection = configSection,
                ClientId = configSection["ClientId"] ?? throw new InvalidOperationException($"ClientId not found in configuration section '{keyName}'"),
                TenantId = configSection["TenantId"] ?? string.Empty,
                AgenticClientId = configSection["AgenticClientId"],
                AgenticTenantId = configSection["AgenticTenantId"],
                BotScope = configSection["Scope"] ?? DefaultScope,
                AgenticScope = configSection["AgenticScope"]
            };
        }

        private static void ConfigureTokenValidation(IServiceCollection services, AdapterConfig config)
        {
            // This demonstrates an edge case scenario where two token validation schemes are registered
            // with different audiences (client IDs). The authorization policy will succeed if EITHER
            // scheme validates successfully - only one token needs to pass, not both.
            // Use case: When a bot is also registered as an agentic application and needs to accept
            // tokens from both the bot registration AND the agentic application registration.


            AuthenticationBuilder authBuilder = services.AddAuthentication();

            // Configure authentication schemes for bot and optional agentic credentials
            string botScheme = $"{config.KeyName}_Bot";
            authBuilder.AddBotAuthentication(config.ClientId, config.TenantId, botScheme);

            string? agenticScheme = null;
            if (!string.IsNullOrEmpty(config.AgenticClientId))
            {
                agenticScheme = $"{config.KeyName}_Agentic";
                authBuilder.AddBotAuthentication(config.AgenticClientId, config.AgenticTenantId ?? string.Empty, agenticScheme);
            }

            // Create policy scheme that routes based on token audience
            string policyScheme = config.KeyName;
            authBuilder.AddPolicyScheme(policyScheme, policyScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                    SelectAuthenticationScheme(context, config, botScheme, agenticScheme);
            });

            // Create authorization policy
            services.AddAuthorizationBuilder()
                .AddPolicy(config.KeyName, policy =>
                {
                    policy.AuthenticationSchemes.Add(policyScheme);
                    policy.RequireAuthenticatedUser();
                });
        }

        private static string SelectAuthenticationScheme(
            HttpContext context,
            AdapterConfig config,
            string botScheme,
            string? agenticScheme)
        {
            string? authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return botScheme;
            }

            try
            {
                string token = authHeader["Bearer ".Length..].Trim();
                JsonWebToken jwt = new(token);
                string? audience = jwt.GetClaim("aud")?.Value;

                if (audience == config.ClientId || audience == $"api://{config.ClientId}")
                {
                    return botScheme;
                }
                else if (agenticScheme is not null &&
                        (audience == config.AgenticClientId || audience == $"api://{config.AgenticClientId}"))
                {
                    return agenticScheme;
                }
            }
            catch
            {
                // If token parsing fails, default to bot scheme
            }

            return botScheme;
        }

        private static void ConfigureMsalOptions(IServiceCollection services, AdapterConfig config)
        {

            // Configure MSAL options for bot credentials
            services.Configure<MicrosoftIdentityApplicationOptions>(config.KeyName, config.ConfigSection);

            // Configure MSAL options for agentic credentials if provided
            if (!string.IsNullOrEmpty(config.AgenticClientId))
            {
                string agenticKeyName = $"{config.KeyName}_Agentic";
                services.Configure<MicrosoftIdentityApplicationOptions>(agenticKeyName, options =>
                {
                    options.Instance = config.ConfigSection["Instance"] ?? "https://login.microsoftonline.com/";
                    options.TenantId = config.AgenticTenantId ?? string.Empty;
                    options.ClientId = config.AgenticClientId;
                    options.ClientCredentials = new List<CredentialDescription>();
                    config.ConfigSection.Bind("AgenticClientCredentials", options.ClientCredentials);
                });
            }
        }

        private static void RegisterRoutedTokenService(IServiceCollection services, AdapterConfig config)
        {
            services.AddKeyedSingleton<IRoutedTokenAcquisitionService>(config.KeyName, (sp, key) =>
            {
                return new RoutedTokenAcquisitionService(
                    config.KeyName,
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<RoutedTokenAcquisitionService>>());
            });
        }

        private static void RegisterHttpClients(IServiceCollection services, AdapterConfig config)
        {
            services.AddHttpClient($"{config.KeyName}_ConversationClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, config));

            services.AddHttpClient($"{config.KeyName}_UserTokenClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, config));

            services.AddHttpClient($"{config.KeyName}_TeamsApiClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, config));
        }

        private static void RegisterBotClients(IServiceCollection services, AdapterConfig config)
        {
            // Register keyed ConversationClient
            services.AddKeyedSingleton<ConversationClient>(config.KeyName, (sp, key) =>
            {
                HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{config.KeyName}_ConversationClient");
                return new ConversationClient(httpClient, sp.GetRequiredService<ILogger<ConversationClient>>());
            });

            // Register keyed UserTokenClient
            services.AddKeyedSingleton<UserTokenClient>(config.KeyName, (sp, key) =>
            {
                HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{config.KeyName}_UserTokenClient");
                return new UserTokenClient(
                    httpClient,
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<ILogger<UserTokenClient>>());
            });

            // Register keyed TeamsApiClient
            services.AddKeyedSingleton<TeamsApiClient>(config.KeyName, (sp, key) =>
            {
                HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient($"{config.KeyName}_TeamsApiClient");
                return new TeamsApiClient(httpClient, sp.GetRequiredService<ILogger<TeamsApiClient>>());
            });

            // Register keyed TeamsBotApplication
            services.AddKeyedSingleton(config.KeyName, (sp, key) =>
            {
                return new TeamsBotApplication(
                    sp.GetRequiredKeyedService<ConversationClient>(config.KeyName),
                    sp.GetRequiredKeyedService<UserTokenClient>(config.KeyName),
                    sp.GetRequiredKeyedService<TeamsApiClient>(config.KeyName),
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<TeamsBotApplication>>()
                );
            });
        }

        private static DelegatingHandler CreatePACustomAuthHandler(IServiceProvider sp, AdapterConfig config)
        {
            return new PACustomAuthHandler(
                sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                sp.GetRequiredKeyedService<IRoutedTokenAcquisitionService>(config.KeyName),
                sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                config.BotScope,
                config.AgenticScope,
                sp.GetService<IOptions<ManagedIdentityOptions>>());
        }
    }
}
