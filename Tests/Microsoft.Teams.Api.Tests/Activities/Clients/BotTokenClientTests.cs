using System.Net;
using System.Reflection;

using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;


namespace Microsoft.Teams.Api.Tests.Clients;

public class BotTokenClientTests
{
    string accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30";
    
    [Fact]
    public async Task BotTokenClient_defaultAsync()
    {
        var cancellationToken = new CancellationToken();

        TokenFactory tokenFactory = new TokenFactory(async (tenantId, scope) =>
        {
            return await Task.FromResult<ITokenResponse>(new TokenResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken
            });
        });
        var credentials = new TokenCredentials("clientId", tokenFactory);
        var botTokenClient = new BotTokenClient(cancellationToken);

        Assert.NotNull(botTokenClient);
        var botToken = await botTokenClient.GetAsync(credentials);
        Assert.NotNull(botToken);
        Assert.Equal(accessToken, new JsonWebToken(botToken.AccessToken).ToString());

        var botGraphToken = await botTokenClient.GetGraphAsync(credentials);
        Assert.Equal(accessToken, new JsonWebToken(botGraphToken.AccessToken).ToString());

    }
}
