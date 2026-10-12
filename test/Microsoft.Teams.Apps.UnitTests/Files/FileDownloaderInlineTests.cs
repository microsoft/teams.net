// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// The inline image fetch route: the Bot API token, the service URL guard, byte-based naming, and error mapping.
/// </summary>
public class FileDownloaderInlineTests
{
    private const string ServiceUrl = "https://smba.trafficmanager.net/amer/";
    private const string ContentUrl = $"{ServiceUrl}v3/attachments/a1/views/original";

    private static readonly byte[] Png = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0];
    private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0, 0];
    private static readonly byte[] Jpeg = [0xff, 0xd8, 0xff, 0xe0, 0, 0];
    private static readonly byte[] Webp = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];
    private static readonly byte[] Unknown = [0x42, 0x4d, 0, 0, 0, 0];

    private sealed class RecordingHandler(HttpStatusCode status, byte[]? body = null, string? contentType = null) : HttpMessageHandler
    {
        public List<(Uri? Url, string? Authorization)> Calls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request.RequestUri, request.Headers.Authorization?.ToString()));

            HttpResponseMessage response = new(status);

            if (body is not null)
            {
                response.Content = new ByteArrayContent(body);

                if (contentType is not null)
                {
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                }
            }

            return Task.FromResult(response);
        }
    }

    private static byte[] Utf8(string text) => System.Text.Encoding.UTF8.GetBytes(text);

    private static BotApiCredential BotApi(FileActor actor = FileActor.App, string? token = "bot-api-token", string? serviceUrl = ServiceUrl)
        => new(actor, _ => Task.FromResult(token), serviceUrl is null ? null : new Uri(serviceUrl));

    private static IncomingFile InlineFile(
        RecordingHandler handler,
        string name = "image-a1.gif",
        string contentType = "image/gif",
        ConversationType? scope = null,
        BotApiCredential? credential = null,
        string contentUrl = ContentUrl)
        => new(name, scope ?? ConversationType.GroupChat, FileSource.BotActivity, new FileDownloader(new HttpClient(handler)))
        {
            ContentType = contentType,
            Extension = "gif",
            AttachmentStyle = AttachmentStyle.InlineImage,
            ContentUrl = new Uri(contentUrl),
            InlineImageBaseName = "image-a1",
            BotApiCredential = credential ?? BotApi(),
        };

    // ==================== inline image download ====================

    [Fact]
    public async Task ReadsTheContentUrlWithTheBotApiToken_InAScopeTheFileRoutesDoNotOpen()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Png);

        await InlineFile(handler, scope: ConversationType.Channel).DownloadAsync();

        (Uri? url, string? authorization) = Assert.Single(handler.Calls);
        Assert.Equal(new Uri(ContentUrl), url);
        Assert.Equal("Bearer bot-api-token", authorization);
    }

    public static TheoryData<string, byte[], string, string> RecognizedBytes => new()
    {
        { "PNG", Png, "image-a1.png", "image/png" },
        { "JPEG", Jpeg, "image-a1.jpg", "image/jpeg" },
        { "GIF", Gif, "image-a1.gif", "image/gif" },
        { "WebP", Webp, "image-a1.webp", "image/webp" },
    };

    [Theory]
    [MemberData(nameof(RecognizedBytes))]
    public async Task NamesAndTypesRecognizedBytes_FromTheBytes_OverTheHintAndTheResponseHeader(string format, byte[] bytes, string filename, string contentType)
    {
        _ = format;
        RecordingHandler handler = new(HttpStatusCode.OK, bytes, "application/octet-stream");

        DownloadedFile downloaded = await InlineFile(handler, name: "image-a1.gif", contentType: "image/gif").DownloadAsync();

        Assert.Equal(filename, downloaded.Filename);
        Assert.Equal(contentType, downloaded.ContentType);
    }

    [Fact]
    public async Task FallsBackToTheHintDerivedNameAndType_WhenTheBytesAreNotRecognized()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Unknown, "image/png");

        DownloadedFile downloaded = await InlineFile(handler, name: "image-a1.gif", contentType: "image/gif").DownloadAsync();

        Assert.Equal("image-a1.gif", downloaded.Filename);
        Assert.Equal("image/gif", downloaded.ContentType);
    }

    [Fact]
    public async Task FallsBackToNoExtensionAndOctetStream_WhenNeitherTheBytesNorTheHintNameAFormat()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Unknown, "image/bmp");

        DownloadedFile downloaded = await InlineFile(handler, name: "image-a1", contentType: "application/octet-stream").DownloadAsync();

        Assert.Equal("image-a1", downloaded.Filename);
        Assert.Equal("application/octet-stream", downloaded.ContentType);
    }

    [Fact]
    public async Task LeavesTheHandleUnchanged_WhenTheBytesDisagreeWithTheHint()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Png);
        IncomingFile file = InlineFile(handler, name: "image-a1.gif", contentType: "image/gif");

        await file.DownloadAsync();

        Assert.Equal("image-a1.gif", file.Name);
        Assert.Equal("image/gif", file.ContentType);
        Assert.Equal("gif", file.Extension);
    }

    [Fact]
    public async Task ThrowsBeforeAnyRequest_WhenNoBotApiTokenIsAvailable()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Png);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InlineFile(handler, credential: BotApi(token: null)).DownloadAsync());

        Assert.Contains("no Bot API token is available", error.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task RefusesAContentUrlThatIsNotHttps()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Png);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new FileDownloader(new HttpClient(handler)).OpenFileStreamAsync(
                ConversationType.Personal,
                downloadUrl: null,
                contentUrl: new Uri("http://smba.trafficmanager.net/amer/v3/attachments/a1/views/original"),
                contentType: null,
                priorFetchSucceeded: false,
                credential: null,
                AttachmentStyle.InlineImage,
                BotApi(),
                CancellationToken.None));

        Assert.Contains("must use https", error.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Calls);
    }

    // ==================== IsUnderServiceUrl ====================

    [Theory]
    [InlineData("the service URL path", $"{ServiceUrl}v3/attachments/a1/views/original", ServiceUrl)]
    [InlineData("a service URL without a trailing slash", $"{ServiceUrl}v3/x", "https://smba.trafficmanager.net/amer")]
    [InlineData("a host that differs only in case", "https://SMBA.trafficmanager.net/amer/v3/x", ServiceUrl)]
    [InlineData("a service URL with no path", "https://smba.trafficmanager.net/v3/x", "https://smba.trafficmanager.net")]
    [InlineData("a dot segment that stays inside", $"{ServiceUrl}v3/../v3/x", ServiceUrl)]
    [InlineData("a percent escape that differs only in the case of its hex digits", "https://smba.trafficmanager.net/a%2fb/v3/x", "https://smba.trafficmanager.net/a%2Fb/")]
    public void IsUnderServiceUrl_Accepts(string description, string url, string serviceUrl)
    {
        _ = description;
        Assert.True(FileDownloader.IsUnderServiceUrl(new Uri(url), new Uri(serviceUrl)));
    }

    [Theory]
    [InlineData("a sibling path that shares a prefix", "https://smba.trafficmanager.net/americas/v3/x", "https://smba.trafficmanager.net/amer")]
    [InlineData("a dot segment that climbs out", $"{ServiceUrl}../v3/x", ServiceUrl)]
    [InlineData("a percent-encoded dot segment that climbs out", $"{ServiceUrl}%2e%2e/v3/x", ServiceUrl)]
    [InlineData("an upper-case percent-encoded dot segment that climbs out", $"{ServiceUrl}%2E%2E/v3/x", ServiceUrl)]
    [InlineData("a mixed dot segment that climbs out", $"{ServiceUrl}.%2e/v3/x", ServiceUrl)]
    [InlineData("a path that differs only in case", "https://smba.trafficmanager.net/AMER/v3/x", ServiceUrl)]
    [InlineData("another host", "https://attacker.example/amer/v3/x", ServiceUrl)]
    [InlineData("a lookalike host", "https://smba.trafficmanager.net.attacker.example/amer/v3/x", "https://smba.trafficmanager.net")]
    [InlineData("another port", "https://smba.trafficmanager.net:8443/amer/v3/x", ServiceUrl)]
    [InlineData("an http target", "http://smba.trafficmanager.net/amer/v3/x", ServiceUrl)]
    [InlineData("an http service URL", "http://smba.trafficmanager.net/amer/v3/x", "http://smba.trafficmanager.net/amer/")]
    [InlineData("a missing service URL", $"{ServiceUrl}v3/x", null)]
    [InlineData("a relative service URL", $"{ServiceUrl}v3/x", "not a url")]
    [InlineData("a relative target", "not a url", ServiceUrl)]
    public void IsUnderServiceUrl_Rejects(string description, string url, string? serviceUrl)
    {
        _ = description;
        Uri target = new(url, UriKind.RelativeOrAbsolute);
        Uri? service = serviceUrl is null ? null : new Uri(serviceUrl, UriKind.RelativeOrAbsolute);

        Assert.False(FileDownloader.IsUnderServiceUrl(target, service));
    }

    // ==================== inline image destination ====================

    [Theory]
    [InlineData("another host", "https://attacker.example/amer/v3/attachments/a1/views/original")]
    [InlineData("a dot segment that climbs out of the service URL path", $"{ServiceUrl}../v3/attachments/a1/views/original")]
    [InlineData("a percent-encoded dot segment that climbs out of the service URL path", $"{ServiceUrl}%2e%2e/v3/attachments/a1/views/original")]
    [InlineData("a half-encoded dot segment (.%2e) that climbs out of the service URL path", $"{ServiceUrl}.%2e/v3/attachments/a1/views/original")]
    [InlineData("a half-encoded dot segment (%2E.) that climbs out of the service URL path", $"{ServiceUrl}%2E./v3/attachments/a1/views/original")]
    [InlineData("another port", "https://smba.trafficmanager.net:8443/amer/v3/attachments/a1/views/original")]
    public async Task RefusesAForgedTarget_BeforeResolvingTheTokenOrSendingARequest(string description, string contentUrl)
    {
        _ = description;
        RecordingHandler handler = new(HttpStatusCode.OK, Png);
        int tokenCalls = 0;
        BotApiCredential credential = new(FileActor.App, _ =>
        {
            tokenCalls++;
            return Task.FromResult<string?>("bot-api-token");
        }, new Uri(ServiceUrl));

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InlineFile(handler, contentUrl: contentUrl, credential: credential).DownloadAsync());

        Assert.Equal("cannot download inline image: content URL is not under the activity's service URL", error.Message);
        Assert.Equal(0, tokenCalls);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task RefusesEveryInlineImage_WhenTheCredentialCarriesNoServiceUrl()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, Png);
        int tokenCalls = 0;
        BotApiCredential credential = new(FileActor.App, _ =>
        {
            tokenCalls++;
            return Task.FromResult<string?>("bot-api-token");
        });

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InlineFile(handler, credential: credential).DownloadAsync());

        Assert.Contains("not under the activity's service URL", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, tokenCalls);
        Assert.Empty(handler.Calls);
    }

    // ==================== inline image download errors ====================

    private static readonly string TransformationBody = JsonSerializer.Serialize(new
    {
        error = new { code = "TransformationFailed", message = "Failed to transform the image" },
    });

    [Fact]
    public async Task Maps415ToInlineImageTransformationException_WithTheStatusCodeAndServiceText()
    {
        RecordingHandler handler = new(HttpStatusCode.UnsupportedMediaType, Utf8(TransformationBody));

        InlineImageTransformationException error = await Assert.ThrowsAsync<InlineImageTransformationException>(
            () => InlineFile(handler).DownloadAsync());

        Assert.IsAssignableFrom<FileException>(error);
        Assert.Equal(415, error.Status);
        Assert.Equal("TransformationFailed", error.Code);
        Assert.Equal("TransformationFailed: Failed to transform the image", error.Details);
        Assert.Equal(FileActor.App, error.Actor);
        Assert.Equal(
            "cannot download inline image: the platform could not convert it for download (status 415, code TransformationFailed). Some image formats, such as BMP and AVIF, cannot be converted by the platform, so Teams shows a placeholder, and they cannot be downloaded. Ask the user to send the image as a file attachment instead (service said: TransformationFailed: Failed to transform the image)",
            error.Message);
    }

    [Fact]
    public async Task OmitsTheCodeSegmentAndKeepsTheRawText_WhenA415BodyIsNotJson()
    {
        RecordingHandler handler = new(HttpStatusCode.UnsupportedMediaType, Utf8("<html>unsupported</html>"));

        InlineImageTransformationException error = await Assert.ThrowsAsync<InlineImageTransformationException>(
            () => InlineFile(handler).DownloadAsync());

        Assert.Null(error.Code);
        Assert.Equal("<html>unsupported</html>", error.Details);
        Assert.Contains("(status 415). Some image formats", error.Message, StringComparison.Ordinal);
        Assert.Contains("(service said: <html>unsupported</html>)", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OmitsTheServiceSaidSuffix_WhenA415HasNoBody()
    {
        RecordingHandler handler = new(HttpStatusCode.UnsupportedMediaType);

        InlineImageTransformationException error = await Assert.ThrowsAsync<InlineImageTransformationException>(
            () => InlineFile(handler).DownloadAsync());

        Assert.Null(error.Details);
        Assert.EndsWith("send the image as a file attachment instead", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BoundsALongServiceMessage()
    {
        // The error body is read only up to its limit, so the kept text is exactly that long.
        RecordingHandler handler = new(HttpStatusCode.UnsupportedMediaType, Utf8(new string('x', 10_000)));

        InlineImageTransformationException error = await Assert.ThrowsAsync<InlineImageTransformationException>(
            () => InlineFile(handler).DownloadAsync());

        Assert.Equal(new string('x', 2048), error.Details);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task MapsAccessFailuresToInlineImageAccessException_NamingTheIdentity(int status)
    {
        RecordingHandler handler = new((HttpStatusCode)status, Utf8(JsonSerializer.Serialize(new { error = new { code = "Unauthorized", message = "nope" } })));

        InlineImageAccessException error = await Assert.ThrowsAsync<InlineImageAccessException>(
            () => InlineFile(handler, credential: BotApi(FileActor.AgenticUser, "agent-token")).DownloadAsync());

        Assert.IsAssignableFrom<FileException>(error);
        Assert.Equal(status, error.Status);
        Assert.Equal(FileActor.AgenticUser, error.Actor);
        Assert.Equal("Unauthorized: nope", error.Details);
        Assert.Equal(
            $"cannot download inline image: the Bot API token presented for the agentic user was rejected with status {status} (service said: Unauthorized: nope)",
            error.Message);
    }

    [Fact]
    public async Task SurfacesAnotherFailureStatusAsAPlainException_CarryingTheServiceText()
    {
        RecordingHandler handler = new(HttpStatusCode.InternalServerError, Utf8(JsonSerializer.Serialize(new { error = new { code = "ServerError", message = "boom" } })));

        Exception error = await Assert.ThrowsAnyAsync<Exception>(() => InlineFile(handler).DownloadAsync());

        Assert.IsNotAssignableFrom<FileException>(error);
        Assert.Contains("500", error.Message, StringComparison.Ordinal);
        Assert.Contains("ServerError: boom", error.Message, StringComparison.Ordinal);
    }
}
