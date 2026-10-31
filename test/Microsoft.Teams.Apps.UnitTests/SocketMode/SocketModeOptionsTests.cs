// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeOptionsTests
{
    [Fact]
    public void Properties_ContainOnlyNegotiateBaseUrlAndGeos()
    {
        var properties = typeof(SocketModeOptions).GetProperties(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        Assert.Equal(
            [nameof(SocketModeOptions.Geos), nameof(SocketModeOptions.NegotiateBaseUrl)],
            properties.Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(properties, property =>
        {
            Assert.True(property.GetMethod?.IsPublic);
            Assert.True(property.SetMethod?.IsPublic);
        });
    }

    [Fact]
    public void Defaults_MatchTheOtherSdks()
    {
        SocketModeOptions options = new();

        Assert.Equal(new Uri("https://botapi.skype.com"), options.NegotiateBaseUrl);
        Assert.Equal(["amer", "emea", "apac"], options.Geos);
        SocketModeTransportOptions transport = options.ToTransportOptions();
        Assert.Equal(TimeSpan.FromSeconds(30), transport.StartupTimeout);
        Assert.Null(transport.ReconnectDelays);
    }

    [Fact]
    public void ToTransportOptions_PassesInvalidValuesThroughForTheTransportToReject()
    {
        SocketModeTransportOptions transport = new SocketModeOptions { Geos = null! }
            .ToTransportOptions();

        Assert.Null(transport.Geos);
    }

    [Fact]
    public void ToTransportOptions_CopiesEndpointAndSnapshotsGeos()
    {
        List<string> geos = ["emea"];
        SocketModeOptions options = new()
        {
            NegotiateBaseUrl = new Uri("https://example.test"),
            Geos = geos,
        };

        SocketModeTransportOptions transport = options.ToTransportOptions();
        geos.Add("apac");

        Assert.Equal(new Uri("https://example.test"), transport.NegotiateBaseUri);
        Assert.Equal(["emea"], transport.Geos);
    }

    [Fact]
    public void UseSocketMode_AppliesTheDelegate()
    {
        TeamsBotApplicationOptions options = new();

        Assert.Null(options.SocketMode);
        Assert.Same(options, options.UseSocketMode(o => o.Geos = ["emea"]));
        Assert.Equal(["emea"], options.SocketMode!.Geos);
    }

    [Fact]
    public void UseSocketMode_WithoutArgumentsUsesDefaults()
    {
        TeamsBotApplicationOptions options = new();

        options.UseSocketMode();

        Assert.Equal(SocketModeProtocol.DefaultGeos, options.SocketMode!.Geos);
    }

    [Fact]
    public void UseSocketMode_TrueUsesDefaults()
    {
        TeamsBotApplicationOptions options = new();

        Assert.Same(options, options.UseSocketMode(true));

        Assert.Equal(SocketModeProtocol.DefaultGeos, options.SocketMode!.Geos);
    }

    [Fact]
    public void UseSocketMode_FalseClearsEarlierConfiguration()
    {
        TeamsBotApplicationOptions options = new();
        options.UseSocketMode(o => o.Geos = ["emea"]);

        Assert.Same(options, options.UseSocketMode(false));

        Assert.Null(options.SocketMode);
    }

    [Fact]
    public void UseSocketMode_NullDelegateThrows()
        => Assert.Throws<ArgumentNullException>(() => new TeamsBotApplicationOptions().UseSocketMode(null!));
}
