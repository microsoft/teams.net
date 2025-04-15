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
    public required HttpResponseHeaders Headers { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public required TBody Body { get; init; }
}