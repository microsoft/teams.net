using Microsoft.Graph;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;

using Moq;

namespace Microsoft.Teams.Extensions.Context;

public class ContextExtensionsTests
{
    [Fact]
    public void ContextExtensions_GetUserGraphClient_ShouldReturnNull()
    {
        // Arrange
        var context = new Mock<IContext<IActivity>>();
        context.Setup(c => c.UserGraphToken).Returns((JsonWebToken?)null);

        // Act
        var client = context.Object.GetUserGraphClient();

        // Assert
        Assert.Null(client);
    }

    [Fact]
    public void ContextExtensions_GetUserGraphClient_ShouldReturnGraphClient()
    {
        // Arrange
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTc1MzI1MjAzNSwiaWF0IjoxNzUzMjUyMDM1fQ.J-DWberQuMBSnAECP0jmK-zX6BzB4o-rMEshkR0mN-A";
        var jwtToken = new JsonWebToken(token);
        var context = new Mock<IContext<IActivity>>();
        context.Setup(c => c.UserGraphToken).Returns(jwtToken);
        context.Setup(c => c.Extra).Returns(new Dictionary<string, object>());

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
}