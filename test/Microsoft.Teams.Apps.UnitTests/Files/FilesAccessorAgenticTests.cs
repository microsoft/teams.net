// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary> Attachment mapping for a file that arrives with a <c>contentUrl</c> and no <c>downloadUrl</c>, which is the shape the platform sends an Agentic User.</summary>
public class FilesAccessorAgenticTests
{
    private static readonly NullLogger Log = NullLogger.Instance;

    // These tests only exercise attachment mapping, never a download, so the client is never used.
    private static readonly FileDownloader Downloader = new(new HttpClient());

    private const string ContentUrl = "https://contoso.sharepoint.com/personal/a/Documents/report.pdf";

    /// <summary>An attachment shaped the way the platform sends one to an Agentic User: a browsable <c>contentUrl</c>, and no <c>downloadUrl</c> anywhere in <c>content</c>.</summary>
    private static TeamsAttachment AgenticAttachment(
        string? name = "report.pdf",
        string? contentUrl = ContentUrl,
        FileDownloadInfo? content = null)
        => new()
        {
            ContentType = AttachmentContentType.FileDownloadInfo,
            ContentUrl = contentUrl is null ? null : new Uri(contentUrl),
            Name = name,
            Content = content ?? new FileDownloadInfo { UniqueId = "odsp-unique-id", FileType = "pdf" },
        };

    private static MessageActivity ActivityWith(IList<TeamsAttachment> attachments, string conversationType = "personal")
    {
        CoreActivity core = new() { Type = TeamsActivityTypes.Message };
        core.Properties["attachments"] = JsonSerializer.SerializeToElement(attachments);

        Conversation conversation = new("conv-1");
        conversation.Properties["conversationType"] = JsonSerializer.SerializeToElement(conversationType);
        core.Conversation = conversation;

        return MessageActivity.FromActivity(core);
    }

    [Fact]
    public async Task SurfacesAContentUrlOnlyAttachmentAsAFile()
    {
        // An Agentic User's attachment carries a `contentUrl` and no `downloadUrl`, so a mapper that requires `downloadUrl` returns an empty ListAsync() for every file in every scope.
        FilesAccessor accessor = new(ActivityWith([AgenticAttachment()]), Log, Downloader);

        IncomingFile file = Assert.Single(await accessor.ListAsync());

        Assert.Equal("report.pdf", file.Name);
        Assert.Equal(new Uri(ContentUrl), file.ContentUrl);
        Assert.Equal("odsp-unique-id", file.UniqueId);
        Assert.Equal("pdf", file.Extension);
    }

    [Theory]
    [InlineData("groupChat")]
    [InlineData("channel")]
    public async Task SkipsAContentUrlOnlyAttachmentOutsidePersonalScope(string scope)
    {
        // The platform's agentic path applies no scope filter, so an agent in a group chat does receive these.
        // Admitting them would put a handle in ListAsync() that then fails at DownloadAsync() with the scope error.
        FilesAccessor accessor = new(ActivityWith([AgenticAttachment()], scope), Log, Downloader);

        Assert.Empty(await accessor.ListAsync());
    }

    [Fact]
    public async Task StillSurfacesADownloadUrlAttachmentOutsidePersonalScope()
    {
        // The scope condition rides on the contentUrl branch only, so traditional-bot behavior is unchanged: these are surfaced by ListAsync() and throw the scope error at download time.
        TeamsAttachment attachment = AgenticAttachment(content: new FileDownloadInfo
        {
            DownloadUrl = new Uri("https://download.example/r.pdf?tempauth=abc"),
            FileType = "pdf",
        });

        FilesAccessor accessor = new(ActivityWith([attachment], "groupChat"), Log, Downloader);

        Assert.Single(await accessor.ListAsync());
    }

    [Fact]
    public async Task SkipsAnAttachmentWithNeitherUrl()
    {
        FilesAccessor accessor = new(ActivityWith([AgenticAttachment(contentUrl: null)]), Log, Downloader);

        Assert.Empty(await accessor.ListAsync());
    }

    [Fact]
    public async Task SkipsAnAttachmentWithNoName()
    {
        FilesAccessor accessor = new(ActivityWith([AgenticAttachment(name: null)]), Log, Downloader);

        Assert.Empty(await accessor.ListAsync());
    }

    [Fact]
    public async Task DoesNotDropTheRestOfTheListWhenOneEntryIsUnusable()
    {
        FilesAccessor accessor = new(
            ActivityWith([AgenticAttachment(contentUrl: null), AgenticAttachment()]),
            Log,
            Downloader);

        Assert.Single(await accessor.ListAsync());
    }

    [Fact]
    public async Task HandsTheCredentialToEveryFileItSurfaces()
    {
        // The seam the Graph fetch path depends on. Without it every handle would resolve no credential and report NoGraphCredential, which reads like a consent problem rather than a wiring one.
        GraphCredential credential = new(FileActor.AgenticUser, _ => Task.FromResult<string?>("agent-token"));
        FilesAccessor accessor = new(ActivityWith([AgenticAttachment()]), Log, Downloader, credential);

        IncomingFile file = Assert.Single(await accessor.ListAsync());

        Assert.Same(credential, file.Credential);
    }
    /// <summary>An attachment whose <c>content</c> is raw wire JSON rather than a typed model, which is the only way to express a wrong-typed field.</summary>
    private static TeamsAttachment RawContentAttachment(object content)
        => new()
        {
            ContentType = AttachmentContentType.FileDownloadInfo,
            ContentUrl = new Uri(ContentUrl),
            Name = "report.pdf",
            Content = content,
        };

    [Fact]
    public async Task KeepsThePreauthRouteWhenAMetadataFieldIsWrongTyped()
    {
        // `uniqueId` and `fileType` are metadata. Rejecting the whole `content` over one of them drops the `downloadUrl` beside it, and the file then routes through Graph and fails on a bot holding no Graph credential, reporting a consent problem for what is really bad data.
        // Asserted outside personal scope because the Graph route is personal-only, so a file surfaced here can only have reached the list on its `downloadUrl`.
        TeamsAttachment attachment = RawContentAttachment(
            new { downloadUrl = "https://download.example/tempauth=abc", uniqueId = 42, fileType = 7 });

        FilesAccessor accessor = new(ActivityWith([attachment], "groupChat"), Log, Downloader);

        IncomingFile file = Assert.Single(await accessor.ListAsync());

        // Dropped one at a time rather than taken at face value, which would fail later in the sharing-url encoder.
        Assert.Null(file.UniqueId);
        Assert.Null(file.Extension);
    }

    [Fact]
    public async Task DropsOnlyTheWrongTypedField()
    {
        TeamsAttachment attachment = RawContentAttachment(
            new { downloadUrl = "https://download.example/tempauth=abc", uniqueId = "odsp-unique-id", fileType = 7 });

        FilesAccessor accessor = new(ActivityWith([attachment], "groupChat"), Log, Downloader);

        IncomingFile file = Assert.Single(await accessor.ListAsync());

        Assert.Equal("odsp-unique-id", file.UniqueId);
        Assert.Null(file.Extension);
    }

    [Fact]
    public async Task DoesNotOpenTheGraphRouteWhenTheDownloadUrlIsWrongTyped()
    {
        // A declared `downloadUrl` the SDK could not use is a broken attachment, not the agentic shape, so no route applies in any scope. Falling to Graph here would resolve a payload already judged malformed, and would do it on whichever identity the turn happens to carry.
        TeamsAttachment attachment = RawContentAttachment(
            new { downloadUrl = 42, uniqueId = "odsp-unique-id", fileType = "pdf" });

        Assert.Empty(await new FilesAccessor(ActivityWith([attachment], "groupChat"), Log, Downloader).ListAsync());
        Assert.Empty(await new FilesAccessor(ActivityWith([attachment]), Log, Downloader).ListAsync());
    }

    [Fact]
    public async Task OpensTheGraphRouteForContentThatDeclaresNoDownloadUrl()
    {
        // The agentic shape itself, which is the one case the route exists for.
        Assert.Single(await new FilesAccessor(ActivityWith([AgenticAttachment()]), Log, Downloader).ListAsync());
    }

}
