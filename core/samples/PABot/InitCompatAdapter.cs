// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace PABot
{
    internal static class InitCompatAdapter
    {
        private const string DefaultScope = "https://api.botframework.com/.default";
        private const string AdapterKeyName = "BotAdapter";

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
        }

        /// <summary>
        /// Configuration values for a bot adapter.
        /// </summary>
        private sealed record AdapterConfig
        {
            public MsalIdentityConfig? BotIdentity { get; init; }
            public MsalIdentityConfig? AgentIdentity { get; init; }
        }

        public static IServiceCollection AddTeamsBotApplications(this IServiceCollection services)
        {
            // Register shared services (needed once for all adapters)
            services.AddHttpClient();
            services.AddTokenAcquisition(true);
            services.AddInMemoryTokenCaches();
            services.AddAgentIdentities();
            services.AddHttpContextAccessor();

            // Register adapter using standard MsalBot/MsalAgent configuration
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

            IConfigurationSection msalBotSection = configuration.GetSection("MsalBot");
            IConfigurationSection msalAgentSection = configuration.GetSection("MsalAgent");

            // Read bot identity configuration if provided
            MsalIdentityConfig? botIdentity = null;
            string? botClientId = msalBotSection["ClientId"];
            if (!string.IsNullOrEmpty(botClientId))
            {
                botIdentity = new MsalIdentityConfig
                {
                    ConfigSection = msalBotSection,
                    ClientId = botClientId,
                    TenantId = msalBotSection["TenantId"] ?? string.Empty,
                    Scope = msalBotSection["Scope"] ?? DefaultScope,
                    Instance = msalBotSection["Instance"] ?? "https://login.microsoftonline.com/"
                };
            }

            // Read agent identity configuration if provided
            MsalIdentityConfig? agentIdentity = null;
            string? agentClientId = msalAgentSection["ClientId"];
            if (!string.IsNullOrEmpty(agentClientId))
            {
                agentIdentity = new MsalIdentityConfig
                {
                    ConfigSection = msalAgentSection,
                    ClientId = agentClientId,
                    TenantId = msalAgentSection["TenantId"] ?? string.Empty,
                    Scope = msalAgentSection["Scope"] ?? DefaultScope,
                    Instance = msalAgentSection["Instance"] ?? botIdentity?.Instance ?? "https://login.microsoftonline.com/"
                };
            }

            // At least one identity must be configured
            if (botIdentity is null && agentIdentity is null)
            {
                throw new InvalidOperationException("At least one identity (MsalBot or MsalAgent) must be configured with a ClientId");
            }

            return new AdapterConfig
            {
                BotIdentity = botIdentity,
                AgentIdentity = agentIdentity
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

            // Configure authentication schemes for bot identity if present
            string? botScheme = null;
            if (config.BotIdentity is not null)
            {
                botScheme = "MsalBot";
                authBuilder.AddBotAuthentication(config.BotIdentity.ClientId, config.BotIdentity.TenantId, botScheme);
            }

            // Configure authentication schemes for agent identity if present
            string? agentScheme = null;
            if (config.AgentIdentity is not null)
            {
                agentScheme = "MsalAgent";
                authBuilder.AddBotAuthentication(config.AgentIdentity.ClientId, config.AgentIdentity.TenantId, agentScheme);
            }

            // Create policy scheme that routes based on token audience
            authBuilder.AddPolicyScheme(AdapterKeyName, AdapterKeyName, options =>
            {
                options.ForwardDefaultSelector = context =>
                    SelectAuthenticationScheme(context, config, botScheme, agentScheme);
            });

            // Create authorization policy
            services.AddAuthorizationBuilder()
                .AddPolicy(AdapterKeyName, policy =>
                {
                    policy.AuthenticationSchemes.Add(AdapterKeyName);
                    policy.RequireAuthenticatedUser();
                });
        }

        private static string SelectAuthenticationScheme(
            HttpContext context,
            AdapterConfig config,
            string? botScheme,
            string? agentScheme)
        {
            // Default to first available scheme
            string defaultScheme = botScheme ?? agentScheme ?? throw new InvalidOperationException("No authentication scheme configured");

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

                // Check bot identity
                if (config.BotIdentity is not null && botScheme is not null &&
                    (audience == config.BotIdentity.ClientId || audience == $"api://{config.BotIdentity.ClientId}"))
                {
                    return botScheme;
                }

                // Check agent identity
                if (config.AgentIdentity is not null && agentScheme is not null &&
                    (audience == config.AgentIdentity.ClientId || audience == $"api://{config.AgentIdentity.ClientId}"))
                {
                    return agentScheme;
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
            // Configure MSAL options for bot identity if present - bind directly from MsalBot configuration section
            if (config.BotIdentity is not null)
            {
                services.Configure<MicrosoftIdentityApplicationOptions>("MsalBot", config.BotIdentity.ConfigSection);
            }

            // Configure MSAL options for agent identity if present - bind directly from MsalAgent configuration section
            if (config.AgentIdentity is not null)
            {
                services.Configure<MicrosoftIdentityApplicationOptions>("MsalAgent", config.AgentIdentity.ConfigSection);
            }
        }

        private static void RegisterRoutedTokenService(IServiceCollection services, AdapterConfig config)
        {
            services.AddSingleton<IRoutedTokenAcquisitionService>(sp =>
            {
                return new RoutedTokenAcquisitionService(
                    config.BotIdentity is not null,
                    config.AgentIdentity is not null,
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
            string? botScope = config.BotIdentity?.Scope;
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
