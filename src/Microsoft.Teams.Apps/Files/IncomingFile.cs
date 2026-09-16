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
    private readonly FileDownloader _downloader;

    /// <summary>Initializes a new instance of the <see cref="IncomingFile"/> class.</summary>
    /// <param name="name">Display name including extension when known.</param>
    /// <param name="scope">Conversation scope the file arrived in.</param>
    /// <param name="source">Where the SDK found the file.</param>
    /// <param name="downloader">Opens the byte stream when a byte method is called.</param>
    internal IncomingFile(string name, ConversationType scope, FileSource source, FileDownloader downloader)
    {
        Name = name;
        Scope = scope;
        Source = source;
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
    }

    /// <summary>
    /// The ODSP/OneDrive identifier for the file when the platform reports it (<c>content.uniqueId</c>).
    /// Useful for correlation, dedup and logging, but not for retrieval: the Graph fetch resolves bytes from
    /// <see cref="ContentUrl"/> through <c>/shares</c>, and this value arrives as a GUID, which is a SharePoint
    /// <c>listItemUniqueId</c> shape rather than a Graph <c>driveItem.id</c>.
    /// Present only when the wire provided it.
    /// </summary>
    public string? UniqueId { get; init; }

    /// <summary>Display name including extension when known.</summary>
    public string Name { get; }

    /// <summary>
    /// The file's MIME type when the source provides one.
    /// Always <c>null</c> for <see cref="FileSource.BotActivity"/> files: a <c>file.download.info</c> attachment carries no MIME type, only the <c>fileType</c> extension surfaced as <see cref="Extension"/>.
    /// Populated for sources that do carry one, such as a <see cref="FileSource.Graph"/> drive item. To learn the type of the bytes you actually received, read <see cref="DownloadedFile.ContentType"/>, which is resolved from the download response.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>File extension without the dot (e.g. <c>pdf</c>), taken from the platform-supplied <c>fileType</c>. Absent when the wire omits it.</summary>
    public string? Extension { get; init; }

    /// <summary>Conversation scope the file arrived in (the SDK's <see cref="ConversationType"/>).</summary>
    public ConversationType Scope { get; }

    /// <summary>Where the SDK found the file. Only <see cref="FileSource.BotActivity"/> is produced today.</summary>
    public FileSource Source { get; }

    /// <summary>
    /// Browsable URL to the file in OneDrive/SharePoint, as sent on the attachment's <c>contentUrl</c>.
    /// Not fetchable for bytes despite the name, but it is the locator a Graph <c>/shares</c> resolution keys off; bytes come from <see cref="DownloadAsync"/> or <see cref="StreamAsync"/>.
    /// </summary>
    public Uri? ContentUrl { get; init; }

    /// <summary>The raw underlying attachment/graph object for escape-hatch access.</summary>
    public object? Raw { get; init; }

    /// <summary>Short-lived, pre-authorized download URL (personal scope). Scope-dependent rather than universal, so it stays an initializer: the Graph path keys off <see cref="ContentUrl"/> instead.</summary>
    internal Uri? DownloadUrl { get; init; }

    /// <summary>Graph credential for the current actor, resolved at fetch time rather than captured here.</summary>
    internal GraphCredential? Credential { get; init; }

    private bool _priorFetchSucceeded;

    /// <summary>Stream the bytes. Low-level primitive: returns the response body stream directly, single-consumption, not buffered or retained. Use for large files and pipelines (parse-as-you-go, pipe to disk). <see cref="DownloadAsync"/> is built on this. Uncapped: the consumer bounds it by how much it reads. Dispose the returned stream to release the underlying connection.</summary>
    /// <param name="cancellationToken">A token to cancel opening the stream.</param>
    public async Task<Stream> StreamAsync(CancellationToken cancellationToken = default)
    {
        OpenedFileStream opened = await _downloader
            .OpenFileStreamAsync(Scope, DownloadUrl, ContentUrl, ContentType, _priorFetchSucceeded, Credential, cancellationToken)
            .ConfigureAwait(false);
        _priorFetchSucceeded = true;
        return opened;
    }

    /// <summary>Fetch the whole file and buffer it into a <see cref="DownloadedFile"/> snapshot you own. Lazy and not memoized: calling again re-fetches. If you already hold a <see cref="DownloadedFile"/>, call its <see cref="DownloadedFile.SaveAsAsync"/> rather than this handle's, which would re-fetch.</summary>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    public async Task<DownloadedFile> DownloadAsync(CancellationToken cancellationToken = default)
    {
        OpenedFileStream opened = await _downloader
            .OpenFileStreamAsync(Scope, DownloadUrl, ContentUrl, ContentType, _priorFetchSucceeded, Credential, cancellationToken)
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
        OpenedFileStream opened = await _downloader
            .OpenFileStreamAsync(Scope, DownloadUrl, ContentUrl, ContentType, _priorFetchSucceeded, Credential, cancellationToken)
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
