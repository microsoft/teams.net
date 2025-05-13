using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;




namespace Microsoft.Teams.Api.Tests.Clients;

public class BotTokenClientTests
{
    readonly string accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30";

    [Fact]
    public async Task BotTokenClient_Default_GetAsync_Async()
    {
        var cancellationToken = new CancellationToken();

        string? actualTenantId = "";
        string[] actualScope = [""];
        TokenFactory tokenFactory = new TokenFactory(async (tenantId, scope) =>
        {
            actualTenantId = tenantId;
            actualScope = scope;
            return await Task.FromResult<ITokenResponse>(new TokenResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken
            });
        });
        var credentials = new TokenCredentials("clientId", tokenFactory);
        var botTokenClient = new BotTokenClient(cancellationToken);

        var botToken = await botTokenClient.GetAsync(credentials);

        Assert.NotNull(botToken);
        Assert.Equal(accessToken, new JsonWebToken(botToken.AccessToken).ToString());
        Assert.Null(actualTenantId);
        Assert.Equal("https://api.botframework.com/.default", actualScope[0]);
    }

    [Fact]
    public async Task BotTokenClient_Default_GetGraphAsync_Async()
    {
        var cancellationToken = new CancellationToken();
        string? actualTenantId = "";
        string[] actualScope = [""];
        TokenFactory tokenFactory = new TokenFactory(async (tenantId, scope) =>
        {
            actualTenantId = tenantId;
            actualScope = scope;
            return await Task.FromResult<ITokenResponse>(new TokenResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken
            });
        });
        var credentials = new TokenCredentials("clientId", tokenFactory);
        var botTokenClient = new BotTokenClient(cancellationToken);

        var botGraphToken = await botTokenClient.GetGraphAsync(credentials);

        Assert.Equal(accessToken, new JsonWebToken(botGraphToken.AccessToken).ToString());
        Assert.Null(actualTenantId);
        Assert.Equal("https://graph.microsoft.com/.default", actualScope[0]);
    }

    [Fact]
    public async Task BotTokenClient_withTenantIdAsync()
    {
        var cancellationToken = new CancellationToken();
        string? actualTenantId = "";
        string[] actualScope = [""];
        TokenFactory tokenFactory = new TokenFactory(async (tenantId, scope) =>
        {
            actualTenantId = tenantId;
            actualScope = scope;
            return await Task.FromResult<ITokenResponse>(new TokenResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken
            });
        });
        var credentials = new TokenCredentials("clientId", "123-abc", tokenFactory);
        var botTokenClient = new BotTokenClient(cancellationToken);
        string expectedTenantId = "123-abc";

        var botGraphToken = await botTokenClient.GetGraphAsync(credentials);

        Assert.Equal(accessToken, new JsonWebToken(botGraphToken.AccessToken).ToString());
        Assert.Equal(expectedTenantId, actualTenantId);
        Assert.Equal("https://graph.microsoft.com/.default", actualScope[0]);
    }

    [Fact]
    public async Task BotTokenClient_httpClient_Async()
    {
        var cancellationToken = new CancellationToken();
        string? actualTenantId = "";
        string[] actualScope = [""];
        TokenFactory tokenFactory = new TokenFactory(async (tenantId, scope) =>
        {
            actualTenantId = tenantId;
            actualScope = scope;
            return await Task.FromResult<ITokenResponse>(new TokenResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken
            });
        });

        var credentials = new TokenCredentials("clientId", "123-abc", tokenFactory);
        var httpClient = new Common.Http.HttpClient();
        var botTokenClient = new BotTokenClient(httpClient, cancellationToken);

        var botToken = await botTokenClient.GetAsync(credentials);

        string expectedTenantId = "123-abc";
        Assert.Equal(expectedTenantId, actualTenantId);
        Assert.Equal("https://api.botframework.com/.default", actualScope[0]);

    }

    [Fact]
    public async Task BotTokenClient_HttpClientOptions_Async()
    {
        var cancellationToken = new CancellationToken();
        string? actualTenantId = "";
        string[] actualScope = [""];
        TokenFactory tokenFactory = new TokenFactory(async (tenantId, scope) =>
        {
            actualTenantId = tenantId;
            actualScope = scope;
            return await Task.FromResult<ITokenResponse>(new TokenResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken
            });
        });
        var credentials = new TokenCredentials("clientId", "123-abc", tokenFactory);
        var httpClientOtions = new Common.Http.HttpClientOptions();
        var botTokenClient = new BotTokenClient(httpClientOtions, cancellationToken);

        var botToken = await botTokenClient.GetAsync(credentials);

        string expectedTenantId = "123-abc";
        Assert.Equal(expectedTenantId, actualTenantId);
        Assert.Equal("https://api.botframework.com/.default", actualScope[0]);
    }
}
