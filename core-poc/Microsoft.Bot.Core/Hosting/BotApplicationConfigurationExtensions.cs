using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Bot.Core.Hosting;

public static class BotApplicationConfigurationExtensions
{
    internal const string ConversationHttpClientName = "BotFrameworkConversation";

    internal const string UserTokenHttpClientName = "BotFrameworkUserToken";

    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services) where TApp : BotApplication, new()
    {
        services.AddBotAuthorization();
        services.AddBotApplicationClients();
        services.AddSingleton<TApp>();
        return services;
    }

    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, TApp app) where TApp : BotApplication, new()
    {
        services.AddBotApplicationClients();
        services.AddSingleton(app);
        return services;
    }

    public static IServiceCollection AddBotApplicationClients(this IServiceCollection services, string aadConfigSectionName = "AzureAd")
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        services
            .AddHttpClient()
            .AddTokenAcquisition(false)
            .AddInMemoryTokenCaches()
            .AddAgentIdentities();

        services.Configure<MicrosoftIdentityApplicationOptions>(aadConfigSectionName, configuration.GetSection(aadConfigSectionName));

        string agentScope = configuration[$"{aadConfigSectionName}:Scope"] ?? "https://api.botframework.com/.default";

        services.AddHttpClient(ConversationHttpClientName)
            .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                agentScope,
                aadConfigSectionName));

        services.AddHttpClient(UserTokenHttpClientName)
            .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                "https://api.botframework.com/.default",
                aadConfigSectionName));

        static ConversationClient ConversationClientFactory(IServiceProvider provider, object serviceKey) => new(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(ConversationHttpClientName),
            provider.GetService<ILogger<ConversationClient>>()!
            );

        services.AddKeyedScoped(aadConfigSectionName, ConversationClientFactory);

        services.AddScoped(sp => new UserTokenClient(
            sp.GetRequiredService<ILogger<UserTokenClient>>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(UserTokenHttpClientName)));

        return services;
    }
}
