// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
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
        public static IServiceCollection AddCustomCompatAdapter(this IServiceCollection services)
        {
            
            ILogger logger = GetOrCreateLogger(services);
            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            services.AddAuthorization(logger, "TeamsInAPX");

            var msalABSConfigSection = configuration.GetSection("TeamsABS");
            var scopeAbs = msalABSConfigSection.GetValue<string>("Scope") ?? "https://api.botframework.com/.default";
            services.Configure<MicrosoftIdentityApplicationOptions>("MsalABS", msalABSConfigSection!);

            var msalAPXConfigSection = configuration.GetSection("TeamsAPX");
            var scopeApx = msalAPXConfigSection.GetValue<string>("Scope") ?? "https://botapi.skype.com/.default";
            services.Configure<MicrosoftIdentityApplicationOptions>("MsalAPX", msalAPXConfigSection!);

            services
                .AddHttpClient()
                .AddTokenAcquisition(true)
                .AddInMemoryTokenCaches()
                .AddAgentIdentities();

            services.AddHttpClient<ConversationClient>("BotConversationClient")
               .AddHttpMessageHandler(sp =>
               {
                   return new PACustomAuthHandler(
                       "MsalAPX",
                       sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                       sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                       scopeApx);
               });

            services.AddHttpClient<UserTokenClient>("BotUserTokenClient")
               .AddHttpMessageHandler(sp =>
               {
                   return new PACustomAuthHandler(
                       "MsalABS",
                       sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                       sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                       scopeAbs);
               });

            services.AddHttpClient<TeamsApiClient>("TeamsAPXClient")
                .AddHttpMessageHandler(sp =>
                {
                    return new PACustomAuthHandler(
                        "MsalABS",
                        sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                        sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                        scopeAbs);
                });

            services.AddSingleton<TeamsBotApplication>();
            services.AddSingleton<CompatBotAdapter>();
            services.AddSingleton<IBotFrameworkHttpAdapter, CompatAdapter>();
            return services;
        }

        private static ILogger GetOrCreateLogger(IServiceCollection services)
        {
            var loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
            var loggerFactory = loggerFactoryDescriptor?.ImplementationInstance as ILoggerFactory;

            ILogger logger = loggerFactory?.CreateLogger<BotApplication>()
                ?? (ILogger)Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            return logger;
        }
    }
}
