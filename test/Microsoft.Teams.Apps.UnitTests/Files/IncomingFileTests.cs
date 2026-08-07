// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

public class IncomingFileTests
{
    private const string DownloadUrl = "https://download.example/notes.txt?tempauth=abc";

    // Hands back the enqueued responses in call order and records the URLs it saw.
    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public List<Uri?> Calls { get; } = [];

        public SequenceHttpMessageHandler Enqueue(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request.RequestUri);
            HttpResponseMessage response = _responses.Count > 0
                ? _responses.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        }
    }

    private static HttpResponseMessage Body(string text, string? contentType = null)
    {
        ByteArrayContent content = new(Encoding.UTF8.GetBytes(text));

        if (contentType is not null)
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage Status(HttpStatusCode status, string? reasonPhrase = null)
        => new(status) { ReasonPhrase = reasonPhrase };

    private static IncomingFile PersonalFile(SequenceHttpMessageHandler handler, string? contentType = null, string? downloadUrl = DownloadUrl, ConversationType? scope = null)
        => new()
        {
            Name = "notes.txt",
            ContentType = contentType,
            Scope = scope ?? ConversationType.Personal,
            Source = FileSource.BotActivity,
            DownloadUrl = downloadUrl is null ? null : new Uri(downloadUrl),
            HttpClient = new HttpClient(handler),
        };

    [Fact]
    public async Task Download_FetchesUrlAndBuffersBytes()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("hello world", "text/plain"));
        IncomingFile file = PersonalFile(handler);

        DownloadedFile downloaded = await file.DownloadAsync();

        Assert.Equal([new Uri(DownloadUrl)], handler.Calls);
        Assert.Equal("hello world", downloaded.Text());
        Assert.Equal("text/plain", downloaded.ContentType);
        Assert.Equal("notes.txt", downloaded.Filename);
        Assert.Equal(new Uri(DownloadUrl), downloaded.SourceUrl);
    }

    [Fact]
    public async Task Download_RefetchesOnEachCall()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler()
            .Enqueue(Body("a"))
            .Enqueue(Body("b"));
        IncomingFile file = PersonalFile(handler);

        Assert.Equal("a", (await file.DownloadAsync()).Text());
        Assert.Equal("b", (await file.DownloadAsync()).Text());
        Assert.Equal(2, handler.Calls.Count);
    }

    [Fact]
    public async Task Download_FallsBackToFileContentTypeWhenResponseOmitsOne()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("bytes"));
        IncomingFile file = PersonalFile(handler, contentType: "application/pdf");

        Assert.Equal("application/pdf", (await file.DownloadAsync()).ContentType);
    }

    [Fact]
    public async Task Text_DecodesDownloadedBytes()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("hello"));
        IncomingFile file = PersonalFile(handler);

        Assert.Equal("hello", await file.TextAsync());
    }

    [Fact]
    public async Task Download_FirstUnauthorized_ThrowsFirstFetch()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Status(HttpStatusCode.Unauthorized));
        IncomingFile file = PersonalFile(handler);

        FileUrlExpiredException ex = await Assert.ThrowsAsync<FileUrlExpiredException>(() => file.DownloadAsync());
        Assert.Equal(FileUrlExpiredReason.FirstFetch, ex.Reason);
    }

    [Fact]
    public async Task Download_Forbidden_TreatedAsExpired()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Status(HttpStatusCode.Forbidden));
        IncomingFile file = PersonalFile(handler);

        await Assert.ThrowsAsync<FileUrlExpiredException>(() => file.DownloadAsync());
    }

    [Fact]
    public async Task Download_RereadAfterSuccess_ThrowsReread()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler()
            .Enqueue(Body("first read ok"))
            .Enqueue(Status(HttpStatusCode.Unauthorized));
        IncomingFile file = PersonalFile(handler);

        Assert.Equal("first read ok", (await file.DownloadAsync()).Text());

        FileUrlExpiredException ex = await Assert.ThrowsAsync<FileUrlExpiredException>(() => file.DownloadAsync());
        Assert.Equal(FileUrlExpiredReason.Reread, ex.Reason);
    }

    [Fact]
    public async Task Stream_ReturnsRawBodyStream()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("streamed"));
        IncomingFile file = PersonalFile(handler);

        await using Stream stream = await file.StreamAsync();
        using StreamReader reader = new(stream);

        Assert.Equal("streamed", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Download_UnsupportedScope_Throws()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("unused"));
        IncomingFile file = PersonalFile(handler, scope: ConversationType.GroupChat);

        FileScopeNotSupportedException ex = await Assert.ThrowsAsync<FileScopeNotSupportedException>(() => file.DownloadAsync());
        Assert.Equal(ConversationType.GroupChat, ex.Scope);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task Download_NoDownloadUrl_Throws()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("unused"));
        IncomingFile file = PersonalFile(handler, downloadUrl: null);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => file.DownloadAsync());
        Assert.Contains("no download URL", ex.Message);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task Download_NonHttpsDownloadUrl_Throws()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("unused"));
        IncomingFile file = PersonalFile(handler, downloadUrl: "http://download.example/notes.txt");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => file.DownloadAsync());
        Assert.Contains("must use https", ex.Message);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task Download_NonAuthError_Throws()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Status(HttpStatusCode.InternalServerError, "Server Error"));
        IncomingFile file = PersonalFile(handler);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => file.DownloadAsync());
        Assert.Contains("failed to download file: 500", ex.Message);
    }

    [Fact]
    public async Task SaveAs_StreamsBytesToLocalFile()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("saved contents"));
        IncomingFile file = PersonalFile(handler);
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await file.SaveAsAsync(path);
            Assert.Equal("saved contents", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DownloadedFile_SaveAs_WritesSnapshotWithoutRefetching()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Enqueue(Body("snapshot bytes"));
        DownloadedFile downloaded = await PersonalFile(handler).DownloadAsync();
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await downloaded.SaveAsAsync(path);
            Assert.Equal("snapshot bytes", await File.ReadAllTextAsync(path));
            Assert.Single(handler.Calls);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
