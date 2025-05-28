using Microsoft.Teams.Api.TaskModules;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Common.Tests.Http;

public class HttpRequestTests
{


    [Fact]
    public void HttpRequest_ValidateHttpRequest_Get()
    {
        HttpRequest request = HttpRequest.Get("https://mocked-url.com");
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://mocked-url.com", request.Url);
        Assert.Null(request.Body);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_GetWithHeader()
    {
        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");

        HttpRequest request = HttpRequest.Get("https://mocked-url.com", options);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://mocked-url.com", request.Url);
        Assert.Null(request.Body);
        Assert.Equal(2, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_Post()
    {
        HttpRequest request = HttpRequest.Post("https://mocked-url.com?data=true");
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://mocked-url.com?data=true", request.Url);
        Assert.Null(request.Body);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_PostWithHeader()
    {
        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");
        options.AddUserAgent("mocked-user-agent");

        HttpRequest request = HttpRequest.Post("https://mocked-url.com?data=true", null, options);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://mocked-url.com?data=true", request.Url);
        Assert.Null(request.Body);
        Assert.Equal(3, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
        Assert.Contains("mocked-user-agent", request.Headers["User-Agent"]);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_PostWithBody()
    {
        ContinueTask continueTask = new ContinueTask(new TaskInfo()
        {
            Title = "mocked-title",
            Url = "https://mocked-url.com",
            Card = null,
            FallbackUrl = "https://mocked-fallback-url.com",
            CompletionBotId = "mocked-bot-id",
        });

        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");


        HttpRequest request = HttpRequest.Post("https://mocked-url.com?data=true", continueTask, options);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://mocked-url.com?data=true", request.Url);
        Assert.Equal(2, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_Patch()
    {
        HttpRequest request = HttpRequest.Patch("https://mocked-url.com?data=true");
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://mocked-url.com?data=true", request.Url);
        Assert.Null(request.Body);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_PatchWithHeader()
    {
        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");
        options.AddUserAgent(["mocked-user-agent1", "mocked-user-agent2"]);

        HttpRequest request = HttpRequest.Patch("https://mocked-url.com?data=true", null, options);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://mocked-url.com?data=true", request.Url);
        Assert.Null(request.Body);
        Assert.Equal(3, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
        Assert.Equal(2, request.Headers["User-Agent"].Count);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_PatchWithBody()
    {
        ContinueTask continueTask = new ContinueTask(new TaskInfo()
        {
            Title = "mocked-title",
            Url = "https://mocked-url.com",
            Card = null,
            FallbackUrl = "https://mocked-fallback-url.com",
            CompletionBotId = "mocked-bot-id",
        });

        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");


        HttpRequest request = HttpRequest.Patch("https://mocked-url.com?data=true", continueTask, options);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://mocked-url.com?data=true", request.Url);
        Assert.Equal(2, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_Put()
    {
        ContinueTask continueTask = new ContinueTask(new TaskInfo()
        {
            Title = "mocked-title",
            Url = "https://mocked-url.com",
            Card = null,
            FallbackUrl = "https://mocked-fallback-url.com",
            CompletionBotId = "mocked-bot-id",
        });

        HttpRequest request = HttpRequest.Put("https://mocked-url.com", continueTask);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("https://mocked-url.com", request.Url);
        Assert.Equal(continueTask, request.Body);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_PutWithHeader()
    {
        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");
        ContinueTask continueTask = new ContinueTask(new TaskInfo()
        {
            Title = "mocked-title",
            Url = "https://mocked-url.com",
            Card = null,
            FallbackUrl = "https://mocked-fallback-url.com",
            CompletionBotId = "mocked-bot-id",
        });

        HttpRequest request = HttpRequest.Put("https://mocked-url.com", continueTask, options);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("https://mocked-url.com", request.Url);
        Assert.Equal(continueTask, request.Body);
        Assert.Equal(2, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_Delete()
    {

        HttpRequest request = HttpRequest.Delete("https://mocked-url.com?tabid=abc");
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("https://mocked-url.com?tabid=abc", request.Url);
        Assert.Null(request.Body);
    }

    [Fact]
    public void HttpRequest_ValidateHttpRequest_DeleteWithHeader()
    {
        var options = new Common.Http.HttpRequestOptions();
        options.AddHeader("Authorization", "Bearer mocked-token");
        options.AddHeader("key", "value");

        HttpRequest request = HttpRequest.Delete("https://mocked-url.com?tabid=abc", options);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("https://mocked-url.com?tabid=abc", request.Url);
        Assert.Null(request.Body);
        Assert.Equal(2, request.Headers.Count);
        Assert.True(request.Headers.ContainsKey("Authorization"));
        Assert.Contains("Bearer mocked-token", request.Headers["Authorization"]);
        Assert.Contains("value", request.Headers["key"]);
    }
}