// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Teams.Common.Http;

public interface IHttpResponse<TBody>
{
    public HttpResponseHeaders Headers { get; }
    public HttpStatusCode StatusCode { get; }
    public TBody Body { get; }
}

public class HttpResponse<TBody> : IHttpResponse<TBody>
{
    public required HttpResponseHeaders Headers { get; set; }
    public required HttpStatusCode StatusCode { get; set; }
    public required TBody Body { get; set; }
}