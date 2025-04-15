namespace Microsoft.Teams.Common.Http;

public interface IHttpClientFactory
{
    public IHttpClient CreateClient();
    public IHttpClient CreateClient(string name);
}