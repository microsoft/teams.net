// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Teams.Apps.Files;

namespace Microsoft.Teams.Apps.UnitTests.Files;

public class FileRedirectHandlerTests
{
    /// <summary>Answers each request with the next scripted response and records what was sent.</summary>
    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(Uri Url, AuthenticationHeaderValue? Authorization)> Sent { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Sent.Add((request.RequestUri!, request.Headers.Authorization));
            HttpResponseMessage response = respond(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private static HttpResponseMessage Redirect(string location) => new(HttpStatusCode.Found) { Headers = { Location = new Uri(location, UriKind.RelativeOrAbsolute) } };

    private static async Task<HttpResponseMessage> SendAsync(ScriptedHandler inner, string url)
    {
        using HttpMessageInvoker invoker = new(new FileRedirectHandler { InnerHandler = inner });
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-credential");
        return await invoker.SendAsync(request, CancellationToken.None);
    }

    [Fact]
    public async Task RefusesAnHttpsToHttpRedirect_BeforeSendingIt()
    {
        ScriptedHandler inner = new(_ => Redirect("http://storage.example/blob"));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(inner, "https://smba.trafficmanager.net/amer/v3/attachments/a1/views/original"));

        Assert.Equal("cannot download file: a redirect destination must use https, got 'http://storage.example'. The file's bytes would cross that hop in the clear.", ex.Message);
        Assert.Single(inner.Sent);
    }

    [Fact]
    public async Task KeepsAuthorizationOnARelativeRedirect_ToTheSameOrigin()
    {
        ScriptedHandler inner = new(r => r.RequestUri!.AbsolutePath == "/next" ? new HttpResponseMessage(HttpStatusCode.OK) : Redirect("/next"));

        using HttpResponseMessage response = await SendAsync(inner, "https://smba.trafficmanager.net/start");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Uri("https://smba.trafficmanager.net/next"), inner.Sent[1].Url);
        Assert.Equal("test-credential", inner.Sent[1].Authorization?.Parameter);
    }

    [Fact]
    public async Task DropsAuthorizationForGood_OnceAHopLeavesTheOrigin()
    {
        ScriptedHandler inner = new(r => r.RequestUri!.AbsolutePath switch
        {
            "/start" => Redirect("https://storage.example/away"),
            "/away" => Redirect("https://smba.trafficmanager.net/back"),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
        });

        using HttpResponseMessage response = await SendAsync(inner, "https://smba.trafficmanager.net/start");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, inner.Sent.Count);
        Assert.Null(inner.Sent[1].Authorization);
        Assert.Null(inner.Sent[2].Authorization);
    }

    [Fact]
    public async Task StopsFollowingAfterTheRedirectLimit()
    {
        ScriptedHandler inner = new(_ => Redirect("https://smba.trafficmanager.net/again"));

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => SendAsync(inner, "https://smba.trafficmanager.net/start"));

        Assert.Equal("cannot download file: too many redirects", ex.Message);
        Assert.Equal(FileRedirectHandler.MaxRedirects + 1, inner.Sent.Count);
    }
}
