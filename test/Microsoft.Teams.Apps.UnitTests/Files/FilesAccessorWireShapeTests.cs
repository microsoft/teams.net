// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// Mapping tests against the shape Teams actually puts on the wire for a personal-scope file upload, as opposed to
/// the shapes <see cref="FilesAccessorTests"/> invents. The distinguishing features of the real payload, none of
/// which the hand-written tests exercise, are that it carries <em>two</em> attachments (the file plus an empty
/// <c>text/html</c> sibling), that <c>content</c> has exactly three keys with <b>no <c>etag</c></b> despite
/// <see cref="FileDownloadInfo.Etag"/> modelling one, and that the activity has <b>no <c>text</c> property at all</b>
/// when a file is attached with no message.
/// <para>The fixture is constructed by allowlist rather than redacted from a capture: it contains only the seven
/// fields the mapper reads, with synthetic values throughout. A real capture carries identity in at least three
/// places that a denylist misses (the download URL's query token, the user's UPN inside both URL <em>paths</em>, and
/// the activity's <c>from</c>/<c>recipient</c>/<c>conversation</c>/<c>channelData</c> blocks), so an allowlist is
/// what fails closed. Do not add fields here that the mapper does not read.</para>
/// </summary>
public class FilesAccessorWireShapeTests
{
    private static readonly NullLogger Log = NullLogger.Instance;

    // These tests only exercise attachment mapping, never a download, so the client is never used.
    private static readonly FileDownloader Downloader = new(new HttpClient());

    private const string ExpectedName = "quarterly report.pdf";
    private const string ExpectedUniqueId = "00000000-0000-4000-8000-00000000f11e";

    // Percent-encoded spaces, exactly as Teams sends them in the browsable OneDrive path.
    private const string ExpectedWebUrl =
        "https://example.sharepoint.com/personal/synthetic_user_example_com/Documents/Microsoft%20Teams%20Chat%20Files/quarterly%20report.pdf";

    private const string ExpectedDownloadUrl =
        "https://example.sharepoint.com/personal/synthetic_user_example_com/_layouts/15/download.aspx?UniqueId=00000000-0000-4000-8000-00000000f11e&Translate=false&tempauth=synthetic-not-a-real-token&ApiVersion=2.1";

    /// <summary>
    /// The real wire shape with synthetic values. Deliberately minimal: `type` plus the seven allowlisted fields.
    /// Every omission is load-bearing, so read the assertions before adding anything.
    /// </summary>
    private const string CapturedShapeJson = $$"""
    {
      "type": "message",
      "conversation": {
        "conversationType": "personal"
      },
      "attachments": [
        {
          "contentType": "application/vnd.microsoft.teams.file.download.info",
          "contentUrl": "{{ExpectedWebUrl}}",
          "name": "{{ExpectedName}}",
          "content": {
            "downloadUrl": "{{ExpectedDownloadUrl}}",
            "uniqueId": "{{ExpectedUniqueId}}",
            "fileType": "pdf"
          }
        },
        {
          "contentType": "text/html",
          "content": ""
        }
      ]
    }
    """;

    // Hydrates the fixture the way a real receive does: raw wire JSON through the inbound deserializer, then
    // FromActivity. Nothing here hand-builds a TeamsAttachment, so a regression in either step fails these tests.
    private static MessageActivity CapturedActivity()
        => MessageActivity.FromActivity(CoreActivity.FromJsonString(CapturedShapeJson));

    [Fact]
    public async Task RealWireShape_YieldsExactlyOneFile_FromATwoAttachmentActivity()
    {
        MessageActivity activity = CapturedActivity();

        // Positive control: assert the sibling really arrived, so "one file" below means the mapper skipped it
        // rather than the fixture never having carried it.
        Assert.Equal(2, activity.Attachments!.Count);

        IList<IncomingFile> files = await new FilesAccessor(activity, Log, Downloader).ListAsync();

        IncomingFile file = Assert.Single(files);
        Assert.Equal(ExpectedName, file.Name);
        Assert.Equal("pdf", file.Extension);
        Assert.Equal(ExpectedUniqueId, file.UniqueId);
        Assert.Equal(new Uri(ExpectedWebUrl), file.WebUrl);
        Assert.Equal(new Uri(ExpectedDownloadUrl), file.DownloadUrl);
        Assert.Equal(ConversationType.Personal, file.Scope);
        Assert.Equal(FileSource.BotActivity, file.Source);

        // `TeamsAttachment.ContentUrl` is a `Uri`, so the wire value is normalized before anything reads it. Teams
        // percent-encodes the spaces in the OneDrive path; pin that they survive the round-trip, since a URL that
        // silently decoded them would no longer address the item.
        Assert.Contains("Microsoft%20Teams%20Chat%20Files", file.WebUrl!.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain(' ', file.WebUrl.AbsoluteUri);
    }

    [Fact]
    public async Task RealWireShape_DoesNotLeakTheEmptyHtmlSibling()
    {
        MessageActivity activity = CapturedActivity();
        TeamsAttachment htmlSibling = activity.Attachments![1];

        // The sibling is the thing the content-type guard has to reject: an attachment whose content is an empty
        // string rather than an object, so it would not survive coercion into FileDownloadInfo either.
        Assert.Equal(new AttachmentContentType("text/html"), htmlSibling.ContentType);

        IncomingFile file = Assert.Single(await new FilesAccessor(activity, Log, Downloader).ListAsync());

        // Raw identifies which attachment produced the file, so this pins the mapping to index 0 and proves the
        // sibling was skipped rather than mapped into some second, malformed handle.
        Assert.Same(activity.Attachments[0], file.Raw);
        Assert.NotSame(htmlSibling, file.Raw);
    }

    [Fact]
    public async Task RealWireShape_MapsWithNoEtagAndNoTextProperty()
    {
        using JsonDocument fixture = JsonDocument.Parse(CapturedShapeJson);
        JsonElement content = fixture.RootElement.GetProperty("attachments")[0].GetProperty("content");

        // Guards the fixture itself: Teams sends exactly downloadUrl, uniqueId and fileType on this path, so the
        // mapper is only ever proven against the absent-etag shape if the fixture keeps it absent.
        Assert.Equal(3, content.EnumerateObject().Count());
        Assert.False(content.TryGetProperty("etag", out _));
        Assert.False(fixture.RootElement.TryGetProperty("text", out _));

        MessageActivity activity = CapturedActivity();
        Assert.Null(activity.Text);

        // The payload still maps, which is what would break if Etag were ever made required or if a missing `text`
        // started tripping activity hydration.
        Assert.Single(await new FilesAccessor(activity, Log, Downloader).ListAsync());
    }
}
