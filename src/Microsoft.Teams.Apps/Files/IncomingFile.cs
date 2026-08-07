// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// A lazy handle to a file attached to the current inbound activity.
/// <para>Nothing is downloaded until a byte method is called. The handle stays live and holds no memoized bytes, so each of <see cref="StreamAsync"/>/<see cref="DownloadAsync"/>/<see cref="TextAsync"/>/<see cref="SaveAsAsync"/> fetches afresh. For a personal file that re-fetch is bounded by the short-lived download URL lifetime and may hit its expiry; to read the same file several ways, call <see cref="DownloadAsync"/> once and reuse the returned <see cref="DownloadedFile"/>.</para>
/// </summary>
public sealed class IncomingFile
{
    /// <summary>The OneDrive/ODSP drive-item id when the platform reports it (<c>content.uniqueId</c>); the storage-specific locator a Graph fetch keys off. Present only when the wire provided it.</summary>
    public string? UniqueId { get; init; }

    /// <summary>Display name including extension when known.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The file's MIME type, when Teams provides it with the file. Files received today do not include one, so
    /// this is usually <c>null</c>; the type resolved from the download response is on the returned
    /// <see cref="DownloadedFile"/>, not written back here.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>File extension without the dot (e.g. <c>pdf</c>), taken from the platform-supplied <c>fileType</c>. Absent when the wire omits it.</summary>
    public string? Extension { get; init; }

    /// <summary>Conversation scope the file arrived in (the SDK's <see cref="ConversationType"/>).</summary>
    public required ConversationType Scope { get; init; }

    /// <summary>Where the SDK found the file. Only <see cref="FileSource.BotActivity"/> is produced today.</summary>
    public required FileSource Source { get; init; }

    /// <summary>Web URL to the file in OneDrive/SharePoint when known.</summary>
    public Uri? WebUrl { get; init; }

    /// <summary>The raw underlying attachment/graph object for escape-hatch access.</summary>
    public object? Raw { get; init; }

    /// <summary>Short-lived, pre-authorized download URL (personal scope).</summary>
    internal Uri? DownloadUrl { get; init; }

    /// <summary>Injectable HTTP client, defaulting to a shared client; used to keep tests off the network.</summary>
    internal HttpClient? HttpClient { get; init; }

    private bool _priorFetchSucceeded;

    /// <summary>Stream the bytes. Low-level primitive: returns the response body stream directly, single-consumption, not buffered or retained. Use for large files and pipelines (parse-as-you-go, pipe to disk). <see cref="DownloadAsync"/> is built on this. Uncapped: the consumer bounds it by how much it reads. Dispose the returned stream to release the underlying connection.</summary>
    /// <param name="cancellationToken">A token to cancel opening the stream.</param>
    public async Task<Stream> StreamAsync(CancellationToken cancellationToken = default)
    {
        OpenedFileStream opened = await FileDownload
            .OpenFileStreamAsync(Scope, DownloadUrl, ContentType, _priorFetchSucceeded, HttpClient, cancellationToken)
            .ConfigureAwait(false);
        _priorFetchSucceeded = true;
        return opened;
    }

    /// <summary>Fetch the whole file and buffer it into a <see cref="DownloadedFile"/> snapshot you own. Lazy and not memoized: calling again re-fetches. If you already hold a <see cref="DownloadedFile"/>, call its <see cref="DownloadedFile.SaveAsAsync"/> rather than this handle's, which would re-fetch.</summary>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    public async Task<DownloadedFile> DownloadAsync(CancellationToken cancellationToken = default)
    {
        OpenedFileStream opened = await FileDownload
            .OpenFileStreamAsync(Scope, DownloadUrl, ContentType, _priorFetchSucceeded, HttpClient, cancellationToken)
            .ConfigureAwait(false);
        _priorFetchSucceeded = true;

        await using (opened.ConfigureAwait(false))
        {
            using MemoryStream buffer = new();
            await opened.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

            return new DownloadedFile(buffer.ToArray(), opened.ContentType, Name, opened.SourceUrl);
        }
    }

    /// <summary>Convenience: run <see cref="DownloadAsync"/> then decode the bytes as UTF-8 (or a provided encoding). Re-fetches on each call (no memoized bytes); to read bytes several ways hold one <see cref="DownloadedFile"/> instead. No content-type check; decoding is lossy (invalid bytes become U+FFFD and never throw). For strict or binary-safe reads, use the <see cref="DownloadedFile.Bytes"/> of a <see cref="DownloadAsync"/> result.</summary>
    /// <param name="encoding">Encoding to decode with. Defaults to UTF-8.</param>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    public async Task<string> TextAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        DownloadedFile downloaded = await DownloadAsync(cancellationToken).ConfigureAwait(false);
        return downloaded.Text(encoding);
    }

    /// <summary>Stream the bytes straight to a local file path, so saving a large file never materializes it in memory.</summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="cancellationToken">A token to cancel the download or write.</param>
    public async Task SaveAsAsync(string path, CancellationToken cancellationToken = default)
    {
        OpenedFileStream opened = await FileDownload
            .OpenFileStreamAsync(Scope, DownloadUrl, ContentType, _priorFetchSucceeded, HttpClient, cancellationToken)
            .ConfigureAwait(false);
        _priorFetchSucceeded = true;

        await using (opened.ConfigureAwait(false))
        {
            FileStream file = new(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await using (file.ConfigureAwait(false))
            {
                await opened.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
