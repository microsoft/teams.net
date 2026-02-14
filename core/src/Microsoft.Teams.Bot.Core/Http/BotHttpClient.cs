// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Core.Http;
/// <summary>
/// Provides shared HTTP request functionality for bot clients.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests.</param>
/// <param name="logger">The logger instance used for logging. Optional.</param>
public class BotHttpClient(HttpClient httpClient, ILogger? logger = null)
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Sends an HTTP request and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="url">The full URL for the request.</param>
    /// <param name="body">The request body content. Optional.</param>
    /// <param name="options">The request options. Optional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response, or null if the response is empty or 404 (when ReturnNullOnNotFound is true).</returns>
    /// <exception cref="HttpRequestException">Thrown if the request fails and the failure is not handled by options.</exception>
    public async Task<T?> SendAsync<T>(
        HttpMethod method,
        string url,
        string? body = null,
        BotRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new BotRequestOptions();

        using HttpRequestMessage request = CreateRequest(method, url, body, options);

        logger?.LogTrace("Sending HTTP {Method} request to {Url} with body: {Body}", method, url, body);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await HandleResponseAsync<T>(response, method, url, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP request with query parameters and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="baseUrl">The base URL for the request.</param>
    /// <param name="endpoint">The endpoint path to append to the base URL.</param>
    /// <param name="queryParams">The query parameters to include in the request. Optional.</param>
    /// <param name="body">The request body content. Optional.</param>
    /// <param name="options">The request options. Optional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response, or null if the response is empty or 404 (when ReturnNullOnNotFound is true).</returns>
    /// <exception cref="HttpRequestException">Thrown if the request fails and the failure is not handled by options.</exception>
    public async Task<T?> SendAsync<T>(
        HttpMethod method,
        string baseUrl,
        string endpoint,
        Dictionary<string, string?>? queryParams = null,
        string? body = null,
        BotRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentNullException.ThrowIfNull(endpoint);

        string fullPath = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        string url = queryParams?.Count > 0
            ? QueryHelpers.AddQueryString(fullPath, queryParams)
            : fullPath;

        return await SendAsync<T>(method, url, body, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP request without expecting a response body.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="url">The full URL for the request.</param>
    /// <param name="body">The request body content. Optional.</param>
    /// <param name="options">The request options. Optional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the request fails.</exception>
    public async Task SendAsync(
        HttpMethod method,
        string url,
        string? body = null,
        BotRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(method, url, body, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP request with query parameters without expecting a response body.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="baseUrl">The base URL for the request.</param>
    /// <param name="endpoint">The endpoint path to append to the base URL.</param>
    /// <param name="queryParams">The query parameters to include in the request. Optional.</param>
    /// <param name="body">The request body content. Optional.</param>
    /// <param name="options">The request options. Optional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the request fails.</exception>
    public async Task SendAsync(
        HttpMethod method,
        string baseUrl,
        string endpoint,
        Dictionary<string, string?>? queryParams = null,
        string? body = null,
        BotRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(method, baseUrl, endpoint, queryParams, body, options, cancellationToken).ConfigureAwait(false);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? body, BotRequestOptions options)
    {
        HttpRequestMessage request = new(method, url);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        if (options.AgenticIdentity is not null)
        {
            request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, options.AgenticIdentity);
        }

        if (options.DefaultHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in options.DefaultHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (options.CustomHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in options.CustomHeaders)
            {
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return request;
    }

    private async Task<T?> HandleResponseAsync<T>(
        HttpResponseMessage response,
        HttpMethod method,
        string url,
        BotRequestOptions options,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return await DeserializeResponseAsync<T>(response, options, cancellationToken).ConfigureAwait(false);
        }

        if (response.StatusCode == HttpStatusCode.NotFound && options.ReturnNullOnNotFound)
        {
            logger?.LogWarning("Resource not found: {Url}", url);
            return default;
        }

        string errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string responseHeaders = FormatResponseHeaders(response);

        logger?.LogWarning(
            "HTTP request error {Method} {Url}\nStatus Code: {StatusCode}\nResponse Headers: {ResponseHeaders}\nResponse Body: {ResponseBody}",
            method, url, response.StatusCode, responseHeaders, errorContent);

        string operationDescription = options.OperationDescription ?? "request";
        throw new HttpRequestException(
            $"Error {operationDescription} {response.StatusCode}. {errorContent}",
            inner: null,
            statusCode: response.StatusCode);
    }

    private static async Task<T?> DeserializeResponseAsync<T>(
        HttpResponseMessage response,
        BotRequestOptions options,
        CancellationToken cancellationToken)
    {
        string responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(responseString) || responseString.Length <= 2)
        {
            return default;
        }

        if (typeof(T) == typeof(string))
        {
            try
            {
                T? result = JsonSerializer.Deserialize<T>(responseString, DefaultJsonOptions);
                return result ?? (T)(object)responseString;
            }
            catch (JsonException)
            {
                return (T)(object)responseString;
            }
        }

        T? deserializedResult = JsonSerializer.Deserialize<T>(responseString, DefaultJsonOptions);

        if (deserializedResult is null)
        {
            string operationDescription = options.OperationDescription ?? "request";
            throw new InvalidOperationException($"Failed to deserialize response for {operationDescription}");
        }

        return deserializedResult;
    }

    private static string FormatResponseHeaders(HttpResponseMessage response)
    {
        StringBuilder sb = new();

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Response header: {header.Key} : {string.Join(",", header.Value)}");
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.TrailingHeaders)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Response trailing header: {header.Key} : {string.Join(",", header.Value)}");
        }

        return sb.ToString();
    }
}
