// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

public class BotAuthenticationHandlerTests
{
    private static readonly string TestAppId = Guid.NewGuid().ToString();
    private static readonly string TestUserId = Guid.NewGuid().ToString();

    private static (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) CreateHandler()
    {
        Mock<IAuthorizationHeaderProvider> mockProvider = new();
        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer fake-token");

        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderForAppAsync(
                It.IsAny<string>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer fake-app-token");

        BotAuthenticationHandler handler = new(
            mockProvider.Object,
            NullLogger<BotAuthenticationHandler>.Instance);

        handler.InnerHandler = new StubInnerHandler();

        return (handler, mockProvider);
    }

    private static HttpRequestMessage CreateAgenticRequest(string appId, string userId)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "https://smba.trafficmanager.net/test/v3/conversations");
        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, new AgenticIdentity
        {
            AgenticAppId = appId,
            AgenticUserId = userId,
        });
        return request;
    }

    private static MemoryCache GetPrivateCache(BotAuthenticationHandler handler)
    {
        FieldInfo field = typeof(BotAuthenticationHandler)
            .GetField("_agenticPrincipalCache", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (MemoryCache)field.GetValue(handler)!;
    }

    private static ConcurrentDictionary<string, SemaphoreSlim> GetPrivateLocks(BotAuthenticationHandler handler)
    {
        FieldInfo field = typeof(BotAuthenticationHandler)
            .GetField("_agenticLocks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (ConcurrentDictionary<string, SemaphoreSlim>)field.GetValue(handler)!;
    }

    [Fact]
    public async Task AgenticRequest_ReusesSameClaimsPrincipal_OnSubsequentCalls()
    {
        (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) = CreateHandler();
        List<ClaimsPrincipal> capturedPrincipals = [];

        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, AuthorizationHeaderProviderOptions, ClaimsPrincipal, CancellationToken>(
                (_, _, principal, _) => capturedPrincipals.Add(principal))
            .ReturnsAsync("Bearer fake-token");

        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);

        await invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None);
        await invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None);

        Assert.Equal(2, capturedPrincipals.Count);
        Assert.Same(capturedPrincipals[0], capturedPrincipals[1]);

        handler.Dispose();
    }

    [Fact]
    public async Task AgenticRequest_ConcurrentCallsForSameIdentity_AreSerialised()
    {
        (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) = CreateHandler();

        int concurrentCount = 0;
        int maxConcurrent = 0;

        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<string>, AuthorizationHeaderProviderOptions, ClaimsPrincipal, CancellationToken>(
                async (_, _, _, ct) =>
                {
                    int current = Interlocked.Increment(ref concurrentCount);
                    int snapshot;
                    do
                    {
                        snapshot = maxConcurrent;
                    } while (current > snapshot && Interlocked.CompareExchange(ref maxConcurrent, current, snapshot) != snapshot);

                    await Task.Delay(50, ct);
                    Interlocked.Decrement(ref concurrentCount);
                    return "Bearer fake-token";
                });

        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);

        Task[] tasks = Enumerable.Range(0, 5)
            .Select(_ => invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxConcurrent);

        handler.Dispose();
    }

    [Fact]
    public async Task AgenticRequest_DifferentIdentities_RunConcurrently()
    {
        (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) = CreateHandler();

        int concurrentCount = 0;
        int maxConcurrent = 0;
        TaskCompletionSource allStarted = new();

        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<string>, AuthorizationHeaderProviderOptions, ClaimsPrincipal, CancellationToken>(
                async (_, _, _, ct) =>
                {
                    int current = Interlocked.Increment(ref concurrentCount);
                    int snapshot;
                    do
                    {
                        snapshot = maxConcurrent;
                    } while (current > snapshot && Interlocked.CompareExchange(ref maxConcurrent, current, snapshot) != snapshot);

                    // Wait until all tasks have entered the critical section
                    if (current >= 3)
                    {
                        allStarted.TrySetResult();
                    }

                    await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
                    Interlocked.Decrement(ref concurrentCount);
                    return "Bearer fake-token";
                });

        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);

        Task[] tasks = Enumerable.Range(0, 3)
            .Select(i => invoker.SendAsync(
                CreateAgenticRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.True(maxConcurrent >= 2, $"Expected concurrent execution for different identities but maxConcurrent was {maxConcurrent}");

        handler.Dispose();
    }

    [Fact]
    public async Task AgenticRequest_CacheEviction_RemovesLockEntry_AndSubsequentCallSucceeds()
    {
        // After a principal is evicted from the cache, the matching lock entry must be
        // removed from _agenticLocks so the dictionary stays bounded. A subsequent call
        // for the same identity must still succeed (creating a fresh semaphore + principal)
        // without ever throwing ObjectDisposedException.
        (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) = CreateHandler();
        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);

        // First call — populates both the cache entry and the semaphore.
        await invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None);

        ConcurrentDictionary<string, SemaphoreSlim> locks = GetPrivateLocks(handler);
        MemoryCache cache = GetPrivateCache(handler);

        Assert.NotEmpty(locks);

        // Force eviction by compacting the entire cache.
        cache.Compact(1.0);

        // The post-eviction callback runs on the thread pool; wait briefly for it to remove
        // the lock entry. Bounded by 2s to fail fast if the callback is not wired up.
        bool removed = SpinWait.SpinUntil(() => locks.IsEmpty, TimeSpan.FromSeconds(2));
        Assert.True(removed, "Expected the lock entry to be removed from _agenticLocks after cache eviction.");

        // Second call for the same identity — must NOT throw ObjectDisposedException
        // and must repopulate both the cache and the lock dictionary.
        await invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None);

        Assert.NotEmpty(locks);

        handler.Dispose();
    }

    [Fact]
    public async Task AgenticRequest_AfterCacheEviction_CreatesNewPrincipal()
    {
        (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) = CreateHandler();
        List<ClaimsPrincipal> capturedPrincipals = [];

        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, AuthorizationHeaderProviderOptions, ClaimsPrincipal, CancellationToken>(
                (_, _, principal, _) => capturedPrincipals.Add(principal))
            .ReturnsAsync("Bearer fake-token");

        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);

        // First call — populates cache.
        await invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None);

        // Evict everything.
        MemoryCache cache = GetPrivateCache(handler);
        cache.Compact(1.0);

        // Second call — should create a new ClaimsPrincipal since cache was evicted.
        await invoker.SendAsync(CreateAgenticRequest(TestAppId, TestUserId), CancellationToken.None);

        Assert.Equal(2, capturedPrincipals.Count);
        Assert.NotSame(capturedPrincipals[0], capturedPrincipals[1]);

        handler.Dispose();
    }

    [Fact]
    public async Task AgenticRequest_ConcurrentCallsDuringCacheEviction_DoNotThrow()
    {
        (BotAuthenticationHandler handler, Mock<IAuthorizationHeaderProvider> mockProvider) = CreateHandler();
        SemaphoreSlim gate = new(0, 1);

        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<string>, AuthorizationHeaderProviderOptions, ClaimsPrincipal, CancellationToken>(
                async (_, _, _, ct) =>
                {
                    // Signal that we are inside the critical section, then wait.
                    gate.Release();
                    await Task.Delay(100, ct);
                    return "Bearer fake-token";
                });

        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);

        string appId = Guid.NewGuid().ToString();
        string userId = Guid.NewGuid().ToString();

        // Start a call that will hold the semaphore.
        Task firstCall = invoker.SendAsync(CreateAgenticRequest(appId, userId), CancellationToken.None);

        // Wait until it's inside the critical section.
        await gate.WaitAsync();

        // Evict the cache entry while the semaphore is held.
        MemoryCache cache = GetPrivateCache(handler);
        cache.Compact(1.0);

        // The first call should complete without ObjectDisposedException.
        await firstCall;

        // A follow-up call should also work.
        // Reset mock to return immediately.
        mockProvider
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer fake-token");

        await invoker.SendAsync(CreateAgenticRequest(appId, userId), CancellationToken.None);

        handler.Dispose();
    }

    [Fact]
    public void Dispose_CleansUpSemaphoresAndCache()
    {
        (BotAuthenticationHandler handler, _) = CreateHandler();
        ConcurrentDictionary<string, SemaphoreSlim> locks = GetPrivateLocks(handler);

        // Manually add some semaphores to simulate cached agentic identities.
        locks.TryAdd("key1", new SemaphoreSlim(1, 1));
        locks.TryAdd("key2", new SemaphoreSlim(1, 1));

        SemaphoreSlim s1 = locks["key1"];
        SemaphoreSlim s2 = locks["key2"];

        handler.Dispose();

        Assert.Empty(locks);
        // Disposed semaphores throw on WaitAsync.
        Assert.Throws<ObjectDisposedException>(() => s1.Wait(0));
        Assert.Throws<ObjectDisposedException>(() => s2.Wait(0));
    }

    /// <summary>
    /// Stub inner handler that returns 200 OK for all requests.
    /// </summary>
    private class StubInnerHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
