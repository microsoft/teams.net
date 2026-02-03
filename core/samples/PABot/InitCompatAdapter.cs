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

            services.AddAuthorization(logger, "RidoABSOne");
            services.AddAuthorization(logger, "RidoABSTwo");

            var msalABSConfigSection = configuration.GetSection("RidoABSOne");
            var scopeAbs = msalABSConfigSection.GetValue<string>("Scope") ?? "https://api.botframework.com/.default";
            services.Configure<MicrosoftIdentityApplicationOptions>("RidoABSOne", msalABSConfigSection!);

            var msalABSConfigSection2 = configuration.GetSection("RidoABSTwo");
            var scopeAbs2 = msalABSConfigSection2.GetValue<string>("Scope") ?? "https://api.botframework.com/.default";
            services.Configure<MicrosoftIdentityApplicationOptions>("RidoABSTwo", msalABSConfigSection2!);

            services
                .AddHttpClient()
                .AddTokenAcquisition(true)
                .AddInMemoryTokenCaches()
                .AddAgentIdentities();

            // === RidoABSOne ===
            services.AddHttpClient<ConversationClient>("BotConversationClient")
               .AddHttpMessageHandler(sp =>
               {
                   return new PACustomAuthHandler(
                       "RidoABSOne",
                       sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                       sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                       scopeAbs);
               });

            services.AddHttpClient<UserTokenClient>("BotConversationClient")
               .AddHttpMessageHandler(sp =>
               {
                   return new PACustomAuthHandler(
                       "RidoABSOne",
                       sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                       sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                       scopeAbs);
               });

            services.AddHttpClient<TeamsApiClient>("BotConversationClient")
               .AddHttpMessageHandler(sp =>
               {
                   return new PACustomAuthHandler(
                       "RidoABSOne",
                       sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                       sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                       scopeAbs);
               });

            services.AddKeyedSingleton<TeamsBotApplication>("RidoABSOne");
            services.AddKeyedSingleton<CompatBotAdapter>("RidoABSOne", (sp, keyName) =>
            {
                return new CompatBotAdapter(
                    sp,
                    sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<CompatBotAdapter>>(),
                    keyName.ToString()!);
            });
            //services.AddKeyedSingleton<IBotFrameworkHttpAdapter, CompatAdapter>("RidoABSOne");

            // === RidoABSTwo ===
            services.AddHttpClient<ConversationClient>("BotConversationClient")
                 .AddHttpMessageHandler(sp =>
                 {
                     return new PACustomAuthHandler(
                         "RidoABSTwo",
                         sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                         sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                         scopeAbs2);
                 });

            services.AddHttpClient<UserTokenClient>("BotConversationClient")
                .AddHttpMessageHandler(sp =>
                {
                    return new PACustomAuthHandler(
                        "RidoABSTwo",
                        sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                        sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                        scopeAbs2);
                });

            services.AddHttpClient<TeamsApiClient>("BotConversationClient")
                .AddHttpMessageHandler(sp =>
                {
                    return new PACustomAuthHandler(
                        "RidoABSTwo",
                        sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                        sp.GetRequiredService<ILogger<PACustomAuthHandler>>(),
                        scopeAbs2);
                });

            services.AddKeyedSingleton<TeamsBotApplication>("RidoABSTwo");
            services.AddKeyedSingleton<CompatBotAdapter>("RidoABSTwo", (sp, keyName) =>
            {
                return new CompatBotAdapter(
                    sp,
                    sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<CompatBotAdapter>>(),
                    keyName.ToString()!);
            });
            //services.AddKeyedSingleton<IBotFrameworkHttpAdapter, CompatAdapter>("RidoABSTwo");
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
