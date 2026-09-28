// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Configures Socket Mode, where the bot receives activities over outbound WebSocket connections instead of an
/// inbound HTTP endpoint.
/// </summary>
/// <remarks>
/// Socket Mode opens one connection per geo and waits until every geo is ready before the host finishes starting.
/// It is supported only in the public cloud.
/// </remarks>
public sealed class SocketModeOptions
{
    /// <summary>
    /// Gets or sets the base URL used to negotiate each geo connection. Must use HTTPS unless it targets loopback.
    /// Defaults to <c>https://botapi.skype.com</c>.
    /// </summary>
    public Uri NegotiateBaseUrl { get; set; } = new(SocketModeProtocol.DefaultNegotiateBaseUrl);

    /// <summary>
    /// Gets or sets the geos to connect, one connection each. An empty string connects to the base URL without a geo
    /// segment. Defaults to <c>amer</c>, <c>emea</c>, and <c>apac</c>.
    /// </summary>
    public IReadOnlyList<string> Geos { get; set; } = SocketModeProtocol.DefaultGeos;

    /// <summary>
    /// Gets or sets how long each geo has to establish its initial connection. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets an explicit reconnect delay schedule; the last delay repeats. When <c>null</c> or empty, capped
    /// exponential backoff with jitter is used.
    /// </summary>
    public IReadOnlyList<TimeSpan>? ReconnectDelays { get; set; }

    /// <summary>
    /// Gets or sets how long a new connection waits for the service to report it ready. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ReadinessTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval between keep-alive messages sent to the service. Defaults to 15 seconds.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets how long without a message from the service before the connection is considered lost. Must be
    /// greater than <see cref="KeepAliveInterval"/>. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the options, so a misconfiguration fails at registration rather than when the host starts.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a setting is invalid.</exception>
    internal void Validate()
    {
        if (NegotiateBaseUrl is null || !NegotiateBaseUrl.IsAbsoluteUri)
        {
            throw Invalid($"{nameof(NegotiateBaseUrl)} must be an absolute URL.");
        }

        if (!IsSecureOrLoopback(NegotiateBaseUrl))
        {
            throw Invalid($"{nameof(NegotiateBaseUrl)} must use HTTPS unless it targets loopback.");
        }

        if (Geos is null || Geos.Count == 0)
        {
            throw Invalid($"{nameof(Geos)} must contain at least one geo. Use an empty string to connect without a geo segment.");
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string geo in Geos)
        {
            if (geo is null)
            {
                throw Invalid($"{nameof(Geos)} cannot contain null.");
            }

            if (!seen.Add(geo.Trim().Trim('/')))
            {
                throw Invalid($"Geo '{geo}' is listed more than once.");
            }
        }

        if (StartupTimeout < TimeSpan.Zero)
        {
            throw Invalid($"{nameof(StartupTimeout)} cannot be negative.");
        }

        if (ReconnectDelays is not null && ReconnectDelays.Any(delay => delay < TimeSpan.Zero))
        {
            throw Invalid($"{nameof(ReconnectDelays)} cannot contain negative delays.");
        }

        EnsurePositive(ReadinessTimeout, nameof(ReadinessTimeout));
        EnsurePositive(KeepAliveInterval, nameof(KeepAliveInterval));
        EnsurePositive(ServerTimeout, nameof(ServerTimeout));

        if (ServerTimeout <= KeepAliveInterval)
        {
            throw Invalid($"{nameof(ServerTimeout)} must be greater than {nameof(KeepAliveInterval)}.");
        }
    }

    /// <summary>
    /// Snapshots the transport-level settings, so later changes to these options do not affect a running transport.
    /// </summary>
    /// <returns>The transport options.</returns>
    internal SocketModeTransportOptions ToTransportOptions() => new()
    {
        NegotiateBaseUri = NegotiateBaseUrl,
        Geos = [.. Geos],
        StartupTimeout = StartupTimeout,
        ReconnectDelays = ReconnectDelays is { Count: > 0 } delays ? [.. delays] : null,
    };

    private static void EnsurePositive(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero)
        {
            throw Invalid($"{name} must be positive.");
        }
    }

    // Matches the negotiator's own check, which runs again on every negotiate request.
    private static bool IsSecureOrLoopback(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host))
        {
            return true;
        }

        string host = uri.Host.Trim('[', ']');
        return uri.Scheme == Uri.UriSchemeHttp
            && (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || host.Equals("127.0.0.1", StringComparison.Ordinal)
                || host.Equals("::1", StringComparison.Ordinal));
    }

    private static InvalidOperationException Invalid(string message)
        => new($"Invalid Socket Mode options: {message}");
}
