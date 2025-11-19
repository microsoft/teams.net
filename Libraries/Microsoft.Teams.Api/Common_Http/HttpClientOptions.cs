// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;
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
    public Task Apply(System.Net.Http.HttpClient client);

    /// <summary>
    /// apply options to an http request
    /// </summary>
    /// <param name="request">the request to apply the http options to</param>
    public Task Apply(HttpRequestMessage request, AgenticIdentity aid);

    /// <summary>
    /// a factory for adding a token to http requests
    /// </summary>
    public delegate Task<object?> HttpTokenFactory(AgenticIdentity? aid);
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
    public async Task Apply(System.Net.Http.HttpClient client)
    {
        if (Timeout is not null)
            client.Timeout = (TimeSpan)Timeout;

        if (Token is not null)
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

        foreach (var kv in Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value);
        }
    }

    /// <summary>
    /// apply options to an http request
    /// </summary>
    /// <param name="request">the request to apply the http options to</param>
    public async Task Apply(HttpRequestMessage request, AgenticIdentity? aid)
    {

        if (TokenFactory is not null)
        {
            var token = await TokenFactory(aid);

            if (token is not null)
            {
                request.Headers.Authorization = new("Bearer", token.ToString());
            }
        }

        foreach (var kv in Headers)
        {
            if (kv.Key.StartsWith("Content-"))
            {
                request.Content?.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                continue;
            }

            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }
    }
}