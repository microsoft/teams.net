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

    /// <summary>
    /// Whether a file that arrived with only a <c>contentUrl</c> may be read through Graph in this scope, as this actor.
    /// <para>Shared by the attachment mapper and the download dispatcher so the two cannot drift: a file <see cref="FilesAccessor.ListAsync(CancellationToken)"/> surfaces on its <c>contentUrl</c> is always one <see cref="IncomingFile.DownloadAsync"/> will open. <c>personal</c> is open to every actor. <c>groupChat</c> and <c>channel</c> are open only to an agentic user, the only identity the platform delivers files to there at this time. Any other scope stays closed.</para>
    /// </summary>
    /// <param name="scope">Conversation scope the file arrived in.</param>
    /// <param name="actor">Identity the Graph read would run as, or <c>null</c> when the turn has no credential.</param>
    internal static bool IsGraphRouteOpen(ConversationType? scope, FileActor? actor)
    {
        if (scope == ConversationType.Personal)
        {
            return true;
        }

        return actor == FileActor.AgenticUser && (scope == ConversationType.GroupChat || scope == ConversationType.Channel);
    }

    /// <summary>Open a byte stream for an inbound file through its pre-authorized download URL. Only <c>personal</c> opens this way, and every other scope throws <see cref="FileScopeNotSupportedException"/>: the Graph route that serves an agentic user's <c>groupChat</c> and <c>channel</c> files needs the <c>contentUrl</c> and credential that only the full overload takes.</summary>
    public Task<OpenedFileStream> OpenFileStreamAsync(
        ConversationType? scope,
        Uri? downloadUrl,
        string? contentType,
        bool priorFetchSucceeded,
        CancellationToken cancellationToken)
        => OpenFileStreamAsync(scope, downloadUrl, contentUrl: null, contentType, priorFetchSucceeded, credential: null, cancellationToken);

    /// <summary>
    /// Open a byte stream for an inbound file, resolving it through Microsoft Graph when the pre-authorized download URL is absent.
    /// <para><c>personal</c> takes either route. In <c>groupChat</c> and <c>channel</c> the platform delivers file attachments only to an agentic user, each carrying just a <c>contentUrl</c>, so they open only through Graph, as <c>IsGraphRouteOpen</c> decides. Everything else throws <see cref="FileScopeNotSupportedException"/>.</para>
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
        => OpenFileStreamAsync(scope, downloadUrl, contentUrl, contentType, priorFetchSucceeded, credential, attachmentStyle: null, botApiCredential: null, cancellationToken);

    /// <summary>
    /// Open a byte stream for an inbound file or inline image.
    /// <para>An <c>inlineImage</c> is read from <c>contentUrl</c> with the Bot API credential, in any scope.
    /// Every other file opens as the overload without <paramref name="attachmentStyle"/> describes.</para>
    /// </summary>
    /// <param name="scope">Conversation scope; the dispatcher is keyed on this.</param>
    /// <param name="downloadUrl">Short-lived, pre-authorized download URL (personal scope).</param>
    /// <param name="contentUrl">Browsable URL to the item, used as the Graph sharing locator when no <paramref name="downloadUrl"/> is present. For an inline image, it is the image's Bot API location instead.</param>
    /// <param name="contentType">MIME type reported by the incoming file, used as a fallback when the response omits one.</param>
    /// <param name="priorFetchSucceeded">Whether an earlier fetch through the same handle already succeeded.</param>
    /// <param name="credential">Credential used for the Graph fetch path. Absent means no Graph route is available, so an expired URL cannot be recovered.</param>
    /// <param name="attachmentStyle">How the file was attached; see <see cref="AttachmentStyle"/>. Defaults to <c>file</c>.</param>
    /// <param name="botApiCredential">Credential used for inline images. Absent means inline images cannot be downloaded.</param>
    /// <param name="cancellationToken">A token to cancel opening the stream.</param>
    public Task<OpenedFileStream> OpenFileStreamAsync(
        ConversationType? scope,
        Uri? downloadUrl,
        Uri? contentUrl,
        string? contentType,
        bool priorFetchSucceeded,
        GraphCredential? credential,
        AttachmentStyle? attachmentStyle,
        BotApiCredential? botApiCredential,
        CancellationToken cancellationToken)
    {
        // Inline images live on the Bot API, so neither file route nor its scope restriction applies.
        if (attachmentStyle == AttachmentStyle.InlineImage)
        {
            return OpenInlineImageStreamAsync(contentUrl, contentType, botApiCredential, cancellationToken);
        }

        if (scope == ConversationType.Personal)
        {
            return OpenPersonalFileStreamAsync(downloadUrl, contentUrl, contentType, priorFetchSucceeded, credential, cancellationToken);
        }

        // A `downloadUrl` outside `personal` keeps the scope error: that route is unvalidated there, and Graph is never substituted for a URL the platform supplied.
        if (downloadUrl is null && contentUrl is not null && IsGraphRouteOpen(scope, credential?.Actor))
        {
            return OpenGraphFileStreamAsync(contentUrl, contentType, credential, cancellationToken);
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

        EnsureFinalHopIsHttps(response);

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
            throw new FileCredentialException(actor, tokenFailure);
        }

        Uri url = GraphShare.BuildDriveItemContentUrl(sharingUrl, credential?.BaseUrlRoot);
        _logger.LogDebug("files: resolving bytes through Graph /shares as '{Actor}'", actor);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        EnsureFinalHopIsHttps(response);

        try
        {
            int status = (int)response.StatusCode;

            if (status is 401 or 403)
            {
                // The status is carried rather than collapsed: a 401 means the token itself was rejected and a 403 means the identity lacks the grant, and those have different remedies.
                // Within a 403, an unconsented scope and a file never shared are still indistinguishable, so the service's own text is carried too.
                throw new FileAccessException(
                    status,
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
    /// Whether <paramref name="url"/> is under <paramref name="serviceUrl"/>: both https, the same origin, and a path starting with the service URL's path.
    /// <para>Both are parsed first, so dot segments are resolved before the comparison and cannot climb out of the service URL's path.</para>
    /// </summary>
    internal static bool IsUnderServiceUrl(Uri? url, Uri? serviceUrl)
    {
        if (url is null || serviceUrl is null || !url.IsAbsoluteUri || !serviceUrl.IsAbsoluteUri)
        {
            return false;
        }

        if (!string.Equals(serviceUrl.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            || Uri.Compare(url, serviceUrl, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return false;
        }

        // Normalizes to exactly one trailing `/` so a path such as `/amer` cannot match `/americas`.
        // Paths are case-sensitive, so only the hex digits of percent escapes such as `%2f` are compared without regard to case.
        string basePath = $"{UpperCaseEscapes(serviceUrl.AbsolutePath).TrimEnd('/')}/";
        return UpperCaseEscapes(url.AbsolutePath).StartsWith(basePath, StringComparison.Ordinal);
    }

    // Upper-cases the two hex digits after each `%`, since a parsed path keeps the case of escapes it does not decode.
    private static string UpperCaseEscapes(string path)
    {
        if (!path.Contains('%', StringComparison.Ordinal))
        {
            return path;
        }

        char[] chars = path.ToCharArray();

        for (int i = 0; i < chars.Length - 2; i++)
        {
            if (chars[i] == '%' && Uri.IsHexDigit(chars[i + 1]) && Uri.IsHexDigit(chars[i + 2]))
            {
                chars[i + 1] = char.ToUpperInvariant(chars[i + 1]);
                chars[i + 2] = char.ToUpperInvariant(chars[i + 2]);
                i += 2;
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Fetch an inline image's bytes from the Bot API with the turn's Bot API token.
    /// <para>The token is sent only to a URL under <see cref="BotApiCredential.ServiceUrl"/>.</para>
    /// </summary>
    private async Task<OpenedFileStream> OpenInlineImageStreamAsync(
        Uri? url,
        string? contentType,
        BotApiCredential? credential,
        CancellationToken cancellationToken)
    {
        if (url is null)
        {
            throw new InvalidOperationException("cannot download inline image: no content URL is available");
        }

        if (!url.IsAbsoluteUri || !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("cannot download inline image: content URL must use https");
        }

        if (!IsUnderServiceUrl(url, credential?.ServiceUrl))
        {
            throw new InvalidOperationException("cannot download inline image: content URL is not under the activity's service URL");
        }

        FileActor? actor = credential?.Actor;
        string? token = credential is null ? null : await credential.GetTokenAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("cannot download inline image: no Bot API token is available");
        }

        _logger.LogDebug("files: reading inline image bytes from the Bot API as '{Actor}'", actor);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        EnsureFinalHopIsHttps(response);

        try
        {
            int status = (int)response.StatusCode;

            if (status == 415)
            {
                string? body = await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false);
                throw new InlineImageTransformationException(status, ExtractServiceErrorCode(body), ExtractServiceError(body), actor);
            }

            if (status is 401 or 403)
            {
                throw new InlineImageAccessException(status, actor, await ReadServiceErrorAsync(response, cancellationToken).ConfigureAwait(false));
            }

            if (!response.IsSuccessStatusCode)
            {
                string? details = await ReadServiceErrorAsync(response, cancellationToken).ConfigureAwait(false);
                string message = $"failed to download inline image as '{actor ?? FileActor.App}': {status} {response.ReasonPhrase}".Trim();
                throw new HttpRequestException(string.IsNullOrEmpty(details) ? message : $"{message} ({details})");
            }

            string resolvedContentType = response.Content.Headers.ContentType?.ToString()
                ?? contentType
                ?? InlineImage.OctetStream;

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
    /// Refuse a response whose final hop left HTTPS.
    /// <para>Storage answers with a 302 to the host actually holding the bytes, so redirects have to be followed. They must not be followed <em>down</em> to plaintext: an <c>https</c> to <c>http</c> hop would put the file on the wire in the clear.</para>
    /// <para>Checked after the fact rather than by disabling <see cref="HttpClientHandler.AllowAutoRedirect"/>, because the downloader is handed an <see cref="HttpClient"/> it does not own and cannot reconfigure. Paired with <see cref="HttpCompletionOption.ResponseHeadersRead"/> this still runs before the body is read, so the file's bytes never cross the downgraded hop.</para>
    /// <para>The registered client follows redirects through <see cref="FileRedirectHandler"/>, which refuses the downgrade before the hop is sent and keeps <c>Authorization</c> only on the original scheme, host, and port. This check covers a client registered without it.</para>
    /// </summary>
    private static void EnsureFinalHopIsHttps(HttpResponseMessage response)
    {
        Uri? finalUri = response.RequestMessage?.RequestUri;

        if (finalUri is not null && !string.Equals(finalUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            response.Dispose();
            throw new InvalidOperationException(
                $"cannot download file: a redirect destination must use https, got '{finalUri.Scheme}://{finalUri.Host}'. The file's bytes would cross that hop in the clear.");
        }
    }

    private static async Task<string?> ReadServiceErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        => ExtractServiceError(await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false));

    /// <summary>Read up to <see cref="ErrorBodyLimit"/> bytes of an error body, or <c>null</c> when it cannot be read.</summary>
    private static async Task<string?> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
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

            return Encoding.UTF8.GetString(buffer, 0, read);
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
    }

    /// <summary>
    /// Pull the human-readable part out of a Graph error envelope, falling back to the raw text.
    /// <para>Graph replies <c>{ "error": { "code", "message" } }</c>, but a 401 can also come from the edge as HTML, so this must not assume JSON.
    /// Bounded because it runs on a stream the SDK does not size, and lands in an exception message.</para>
    /// </summary>
    private static string? ExtractServiceError(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
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

    /// <summary>The <c>error.code</c> from a JSON error envelope, or <c>null</c> when the body is not one.</summary>
    private static string? ExtractServiceErrorCode(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        try
        {
            using JsonDocument envelope = JsonDocument.Parse(raw);

            if (envelope.RootElement.ValueKind == JsonValueKind.Object
                && envelope.RootElement.TryGetProperty("error", out JsonElement error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty("code", out JsonElement code)
                && code.ValueKind == JsonValueKind.String)
            {
                string? trimmed = code.GetString()?.Trim();
                return string.IsNullOrEmpty(trimmed) ? null : trimmed;
            }
        }
        catch (JsonException)
        {
        }

        return null;
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
