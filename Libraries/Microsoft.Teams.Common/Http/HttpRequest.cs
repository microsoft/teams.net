namespace Microsoft.Teams.Common.Http;

public interface IHttpRequest : IHttpRequestOptions
{
    public HttpMethod Method { get; set; }
    public string Url { get; set; }
    public object? Body { get; set; }
}

public class HttpRequest : HttpRequestOptions, IHttpRequest
{
    public required HttpMethod Method { get; set; }
    public required string Url { get; set; }
    public object? Body { get; set; }

    public static HttpRequest Get(string url, IHttpRequestOptions? options = null)
    {
        return new HttpRequest()
        {
            Method = HttpMethod.Get,
            Url = url,
            Headers = options?.Headers ?? new Dictionary<string, IList<string>>(),
        };
    }

    public static HttpRequest Post(string url, object? body = default, IHttpRequestOptions? options = null)
    {
        return new HttpRequest()
        {
            Method = HttpMethod.Post,
            Url = url,
            Body = body,
            Headers = options?.Headers ?? new Dictionary<string, IList<string>>(),
        };
    }

    public static HttpRequest Patch(string url, object? body = default, IHttpRequestOptions? options = null)
    {
        return new HttpRequest()
        {
            Method = HttpMethod.Patch,
            Url = url,
            Body = body,
            Headers = options?.Headers ?? new Dictionary<string, IList<string>>(),
        };
    }

    public static HttpRequest Put(string url, object? body = default, IHttpRequestOptions? options = null)
    {
        return new HttpRequest()
        {
            Method = HttpMethod.Put,
            Url = url,
            Body = body,
            Headers = options?.Headers ?? new Dictionary<string, IList<string>>(),
        };
    }

    public static HttpRequest Delete(string url, IHttpRequestOptions? options = null)
    {
        return new HttpRequest()
        {
            Method = HttpMethod.Delete,
            Url = url,
            Headers = options?.Headers ?? new Dictionary<string, IList<string>>(),
        };
    }
}