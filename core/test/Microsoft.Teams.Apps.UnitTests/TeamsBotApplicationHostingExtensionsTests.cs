// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core.Hosting;

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

        BotApplicationOptions botOptions = serviceProvider.GetRequiredService<BotApplicationOptions>();
        Assert.Equal("teams-bundle-client-id", botOptions.AppId);
        Assert.Equal(options.AppId, botOptions.AppId);
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

    [Fact]
    public void AddTeamsBotApplication_UseState_RegistersStateLoader()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "state-client-id",
            ["AzureAd:TenantId"] = "state-tenant-id"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddTeamsBotApplication(options => options.UseState());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        TurnStateLoader? loader = serviceProvider.GetService<TurnStateLoader>();

        Assert.NotNull(loader);
    }

    [Fact]
    public void AddTeamsBotApplication_UseState_CustomOptions_Applied()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "state-client-id",
            ["AzureAd:TenantId"] = "state-tenant-id"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddTeamsBotApplication(options =>
            options.UseState(state =>
                state.CacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(15)));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Microsoft.Extensions.Options.IOptions<TurnStateOptions> stateOptions =
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TurnStateOptions>>();

        Assert.Equal(TimeSpan.FromMinutes(15), stateOptions.Value.CacheEntryOptions.SlidingExpiration);
    }

    [Fact]
    public void AddTeamsBotApplication_WithoutState_DoesNotRegisterStateLoader()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "no-state-client-id",
            ["AzureAd:TenantId"] = "no-state-tenant-id"
        };

        using ServiceProvider serviceProvider = BuildServiceProvider(configData);
        TurnStateLoader? middleware = serviceProvider.GetService<TurnStateLoader>();

        Assert.Null(middleware);
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
