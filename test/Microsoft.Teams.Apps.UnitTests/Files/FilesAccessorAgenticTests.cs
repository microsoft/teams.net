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
}
