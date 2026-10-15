// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

public class FilesAccessorInlineTests
{
    private const string ServiceUrl = "https://smba.trafficmanager.net/amer/";

    // These tests only exercise attachment mapping, never a download, so the client is never used.
    private static readonly FileDownloader Downloader = new(new HttpClient());

    private readonly RecordingLogger _log = new();

    private static TeamsAttachment InlineImageAttachment(string id, string contentType = "image/*", string serviceUrl = ServiceUrl)
        => new()
        {
            ContentType = new AttachmentContentType(contentType),
            ContentUrl = new Uri($"{serviceUrl}v3/attachments/{id}/views/original"),
        };

    private static TeamsAttachment Html(params (string Id, string? Scope)[] images)
    {
        string tags = string.Concat(images.Select(image =>
        {
            string itemscope = image.Scope is null ? string.Empty : $" itemscope=\"{image.Scope}\"";
            return $"<img src=\"{ServiceUrl}v3/attachments/{image.Id}/views/original\" itemtype=\"http://schema.skype.com/AMSImage\"{itemscope} itemid=\"{image.Id}\" width=\"250\">";
        }));

        return HtmlContent($"<p>look</p><p>{tags}</p>");
    }

    private static TeamsAttachment HtmlContent(string content)
        => new() { ContentType = new AttachmentContentType("text/html"), Content = content };

    private static readonly TeamsAttachment ReportPdf = new()
    {
        ContentType = AttachmentContentType.FileDownloadInfo,
        ContentUrl = new Uri("https://contoso.sharepoint.com/report.pdf"),
        Name = "report.pdf",
        Content = new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/report.pdf?tempauth=abc"), FileType = "pdf" },
    };

    // Round-trips the attachments through JSON, as a real receive does.
    private static MessageActivity ActivityWith(IList<TeamsAttachment> attachments, string? serviceUrl = ServiceUrl)
    {
        CoreActivity core = new()
        {
            Type = TeamsActivityTypes.Message,
            ServiceUrl = serviceUrl is null ? null : new Uri(serviceUrl),
        };
        core.Properties["attachments"] = JsonSerializer.SerializeToElement(attachments);

        Conversation conversation = new("conv-1");
        conversation.Properties["conversationType"] = JsonSerializer.SerializeToElement("groupChat");
        core.Conversation = conversation;

        return MessageActivity.FromActivity(core);
    }

    private FilesAccessor Accessor(MessageActivity activity) => new(activity, _log, Downloader);

    [Theory]
    [InlineData("png", "image-a1.png", "png", "image/png")]
    [InlineData("jpeg", "image-a1.jpg", "jpg", "image/jpeg")]
    [InlineData("gif", "image-a1.gif", "gif", "image/gif")]
    [InlineData("webp", "image-a1.webp", "webp", "image/webp")]
    public async Task NamesAndTypes_AHint(string hint, string name, string extension, string contentType)
    {
        MessageActivity activity = ActivityWith([InlineImageAttachment("a1"), Html(("a1", hint))]);

        IList<IncomingFile> files = await Accessor(activity).ListAsync();

        IncomingFile file = Assert.Single(files);
        Assert.Equal(name, file.Name);
        Assert.Equal(extension, file.Extension);
        Assert.Equal(contentType, file.ContentType);
        Assert.Equal(AttachmentStyle.InlineImage, file.AttachmentStyle);
        Assert.Equal(FileSource.BotActivity, file.Source);
        Assert.Equal(ConversationType.GroupChat, file.Scope);
        Assert.Equal(new Uri($"{ServiceUrl}v3/attachments/a1/views/original"), file.ContentUrl);
        Assert.Same(activity.Attachments![0], file.Raw);
        Assert.Null(file.UniqueId);
    }

    [Theory]
    [InlineData("bmp")]
    [InlineData("avif")]
    [InlineData("")]
    public async Task GivesAnUnknownHint_NoExtensionAndOctetStream(string hint)
    {
        IList<IncomingFile> files = await Accessor(ActivityWith([InlineImageAttachment("a1"), Html(("a1", hint))])).ListAsync();

        Assert.Equal("image-a1", files[0].Name);
        Assert.Null(files[0].Extension);
        Assert.Equal("application/octet-stream", files[0].ContentType);
    }

    [Fact]
    public async Task GivesAMissingHint_NoExtensionAndOctetStream_RatherThanAssumingPng()
    {
        IList<IncomingFile> files = await Accessor(ActivityWith([InlineImageAttachment("a1"), Html(("a1", null))])).ListAsync();

        Assert.Equal("image-a1", files[0].Name);
        Assert.Equal("application/octet-stream", files[0].ContentType);
    }

    [Fact]
    public async Task FallsBackToTheAttachmentContentType_OnlyWhenNoHintIsPresent()
    {
        IList<IncomingFile> withoutHtml = await Accessor(ActivityWith([InlineImageAttachment("a1", "image/jpeg")])).ListAsync();
        IList<IncomingFile> wildcard = await Accessor(ActivityWith([InlineImageAttachment("a1", "image/*")])).ListAsync();
        IList<IncomingFile> hintWins = await Accessor(ActivityWith([InlineImageAttachment("a1", "image/jpeg"), Html(("a1", "bmp"))])).ListAsync();

        Assert.Equal(("image-a1.jpg", "image/jpeg"), (withoutHtml[0].Name, withoutHtml[0].ContentType));
        Assert.Equal(("image-a1", "application/octet-stream"), (wildcard[0].Name, wildcard[0].ContentType));
        Assert.Equal(("image-a1", "application/octet-stream"), (hintWins[0].Name, hintWins[0].ContentType));
    }

    [Fact]
    public async Task ListsSeveralInlineImagesAlongsideAFileAttachment_EachWithItsOwnStyle()
    {
        TeamsAttachment file = new()
        {
            ContentType = AttachmentContentType.FileDownloadInfo,
            ContentUrl = new Uri("https://contoso.sharepoint.com/report.pdf"),
            Name = "report.pdf",
            Content = new FileDownloadInfo { DownloadUrl = new Uri("https://download.example/report.pdf?tempauth=abc"), FileType = "pdf", UniqueId = "u1" },
        };
        MessageActivity activity = ActivityWith([InlineImageAttachment("a1"), file, InlineImageAttachment("b2"), Html(("a1", "png"), ("b2", "webp"))]);

        IList<IncomingFile> files = await Accessor(activity).ListAsync();

        Assert.Equal(
            [("image-a1.png", AttachmentStyle.InlineImage), ("report.pdf", AttachmentStyle.File), ("image-b2.webp", AttachmentStyle.InlineImage)],
            files.Select(f => (f.Name, f.AttachmentStyle)));
        Assert.Equal("u1", files[1].UniqueId);
    }

    // ==================== AttachmentStyle option ====================

    private static MessageActivity Mixed()
        => ActivityWith([InlineImageAttachment("a1"), ReportPdf, InlineImageAttachment("b2"), Html(("a1", "png"), ("b2", "png"))]);

    [Fact]
    public async Task AttachmentStyleOption_ReturnsBothStyles_WhenOmitted()
    {
        IList<IncomingFile> files = await Accessor(Mixed()).ListAsync(new FileListOptions());

        Assert.Equal(["image-a1.png", "report.pdf", "image-b2.png"], files.Select(f => f.Name));
    }

    [Fact]
    public async Task AttachmentStyleOption_ReturnsOnlyFileAttachments_ForFile()
    {
        IList<IncomingFile> files = await Accessor(Mixed()).ListAsync(new FileListOptions { AttachmentStyle = AttachmentStyle.File });

        Assert.Equal(["report.pdf"], files.Select(f => f.Name));
    }

    [Fact]
    public async Task AttachmentStyleOption_ReturnsOnlyPastedImages_ForInlineImage()
    {
        IList<IncomingFile> files = await Accessor(Mixed()).ListAsync(new FileListOptions { AttachmentStyle = AttachmentStyle.InlineImage });

        Assert.Equal(["image-a1.png", "image-b2.png"], files.Select(f => f.Name));
    }

    [Fact]
    public async Task AttachmentStyleOption_LetsFirstSkipALeadingPastedImage()
    {
        FilesAccessor accessor = Accessor(Mixed());

        Assert.Equal("image-a1.png", (await accessor.FirstAsync())?.Name);
        Assert.Equal("report.pdf", (await accessor.FirstAsync(new FileListOptions { AttachmentStyle = AttachmentStyle.File }))?.Name);
    }

    [Fact]
    public async Task AttachmentStyleOption_LetsFirstReturnNull_WhenNoFileHasTheStyle()
    {
        FilesAccessor accessor = Accessor(ActivityWith([InlineImageAttachment("a1")]));

        Assert.Null(await accessor.FirstAsync(new FileListOptions { AttachmentStyle = AttachmentStyle.File }));
    }

    [Fact]
    public async Task AttachmentStyleOption_DoesNotWarnAboutAnInlineImageItWasNeverGoingToReturn()
    {
        TeamsAttachment elsewhere = InlineImageAttachment("a1", "image/*", "https://attacker.example/amer/");

        IList<IncomingFile> files = await Accessor(ActivityWith([elsewhere, ReportPdf])).ListAsync(new FileListOptions { AttachmentStyle = AttachmentStyle.File });

        Assert.Equal(["report.pdf"], files.Select(f => f.Name));
        Assert.Empty(_log.Warnings);
    }

    [Fact]
    public async Task AttachmentStyleOption_RejectsNullOptions()
    {
        FilesAccessor accessor = Accessor(Mixed());

        await Assert.ThrowsAsync<ArgumentNullException>(() => accessor.ListAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => accessor.FirstAsync(null!));
    }

    // ==================== Hints ====================

    [Fact]
    public async Task ReadsHints_RegardlessOfAttributeOrderOrQuoteStyle()
    {
        TeamsAttachment reordered = HtmlContent("<img itemid='a1' itemscope='gif' alt=\"x\" itemtype='http://schema.skype.com/AMSImage'>");

        IList<IncomingFile> files = await Accessor(ActivityWith([InlineImageAttachment("a1"), reordered])).ListAsync();

        Assert.Equal("image-a1.gif", files[0].Name);
    }

    [Fact]
    public async Task IgnoresAnImgHint_ThatIsNotMarkedAsAnInlineImage()
    {
        TeamsAttachment other = HtmlContent("<img itemid=\"a1\" itemscope=\"png\" itemtype=\"other\">");

        IList<IncomingFile> files = await Accessor(ActivityWith([InlineImageAttachment("a1"), other])).ListAsync();

        Assert.Equal("image-a1", files[0].Name);
    }

    // ==================== serviceUrl guard ====================

    [Fact]
    public async Task SkipsAnInlineImageThatIsNotOnTheActivityServiceUrl_WithAWarning()
    {
        TeamsAttachment elsewhere = InlineImageAttachment("a1", "image/*", "https://attacker.example/amer/");

        IList<IncomingFile> files = await Accessor(ActivityWith([elsewhere, InlineImageAttachment("b2")])).ListAsync();

        Assert.Equal(["image-b2"], files.Select(f => f.Name));
        string warning = Assert.Single(_log.Warnings);
        Assert.Equal("files: skipping inline image attachment at index 0; its location is not on the activity's service URL", warning);
    }

    [Fact]
    public async Task DoesNotLetALookalikeHostMatchAServiceUrlThatLacksATrailingSlash()
    {
        TeamsAttachment lookalike = InlineImageAttachment("a1", "image/*", "https://smba.trafficmanager.net.attacker.example/");

        IList<IncomingFile> files = await Accessor(ActivityWith([lookalike], "https://smba.trafficmanager.net")).ListAsync();

        Assert.Empty(files);
        Assert.NotEmpty(_log.Warnings);
    }

    [Fact]
    public async Task DoesNotLetADotSegmentClimbOutOfTheServiceUrlPath()
    {
        TeamsAttachment climbing = InlineImageAttachment("a1", "image/*", "https://smba.trafficmanager.net/amer/TENANT/../../");

        IList<IncomingFile> files = await Accessor(ActivityWith([climbing], "https://smba.trafficmanager.net/amer/TENANT/")).ListAsync();

        Assert.Empty(files);
        Assert.NotEmpty(_log.Warnings);
    }

    [Fact]
    public async Task MatchesAServiceUrlWithoutATrailingSlash()
    {
        IList<IncomingFile> files = await Accessor(ActivityWith(
            [InlineImageAttachment("a1", "image/*", "https://smba.trafficmanager.net/amer/")],
            "https://smba.trafficmanager.net/amer")).ListAsync();

        Assert.Single(files);
    }

    [Fact]
    public async Task MatchesAServiceUrlWhoseHostDiffersOnlyInCase_KeepingTheContentUrlAsSent()
    {
        TeamsAttachment image = InlineImageAttachment("a1", "image/*", "https://SMBA.TrafficManager.net/amer/");

        IList<IncomingFile> files = await Accessor(ActivityWith([image], "https://smba.trafficmanager.net/amer/")).ListAsync();

        IncomingFile file = Assert.Single(files);
        Assert.Equal(image.ContentUrl!.OriginalString, file.ContentUrl!.OriginalString);
    }

    [Fact]
    public async Task SkipsInlineImages_WhenTheActivityHasNoServiceUrl()
    {
        IList<IncomingFile> files = await Accessor(ActivityWith([InlineImageAttachment("a1")], null)).ListAsync();

        Assert.Empty(files);
        Assert.NotEmpty(_log.Warnings);
    }

    [Fact]
    public async Task LeavesAnImageAttachmentThatIsNotAtAnInlineImageLocationAlone()
    {
        TeamsAttachment elsewhere = new()
        {
            ContentType = new AttachmentContentType("image/png"),
            ContentUrl = new Uri($"{ServiceUrl}v3/somewhere/else.png"),
        };

        IList<IncomingFile> files = await Accessor(ActivityWith([elsewhere])).ListAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task LeavesANonImageAttachmentAtAnInlineImageLocationAlone()
    {
        TeamsAttachment notAnImage = InlineImageAttachment("a1", "application/pdf");

        IList<IncomingFile> files = await Accessor(ActivityWith([notAnImage])).ListAsync();

        Assert.Empty(files);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
