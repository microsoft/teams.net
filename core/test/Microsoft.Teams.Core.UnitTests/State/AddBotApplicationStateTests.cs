// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.State;

namespace Microsoft.Teams.Core.UnitTests.State;

public class AddBotApplicationStateTests
{
    private static ServiceProvider BuildServiceProvider(Action<TurnStateOptions>? configure = null)
    {
        ServiceCollection services = new();
        services.AddSingleton<IDistributedCache, NullDistributedCache>();
        services.AddBotApplicationState(configure);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void MiddlewareIsRegisteredAndResolvable()
    {
        using ServiceProvider provider = BuildServiceProvider();

        TurnStateMiddleware middleware = provider.GetRequiredService<TurnStateMiddleware>();

        Assert.NotNull(middleware);
    }

    [Fact]
    public void MiddlewareIsRegisteredAsSingleton()
    {
        using ServiceProvider provider = BuildServiceProvider();

        TurnStateMiddleware first = provider.GetRequiredService<TurnStateMiddleware>();
        TurnStateMiddleware second = provider.GetRequiredService<TurnStateMiddleware>();

        Assert.Same(first, second);
    }

    [Fact]
    public void DefaultOptions_HaveOneHourSlidingExpiration()
    {
        using ServiceProvider provider = BuildServiceProvider();

        TurnStateOptions options = provider.GetRequiredService<IOptions<TurnStateOptions>>().Value;

        Assert.Equal(TimeSpan.FromHours(1), options.CacheEntryOptions.SlidingExpiration);
    }

    [Fact]
    public void CustomConfigureCallback_IsApplied()
    {
        TimeSpan customExpiration = TimeSpan.FromMinutes(30);
        using ServiceProvider provider = BuildServiceProvider(opts =>
            opts.CacheEntryOptions.SlidingExpiration = customExpiration);

        TurnStateOptions options = provider.GetRequiredService<IOptions<TurnStateOptions>>().Value;

        Assert.Equal(customExpiration, options.CacheEntryOptions.SlidingExpiration);
    }

    /// <summary>
    /// Minimal no-op distributed cache used for DI resolution in tests.
    /// </summary>
    private sealed class NullDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult<byte[]?>(null);

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Remove(string key) { }

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            Task.CompletedTask;
    }
}
