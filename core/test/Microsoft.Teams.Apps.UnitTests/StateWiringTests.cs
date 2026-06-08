// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests;

public class StateWiringTests
{
    [Fact]
    public void Options_UseState_Enables()
    {
        var options = new TeamsBotApplicationOptions();

        TeamsBotApplicationOptions returned = options.UseState();

        Assert.Same(options, returned);
        Assert.True(options.StateEnabled);
    }

    [Fact]
    public void Options_DefaultsToNoState()
        => Assert.False(new TeamsBotApplicationOptions().StateEnabled);

    [Fact]
    public void AppBuilder_UseState_EnablesOnOptions()
    {
        AppBuilder builder = App.Builder().UseState();

        Assert.True(builder.Options.StateEnabled);
    }

    [Fact]
    public void UseState_RegistersResolvableTurnStateStore_WithDefaultCache()
    {
        // No cache registered explicitly — UseState defaults to an in-process IDistributedCache.
        ServiceProvider provider = BuildProvider(o => o.UseState());

        Assert.NotNull(provider.GetService<TurnStateStore>());
    }

    [Fact]
    public void WithoutUseState_NoTurnStateStoreRegistered()
    {
        ServiceProvider provider = BuildProvider(configure: null);

        Assert.Null(provider.GetService<TurnStateStore>());
    }

    private static ServiceProvider BuildProvider(Action<TeamsBotApplicationOptions>? configure)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AzureAd:ClientId"] = "test-client-id" })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddLogging();

        if (configure is null)
        {
            services.AddTeamsBotApplication();
        }
        else
        {
            services.AddTeamsBotApplication(configure);
        }

        return services.BuildServiceProvider();
    }
}
