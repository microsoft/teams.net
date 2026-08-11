// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// A freshly opened, single-consumption byte stream plus the metadata resolved while opening it. Disposing releases
/// the underlying response and connection.
/// </summary>
/// <param name="Stream">The response body. Read-only, non-seekable, and owns the underlying response.</param>
/// <param name="SourceUrl">The URL the bytes were actually fetched from.</param>
/// <param name="ContentType">MIME type resolved from the response, falling back to the incoming file's when the response omits one.</param>
internal sealed record OpenedFile(Stream Stream, Uri SourceUrl, string ContentType) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}

/// <summary>
/// Wraps a response body stream so that disposing it also disposes the <see cref="HttpResponseMessage"/> it was read
/// from, releasing the connection. Carries no metadata: it exists only to tie the two lifetimes together, so the
/// response can be handed to a caller as a plain <see cref="Stream"/> without leaking.
/// </summary>
internal sealed class ResponseOwningStream : Stream
{
    private readonly Stream _inner;
    private readonly HttpResponseMessage? _response;

    public ResponseOwningStream(Stream inner, HttpResponseMessage? response = null)
    {
        _inner = inner;
        _response = response;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => _inner.Read(buffer);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, cancellationToken);

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
            _response?.Dispose();
        }

        base.Dispose(disposing);
    }

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
internal sealed class FileDownloader(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>Open a byte stream for an inbound file. Only <c>personal</c> is implemented; other scopes throw <see cref="FileScopeNotSupportedException"/> until their Graph receive path lands.</summary>
    public Task<OpenedFile> OpenFileStreamAsync(
        ConversationType? scope,
        Uri? downloadUrl,
        string? contentType,
        bool priorFetchSucceeded,
        CancellationToken cancellationToken)
    {
        if (scope == ConversationType.Personal)
        {
            return OpenPersonalFileStreamAsync(downloadUrl, contentType, priorFetchSucceeded, cancellationToken);
        }

        throw new FileScopeNotSupportedException(scope);
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Ownership of the stream transfers to the returned OpenedFile, which the caller disposes. The catch below disposes the response if construction fails.")]
    private async Task<OpenedFile> OpenPersonalFileStreamAsync(
        Uri? downloadUrl,
        string? contentType,
        bool priorFetchSucceeded,
        CancellationToken cancellationToken)
    {
        if (downloadUrl is null)
        {
            throw new InvalidOperationException("cannot download personal file: no download URL is available");
        }

        if (!downloadUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("cannot download file: download URL must use https");
        }

        // Plain GET with no bearer token: the download URL embeds its own `tempauth` credential, and attaching a
        // credential can get the request rejected.
        HttpResponseMessage response = await _httpClient
            .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            int status = (int)response.StatusCode;

            if (status is 401 or 403)
            {
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

            return new OpenedFile(new ResponseOwningStream(stream, response), downloadUrl, resolvedContentType);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
