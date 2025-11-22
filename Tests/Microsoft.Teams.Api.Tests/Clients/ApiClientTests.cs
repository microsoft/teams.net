
using Microsoft.Teams.Api.Clients;

namespace Microsoft.Teams.Api.Tests.Clients;
public class ApiClientTests
{
    [Fact]
    public void ApiClient_Default()
    {
        var serviceUrl = "https://api.botframework.com";
        var mockHandler = new Moq.Mock<Microsoft.Teams.Common.Http.IHttpClient>();
        var apiClient = new ApiClient(serviceUrl, mockHandler.Object, "scope");

        Assert.Equal(serviceUrl, apiClient.ServiceUrl);
    }

    [Fact]
    public void ApiClient_Users_Default()
    {
        var serviceUrl = "https://api.botframework.com";
        var mockHandler = new Moq.Mock<Microsoft.Teams.Common.Http.IHttpClient>();
        var apiClient = new ApiClient(serviceUrl, mockHandler.Object, "scope");

        Assert.Equal(serviceUrl, apiClient.ServiceUrl);
    }
}