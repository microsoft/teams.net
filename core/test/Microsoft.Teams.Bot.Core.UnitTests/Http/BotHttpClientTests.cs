// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core.Http;
using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Bot.Core.UnitTests.Http;

public class BotHttpClientTests
{
    private static BotHttpClient BuildClient(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Mock<HttpMessageHandler> handler = new();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        return new BotHttpClient(new HttpClient(handler.Object), NullLogger.Instance);
    }

    [Fact]
    public async Task SendAsync_WhenResponseIsJsonString_ReturnsUnquotedString()
    {
        // Body is a JSON-quoted string: "hello world"
        BotHttpClient client = BuildClient("\"hello world\"");

        string? result = await client.SendAsync<string>(HttpMethod.Get, "https://example.com/");

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async Task SendAsync_WhenResponseIsPlainText_ReturnsRawText()
    {
        // Body is plain text (not JSON), e.g. some legacy endpoint
        BotHttpClient client = BuildClient("plain text response");

        string? result = await client.SendAsync<string>(HttpMethod.Get, "https://example.com/");

        Assert.Equal("plain text response", result);
    }

    [Fact]
    public async Task SendAsync_WhenResponseIsJsonObject_DeserializesCorrectly()
    {
        // Normal JSON object deserialization must still work
        BotHttpClient client = BuildClient("""{"id":"act-123"}""");

        TestResponse? result = await client.SendAsync<TestResponse>(HttpMethod.Get, "https://example.com/");

        Assert.NotNull(result);
        Assert.Equal("act-123", result.Id);
    }

    [Fact]
    public async Task SendAsync_WhenResponseBodyIsEmpty_ReturnsNull()
    {
        // Empty body (e.g. 204 No Content with empty string)
        BotHttpClient client = BuildClient(string.Empty);

        string? result = await client.SendAsync<string>(HttpMethod.Get, "https://example.com/");

        Assert.Null(result);
    }

    private sealed record TestResponse(string Id);
}
