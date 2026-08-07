// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// A freshly opened, single-consumption byte stream plus the metadata resolved while opening it. Owns the underlying <see cref="HttpResponseMessage"/>; disposing this stream releases the response and connection. Read-only and non-seekable: it hands back the raw response body without leaking the response.
/// </summary>
internal sealed class OpenedFileStream : Stream
{
    private readonly Stream _inner;
    private readonly HttpResponseMessage? _response;

    public OpenedFileStream(Stream inner, Uri sourceUrl, string contentType, HttpResponseMessage? response = null)
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
internal static class FileDownload
{
    private static readonly HttpClient DefaultHttpClient = new();

    /// <summary>Open a byte stream for an inbound file. Only <c>personal</c> is implemented; other scopes throw <see cref="FileScopeNotSupportedException"/> until their Graph receive path lands.</summary>
    public static Task<OpenedFileStream> OpenFileStreamAsync(
        ConversationType? scope,
        Uri? downloadUrl,
        string? contentType,
        bool priorFetchSucceeded,
        HttpClient? httpClient,
        CancellationToken cancellationToken)
    {
        if (scope == ConversationType.Personal)
        {
            return OpenPersonalFileStreamAsync(downloadUrl, contentType, priorFetchSucceeded, httpClient, cancellationToken);
        }

        throw new FileScopeNotSupportedException(scope);
    }

    private static async Task<OpenedFileStream> OpenPersonalFileStreamAsync(
        Uri? downloadUrl,
        string? contentType,
        bool priorFetchSucceeded,
        HttpClient? httpClient,
        CancellationToken cancellationToken)
    {
        if (downloadUrl is null)
        {
            throw new InvalidOperationException("cannot download personal file: no download URL is available");
        }

        HttpClient client = httpClient ?? DefaultHttpClient;

        // Plain GET with no bearer token: the download URL embeds its own `tempauth` credential, and attaching a
        // credential can get the request rejected.
        HttpResponseMessage response = await client
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

            return new OpenedFileStream(stream, downloadUrl, resolvedContentType, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
