
using Microsoft.Teams.Api.Clients;

namespace Microsoft.Teams.Api.Tests.Clients;

public class BotClientTests
{
    [Fact]
    public void BotClient_Default()
    {
        var botClient = new BotClient();

        Assert.NotNull(botClient.Token);
        Assert.NotNull(botClient.SignIn);
    }


    [Fact]
    public void UserClient_Default()
    {
        var userClient = new UserClient();

        Assert.NotNull(userClient.Token);
    }
}