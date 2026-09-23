// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Negotiates connection details for a Socket Mode transport.
/// </summary>
internal sealed class SocketModeNegotiator
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _httpClient;
    private readonly Func<CancellationToken, Task<string?>> _getBotToken;
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a Socket Mode negotiator.
    /// </summary>
    /// <param name="httpClient">The client used to call the negotiate endpoint.</param>
    /// <param name="getBotToken">Acquires a Bot Framework app token.</param>
    /// <param name="timeout">The negotiate request timeout.</param>
    /// <param name="timeProvider">The time provider used to evaluate retry delays.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is not positive.</exception>
    internal SocketModeNegotiator(
        HttpClient httpClient,
        Func<CancellationToken, Task<string?>> getBotToken,
        TimeSpan? timeout = null,
        TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _getBotToken = getBotToken ?? throw new ArgumentNullException(nameof(getBotToken));
        _timeout = timeout ?? DefaultTimeout;
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (_timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The negotiate timeout must be greater than zero.");
        }
    }

    /// <summary>
    /// Negotiates the endpoint and access token for a Socket Mode connection.
    /// </summary>
    /// <param name="negotiateUri">The Socket Mode negotiate endpoint.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>The negotiated connection details.</returns>
    /// <exception cref="SocketModeNegotiateException">Thrown when the endpoint returns an unsuccessful status code.</exception>
    /// <exception cref="TimeoutException">Thrown when negotiation exceeds the configured timeout.</exception>
    internal async Task<SocketModeNegotiateResponse> NegotiateAsync(
        Uri negotiateUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(negotiateUri);
        EnsureSecureNegotiateUri(negotiateUri);

        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_timeout);

        try
        {
            string? token = await _getBotToken(timeoutSource.Token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "Socket Mode negotiate could not acquire a Bot Framework app token.");
            }

            using HttpRequestMessage request = new(HttpMethod.Post, negotiateUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new SocketModeNegotiateException(
                    response.StatusCode,
                    GetRetryAfter(response));
            }

            string json = await response.Content
                .ReadAsStringAsync(timeoutSource.Token)
                .ConfigureAwait(false);

            SocketModeNegotiateResponse negotiateResponse;
            try
            {
                negotiateResponse = SocketModeJson.Deserialize<SocketModeNegotiateResponse>(json)
                    ?? throw new InvalidDataException("Socket Mode negotiate returned an empty response.");
            }
            catch (System.Text.Json.JsonException exception)
            {
                throw new InvalidDataException(
                    "Socket Mode negotiate returned invalid JSON.",
                    exception);
            }

            if (string.IsNullOrWhiteSpace(negotiateResponse.Url)
                || string.IsNullOrWhiteSpace(negotiateResponse.AccessToken))
            {
                throw new InvalidDataException(
                    "Socket Mode negotiate response is missing url or accessToken.");
            }

            if (!Uri.TryCreate(negotiateResponse.Url, UriKind.Absolute, out Uri? signalRUri))
            {
                throw new InvalidDataException(
                    "Socket Mode negotiate response contains an invalid SignalR URL.");
            }

            EnsureSecureSignalRUri(signalRUri);
            return negotiateResponse;
        }
        catch (OperationCanceledException exception) when (
            !cancellationToken.IsCancellationRequested
            && timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Socket Mode negotiate timed out after {_timeout}.",
                exception);
        }
    }

    /// <summary>
    /// Ensures that a negotiate URI uses an allowed scheme and host.
    /// </summary>
    private static void EnsureSecureNegotiateUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "Socket Mode negotiate URI must be absolute.",
                nameof(uri));
        }

        if (IsSecureOrLoopback(uri))
        {
            return;
        }

        throw new ArgumentException(
            "Socket Mode negotiate URI must use HTTPS unless it targets loopback.",
            nameof(uri));
    }

    /// <summary>
    /// Ensures that a negotiated SignalR URI uses an allowed scheme and host.
    /// </summary>
    private static void EnsureSecureSignalRUri(Uri uri)
    {
        if (IsSecureOrLoopback(uri))
        {
            return;
        }

        throw new InvalidDataException(
            "Socket Mode negotiate response SignalR URL must use HTTPS unless it targets loopback.");
    }

    /// <summary>
    /// Determines whether a URI is secure or an HTTP loopback endpoint.
    /// </summary>
    private static bool IsSecureOrLoopback(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        string host = uri.Host.Trim('[', ']');
        bool isLoopback =
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal);

        return uri.Scheme == Uri.UriSchemeHttp && isLoopback;
    }

    /// <summary>
    /// Gets the server-provided retry delay.
    /// </summary>
    private TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        RetryConditionHeaderValue? retryAfter = response.Headers.RetryAfter;
        TimeSpan? delay = retryAfter?.Delta;

        if (delay is null && retryAfter?.Date is DateTimeOffset retryDate)
        {
            delay = retryDate - _timeProvider.GetUtcNow();
        }

        return delay is null || delay >= TimeSpan.Zero
            ? delay
            : TimeSpan.Zero;
    }
}

/// <summary>
/// Represents an unsuccessful Socket Mode negotiate response.
/// </summary>
[SuppressMessage(
   "Design",
   "CA1032:Implement standard exception constructors",
   Justification = "This internal transport exception is constructed only from an HTTP status and Retry-After value.")]
[SuppressMessage(
    "Design",
    "CA1064:Exceptions should be public",
    Justification = "Socket Mode transport errors are internal implementation details, not public SDK contracts.")]
internal sealed class SocketModeNegotiateException : Exception
{
    /// <summary>
    /// Initializes a Socket Mode negotiation exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status returned by the negotiate endpoint.</param>
    /// <param name="retryAfter">The server-provided retry delay, when available.</param>
    internal SocketModeNegotiateException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter)
        : base($"Socket Mode negotiate failed with HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the HTTP status returned by the negotiate endpoint.
    /// </summary>
    internal HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the server-provided retry delay, when available.
    /// </summary>
    internal TimeSpan? RetryAfter { get; }
}
