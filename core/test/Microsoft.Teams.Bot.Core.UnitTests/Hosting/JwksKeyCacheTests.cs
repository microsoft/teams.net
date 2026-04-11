// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Teams.Bot.Core.Hosting;
using Moq;

namespace Microsoft.Teams.Bot.Core.UnitTests.Hosting;

public class JwksKeyCacheTests
{
    /// <summary>
    /// Creates a ConfigurationManager whose GetConfigurationAsync always returns a
    /// configuration with the supplied signing key.
    /// </summary>
    private static ConfigurationManager<OpenIdConnectConfiguration> BuildManager(SecurityKey key)
    {
        OpenIdConnectConfiguration config = new();
        config.SigningKeys.Add(key);

        Mock<IConfigurationRetriever<OpenIdConnectConfiguration>> mockRetriever = new();
        mockRetriever
            .Setup(r => r.GetConfigurationAsync(It.IsAny<string>(), It.IsAny<IDocumentRetriever>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        return new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://example.com/.well-known/openid-configuration",
            mockRetriever.Object);
    }

    [Fact]
    public async Task GetKeys_ReturnsKeysAfterBackgroundWarmup()
    {
        // Arrange
        RsaSecurityKey key = new(System.Security.Cryptography.RSA.Create(2048));
        ConfigurationManager<OpenIdConnectConfiguration> manager = BuildManager(key);
        JwksKeyCache cache = new(manager);

        // Give the background task a moment to complete
        await Task.Delay(200);

        // Act
        IEnumerable<SecurityKey> keys = cache.GetKeys();

        // Assert – the background fetch populated the cache
        Assert.Single(keys);
        Assert.Same(key, keys.First());
    }

    [Fact]
    public void GetKeys_WhenCalledConcurrently_DoesNotThrow()
    {
        // Arrange
        RsaSecurityKey key = new(System.Security.Cryptography.RSA.Create(2048));
        ConfigurationManager<OpenIdConnectConfiguration> manager = BuildManager(key);
        JwksKeyCache cache = new(manager);

        // Act – hammer GetKeys from many threads before warm-up completes
        ConcurrentBag<IEnumerable<SecurityKey>> results = [];
        Parallel.For(0, 50, _ =>
        {
            results.Add(cache.GetKeys());
        });

        // Assert – every call returned a non-empty list; no exceptions thrown
        Assert.All(results, r => Assert.NotEmpty(r));
    }

    [Fact]
    public async Task GetKeys_WarmCache_DoesNotBlockCallingThread()
    {
        // Arrange
        RsaSecurityKey key = new(System.Security.Cryptography.RSA.Create(2048));
        ConfigurationManager<OpenIdConnectConfiguration> manager = BuildManager(key);
        JwksKeyCache cache = new(manager);

        // Let the background task finish so the cache is warm
        await Task.Delay(300);

        // Act – on a warm cache GetKeys should complete quickly (no network I/O)
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        IEnumerable<SecurityKey> keys = cache.GetKeys();
        sw.Stop();

        // Assert – served from volatile cache, well under 100 ms
        Assert.NotEmpty(keys);
        Assert.True(sw.ElapsedMilliseconds < 100, $"Warm-cache GetKeys took {sw.ElapsedMilliseconds} ms (expected < 100 ms)");
    }
}
