// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Describes the lifecycle state of a Socket Mode transport or one of its geos.
/// </summary>
internal enum SocketModeStatus
{
    /// <summary>
    /// The transport has not started.
    /// </summary>
    Idle,

    /// <summary>
    /// A connection is being established.
    /// </summary>
    Connecting,

    /// <summary>
    /// Every connection is ready to receive activities.
    /// </summary>
    Ready,

    /// <summary>
    /// A connection dropped unexpectedly and is recovering.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The transport has stopped.
    /// </summary>
    Stopped,
}

/// <summary>
/// Configures a <see cref="SocketModeTransport"/>.
/// </summary>
internal sealed class SocketModeTransportOptions
{
    /// <summary>
    /// Gets the base URL used to negotiate each geo connection.
    /// </summary>
    internal Uri NegotiateBaseUri { get; init; } = new(SocketModeProtocol.DefaultNegotiateBaseUrl);

    /// <summary>
    /// Gets the geos to connect. An empty string connects to the base URL without a geo segment.
    /// </summary>
    internal IReadOnlyList<string> Geos { get; init; } = SocketModeProtocol.DefaultGeos;

    /// <summary>
    /// Gets the time each geo has to establish its initial connection.
    /// </summary>
    internal TimeSpan StartupTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets an explicit reconnect delay schedule. The last delay repeats. When omitted, capped exponential
    /// backoff with full jitter is used.
    /// </summary>
    internal IReadOnlyList<TimeSpan>? ReconnectDelays { get; init; }
}

/// <summary>
/// Connects one <see cref="GeoSocket"/> per configured geo and routes their activities to the app.
/// </summary>
/// <remarks>
/// Startup is all-or-nothing: every geo must become ready or the transport stops and startup fails. Once
/// started, each geo is supervised independently.
/// </remarks>
internal sealed class SocketModeTransport : IGeoSocketOwner, IAsyncDisposable
{
    private static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HandoffWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconnectInitialDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(15);

    private readonly SocketModeTransportOptions _options;
    private readonly IReadOnlyList<(string Geo, Uri NegotiateUri)> _geos;
    private readonly ISocketConnectionFactory _connectionFactory;
    private readonly Func<CoreActivity, Task<SocketDispatchResult>> _dispatch;
    private readonly Func<Exception, Task>? _onError;
    private readonly string? _botKey;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Random _random;
    private readonly object _sync = new();
    private readonly Dictionary<string, SocketModeStatus> _geoStatuses = [];

    private GeoSocket[] _geoSockets = [];
    private SocketModeStatus _lifecycle = SocketModeStatus.Idle;
    private Task? _stopTask;

    /// <summary>
    /// Initializes a Socket Mode transport and resolves its geos.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="connectionFactory">Creates one connection per geo generation.</param>
    /// <param name="dispatch">Processes an inbound activity and returns its status and optional body.</param>
    /// <param name="logger">The logger for transport lifecycle and dispatch failures.</param>
    /// <param name="botKey">The bot key echoed on reply frames, when available.</param>
    /// <param name="onError">Observes activity processing failures before the failure reply is returned.</param>
    /// <param name="timeProvider">The time provider for supervision timing and reply timestamps.</param>
    /// <param name="random">The random source for reconnect jitter.</param>
    /// <exception cref="ArgumentException">Thrown when the geos are empty, null, or duplicated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the startup timeout is negative.</exception>
    internal SocketModeTransport(
        SocketModeTransportOptions options,
        ISocketConnectionFactory connectionFactory,
        Func<CoreActivity, Task<SocketDispatchResult>> dispatch,
        ILogger logger,
        string? botKey = null,
        Func<Exception, Task>? onError = null,
        TimeProvider? timeProvider = null,
        Random? random = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _botKey = botKey;
        _onError = onError;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _random = random ?? Random.Shared;

        ArgumentNullException.ThrowIfNull(options.NegotiateBaseUri, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.StartupTimeout, TimeSpan.Zero, nameof(options));
        _geos = ResolveGeos(options.NegotiateBaseUri, options.Geos);
    }

    /// <summary>
    /// Gets the aggregate status: ready when every geo is ready, disconnected when any geo is recovering from a
    /// drop, and connecting otherwise.
    /// </summary>
    internal SocketModeStatus Status
    {
        get
        {
            lock (_sync)
            {
                if (_lifecycle is SocketModeStatus.Idle or SocketModeStatus.Stopped)
                {
                    return _lifecycle;
                }

                if (_geoStatuses.Count > 0 && _geoStatuses.Values.All(status => status == SocketModeStatus.Ready))
                {
                    return SocketModeStatus.Ready;
                }

                return _geoStatuses.ContainsValue(SocketModeStatus.Disconnected)
                    ? SocketModeStatus.Disconnected
                    : SocketModeStatus.Connecting;
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of each geo's status.
    /// </summary>
    internal IReadOnlyDictionary<string, SocketModeStatus> GeoStatuses
    {
        get
        {
            lock (_sync)
            {
                return new Dictionary<string, SocketModeStatus>(_geoStatuses);
            }
        }
    }

    /// <inheritdoc />
    TimeSpan IGeoSocketOwner.StartupTimeout => _options.StartupTimeout;

    /// <inheritdoc />
    TimeSpan IGeoSocketOwner.TokenRefreshMargin => TokenRefreshMargin;

    /// <inheritdoc />
    TimeSpan IGeoSocketOwner.HandoffWindow => HandoffWindow;

    /// <summary>
    /// Connects every geo. Completes once all are ready; otherwise stops the transport and throws the first
    /// geo failure.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling startup.</param>
    internal async Task StartAsync(CancellationToken cancellationToken = default)
    {
        GeoSocket[] geoSockets;
        lock (_sync)
        {
            if (_lifecycle != SocketModeStatus.Idle)
            {
                throw new InvalidOperationException("A Socket Mode transport can only be started once.");
            }

            _lifecycle = SocketModeStatus.Connecting;
            geoSockets = [.. _geos.Select(geo => new GeoSocket(
                this,
                geo.Geo,
                geo.NegotiateUri,
                _connectionFactory,
                _logger,
                _timeProvider))];
            foreach (GeoSocket geoSocket in geoSockets)
            {
                _geoStatuses[geoSocket.Geo] = SocketModeStatus.Connecting;
            }

            _geoSockets = geoSockets;
        }

        _logger.LogInformation(
            "Socket Mode connecting {Count} geo(s): {Geos}.",
            geoSockets.Length,
            string.Join(", ", geoSockets.Select(geoSocket => geoSocket.Geo)));

        using CancellationTokenSource startSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Exception? firstFailure = null;

        async Task StartGeoAsync(GeoSocket geoSocket)
        {
            try
            {
                await geoSocket.StartAsync(startSource.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (Interlocked.CompareExchange(ref firstFailure, exception, null) is null)
                {
                    await startSource.CancelAsync().ConfigureAwait(false);
                }

                throw;
            }
        }

        try
        {
            await Task.WhenAll(geoSockets.Select(StartGeoAsync)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            bool stoppedExternally;
            lock (_sync)
            {
                stoppedExternally = _stopTask is not null;
            }

            await StopAsync().ConfigureAwait(false);

            if (stoppedExternally)
            {
                throw new OperationCanceledException("Socket Mode transport stopped before startup completed.", exception);
            }

            ExceptionDispatchInfo.Throw(firstFailure ?? exception);
        }

        lock (_sync)
        {
            if (_stopTask is not null)
            {
                throw new OperationCanceledException("Socket Mode transport stopped before startup completed.");
            }
        }

        _logger.LogInformation("Socket Mode ready across {Count} geo(s).", geoSockets.Length);
    }

    /// <summary>
    /// Stops every geo and disposes their connections. Idempotent.
    /// </summary>
    internal Task StopAsync()
    {
        lock (_sync)
        {
            if (_stopTask is not null)
            {
                return _stopTask;
            }

            _lifecycle = SocketModeStatus.Stopped;
            foreach (string geo in _geoStatuses.Keys.ToArray())
            {
                _geoStatuses[geo] = SocketModeStatus.Stopped;
            }
            _stopTask = Task.WhenAll(_geoSockets.Select(geoSocket => geoSocket.DisposeAsync().AsTask()));
            return _stopTask;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(StopAsync());

    /// <inheritdoc />
    TimeSpan? IGeoSocketOwner.GetRetryAfter(Exception? error)
        => (error as SocketModeNegotiateException)?.RetryAfter;

    /// <inheritdoc />
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Jitter only spreads reconnect attempts; it has no security purpose.")]
    TimeSpan IGeoSocketOwner.GetBackoffDelay(int attempt)
    {
        if (_options.ReconnectDelays is { Count: > 0 } schedule)
        {
            return schedule[Math.Min(attempt, schedule.Count - 1)];
        }

        double capSeconds = Math.Min(
            ReconnectInitialDelay.TotalSeconds * Math.Pow(2, Math.Min(attempt, 30)),
            ReconnectMaxDelay.TotalSeconds);
        return TimeSpan.FromSeconds(capSeconds * _random.NextDouble());
    }

    /// <inheritdoc />
    Task<SocketReplyFrame?> IGeoSocketOwner.DispatchAsync(string geo, SocketActivityEnvelope envelope)
        => HandleEnvelopeAsync(envelope);

    /// <inheritdoc />
    void IGeoSocketOwner.OnGeoReady(string geo, SocketReadyFrame frame)
    {
        SetGeoStatus(geo, SocketModeStatus.Ready);
        _logger.LogDebug("Socket Mode geo {Geo} ready with connection {ConnectionId}.", geo, frame.ConnectionId);
    }

    /// <inheritdoc />
    void IGeoSocketOwner.OnGeoDisconnected(string geo, Exception? error)
        => SetGeoStatus(geo, SocketModeStatus.Disconnected);

    /// <inheritdoc />
    void IGeoSocketOwner.OnGeoReconnected(string geo)
        => SetGeoStatus(geo, SocketModeStatus.Ready);

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Any activity processing failure must be logged and answered with a 500 reply frame.")]
    private async Task<SocketReplyFrame?> HandleEnvelopeAsync(SocketActivityEnvelope envelope)
    {
        long receivedAt = NowUnixMilliseconds();

        if (envelope.ProtocolVersion > SocketModeProtocol.CurrentVersion)
        {
            _logger.LogWarning(
                "Socket Mode rejecting envelope {EnvelopeId} with unsupported protocol version {Version}.",
                envelope.EnvelopeId,
                envelope.ProtocolVersion);
            return SocketModeEnvelope.CreateInvokeReply(
                envelope,
                _botKey,
                new SocketDispatchResult(400, new { error = $"unsupported protocolVersion {envelope.ProtocolVersion}" }),
                receivedAt,
                NowUnixMilliseconds());
        }

        if (!SocketModeEnvelope.TryReadActivity(envelope, out CoreActivity? activity) || activity is null)
        {
            _logger.LogWarning("Socket Mode envelope {EnvelopeId} had no activity payload; dropping.", envelope.EnvelopeId);
            return null;
        }

        bool invoke = IsInvoke(envelope, activity);

        try
        {
            SocketDispatchResult result = await _dispatch(activity).ConfigureAwait(false);
            return invoke
                ? SocketModeEnvelope.CreateInvokeReply(envelope, _botKey, result, receivedAt, NowUnixMilliseconds())
                : SocketModeEnvelope.CreateAcknowledgement(
                    envelope,
                    _botKey,
                    receivedAt,
                    NowUnixMilliseconds(),
                    result.Status);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Socket Mode failed to process activity {ActivityType} in envelope {EnvelopeId}.",
                activity.Type,
                envelope.EnvelopeId);
            await ReportErrorAsync(exception).ConfigureAwait(false);

            return invoke
                ? SocketModeEnvelope.CreateInvokeReply(
                    envelope,
                    _botKey,
                    new SocketDispatchResult(500, new { error = "bot handler error" }),
                    receivedAt,
                    NowUnixMilliseconds())
                : SocketModeEnvelope.CreateAcknowledgement(envelope, _botKey, receivedAt, NowUnixMilliseconds(), 500);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A failing error observer is logged so it cannot prevent the failure reply.")]
    private async Task ReportErrorAsync(Exception exception)
    {
        if (_onError is null)
        {
            return;
        }

        try
        {
            await _onError(exception).ConfigureAwait(false);
        }
        catch (Exception hookException)
        {
            _logger.LogWarning(hookException, "Socket Mode error observer failed.");
        }
    }

    private void SetGeoStatus(string geo, SocketModeStatus status)
    {
        lock (_sync)
        {
            if (_lifecycle != SocketModeStatus.Stopped)
            {
                _geoStatuses[geo] = status;
            }
        }
    }

    private long NowUnixMilliseconds() => _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    // An envelope is an invoke when its type says so, falling back to the activity type when the envelope has none.
    private static bool IsInvoke(SocketActivityEnvelope envelope, CoreActivity activity)
        => string.Equals(
            string.IsNullOrEmpty(envelope.Type) ? activity.Type : envelope.Type,
            TeamsActivityTypes.Invoke,
            StringComparison.OrdinalIgnoreCase);

    private static List<(string Geo, Uri NegotiateUri)> ResolveGeos(Uri baseUri, IReadOnlyList<string> geos)
    {
        if (geos is null || geos.Count == 0)
        {
            throw new ArgumentException(
                "Socket Mode requires at least one geo. Use an empty string to connect without a geo segment.",
                nameof(geos));
        }

        string baseUrl = baseUri.AbsoluteUri.TrimEnd('/');
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<(string Geo, Uri NegotiateUri)> resolved = [];

        foreach (string geo in geos)
        {
            if (geo is null)
            {
                throw new ArgumentException("Socket Mode geos cannot contain null.", nameof(geos));
            }

            string segment = geo.Trim().Trim('/');
            if (!seen.Add(segment))
            {
                throw new ArgumentException($"Socket Mode geo '{geo}' is listed more than once.", nameof(geos));
            }

            string path = segment.Length > 0
                ? $"/{Uri.EscapeDataString(segment)}{SocketModeProtocol.NegotiatePath}"
                : SocketModeProtocol.NegotiatePath;
            resolved.Add((segment, new Uri(baseUrl + path, UriKind.Absolute)));
        }

        return resolved;
    }
}
