// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    public void AddTeamsBotApplication_RegistersTeamsBotApplicationDependencies_WithAllFieldsPopulated()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "teams-bundle-client-id",
            ["AzureAd:TenantId"] = "teams-bundle-tenant-id"
        };

        ServiceProvider serviceProvider = BuildServiceProvider(configData);
        TeamsBotApplicationDependencies deps = serviceProvider.GetRequiredService<TeamsBotApplicationDependencies>();

        Assert.NotNull(deps.ConversationClient);
        Assert.NotNull(deps.UserTokenClient);
        Assert.NotNull(deps.TeamsApiClient);
        Assert.NotNull(deps.HttpContextAccessor);
        Assert.NotNull(deps.Logger);
        Assert.NotNull(deps.Options);
        Assert.Equal("teams-bundle-client-id", deps.Options!.AppId);
        Assert.NotNull(deps.TeamsOptions);
    }

    [Fact]
    public void AddTeamsBotApplication_WithCustomSubclass_ResolvesViaBundledCtor()
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
        services.AddTeamsBotApplication<BundleSubclassBot>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        BundleSubclassBot bot = serviceProvider.GetRequiredService<BundleSubclassBot>();

        Assert.True(bot.ConstructedViaBundle);
        Assert.Equal("subclass-client-id", bot.AppId);
    }

    private sealed class BundleSubclassBot : TeamsBotApplication
    {
        public bool ConstructedViaBundle { get; }

        public BundleSubclassBot(TeamsBotApplicationDependencies services) : base(services)
        {
            ConstructedViaBundle = true;
        }
    }
}
