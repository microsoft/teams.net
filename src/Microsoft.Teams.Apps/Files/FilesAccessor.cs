// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Accessor for the uploaded files and inline images on the current inbound activity, exposed as <c>ctx.Files</c>. Reads the files attached to the current inbound activity and exposes them as lazy <see cref="IncomingFile"/> handles.
/// "Files" is a filter over the raw <c>ctx.Activity.Attachments</c> list.
/// <list type="bullet">
/// <item><description>Uploaded files arrive with <c>ContentType</c> set to <c>file.download.info</c> and file metadata rather than the attachment's bytes themselves.
/// The metadata names where the bytes live: a pre-authorized <c>downloadUrl</c> when issued, otherwise a <c>contentUrl</c> resolvable by Graph.
/// What matters is how the content arrived, not its MIME type, so any file type (pdf, docx, png, etc.) sent as an uploaded file is listed.</description></item>
/// <item><description>Images pasted inline into the message are included, with <see cref="IncomingFile.AttachmentStyle"/> set to <see cref="AttachmentStyle.InlineImage"/>.
/// An inline image not hosted on the activity's service URL is skipped with a warning.
/// See <see cref="Files.AttachmentStyle"/> for image formats that cannot be downloaded.</description></item>
/// </list>
/// <para>This accessor maps each listed attachment to an <see cref="IncomingFile"/>, and skips other <c>Attachments</c> content (adaptive cards, mentions, etc.) and malformed file entries, never throwing.
/// For each file returned, the original wire attachment (metadata) is retained on <see cref="IncomingFile.Raw"/>.
/// A malformed or non-file attachment is reachable only through the raw <c>Activity.Attachments</c> list.</para>
/// </summary>
public sealed class FilesAccessor
{
    private readonly TeamsActivity _activity;
    private readonly ILogger _logger;
    private readonly FileDownloader _downloader;
    private readonly GraphCredential? _credential;
    private readonly BotApiCredential? _botApiCredential;

    /// <summary>Initializes a new instance of the <see cref="FilesAccessor"/> class for the given inbound activity.</summary>
    /// <param name="activity">The inbound activity whose attachments are read.</param>
    /// <param name="logger">Logger used to leave a breadcrumb when a file attachment is skipped.</param>
    /// <param name="downloader">Opens byte streams for the files this accessor hands out.</param>
    /// <param name="credential">Graph credential for the current actor, resolved at fetch time. Absent when the app has no Graph route, in which case an expired URL cannot be recovered.</param>
    /// <param name="botApiCredential">Bot API credential for reading inline images, resolved at fetch time.</param>
    internal FilesAccessor(TeamsActivity activity, ILogger logger, FileDownloader downloader, GraphCredential? credential = null, BotApiCredential? botApiCredential = null)
    {
        _activity = activity;
        _logger = logger;
        _downloader = downloader;
        _credential = credential;
        _botApiCredential = botApiCredential;
    }

    /// <summary>
    /// The files attached to the current inbound activity. Async because later scopes hydrate through Graph; the personal path resolves synchronously from the activity but keeps the async signature so the shape never breaks.
    /// <para>Returns uploaded files and inline images, and makes no network requests.
    /// Filter by attachment style using the <see cref="FileListOptions.AttachmentStyle"/> option.</para>
    /// </summary>
    /// <param name="cancellationToken">A token to cancel hydration once later scopes fetch through Graph.</param>
    public Task<IList<IncomingFile>> ListAsync(CancellationToken cancellationToken = default)
        => ListCoreAsync(attachmentStyle: null, cancellationToken);

    /// <inheritdoc cref="ListAsync(CancellationToken)"/>
    /// <param name="options">Filters applied to the listed files.</param>
    /// <param name="cancellationToken">A token to cancel hydration once later scopes fetch through Graph.</param>
    public Task<IList<IncomingFile>> ListAsync(FileListOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ListCoreAsync(options.AttachmentStyle, cancellationToken);
    }

    private Task<IList<IncomingFile>> ListCoreAsync(AttachmentStyle? attachmentStyle, CancellationToken cancellationToken)
    {
        // The personal path resolves synchronously, so honor the token up front rather than accepting one that does nothing. Returns a canceled task instead of throwing synchronously, so the exception surfaces at the await like any other async method.
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IList<IncomingFile>>(cancellationToken);
        }

        // Uploaded files only ride on inbound message activities so we validate the shape and return an empty list rather than throwing.
        if (_activity is not MessageActivity message)
        {
            return Task.FromResult<IList<IncomingFile>>([]);
        }

        IList<TeamsAttachment> attachments = message.Attachments ?? [];
        ConversationType scope = DetectScope();
        Dictionary<string, string> hints = attachmentStyle == AttachmentStyle.File ? new(StringComparer.Ordinal) : InlineImageHints(attachments);

        List<IncomingFile> files = [];

        for (int index = 0; index < attachments.Count; index++)
        {
            TeamsAttachment attachment = attachments[index];
            bool inline = IsInlineImage(attachment);

            // Checked before mapping, so an attachment of the excluded style is never mapped and never logs a warning.
            if (attachmentStyle is not null && attachmentStyle != (inline ? AttachmentStyle.InlineImage : AttachmentStyle.File))
            {
                continue;
            }

            IncomingFile? file = inline ? ToInlineImage(attachment, index, scope, hints) : ToIncomingFile(attachment, index, scope);

            if (file is not null)
            {
                files.Add(file);
            }
        }

        return Task.FromResult<IList<IncomingFile>>(files);
    }

    /// <summary>
    /// Convenience: the first listed file/inline image, or <c>null</c> when none. Sugar over <see cref="ListAsync(CancellationToken)"/>[0]; shares <see cref="ListAsync(CancellationToken)"/>'s resolution so it stays correct when later scopes hydrate through Graph.
    /// Filter by attachment style using the <see cref="FileListOptions.AttachmentStyle"/> option.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel hydration once later scopes fetch through Graph.</param>
    public async Task<IncomingFile?> FirstAsync(CancellationToken cancellationToken = default)
    {
        IList<IncomingFile> files = await ListAsync(cancellationToken).ConfigureAwait(false);
        return files.Count > 0 ? files[0] : null;
    }

    /// <inheritdoc cref="FirstAsync(CancellationToken)"/>
    /// <param name="options">Filters applied to the listed files.</param>
    /// <param name="cancellationToken">A token to cancel hydration once later scopes fetch through Graph.</param>
    public async Task<IncomingFile?> FirstAsync(FileListOptions options, CancellationToken cancellationToken = default)
    {
        IList<IncomingFile> files = await ListAsync(options, cancellationToken).ConfigureAwait(false);
        return files.Count > 0 ? files[0] : null;
    }

    /// <summary>Derive the conversation scope from the inbound activity.</summary>
    private ConversationType DetectScope()
        => _activity.Conversation?.ConversationType ?? ConversationType.Personal;

    /// <summary>
    /// Format hints for inline images, keyed by attachment id, read from the message's HTML attachments.
    /// </summary>
    private Dictionary<string, string> InlineImageHints(IList<TeamsAttachment> attachments)
    {
        Dictionary<string, string> hints = new(StringComparer.Ordinal);

        foreach (TeamsAttachment attachment in attachments)
        {
            string? html = attachment.Content switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => null,
            };

            if (attachment.ContentType?.Value != "text/html" || html is null)
            {
                continue;
            }

            try
            {
                foreach (KeyValuePair<string, string> hint in InlineImage.ParseInlineImageHints(html))
                {
                    hints[hint.Key] = hint.Value;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // A hint only names the format, so an HTML body too slow to parse leaves its images typed from their content type instead.
                _logger.LogDebug("files: skipping inline image format hints; the HTML attachment took too long to parse");
            }
        }

        return hints;
    }

    /// <summary>Whether an attachment is an image pasted inline: an <c>image/*</c> attachment located at a Bot API inline image path.</summary>
    private static bool IsInlineImage(TeamsAttachment attachment)
        => attachment.ContentType?.Value is string contentType
            && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && InlineImage.InlineImageId(attachment.ContentUrl) is not null;

    /// <summary>
    /// Map an inline image attachment to an <see cref="IncomingFile"/>, or <c>null</c> when its location is not on the activity's service URL.
    /// </summary>
    private IncomingFile? ToInlineImage(TeamsAttachment attachment, int index, ConversationType scope, Dictionary<string, string> hints)
    {
        Uri contentUrl = attachment.ContentUrl!;
        string id = InlineImage.InlineImageId(contentUrl)!;

        if (!IsOnServiceUrl(contentUrl))
        {
            _logger.LogWarning(
                "files: skipping inline image attachment at index {Index}; its location is not on the activity's service URL",
                index);
            return null;
        }

        // Typed from the HTML format hint, or from the attachment's content type when there is no hint.
        InlineImageFormat? format = hints.TryGetValue(id, out string? hint)
            ? InlineImage.FormatFromHint(hint)
            : InlineImage.FormatFromContentType(attachment.ContentType?.Value);
        string baseName = $"image-{id}";
        (string name, string? extension, string contentType) = InlineImage.InlineImageNaming(baseName, format);

        return new IncomingFile(name, scope, FileSource.BotActivity, _downloader)
        {
            ContentType = contentType,
            Extension = extension,
            AttachmentStyle = AttachmentStyle.InlineImage,
            ContentUrl = contentUrl,
            Raw = attachment,
            InlineImageBaseName = baseName,
            BotApiCredential = _botApiCredential,
        };
    }

    /// <summary>
    /// Whether a URL is under the activity's service URL over https.
    /// The download path applies the same check through <see cref="BotApiCredential.ServiceUrl"/>, while this one lets listing skip and warn.
    /// </summary>
    private bool IsOnServiceUrl(Uri url)
        => FileDownloader.IsUnderServiceUrl(url, _activity.ServiceUrl);

    /// <summary>Map a single activity attachment to an <see cref="IncomingFile"/>, or <c>null</c> when the attachment is not an uploaded file or is malformed. Never throws: unusable attachments are skipped so one bad entry cannot drop the rest.</summary>
    private IncomingFile? ToIncomingFile(TeamsAttachment attachment, int index, ConversationType scope)
    {
        // Not an uploaded file (card, mention, adaptive card, etc.). Silently ignored. Null-safe: a null ContentType is simply not a match, so a malformed attachment is skipped rather than throwing.
        if (attachment.ContentType != AttachmentContentType.FileDownloadInfo)
        {
            return null;
        }

        (FileDownloadInfo? content, bool declaresDownloadUrl) = AsFileDownloadInfo(attachment.Content);
        Uri? downloadUrl = content?.DownloadUrl;
        Uri? contentUrl = attachment.ContentUrl;
        string? name = attachment.Name;

        // The Agentic User shape: `content` that parsed and declares no `downloadUrl` at all. Content that failed to parse, or that declares a `downloadUrl` too malformed to use, is a broken attachment rather than an agentic one. Both are excluded from the Graph route because both were skipped before it existed, and resolving one would spend a Graph credential on a payload the SDK has already judged untrustworthy.
        bool isAgenticShape = content is not null && !declaresDownloadUrl;

        // `downloadUrl` is fetched directly. A `contentUrl` without one is the agentic user case and resolves through Graph, only where `FileDownloader.IsGraphRouteOpen` allows: the dispatcher applies the same check, so a handle surfaced here cannot then fail at `DownloadAsync()` on scope. The `downloadUrl` branch keeps its existing scope behavior.
        bool hasLocator = downloadUrl is not null || contentUrl is not null;
        bool canFetch = downloadUrl is not null || (FileDownloader.IsGraphRouteOpen(scope, _credential?.Actor) && isAgenticShape && contentUrl is not null);

        if (!canFetch || string.IsNullOrEmpty(name))
        {
            // Split by cause: a malformed attachment is a real defect, while an out-of-scope file is expected noise.
            if (string.IsNullOrEmpty(name) || !hasLocator)
            {
                _logger.LogWarning(
                    "files: skipping file.download.info attachment at index {Index}; missing {Missing}",
                    index,
                    string.IsNullOrEmpty(name) ? "name" : "a download or content URL");
            }
            else
            {
                _logger.LogDebug(
                    "files: skipping file.download.info attachment at index {Index}; '{Scope}' scope files are not fetchable yet",
                    index,
                    scope.Value);
            }

            return null;
        }

        return new IncomingFile(name, scope, FileSource.BotActivity, _downloader)
        {
            UniqueId = content?.UniqueId,
            // `fileType` is the platform-supplied extension (e.g. `pdf`); left null when the wire omits it, matching how peer SDKs surface it.
            Extension = content?.FileType,
            AttachmentStyle = AttachmentStyle.File,
            // Browsable link to the file in OneDrive/SharePoint. Not directly fetchable like `downloadUrl`, but it is the locator a Graph `/shares` resolution keys off.
            ContentUrl = contentUrl,
            Raw = attachment,
            DownloadUrl = downloadUrl,
            Credential = _credential,
        };
    }

    /// <summary>
    /// Coerce an attachment's <c>Content</c> (already-typed, a <see cref="JsonElement"/> from the wire, or another object) into a <see cref="FileDownloadInfo"/>. Returns <c>null</c> on malformed content so the caller can skip it rather than throw.
    /// <para><c>DeclaresDownloadUrl</c> reports whether the payload carried a <c>downloadUrl</c> at all, which a null <see cref="FileDownloadInfo.DownloadUrl"/> cannot express: one means the field was absent, as it is for an Agentic User, and the other means it was present but unusable. Only the first is a shape the Graph route serves.</para>
    /// </summary>
    private static (FileDownloadInfo? Content, bool DeclaresDownloadUrl) AsFileDownloadInfo(object? content)
    {
        try
        {
            return content switch
            {
                null => (null, false),
                FileDownloadInfo info => (info, info.DownloadUrl is not null),
                JsonElement element => (DeserializeDroppingWrongTypedFields(element), DeclaresDownloadUrl(element)),
                _ => AsFileDownloadInfo(JsonSerializer.SerializeToElement(content)),
            };
        }
        catch (JsonException)
        {
            return (null, false);
        }
    }

    /// <summary>Whether the wire payload carried a <c>downloadUrl</c> property, regardless of whether its value was usable.</summary>
    private static bool DeclaresDownloadUrl(JsonElement element)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("downloadUrl", out _);

    /// <summary>
    /// Deserialize a <see cref="FileDownloadInfo"/>, ignoring any property the wire sent with a non-string value.
    /// <para>Every field on this payload is a string, so a wrong-typed one is malformed data rather than a shape to honour. Deserializing the object whole would throw on it and discard the rest, and <c>UniqueId</c> and <c>FileType</c> are metadata: losing a usable <c>DownloadUrl</c> alongside one of them routes a traditional bot's file through Graph, which then fails reporting a consent problem that was never the cause. Filtering rather than reading each property by hand keeps the model's own attribute mapping.</para>
    /// </summary>
    private static FileDownloadInfo? DeserializeDroppingWrongTypedFields(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element.Deserialize<FileDownloadInfo>();
        }

        Dictionary<string, JsonElement> narrowed = [];

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Null)
            {
                narrowed[property.Name] = property.Value;
            }
        }

        return JsonSerializer.SerializeToElement(narrowed).Deserialize<FileDownloadInfo>();
    }
}
