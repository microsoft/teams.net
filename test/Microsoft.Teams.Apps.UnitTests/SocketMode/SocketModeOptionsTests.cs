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
        options.Validate();
    }

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://[::1]:5000")]
    [InlineData("https://example.test")]
    public void Validate_AcceptsHttpsAndLoopback(string url)
    {
        new SocketModeOptions { NegotiateBaseUrl = new Uri(url) }.Validate();
    }

    public static TheoryData<string, Action<SocketModeOptions>> InvalidOptions => new()
    {
        { "absolute", o => o.NegotiateBaseUrl = new Uri("/relative", UriKind.Relative) },
        { "HTTPS", o => o.NegotiateBaseUrl = new Uri("http://example.test") },
        { "at least one geo", o => o.Geos = [] },
        { "null", o => o.Geos = ["amer", null!] },
        { "more than once", o => o.Geos = ["amer", " AMER/"] },
        { "StartupTimeout", o => o.StartupTimeout = TimeSpan.FromSeconds(-1) },
        { "ReconnectDelays", o => o.ReconnectDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1)] },
        { "ReadinessTimeout", o => o.ReadinessTimeout = TimeSpan.Zero },
        { "KeepAliveInterval", o => o.KeepAliveInterval = TimeSpan.Zero },
        { "ServerTimeout must be positive", o => o.ServerTimeout = TimeSpan.Zero },
        { "greater than KeepAliveInterval", o => o.ServerTimeout = o.KeepAliveInterval },
    };

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void Validate_RejectsInvalidSettings(string expectedMessage, Action<SocketModeOptions> configure)
    {
        SocketModeOptions options = new();
        configure(options);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AllowsAnEmptyGeoAndZeroStartupTimeout()
    {
        new SocketModeOptions { Geos = [""], StartupTimeout = TimeSpan.Zero }.Validate();
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
    public void UseSocketMode_WithoutDelegateUsesDefaults()
    {
        TeamsBotApplicationOptions options = new();

        options.UseSocketMode();

        Assert.Equal(SocketModeProtocol.DefaultGeos, options.SocketMode!.Geos);
    }
}
