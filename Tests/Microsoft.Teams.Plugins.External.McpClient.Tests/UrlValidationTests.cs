// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;

namespace Microsoft.Teams.Plugins.External.McpClient.Tests;

public class UrlValidationTests : IDisposable
{
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _originalResolver;

    public UrlValidationTests()
    {
        _originalResolver = UrlValidation.HostResolver;
    }

    public void Dispose()
    {
        UrlValidation.HostResolver = _originalResolver;
        GC.SuppressFinalize(this);
    }

    private static void StubResolver(params IPAddress[] addresses)
    {
        UrlValidation.HostResolver = (_, _) => Task.FromResult(addresses);
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("0.255.255.255", true)]
    [InlineData("100.64.0.1", true)]
    [InlineData("100.127.255.254", true)]
    [InlineData("100.63.255.255", false)]
    [InlineData("100.128.0.1", false)]
    [InlineData("224.0.0.1", true)]
    [InlineData("239.255.255.255", true)]
    [InlineData("240.0.0.1", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("172.15.0.1", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("::1", true)]
    [InlineData("fc00::1", true)]
    [InlineData("fd00::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("fec0::1", true)]
    [InlineData("::", true)]
    [InlineData("2001:4860:4860::8888", false)]
    public void IsPrivateAddress_ReturnsExpectedClassification(string address, bool expected)
    {
        Assert.Equal(expected, UrlValidation.IsPrivateAddress(IPAddress.Parse(address)));
    }

    [Fact]
    public async Task RejectsNonHttpSchemes()
    {
        await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("file:///etc/passwd"))
        );
        await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("ftp://example.com"))
        );
    }

    [Fact]
    public async Task AcceptsPublicUrlWithPublicDns()
    {
        StubResolver(IPAddress.Parse("8.8.8.8"));
        var result = await UrlValidation.ValidateMcpServerUrlAsync(new Uri("https://example.com/mcp"));
        Assert.Equal("https://example.com/mcp", result.ToString());
    }

    [Fact]
    public async Task RejectsUrlResolvingToPrivateIp()
    {
        StubResolver(IPAddress.Parse("10.0.0.5"));
        var ex = await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("https://internal.example.com/mcp"))
        );
        Assert.Contains("private or loopback", ex.Message);
    }

    [Fact]
    public async Task RejectsWhenAnyResolvedAddressIsPrivate()
    {
        StubResolver(IPAddress.Parse("8.8.8.8"), IPAddress.Parse("192.168.1.1"));
        await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("https://mixed.example.com/mcp"))
        );
    }

    [Fact]
    public async Task RejectsIpLiteralPrivate()
    {
        // IP literals short-circuit DNS, so the resolver should never be called.
        bool resolverCalled = false;
        UrlValidation.HostResolver = (_, _) =>
        {
            resolverCalled = true;
            return Task.FromResult(Array.Empty<IPAddress>());
        };

        await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("http://127.0.0.1:3000"))
        );
        Assert.False(resolverCalled);
    }

    [Fact]
    public async Task AcceptsPrivateIpWhenAllowPrivateNetwork()
    {
        var result = await UrlValidation.ValidateMcpServerUrlAsync(
            new Uri("http://127.0.0.1:3000"),
            allowPrivateNetwork: true
        );
        Assert.Equal("http://127.0.0.1:3000/", result.ToString());
    }

    [Fact]
    public async Task AcceptsPrivateHostnameWhenAllowPrivateNetworkSkipsDns()
    {
        bool resolverCalled = false;
        UrlValidation.HostResolver = (_, _) =>
        {
            resolverCalled = true;
            return Task.FromResult(new[] { IPAddress.Parse("192.168.1.1") });
        };

        var result = await UrlValidation.ValidateMcpServerUrlAsync(
            new Uri("https://internal.example.com/mcp"),
            allowPrivateNetwork: true
        );
        Assert.NotNull(result);
        Assert.False(resolverCalled);
    }

    [Fact]
    public async Task ValidateUrlFullyReplacesDefaultChecks()
    {
        var seen = new List<Uri>();
        var result = await UrlValidation.ValidateMcpServerUrlAsync(
            new Uri("file:///etc/passwd"),
            validateUrl: url =>
            {
                seen.Add(url);
                return Task.FromResult(true);
            }
        );
        Assert.Single(seen);
        Assert.Equal("file", seen[0].Scheme);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidateUrlRejectsWhenReturningFalse()
    {
        var ex = await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(
                new Uri("https://example.com/mcp"),
                validateUrl: _ => Task.FromResult(false)
            )
        );
        Assert.Contains("rejected by ValidateUrl", ex.Message);
    }

    [Fact]
    public async Task RejectsWhenDnsLookupFails()
    {
        UrlValidation.HostResolver = (_, _) =>
            throw new System.Net.Sockets.SocketException(11001);  // host not found

        var ex = await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("https://nonexistent.invalid/mcp"))
        );
        Assert.Contains("Could not resolve host", ex.Message);
    }

    [Fact]
    public async Task RejectsWhenDnsReturnsEmptyList()
    {
        StubResolver();  // empty array

        var ex = await Assert.ThrowsAsync<UrlValidationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(new Uri("https://example.com/mcp"))
        );
        Assert.Contains("did not resolve", ex.Message);
    }

    [Fact]
    public async Task PropagatesExceptionsFromValidateUrl()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => UrlValidation.ValidateMcpServerUrlAsync(
                new Uri("https://example.com/mcp"),
                validateUrl: _ => throw new InvalidOperationException("custom failure")
            )
        );
    }
}