// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable ExperimentalTeamsPlan // Tests for the experimental service error.

using System.Net;
using System.Text;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests;

public class PlanConflictExceptionTests
{
    [Theory]
    [InlineData(ActivityType.Typing)]
    [InlineData(ActivityType.Message)]
    public async Task ConversationClient_ClassifiesConflictAndPreservesDetailsWithoutRetry(string activityType)
    {
        const string body = """{"error":{"code":"PlanConflict","message":"Stopped","details":{"reason":"user"}}}""";
        RecordingHandler handler = new(HttpStatusCode.Conflict, body);
        using HttpClient http = new(handler);
        ConversationClient client = new(http);

        PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() =>
            client.SendActivityAsync("conversation", new CoreActivityInput(activityType), new Uri("https://example.com/")));

        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.Equal("PlanConflict", error.ErrorCode);
        Assert.Equal("Stopped", error.ErrorMessage);
        Assert.Equal(body, error.ResponseBody);
        HttpRequestException original = Assert.IsType<HttpRequestException>(error.InnerException);
        Assert.Equal(HttpStatusCode.Conflict, original.StatusCode);
        Assert.Equal(original.Message, error.Message);
        Assert.Contains(body, original.Message);
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData("""{"error":{"code":"PlanConflict"}}""")]
    [InlineData("""{"error":{"code":"PlanConflict","message":null}}""")]
    [InlineData("""{"error":{"code":"PlanConflict","message":{"detail":"Stopped"}}}""")]
    public async Task HttpClient_ClassifiesConflictWithoutRequiringStringMessage(string body)
    {
        RecordingHandler handler = new(HttpStatusCode.Conflict, body);
        using HttpClient http = new(handler);
        BotHttpClient client = new(http);

        PlanConflictException error = await Assert.ThrowsAsync<PlanConflictException>(() =>
            client.SendAsync<object>(HttpMethod.Post, "https://example.com/"));

        Assert.Null(error.ErrorMessage);
        Assert.Equal(body, error.ResponseBody);
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":"Other","message":"PlanConflict"}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":"planconflict"}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"Code":"PlanConflict"}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":null}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":409}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":["PlanConflict"]}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":{}}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":null}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":[]}""")]
    [InlineData(HttpStatusCode.Conflict, """{"error":"PlanConflict"}""")]
    [InlineData(HttpStatusCode.Conflict, """{"code":"PlanConflict"}""")]
    [InlineData(HttpStatusCode.Conflict, "[]")]
    [InlineData(HttpStatusCode.Conflict, "null")]
    [InlineData(HttpStatusCode.Conflict, "not json")]
    [InlineData(HttpStatusCode.Conflict, "")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":{"code":"PlanConflict"}}""")]
    [InlineData(HttpStatusCode.Forbidden, """{"error":{"code":"PlanConflict"}}""")]
    public async Task UnrecognizedServiceErrors_RemainOrdinaryHttpExceptions(HttpStatusCode status, string body)
    {
        RecordingHandler handler = new(status, body);
        using HttpClient http = new(handler);
        ConversationClient client = new(http);

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendActivityAsync("conversation", new CoreActivityInput(), new Uri("https://example.com/")));

        Assert.Equal(status, error.StatusCode);
        Assert.Contains(body, error.Message);
        Assert.Null(error.InnerException);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Classification_DoesNotParseFormattedExceptionMessage()
    {
        RecordingHandler handler = new(HttpStatusCode.Conflict, "{}");
        using HttpClient http = new(handler);
        BotHttpClient client = new(http);
        BotRequestOptions options = new() { OperationDescription = """{"error":{"code":"PlanConflict"}}""" };

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendAsync<object>(HttpMethod.Post, "https://example.com/", options: options));

        Assert.Contains("PlanConflict", error.Message);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task SuccessfulResponse_IsNotClassifiedByErrorCodeAlone()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, """{"id":"answer","error":{"code":"PlanConflict"}}""");
        using HttpClient http = new(handler);
        ConversationClient client = new(http);

        SendActivityResponse? response = await client.SendActivityAsync(
            "conversation", new CoreActivityInput(), new Uri("https://example.com/"));

        Assert.Equal("answer", response!.Id);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
