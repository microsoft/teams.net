// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Accessor for the uploaded files on the current inbound activity, exposed as <c>ctx.Files</c>. Reads the files attached to the current inbound activity and exposes them as lazy <see cref="IncomingFile"/> handles.
/// <para>"Files" is the uploaded-file view over the raw <c>ctx.Activity.Attachments</c> list. Uploaded files arrive as attachments where <c>ContentType</c> is <c>file.download.info</c>, carrying file metadata (a <c>downloadUrl</c> plus identifiers) rather than the bytes themselves, which are fetched from that URL. This accessor maps each to an <see cref="IncomingFile"/>, and skips everything else in <c>Attachments</c> (adaptive cards, mentions, other non-file content) as well as malformed file entries, never throwing. For each file it returns, the original wire attachment (the metadata object, not the bytes) is retained on <see cref="IncomingFile.Raw"/>. A malformed or non-file attachment is reachable only through the raw <c>Activity.Attachments</c> list.</para>
/// <para>This covers the file-upload path, not "any uploaded media". What matters is how the content arrived, not the file's MIME type, so file <em>type</em> is unrestricted (pdf, docx, png, etc.) as long as it was sent as an uploaded file. An image sent as a file appears here, but the same image pasted inline does not.</para>
/// </summary>
public sealed class FilesAccessor
{
    private readonly TeamsActivity _activity;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FilesAccessor"/> class for the given inbound activity.</summary>
    /// <param name="activity">The inbound activity whose attachments are read.</param>
    /// <param name="logger">Logger used to leave a breadcrumb when a malformed file attachment is skipped.</param>
    public FilesAccessor(TeamsActivity activity, ILogger logger)
    {
        _activity = activity;
        _logger = logger;
    }

    /// <summary>
    /// The files attached to the current inbound activity. Async because later scopes hydrate through Graph; the personal path resolves synchronously from the activity but keeps the async signature so the shape never breaks.
    /// <para>Currently takes no filters and returns only uploaded files. The signature is reserved to grow options later so coverage can widen opt-in without a break; the default stays narrow.</para>
    /// </summary>
    /// <param name="cancellationToken">A token to cancel hydration once later scopes fetch through Graph.</param>
    public Task<IList<IncomingFile>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Uploaded files only ride on inbound message activities so we validate the shape and return an empty list rather than throwing.
        if (_activity is not MessageActivity message)
        {
            return Task.FromResult<IList<IncomingFile>>([]);
        }

        IList<TeamsAttachment> attachments = message.Attachments ?? [];
        ConversationType scope = DetectScope();

        List<IncomingFile> files = [];

        for (int index = 0; index < attachments.Count; index++)
        {
            IncomingFile? file = ToIncomingFile(attachments[index], index, scope);

            if (file is not null)
            {
                files.Add(file);
            }
        }

        return Task.FromResult<IList<IncomingFile>>(files);
    }

    /// <summary>Convenience: the first attached file, or <c>null</c> when none. Sugar over <see cref="ListAsync"/>[0]; shares <see cref="ListAsync"/>'s resolution so it stays correct when later scopes hydrate through Graph.</summary>
    /// <param name="cancellationToken">A token to cancel hydration once later scopes fetch through Graph.</param>
    public async Task<IncomingFile?> FirstAsync(CancellationToken cancellationToken = default)
    {
        IList<IncomingFile> files = await ListAsync(cancellationToken).ConfigureAwait(false);
        return files.Count > 0 ? files[0] : null;
    }

    /// <summary>Derive the conversation scope from the inbound activity.</summary>
    private ConversationType DetectScope()
        => _activity.Conversation?.ConversationType ?? ConversationType.Personal;

    /// <summary>Map a single activity attachment to an <see cref="IncomingFile"/>, or <c>null</c> when the attachment is not an uploaded file or is malformed. Never throws: unusable attachments are skipped so one bad entry cannot drop the rest.</summary>
    private IncomingFile? ToIncomingFile(TeamsAttachment attachment, int index, ConversationType scope)
    {
        // Not an uploaded file (card, mention, adaptive card, etc.). Silently ignored.
        if (!attachment.ContentType.Equals(AttachmentContentType.FileDownloadInfo))
        {
            return null;
        }

        FileDownloadInfo? content = AsFileDownloadInfo(attachment.Content);
        Uri? downloadUrl = content?.DownloadUrl;
        string? name = attachment.Name;

        // A `file.download.info` without fetchable URL or name cannot be turned into a usable handle. Skip it and leave a breadcrumb rather than throwing.
        if (downloadUrl is null || string.IsNullOrEmpty(name))
        {
            _logger.LogDebug(
                "files: skipping file.download.info attachment at index {Index}; missing {Missing}",
                index,
                string.IsNullOrEmpty(name) ? "name" : "downloadUrl");
            return null;
        }

        return new IncomingFile
        {
            UniqueId = content?.UniqueId,
            Name = name,
            // `fileType` is the platform-supplied extension (e.g. `pdf`); left null when the wire omits it, matching how peer SDKs surface it.
            Extension = content?.FileType,
            Scope = scope,
            Source = FileSource.BotActivity,
            // Maps the wire's `contentUrl` (a browsable link to the file in OneDrive/SharePoint) to `WebUrl`; not fetchable like `downloadUrl`.
            WebUrl = attachment.ContentUrl,
            Raw = attachment,
            DownloadUrl = downloadUrl,
        };
    }

    /// <summary>Coerce an attachment's <c>Content</c> (already-typed, a <see cref="JsonElement"/> from the wire, or another object) into a <see cref="FileDownloadInfo"/>. Returns <c>null</c> on malformed content so the caller can skip it rather than throw.</summary>
    private static FileDownloadInfo? AsFileDownloadInfo(object? content)
    {
        try
        {
            return content switch
            {
                null => null,
                FileDownloadInfo info => info,
                JsonElement element => element.Deserialize<FileDownloadInfo>(),
                _ => JsonSerializer.SerializeToElement(content).Deserialize<FileDownloadInfo>(),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
