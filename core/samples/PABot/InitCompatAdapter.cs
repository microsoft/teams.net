// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;

namespace PABot
{
    internal static class InitTeamsBotAdapter
    {
        private const string DefaultScope = "https://api.botframework.com/.default";
        private const string AdapterKeyName = "BotAdapter";
        private const string DefaultInstance = "https://login.microsoftonline.com/";

        /// <summary>
        /// Configuration values for MSAL identity (bot or agent).
        /// </summary>
        private sealed record MsalIdentityConfig
        {
            public required IConfigurationSection ConfigSection { get; init; }
            public required string ClientId { get; init; }
            public required string TenantId { get; init; }
            public required string Scope { get; init; }
            public required string Instance { get; init; }

            /// <summary>The ASP.NET authentication scheme name used to validate inbound tokens for this identity.</summary>
            public required string Scheme { get; init; }
        }

        /// <summary>
        /// Configuration values for a bot adapter. Supports multiple bot registrations on one endpoint.
        /// </summary>
        private sealed record AdapterConfig
        {
            public required IReadOnlyList<MsalIdentityConfig> BotIdentities { get; init; }
            public MsalIdentityConfig? AgentIdentity { get; init; }

            /// <summary>The first configured bot, used for default (non app-id-specific) operations.</summary>
            public MsalIdentityConfig? PrimaryBot => BotIdentities.Count > 0 ? BotIdentities[0] : null;
        }

        public static IServiceCollection AddTeamsBotApplications(this IServiceCollection services)
        {
            // Register shared services (needed once for all adapters)
            services.AddHttpClient();
            services.AddTokenAcquisition(true);
            services.AddInMemoryTokenCaches();
            services.AddAgentIdentities();
            services.AddHttpContextAccessor();

            // Register adapter using MsalBot/MsalBots/MsalAgent configuration
            RegisterTeamsBotApplication(services);

            return services;
        }

        private static void RegisterTeamsBotApplication(IServiceCollection services)
        {
            // Read configuration for this adapter
            AdapterConfig config = ReadAdapterConfig(services);

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

        private static AdapterConfig ReadAdapterConfig(IServiceCollection services)
        {
            IConfiguration configuration = services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>();

            // Collect bot identities from the single "MsalBot" section (backward compatible) and the
            // "MsalBots" array (multiple bot registrations on one endpoint).
            List<MsalIdentityConfig> botIdentities = [];

            void AddBot(IConfigurationSection section)
            {
                string? clientId = section["ClientId"];
                if (!string.IsNullOrEmpty(clientId) &&
                    !botIdentities.Any(b => string.Equals(b.ClientId, clientId, StringComparison.OrdinalIgnoreCase)))
                {
                    botIdentities.Add(BuildIdentity(section, clientId, $"MsalBot-{clientId}"));
                }
            }

            AddBot(configuration.GetSection("MsalBot"));
            foreach (IConfigurationSection botSection in configuration.GetSection("MsalBots").GetChildren())
            {
                AddBot(botSection);
            }

            // Read agent identity configuration if provided
            MsalIdentityConfig? agentIdentity = null;
            IConfigurationSection msalAgentSection = configuration.GetSection("MsalAgent");
            string? agentClientId = msalAgentSection["ClientId"];
            if (!string.IsNullOrEmpty(agentClientId))
            {
                agentIdentity = BuildIdentity(
                    msalAgentSection,
                    agentClientId,
                    "MsalAgent");
            }

            // At least one identity must be configured
            if (botIdentities.Count == 0 && agentIdentity is null)
            {
                throw new InvalidOperationException("At least one identity (MsalBot, MsalBots, or MsalAgent) must be configured with a ClientId");
            }

            return new AdapterConfig
            {
                BotIdentities = botIdentities,
                AgentIdentity = agentIdentity
            };
        }

        private static MsalIdentityConfig BuildIdentity(IConfigurationSection section, string clientId, string scheme) =>
            new()
            {
                ConfigSection = section,
                ClientId = clientId,
                TenantId = section["TenantId"] ?? string.Empty,
                Scope = section["Scope"] ?? DefaultScope,
                Instance = section["Instance"] ?? DefaultInstance,
                Scheme = scheme
            };

        private static void ConfigureTokenValidation(IServiceCollection services, AdapterConfig config)
        {
            // Register a token validation scheme per configured identity, each validating its own audience
            // (client id). The authorization policy succeeds if ANY scheme validates — so inbound tokens
            // from any configured bot registration (or the agentic app) are accepted.

            AuthenticationBuilder authBuilder = services.AddAuthentication();

            // One JWT bearer scheme per bot registration.
            foreach (MsalIdentityConfig bot in config.BotIdentities)
            {
                authBuilder.AddBotAuthentication(bot.ClientId, bot.TenantId, bot.Scheme);
            }

            // Agent identity scheme if present.
            if (config.AgentIdentity is not null)
            {
                authBuilder.AddBotAuthentication(config.AgentIdentity.ClientId, config.AgentIdentity.TenantId, config.AgentIdentity.Scheme);
            }

            // Create policy scheme that routes to the matching scheme based on token audience.
            authBuilder.AddPolicyScheme(AdapterKeyName, AdapterKeyName, options =>
            {
                options.ForwardDefaultSelector = context => SelectAuthenticationScheme(context, config);
            });

            // Create authorization policy
            services.AddAuthorizationBuilder()
                .AddPolicy(AdapterKeyName, policy =>
                {
                    policy.AuthenticationSchemes.Add(AdapterKeyName);
                    policy.RequireAuthenticatedUser();
                });
        }

        private static string SelectAuthenticationScheme(HttpContext context, AdapterConfig config)
        {
            // Default to the first available scheme.
            string defaultScheme = config.PrimaryBot?.Scheme
                ?? config.AgentIdentity?.Scheme
                ?? throw new InvalidOperationException("No authentication scheme configured");

            string? authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return defaultScheme;
            }

            try
            {
                string token = authHeader["Bearer ".Length..].Trim();
                JsonWebToken jwt = new(token);
                string? audience = jwt.GetClaim("aud")?.Value;

                // Match the token audience against any configured bot registration.
                MsalIdentityConfig? matchingBot = config.BotIdentities
                    .FirstOrDefault(b => audience == b.ClientId || audience == $"api://{b.ClientId}");
                if (matchingBot is not null)
                {
                    return matchingBot.Scheme;
                }

                // Otherwise check the agent identity.
                if (config.AgentIdentity is not null &&
                    (audience == config.AgentIdentity.ClientId || audience == $"api://{config.AgentIdentity.ClientId}"))
                {
                    return config.AgentIdentity.Scheme;
                }
            }
            catch
            {
                // If token parsing fails, default to first available scheme
            }

            return defaultScheme;
        }

        private static void ConfigureMsalOptions(IServiceCollection services, AdapterConfig config)
        {
            // Default bot credentials ("MsalBot") map to the primary bot, used by the fallback acquisition path.
            if (config.PrimaryBot is not null)
            {
                services.Configure<MicrosoftIdentityApplicationOptions>("MsalBot", config.PrimaryBot.ConfigSection);
            }

            // Register each bot's credentials under a named options keyed by its app (client) id, so
            // PACustomAuthHandler can mint a token AS that specific bot based on the incoming activity.
            foreach (MsalIdentityConfig bot in config.BotIdentities)
            {
                services.Configure<MicrosoftIdentityApplicationOptions>(bot.ClientId, bot.ConfigSection);
            }

            // Configure MSAL options for agent identity if present.
            if (config.AgentIdentity is not null)
            {
                services.Configure<MicrosoftIdentityApplicationOptions>("MsalAgent", config.AgentIdentity.ConfigSection);
            }
        }

        private static void RegisterRoutedTokenService(IServiceCollection services, AdapterConfig config)
        {
            // Every configured bot app id is trusted to be minted as, based on the incoming activity.
            string[] trustedBotAppIds = [.. config.BotIdentities.Select(b => b.ClientId)];

            services.AddSingleton<IRoutedTokenAcquisitionService>(sp =>
            {
                return new RoutedTokenAcquisitionService(
                    config.BotIdentities.Count > 0,
                    config.AgentIdentity is not null,
                    trustedBotAppIds,
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<RoutedTokenAcquisitionService>>());
            });
        }

        private static void RegisterHttpClients(IServiceCollection services, AdapterConfig config)
        {
            services.AddHttpClient("ConversationClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, config));

            services.AddHttpClient("UserTokenClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, config));

            services.AddHttpClient("TeamsApiClient")
                .AddHttpMessageHandler(sp => CreatePACustomAuthHandler(sp, config));
        }

        private static void RegisterBotClients(IServiceCollection services, AdapterConfig config)
        {
            // Register ConversationClient
            services.AddSingleton<ConversationClient>(sp =>
            {
                HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("ConversationClient");
                return new ConversationClient(httpClient, sp.GetRequiredService<ILogger<ConversationClient>>());
            });

            // Register UserTokenClient
            services.AddSingleton<UserTokenClient>(sp =>
            {
                HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("UserTokenClient");
                return new UserTokenClient(
                    httpClient,
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<ILogger<UserTokenClient>>());
            });


            // Register TeamsBotApplication
            services.AddSingleton<BotApplication>(sp =>
            {
                return new BotApplication(
                    sp.GetRequiredService<ConversationClient>(),
                    sp.GetRequiredService<UserTokenClient>(),
                    sp.GetRequiredService<ILogger<BotApplication>>()
                );
            });
        }

        private static DelegatingHandler CreatePACustomAuthHandler(IServiceProvider sp, AdapterConfig config)
        {
            // Use bot scope if available, otherwise use agent scope
            string? botScope = config.PrimaryBot?.Scope;
            string? agentScope = config.AgentIdentity?.Scope;

            return new PACustomAuthHandler(
                sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                sp.GetRequiredService<IRoutedTokenAcquisitionService>(),
                sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                botScope ?? agentScope ?? DefaultScope,
                agentScope,
                sp.GetService<IOptions<ManagedIdentityOptions>>());
        }
    }
}
