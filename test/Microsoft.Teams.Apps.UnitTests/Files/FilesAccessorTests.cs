// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

public class FilesAccessorTests
{
    private static readonly NullLogger Log = NullLogger.Instance;

    // These tests only exercise attachment mapping, never a download, so the client is never used.
    private static readonly FileDownloader Downloader = new(new HttpClient());

    private static TeamsAttachment FileAttachment(string? name, FileDownloadInfo? content, Uri? contentUrl = null)
        => new()
        {
            ContentType = AttachmentContentType.FileDownloadInfo,
            ContentUrl = contentUrl,
            Name = name,
            Content = content,
        };

    // Builds an inbound MessageActivity the sanctioned (non-obsolete) way: populate a CoreActivity's
    // Properties as they arrive on the wire and let FromActivity hydrate it. This also round-trips
    // attachments through JSON, so attachment.Content lands as a JsonElement just like a real receive.
    private static MessageActivity MessageWith(IList<TeamsAttachment>? attachments, string? conversationType = "personal", bool withConversation = true)
    {
        CoreActivity core = new() { Type = TeamsActivityTypes.Message };

        if (attachments is not null)
        {
            core.Properties["attachments"] = JsonSerializer.SerializeToElement(attachments);
        }

        if (withConversation)
        {
            Conversation conversation = new("conv-1");

            if (conversationType is not null)
            {
                conversation.Properties["conversationType"] = JsonSerializer.SerializeToElement(conversationType);
            }

            core.Conversation = conversation;
        }

        return MessageActivity.FromActivity(core);
    }

    [Fact]
    public async Task Maps_FileDownloadInfoAttachment_ToIncomingFile()
    {
        TeamsAttachment attachment = FileAttachment(
            "report.pdf",
            new FileDownloadInfo
            {
                DownloadUrl = new Uri("https://download.example/report.pdf?tempauth=abc"),
                UniqueId = "odsp-unique-id",
                FileType = "pdf",
            },
            new Uri("https://contoso.sharepoint.com/report.pdf"));

        MessageActivity activity = MessageWith([attachment]);
        FilesAccessor accessor = new(activity, Log, Downloader);
        IList<IncomingFile> files = await accessor.ListAsync();

        IncomingFile file = Assert.Single(files);
        Assert.Equal("odsp-unique-id", file.UniqueId);
        Assert.Equal("report.pdf", file.Name);
        Assert.Equal("pdf", file.Extension);
        Assert.Equal(ConversationType.Personal, file.Scope);
        Assert.Equal(FileSource.BotActivity, file.Source);
        Assert.Equal(new Uri("https://contoso.sharepoint.com/report.pdf"), file.ContentUrl);
        Assert.Same(activity.Attachments![0], file.Raw);
    }

    [Fact]
    public async Task Ignores_AttachmentsThatAreNotUploadedFiles()
    {
        TeamsAttachment card = new()
        {
            ContentType = AttachmentContentType.AdaptiveCard,
            Content = new object(),
        };

        IList<IncomingFile> files = await new FilesAccessor(MessageWith([card]), Log, Downloader).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task Skips_MalformedFileMissingDownloadUrl()
    {
        TeamsAttachment attachment = FileAttachment("broken.pdf", new FileDownloadInfo { UniqueId = "no-url" });

        IList<IncomingFile> files = await new FilesAccessor(MessageWith([attachment]), Log, Downloader).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task Skips_FileMissingName()
    {
        TeamsAttachment attachment = FileAttachment(null, new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/anon") });

        IList<IncomingFile> files = await new FilesAccessor(MessageWith([attachment]), Log, Downloader).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task Maps_FileWithoutUniqueId()
    {
        TeamsAttachment attachment = FileAttachment("anon.pdf", new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/anon.pdf") });

        IList<IncomingFile> files = await new FilesAccessor(MessageWith([attachment]), Log, Downloader).ListAsync();

        IncomingFile file = Assert.Single(files);
        Assert.Equal("anon.pdf", file.Name);
        Assert.Null(file.UniqueId);
    }

    [Fact]
    public async Task DefaultsScopeToPersonal_WhenConversationTypeAbsent()
    {
        TeamsAttachment attachment = FileAttachment("a.pdf", new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/a.pdf"), UniqueId = "a" });

        IList<IncomingFile> files = await new FilesAccessor(MessageWith([attachment], withConversation: false), Log, Downloader).ListAsync();

        IncomingFile file = Assert.Single(files);
        Assert.Equal(ConversationType.Personal, file.Scope);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenActivityHasNoAttachments()
    {
        IList<IncomingFile> files = await new FilesAccessor(MessageWith([]), Log, Downloader).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenAttachmentsFieldAbsent()
    {
        IList<IncomingFile> files = await new FilesAccessor(MessageWith(null), Log, Downloader).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task ReturnsEmpty_ForNonMessageActivities()
    {
        TeamsActivity typing = new() { Type = TeamsActivityTypes.Typing };

        IList<IncomingFile> files = await new FilesAccessor(typing, Log, Downloader).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task First_ReturnsFirstMappedFile_OrNullWhenNone()
    {
        TeamsAttachment attachment = FileAttachment("a.pdf", new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/a.pdf"), UniqueId = "a" });

        Assert.NotNull(await new FilesAccessor(MessageWith([attachment]), Log, Downloader).FirstAsync());
        Assert.Null(await new FilesAccessor(MessageWith([]), Log, Downloader).FirstAsync());
    }

    [Fact]
    public async Task Honors_AlreadyCancelledToken()
    {
        TeamsAttachment attachment = FileAttachment("a.pdf", new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/a.pdf"), UniqueId = "a" });
        FilesAccessor accessor = new(MessageWith([attachment]), Log, Downloader);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => accessor.ListAsync(cts.Token));
        await Assert.ThrowsAsync<TaskCanceledException>(() => accessor.FirstAsync(cts.Token));
    }
}
