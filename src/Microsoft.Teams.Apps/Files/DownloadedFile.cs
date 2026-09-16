// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// A buffered, point-in-time snapshot of a downloaded file's bytes that the caller owns.
/// <para>Returned by <see cref="IncomingFile.DownloadAsync"/>. The bytes are already in memory, so the convenience readers here are synchronous and never re-download. Because it is a snapshot, holding one and reusing it is the way to read the same file several ways without re-fetching through the live <see cref="IncomingFile"/> handle.</para>
/// </summary>
public sealed class DownloadedFile
{
    internal DownloadedFile(byte[] bytes, string contentType, string filename, Uri sourceUrl)
    {
        _bytes = bytes;
        ContentType = contentType;
        Filename = filename;
        SourceUrl = sourceUrl;
    }

    private readonly byte[] _bytes;

    /// <summary>The file bytes, buffered from the download stream read to completion.</summary>
    // Intentionally exposes the raw buffer (opaque byte blob, not a logical collection); byte[] matches the repo's byte-payload convention.
#pragma warning disable CA1819 // Properties should not return arrays
    public byte[] Bytes => _bytes;
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>MIME type resolved from the download response header, or the incoming file's metadata type if the response omits one. Falls back to <c>application/octet-stream</c> when neither provides a type, so this is never empty.</summary>
    public string ContentType { get; }

    /// <summary>Resolved filename.</summary>
    public string Filename { get; }

    /// <summary>The URL the bytes were actually fetched from.</summary>
    public Uri SourceUrl { get; }

    /// <summary>Decode bytes as UTF-8 (or a provided encoding). No content-type check. Lossy: invalid bytes become the U+FFFD replacement character and never throw. For strict or binary-safe reads, use <see cref="Bytes"/>.</summary>
    /// <param name="encoding">Encoding to decode with. Defaults to UTF-8.</param>
    public string Text(Encoding? encoding = null) => (encoding ?? Encoding.UTF8).GetString(_bytes);

    /// <summary>Write the already-buffered bytes to a local file path (no re-fetch, unlike <see cref="IncomingFile.SaveAsAsync"/> which streams a fresh download).</summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    public Task SaveAsAsync(string path, CancellationToken cancellationToken = default)
        => File.WriteAllBytesAsync(path, _bytes, cancellationToken);
}
