
using Microsoft.Teams.Api.Clients;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ApiClientTests
{
    [Fact]
    public void ApiClient_Default()
    {
        var serviceUrl = "https://api.botframework.com";
        var apiClient = new ApiClient(serviceUrl);

        Assert.Equal(serviceUrl, apiClient.ServiceUrl);
    }

    [Fact]
    public void ApiClient_Users_Default()
    {
        var serviceUrl = "https://api.botframework.com";
        var apiClient = new ApiClient(serviceUrl);

        Assert.Equal(serviceUrl, apiClient.ServiceUrl);
    }
}