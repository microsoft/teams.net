// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Http;

using IHttpHeaders = IDictionary<string, IList<string>>;

public interface IHttpRequestOptions
{
    public IHttpHeaders Headers { get; set; }

    public void AddUserAgent(IList<string> value);
    public void AddUserAgent(params string[] value);
    public void AddHeader(string key, IList<string> value);
    public void AddHeader(string key, params string[] value);
}

public class HttpRequestOptions : IHttpRequestOptions
{
    public IHttpHeaders Headers { get; set; } = new Dictionary<string, IList<string>>();

    public void AddUserAgent(IList<string> value)
    {
        AddHeader("User-Agent", value);
    }

    public void AddUserAgent(params string[] value)
    {
        AddHeader("User-Agent", value);
    }

    public void AddHeader(string key, IList<string> value)
    {
        Headers.TryGetValue(key, out IList<string>? values);
        values ??= [];

        foreach (var headerValue in value)
        {
            values.Add(headerValue);
        }

        Headers.Add(key, values);
    }

    public void AddHeader(string key, params string[] value)
    {
        Headers.TryGetValue(key, out IList<string>? values);
        values ??= [];

        foreach (var headerValue in value)
        {
            values.Add(headerValue);
        }

        Headers.Add(key, values);
    }
}