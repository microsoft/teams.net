using Microsoft.Graph;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Common.Logging;

using Moq;

namespace Microsoft.Teams.Extensions.Graph.Tests;

public class ContextExtensionsTests
{
    [Fact]
    public void ContextExtensions_GetUserGraphClient_ShouldThrowException()
    {
        // Arrange
        var context = new Mock<IContext<IActivity>>();
        context.Setup(c => c.UserGraphToken).Returns((JsonWebToken?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.Object.GetUserGraphClient());
    }


    [Fact]
    public void ContextExtensions_GetUserGraphClient_ShouldReturnGraphClient()
    {
        // Arrange
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTc1MzI1MjAzNSwiaWF0IjoxNzUzMjUyMDM1fQ.J-DWberQuMBSnAECP0jmK-zX6BzB4o-rMEshkR0mN-A";
        var jwtToken = new JsonWebToken(token);
        var context = new Mock<IContext<IActivity>>();
        context.Setup(c => c.UserGraphToken).Returns(jwtToken);
        context.Setup(c => c.Extra).Returns(new Dictionary<string, object?>());

        // Act
        var graphClient = context.Object.GetUserGraphClient();

        // Assert
        Assert.NotNull(graphClient);
        Assert.True(context.Object.Extra.ContainsKey("UserGraphClient"));
        Assert.IsType<GraphServiceClient>(context.Object.Extra["UserGraphClient"]);
    }

    [Fact]
    public void ContextExtensions_GetUserGraphClient_ShouldReturnSingleGraphClient()
    {
        // Arrange
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTc1MzI1MjAzNSwiaWF0IjoxNzUzMjUyMDM1fQ.J-DWberQuMBSnAECP0jmK-zX6BzB4o-rMEshkR0mN-A";
        var jwtToken = new JsonWebToken(token);
        var credentials = new Mock<Azure.Core.TokenCredential>();
        var graphClientMock = new GraphServiceClient(credentials.Object);
        var context = new Mock<IContext<IActivity>>();
        context.Setup(c => c.UserGraphToken).Returns(jwtToken);
        context.Setup(c => c.Extra.TryGetValue("UserGraphClient", out It.Ref<object>.IsAny!)).Returns((string key, out object value) => { value = graphClientMock; return true; });

        // Act
        var graphClient = context.Object.GetUserGraphClient();

        // Assert
        Assert.NotNull(graphClient);
        Assert.Equal(graphClientMock, graphClient);
    }

    // --- Sovereign cloud Graph routing ---

    private static Mock<IContext<IActivity>> MockContextWith(CloudEnvironment? cloud, Mock<ILogger>? logger = null)
    {
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTc1MzI1MjAzNSwiaWF0IjoxNzUzMjUyMDM1fQ.J-DWberQuMBSnAECP0jmK-zX6BzB4o-rMEshkR0mN-A";
        var jwtToken = new JsonWebToken(token);
        var context = new Mock<IContext<IActivity>>();
        context.Setup(c => c.UserGraphToken).Returns(jwtToken);
        context.Setup(c => c.Extra).Returns(new Dictionary<string, object?>());
        context.Setup(c => c.Cloud).Returns(cloud!);
        context.Setup(c => c.Log).Returns((logger ?? new Mock<ILogger>()).Object);
        return context;
    }

    [Theory]
    [InlineData("USGov")]
    [InlineData("USGovDoD")]
    [InlineData("China")]
    [InlineData("Public")]
    public void GetUserGraphClient_RoutesToCloudSpecificBaseUrl(string cloudName)
    {
        var cloud = CloudEnvironment.FromName(cloudName);
        var expectedBaseUrl = cloud.GraphScope.Replace("/.default", string.Empty);
        var context = MockContextWith(cloud);

        var graphClient = context.Object.GetUserGraphClient();

        Assert.NotNull(graphClient);
        Assert.StartsWith(expectedBaseUrl, graphClient.RequestAdapter.BaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetUserGraphClient_NullCloud_UsesPublicDefault_NoWarning()
    {
        var logger = new Mock<ILogger>();
        var context = MockContextWith(cloud: null, logger: logger);

        var graphClient = context.Object.GetUserGraphClient();

        Assert.NotNull(graphClient);
        logger.Verify(l => l.Warn(It.IsAny<object?[]>()), Times.Never);
    }

    [Fact]
    public void GetUserGraphClient_NonUrlGraphScope_LogsWarningAndFallsBack()
    {
        var cloud = CloudEnvironment.Public.WithOverrides(graphScope: "user.read");
        var logger = new Mock<ILogger>();
        var context = MockContextWith(cloud, logger);

        var graphClient = context.Object.GetUserGraphClient();

        Assert.NotNull(graphClient);
        // Fallback: public base URL used (because scope didn't parse as URL)
        Assert.StartsWith("https://graph.microsoft.com", graphClient.RequestAdapter.BaseUrl, StringComparison.OrdinalIgnoreCase);
        logger.Verify(l => l.Warn(It.IsAny<object?[]>()), Times.Once);
    }
}