using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Common.Http;

public interface IHttpClient : IDisposable
{
    public IHttpClientOptions Options { get; }

    public Task<IHttpResponse<string>> SendAsync(IHttpRequest request, CancellationToken cancellationToken = default);
    public Task<IHttpResponse<TResponseBody>> SendAsync<TResponseBody>(IHttpRequest request, CancellationToken cancellationToken = default);
}

public class HttpClient : IHttpClient
{
    public IHttpClientOptions Options { get; }

    protected System.Net.Http.HttpClient _client;
    protected ILogger _logger;

    public HttpClient()
    {
        _client = new System.Net.Http.HttpClient();
        _logger = new ConsoleLogger().Child("Http.Client");
        Options = new HttpClientOptions();
        Options.Apply(_client);
    }

    public HttpClient(IHttpClientOptions options)
    {
        _client = new System.Net.Http.HttpClient();
        _logger = options.Logger?.Child("Http.Client") ?? new ConsoleLogger().Child("Http.Client");
        Options = options;
        Options.Apply(_client);
    }

    public HttpClient(System.Net.Http.HttpClient client)
    {
        _client = client;
        _logger = new ConsoleLogger().Child("Http.Client");
        Options = new HttpClientOptions();
        Options.Apply(_client);
    }

    public async Task<IHttpResponse<string>> SendAsync(IHttpRequest request, CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateRequest(request);
        var httpResponse = await _client.SendAsync(httpRequest);
        return await CreateResponse(httpResponse, cancellationToken);
    }

    public async Task<IHttpResponse<TResponseBody>> SendAsync<TResponseBody>(IHttpRequest request, CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateRequest(request);
        var httpResponse = await _client.SendAsync(httpRequest, cancellationToken);
        return await CreateResponse<TResponseBody>(httpResponse, cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    protected HttpRequestMessage CreateRequest(IHttpRequest request)
    {
        var httpRequest = new HttpRequestMessage(
            request.Method,
            request.Url
        );

        Options.Apply(httpRequest);

        foreach (var kv in request.Headers)
        {
            if (kv.Key.StartsWith("Content-"))
            {
                httpRequest.Content?.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                continue;
            }

            httpRequest.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        if (request.Body != null)
        {
            if (request.Body is string stringBody)
            {
                httpRequest.Content = new StringContent(stringBody);
                return httpRequest;
            }

            if (request.Body is IEnumerable<KeyValuePair<string, string>> dictionaryBody)
            {
                httpRequest.Content = new FormUrlEncodedContent(dictionaryBody);
                return httpRequest;
            }

            httpRequest.Content = JsonContent.Create(request.Body, options: new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        return httpRequest;
    }

    protected async Task<IHttpResponse<string>> CreateResponse(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken);

            throw new HttpException()
            {
                Headers = response.Headers,
                StatusCode = response.StatusCode,
                Body = errorBody
            };
        }

        var body = await response.Content.ReadAsStringAsync() ?? throw new ArgumentNullException();

        return new HttpResponse<string>()
        {
            Body = body,
            Headers = response.Headers,
            StatusCode = response.StatusCode
        };
    }

    protected async Task<IHttpResponse<TResponseBody>> CreateResponse<TResponseBody>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync() ?? throw new ArgumentNullException();
            object errorBody = content;

            if (content != string.Empty)
            {
                var bodyAsJson = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                if (bodyAsJson != null)
                    errorBody = bodyAsJson;
            }

            throw new HttpException()
            {
                Headers = response.Headers,
                StatusCode = response.StatusCode,
                Body = errorBody,
                Request = response.RequestMessage,
            };
        }

        var body = await response.Content.ReadFromJsonAsync<TResponseBody>(cancellationToken) ?? throw new ArgumentNullException();

        return new HttpResponse<TResponseBody>()
        {
            Body = body,
            Headers = response.Headers,
            StatusCode = response.StatusCode
        };
    }
}