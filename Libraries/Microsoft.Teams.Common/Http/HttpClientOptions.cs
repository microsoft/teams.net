using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Common.Http;

/// <summary>
/// Http Client Options
/// </summary>
public interface IHttpClientOptions : IHttpRequestOptions
{
    /// <summary>
    /// The client name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The authorization token to use
    /// </summary>
    public object? Token { get; set; }

    /// <summary>
    /// The authorization token factory to use
    /// </summary>
    public HttpTokenFactory? TokenFactory { get; set; }

    /// <summary>
    /// ILogger instance to use
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Default request timeout (ms)
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// apply options to an http client
    /// </summary>
    /// <param name="client">the client to apply the http options to</param>
    public void Apply(System.Net.Http.HttpClient client);

    /// <summary>
    /// apply options to an http request
    /// </summary>
    /// <param name="request">the request to apply the http options to</param>
    public void Apply(HttpRequestMessage request);

    /// <summary>
    /// a factory for adding a token to http requests
    /// </summary>
    public delegate object? HttpTokenFactory();
}

/// <summary>
/// Http Client Options
/// </summary>
public class HttpClientOptions : HttpRequestOptions, IHttpClientOptions
{
    /// <summary>
    /// The client name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The authorization token to use
    /// </summary>
    public object? Token { get; set; }

    /// <summary>
    /// The authorization token factory to use
    /// </summary>
    public IHttpClientOptions.HttpTokenFactory? TokenFactory { get; set; }

    /// <summary>
    /// ILogger instance to use
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Default request timeout (ms)
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// apply options to an http client
    /// </summary>
    /// <param name="client">the client to apply the http options to</param>
    public void Apply(System.Net.Http.HttpClient client)
    {
        if (Timeout != null)
            client.Timeout = (TimeSpan)Timeout;

        if (Token != null)
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

        foreach (var (key, value) in Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
        }
    }

    /// <summary>
    /// apply options to an http request
    /// </summary>
    /// <param name="request">the request to apply the http options to</param>
    public void Apply(HttpRequestMessage request)
    {
        if (TokenFactory != null)
        {
            var token = TokenFactory();

            if (token != null)
            {
                request.Headers.Authorization = new("Bearer", token.ToString());
            }
        }

        foreach (var (key, value) in Headers)
        {
            if (key.StartsWith("Content-"))
            {
                request.Content?.Headers.TryAddWithoutValidation(key, value);
                continue;
            }

            request.Headers.TryAddWithoutValidation(key, value);
        }
    }
}