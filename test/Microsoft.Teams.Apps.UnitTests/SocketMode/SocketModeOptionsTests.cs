// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeOptionsTests
{
    [Fact]
    public void Defaults_MatchTheOtherSdks()
    {
        SocketModeOptions options = new();

        Assert.Equal(new Uri("https://botapi.skype.com"), options.NegotiateBaseUrl);
        Assert.Equal(["amer", "emea", "apac"], options.Geos);
        Assert.Equal(TimeSpan.FromSeconds(30), options.StartupTimeout);
        Assert.Null(options.ReconnectDelays);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ReadinessTimeout);
        Assert.Equal(TimeSpan.FromSeconds(15), options.KeepAliveInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ServerTimeout);
    }

    [Fact]
    public void ToTransportOptions_PassesInvalidValuesThroughForTheTransportToReject()
    {
        SocketModeTransportOptions transport = new SocketModeOptions { Geos = null!, StartupTimeout = TimeSpan.FromSeconds(-1) }
            .ToTransportOptions();

        Assert.Null(transport.Geos);
        Assert.Equal(TimeSpan.FromSeconds(-1), transport.StartupTimeout);
    }

    [Fact]
    public void ToTransportOptions_CopiesTransportSettings()
    {
        List<string> geos = ["emea"];
        List<TimeSpan> delays = [TimeSpan.FromSeconds(1)];
        SocketModeOptions options = new()
        {
            NegotiateBaseUrl = new Uri("https://example.test"),
            Geos = geos,
            StartupTimeout = TimeSpan.FromSeconds(5),
            ReconnectDelays = delays,
        };

        SocketModeTransportOptions transport = options.ToTransportOptions();
        geos.Add("apac");
        delays.Add(TimeSpan.FromSeconds(2));

        Assert.Equal(new Uri("https://example.test"), transport.NegotiateBaseUri);
        Assert.Equal(["emea"], transport.Geos);
        Assert.Equal(TimeSpan.FromSeconds(5), transport.StartupTimeout);
        Assert.Equal([TimeSpan.FromSeconds(1)], transport.ReconnectDelays!);
    }

    [Fact]
    public void ToTransportOptions_TreatsAnEmptyScheduleAsDefaultBackoff()
    {
        Assert.Null(new SocketModeOptions { ReconnectDelays = [] }.ToTransportOptions().ReconnectDelays);
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
