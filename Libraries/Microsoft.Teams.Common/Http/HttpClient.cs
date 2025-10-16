// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
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

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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

        if (request.Body is not null)
        {
            if (request.Body is string stringBody)
            {
                httpRequest.Content = new StringContent(stringBody);
            }
            else if (request.Body is IEnumerable<KeyValuePair<string, string>> dictionaryBody)
            {
                httpRequest.Content = new FormUrlEncodedContent(dictionaryBody);
            }
            else
            {
                string body = JsonSerializer.Serialize(request.Body, _jsonSerializerOptions);
                httpRequest.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }
        }

        foreach (var kv in request.Headers)
        {
            if (kv.Key.StartsWith("Content-") && httpRequest.Content != null)
            {
                if (kv.Key == "Content-Type")
                {
                    httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(kv.Value.First());
                    continue;
                }

                httpRequest.Content.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                continue;
            }

            httpRequest.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }
        return httpRequest;
    }

    protected async Task<IHttpResponse<string>> CreateResponse(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await ParseErrorBody(response);

            throw new HttpException()
            {
                Headers = response.Headers,
                StatusCode = response.StatusCode,
                Body = errorBody,
                Request = response.RequestMessage
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
            var errorBody = await ParseErrorBody(response);

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

    private async Task<object> ParseErrorBody(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync() ?? throw new ArgumentNullException();
        object errorBody = content;

        try
        {
            var bodyAsJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (bodyAsJson is not null)
            {
                errorBody = bodyAsJson;
            }
        }
        catch
        {
            // content is probably not a valid json
        }

        return errorBody;
    }
}