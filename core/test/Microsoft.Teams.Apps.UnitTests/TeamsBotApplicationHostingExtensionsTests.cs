// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Api.Clients;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsBotApplicationHostingExtensionsTests
{
    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> configData)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddTeamsBotApplication();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddTeamsBotApplication_RegistersAllRequiredServices()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "teams-bundle-client-id",
            ["AzureAd:TenantId"] = "teams-bundle-tenant-id"
        };

        using ServiceProvider serviceProvider = BuildServiceProvider(configData);

        Assert.NotNull(serviceProvider.GetRequiredService<ApiClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<IHttpContextAccessor>());
        TeamsBotApplicationOptions options = serviceProvider.GetRequiredService<TeamsBotApplicationOptions>();
        Assert.Equal("teams-bundle-client-id", options.AppId);
    }

    [Fact]
    public void AddTeamsBotApplication_WithCustomSubclass_ResolvesViaDI()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "subclass-client-id",
            ["AzureAd:TenantId"] = "subclass-tenant-id"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddTeamsBotApplication<SubclassBot>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        SubclassBot bot = serviceProvider.GetRequiredService<SubclassBot>();

        Assert.True(bot.ConstructedViaDI);
        Assert.Equal("subclass-client-id", bot.AppId);
    }

    private sealed class SubclassBot : TeamsBotApplication
    {
        public bool ConstructedViaDI { get; }

        public SubclassBot(
            ApiClient api,
            IHttpContextAccessor accessor,
            ILogger<SubclassBot> logger,
            TeamsBotApplicationOptions? options = null)
            : base(api, accessor, logger, options)
        {
            ConstructedViaDI = true;
        }
    }
}
