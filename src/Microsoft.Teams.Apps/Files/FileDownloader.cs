// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// A freshly opened, single-consumption byte stream plus the metadata resolved while opening it. Owns the underlying
/// <see cref="HttpResponseMessage"/>; disposing this stream releases the response and connection. Read-only and
/// non-seekable: it hands back the raw response body without leaking the response.
/// </summary>
public sealed class OpenedFileStream : Stream
{
    private readonly Stream _inner;
    private readonly HttpResponseMessage? _response;

    /// <summary>Initializes a new instance of the <see cref="OpenedFileStream"/> class. Internal: instances are produced by <see cref="FileDownloader"/>, which owns tying the response's lifetime to the stream.</summary>
    /// <param name="inner">The response body stream to wrap.</param>
    /// <param name="sourceUrl">The URL the bytes were fetched from.</param>
    /// <param name="contentType">MIME type resolved while opening.</param>
    /// <param name="response">The response whose lifetime is tied to this stream.</param>
    internal OpenedFileStream(Stream inner, Uri sourceUrl, string contentType, HttpResponseMessage? response = null)
    {
        _inner = inner;
        SourceUrl = sourceUrl;
        ContentType = contentType;
        _response = response;
    }

    /// <summary>The URL the bytes were actually fetched from.</summary>
    public Uri SourceUrl { get; }

    /// <summary>MIME type resolved from the response, falling back to the incoming file's.</summary>
    public string ContentType { get; }

    /// <inheritdoc />
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    /// <inheritdoc />
    public override int Read(Span<byte> buffer) => _inner.Read(buffer);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
            _response?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync().ConfigureAwait(false);
        _response?.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Opens byte streams for inbound files, keyed on conversation scope so every scope's receive path extends this one place rather than branching in callers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FileDownloader"/> class.
/// </remarks>
/// <param name="httpClient">Client used to fetch file bytes. Supplied by DI; must not be null.</param>
/// <param name="logger">Logger used to record which route produced the bytes. Which route ran is otherwise invisible from outside, and the two routes fail in different ways, so a developer diagnosing a download has no way to tell them apart without it.</param>
public sealed class FileDownloader(HttpClient httpClient, ILogger<FileDownloader>? logger = null)
{
    // Shared by apps built outside DI, where there is no factory to ask. Static so it is created once and never
    // disposed, which is the supported lifetime for a long-lived HttpClient.
    private static readonly HttpClient SharedClient = new();

    /// <summary>How much of an error body to keep.</summary>
    private const int ErrorBodyLimit = 2048;

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger _logger = logger ?? NullLogger<FileDownloader>.Instance;

    /// <summary>
    /// A downloader backed by a process-wide <see cref="HttpClient"/>, for apps constructed directly rather than through the hosting extensions.
    /// Prefer the DI-registered typed client.
    /// </summary>
    internal static FileDownloader CreateDefault() => new(SharedClient);

    /// <summary>Open a byte stream for an inbound file. Only <c>personal</c> is implemented; other scopes throw <see cref="FileScopeNotSupportedException"/> until their Graph receive path lands.</summary>
    public Task<OpenedFileStream> OpenFileStreamAsync(
        ConversationType? scope,
        Uri? downloadUrl,
        string? contentType,
        bool priorFetchSucceeded,
        CancellationToken cancellationToken)
        => OpenFileStreamAsync(scope, downloadUrl, contentUrl: null, contentType, priorFetchSucceeded, credential: null, cancellationToken);

    /// <summary>
    /// Open a byte stream for an inbound file, resolving it through Microsoft Graph when the pre-authorized download URL is absent or has lapsed.
    /// <para>Only <c>personal</c> is implemented; other scopes throw <see cref="FileScopeNotSupportedException"/> until their Graph receive path lands.</para>
    /// </summary>
    /// <param name="scope">Conversation scope; the dispatcher is keyed on this.</param>
    /// <param name="downloadUrl">Short-lived, pre-authorized download URL (personal scope).</param>
    /// <param name="contentUrl">Browsable URL to the item, used as the Graph sharing locator when no <paramref name="downloadUrl"/> is present.</param>
    /// <param name="contentType">MIME type reported by the incoming file, used as a fallback when the response omits one.</param>
    /// <param name="priorFetchSucceeded">Whether an earlier fetch through the same handle already succeeded.</param>
    /// <param name="credential">Credential used for the Graph fetch path. Absent means no Graph route is available, so an expired URL cannot be recovered.</param>
    /// <param name="cancellationToken">A token to cancel opening the stream.</param>
    public Task<OpenedFileStream> OpenFileStreamAsync(
        ConversationType? scope,
        Uri? downloadUrl,
        Uri? contentUrl,
        string? contentType,
        bool priorFetchSucceeded,
        GraphCredential? credential,
        CancellationToken cancellationToken)
    {
        if (scope == ConversationType.Personal)
        {
            return OpenPersonalFileStreamAsync(downloadUrl, contentUrl, contentType, priorFetchSucceeded, credential, cancellationToken);
        }

        throw new FileScopeNotSupportedException(scope);
    }

    private async Task<OpenedFileStream> OpenPersonalFileStreamAsync(
        Uri? downloadUrl,
        Uri? contentUrl,
        string? contentType,
        bool priorFetchSucceeded,
        GraphCredential? credential,
        CancellationToken cancellationToken)
    {
        // The Agentic User case: a browsable `contentUrl` arrives in place of a `downloadUrl`, so Graph is the only route to the bytes.
        if (downloadUrl is null)
        {
            if (contentUrl is null)
            {
                throw new InvalidOperationException("cannot download personal file: no download URL is available");
            }

            return await OpenGraphFileStreamAsync(contentUrl, contentType, credential, cancellationToken).ConfigureAwait(false);
        }

        if (!downloadUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("cannot download file: download URL must use https");
        }

        // Plain GET with no bearer token: the download URL embeds its own `tempauth` credential, and attaching a credential can get the request rejected.
        // Authentication belongs to the branch, never to the downloader: the Graph branch below sets the header on its own request, so a shared client stays credential-free.
        // This is also why file auth is not a DelegatingHandler, which is the usual shape. A handler authenticates every request on its client, and this one must send nothing at all.
        using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);
        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            int status = (int)response.StatusCode;

            if (status is 401 or 403)
            {
                response.Dispose();

                // Terminal. The URL carried its own credential and that credential has lapsed, and the SDK does not perform a fallback via app identity or user-delegated permissions on the developer's behalf.
                // The file has to be sent again.
                throw new FileUrlExpiredException(priorFetchSucceeded ? FileUrlExpiredReason.Reread : FileUrlExpiredReason.FirstFetch);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"failed to download file: {status} {response.ReasonPhrase}".Trim());
            }

            string resolvedContentType = response.Content.Headers.ContentType?.ToString()
                ?? contentType
                ?? "application/octet-stream";

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return new OpenedFileStream(stream, downloadUrl, resolvedContentType, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Fetch bytes by resolving the item through Graph's <c>/shares</c> endpoint.
    /// <para>Unlike the pre-authorized path, which sends no <c>Authorization</c> because that URL carries its own credential, this is an ordinary authenticated Graph call and fails without a bearer token.</para>
    /// </summary>
    /// <param name="sharingUrl">Browsable URL to the item, used as the Graph sharing locator.</param>
    /// <param name="contentType">MIME type reported by the incoming file, used as a fallback when the response omits one.</param>
    /// <param name="credential">Credential used for the Graph fetch.</param>
    /// <param name="cancellationToken">A token to cancel opening the stream.</param>
    private async Task<OpenedFileStream> OpenGraphFileStreamAsync(
        Uri sharingUrl,
        string? contentType,
        GraphCredential? credential,
        CancellationToken cancellationToken)
    {
        FileActor? actor = credential?.Actor;
        (string? token, string? tokenFailure) = await TryResolveTokenAsync(credential, cancellationToken).ConfigureAwait(false);

        // Detectable before any HTTP call, so a missing consent names itself instead of arriving as an opaque Graph 401.
        if (token is null || CarriesNoGraphPermissions(token))
        {
            // An acquisition that threw is not the same as an identity with no permissions, and the guidance for one is wrong for the other, so the cause is carried rather than dropped.
            throw new FileRetrievalException(FileRetrievalFailureReason.NoGraphCredential, actor, tokenFailure);
        }

        Uri url = GraphShare.BuildDriveItemContentUrl(sharingUrl, credential?.BaseUrlRoot);
        _logger.LogDebug("files: resolving bytes through Graph /shares as '{Actor}'", actor);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            int status = (int)response.StatusCode;

            if (status is 401 or 403)
            {
                // An unconsented scope, a file never shared with this identity, and a drive item that does not exist are all 403, differing only in message text.
                // The SDK cannot branch on that, but the developer can read it, so it is carried rather than dropped.
                throw new FileRetrievalException(
                    FileRetrievalFailureReason.AccessDenied,
                    actor,
                    await ReadServiceErrorAsync(response, cancellationToken).ConfigureAwait(false));
            }

            if (!response.IsSuccessStatusCode)
            {
                // Carry Graph's own text and name the identity, as the 401/403 arm does.
                // An unexpected status here is often diagnosable only from the service message, so dropping it leaves a bare status code.
                string? details = await ReadServiceErrorAsync(response, cancellationToken).ConfigureAwait(false);
                string message = $"failed to download file through Graph as '{actor}': {status} {response.ReasonPhrase}".Trim();
                throw new HttpRequestException(string.IsNullOrEmpty(details) ? message : $"{message} ({details})");
            }

            string resolvedContentType = response.Content.Headers.ContentType?.ToString()
                ?? contentType
                ?? "application/octet-stream";

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return new OpenedFileStream(stream, url, resolvedContentType, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Resolve a Graph token without throwing.
    /// <para>On the expiry path the caller already holds a more precise error, so an acquisition failure must leave it intact rather than surfacing as an unrelated exception.</para>
    /// </summary>
    private async Task<(string? Token, string? Failure)> TryResolveTokenAsync(GraphCredential? credential, CancellationToken cancellationToken)
    {
        if (credential is null)
        {
            return (null, null);
        }

        try
        {
            string? token = await credential.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            return (string.IsNullOrEmpty(token) ? null : token, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // any acquisition failure degrades to "no token", by design
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogDebug("files: could not acquire a Graph token: {Message}", ex.Message);
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Pull the human-readable part out of a Graph error envelope, falling back to the raw text.
    /// <para>Graph replies <c>{ "error": { "code", "message" } }</c>, but a 401 can also come from the edge as HTML, so this must not assume JSON.
    /// Bounded because it runs on a stream the SDK does not size, and lands in an exception message.</para>
    /// </summary>
    private static async Task<string?> ReadServiceErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string raw;

        try
        {
            Stream body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            byte[] buffer = new byte[ErrorBodyLimit];
            int read = 0;

            while (read < buffer.Length)
            {
                int chunk = await body.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken).ConfigureAwait(false);

                if (chunk == 0)
                {
                    break;
                }

                read += chunk;
            }

            raw = Encoding.UTF8.GetString(buffer, 0, read);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // diagnostics must never mask the error being raised
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }

        try
        {
            using JsonDocument envelope = JsonDocument.Parse(raw);

            if (envelope.RootElement.ValueKind == JsonValueKind.Object
                && envelope.RootElement.TryGetProperty("error", out JsonElement error)
                && error.ValueKind == JsonValueKind.Object)
            {
                string?[] parts =
                [
                    error.TryGetProperty("code", out JsonElement code) ? code.ToString() : null,
                    error.TryGetProperty("message", out JsonElement message) ? message.ToString() : null,
                ];

                string joined = string.Join(": ", parts.Where(p => !string.IsNullOrEmpty(p)));

                if (!string.IsNullOrEmpty(joined))
                {
                    return Truncate(joined);
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON. The raw text is still better than nothing.
        }

        return Truncate(raw);
    }

    private static string? Truncate(string text)
    {
        string collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (collapsed.Length == 0)
        {
            return null;
        }

        return collapsed.Length > ErrorBodyLimit ? $"{collapsed[..ErrorBodyLimit]}..." : collapsed;
    }

    /// <summary>
    /// The permissions a token carries, as a flat list, or <c>null</c> when the token is not a decodable JWT.
    /// <para>An app-only token lists application permissions in <c>roles</c>; a delegated or agentic-user token lists scopes in <c>scp</c>, space-delimited and absent entirely when there are none. <c>null</c> and an empty list mean different things and both callers depend on the difference: undecodable is "cannot tell", empty is "decoded, and there is nothing there".</para>
    /// </summary>
    private static List<string>? PermissionsOf(string token)
    {
        try
        {
            string[] segments = token.Split('.');

            if (segments.Length < 2 || segments[1].Length == 0)
            {
                return null;
            }

            using JsonDocument claims = JsonDocument.Parse(DecodeBase64Url(segments[1]));

            if (claims.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            List<string> permissions = [];

            if (claims.RootElement.TryGetProperty("roles", out JsonElement roles) && roles.ValueKind == JsonValueKind.Array)
            {
                permissions.AddRange(roles.EnumerateArray()
                    .Where(r => r.ValueKind == JsonValueKind.String)
                    .Select(r => r.GetString()!));
            }

            if (claims.RootElement.TryGetProperty("scp", out JsonElement scp) && scp.ValueKind == JsonValueKind.String)
            {
                permissions.AddRange(scp.GetString()!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            }

            return permissions;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    /// <summary>
    /// Decode a base64url segment. Written out longhand because <c>System.Buffers.Text.Base64Url</c> exists only on net10.0 and later while this package also targets net8.0.
    /// </summary>
    private static byte[] DecodeBase64Url(string segment)
    {
        string padded = segment.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '='));
    }

    /// <summary>
    /// Detect a credential that cannot reach a drive item, before spending a request to find out.
    /// <para>Real Graph answers such a token with a <c>401 generalException</c> or a <c>403</c>, indistinguishable on the wire from a genuine denial but fixed in the identity's consented permissions rather than in file sharing.</para>
    /// <para>The predicate is "carries no file-capable permission", not "carries none at all". Emptiness was sufficient while the app rung was the concern, because an app registration with no Graph permissions really does return <c>roles: []</c>. It is not sufficient for an Agentic User under <c>.default</c>, where a blueprint consented to unrelated scopes such as <c>Mail.Send</c> returns a populated <c>scp</c> that passes an emptiness check and then fails late as an ambiguous 403.</para>
    /// <para>Any <c>Files.*</c> or <c>Sites.*</c> permission is admitted, deliberately generously. <c>Sites.Selected</c> is a known false positive: it begins with <c>Sites.</c> but grants nothing until an admin allowlists specific sites. A false positive degrades to the previous behaviour of calling Graph and reporting what it says, which is the safe direction to be wrong in.</para>
    /// <para>Fails open on an undecodable token, which proceeds to the call rather than blocking a fetch that might have worked.</para>
    /// </summary>
    private static bool CarriesNoGraphPermissions(string token)
    {
        IReadOnlyList<string>? permissions = PermissionsOf(token);

        return permissions is not null
            && !permissions.Any(p =>
                p.StartsWith("Files.", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Sites.", StringComparison.OrdinalIgnoreCase));
    }

}
