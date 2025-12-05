using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Core.Compat.Adapter;

public class CompatUserTokenClient(UserTokenClient utc) : Microsoft.Bot.Connector.Authentication.UserTokenClient
{
    public async override Task<TokenResponse> ExchangeTokenAsync(string userId, string connectionName, string channelId, TokenExchangeRequest exchangeRequest, CancellationToken cancellationToken)
    {
        string resp = await utc.ExchangeTokenAsync(userId, connectionName, channelId, exchangeRequest.ToString()!, cancellationToken);
        return new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = connectionName,
            Token = "token",
            //Expiration = resp.Expiration,
        };
    }

    public override async Task<Dictionary<string, TokenResponse>> GetAadTokensAsync(string userId, string connectionName, string[] resourceUrls, string channelId, CancellationToken cancellationToken)
    {
        string res = await utc.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls, cancellationToken);
        return new Dictionary<string, TokenResponse>();
    }

    public override async Task<SignInResource> GetSignInResourceAsync(string connectionName, Activity activity, string finalRedirect, CancellationToken cancellationToken)
    {
        IUserTokenClient.GetSignInResourceResult res = await utc.GetTokenOrSignInResource(connectionName, activity.From.Id, activity.ChannelId, finalRedirect, cancellationToken);
        return new SignInResource
        {
            SignInLink = res.SignInResource!.SignInLink,
            TokenExchangeResource = null
        };

    }

    public override async Task<TokenStatus[]> GetTokenStatusAsync(string userId, string channelId, string includeFilter, CancellationToken cancellationToken)
    {
        IUserTokenClient.GetTokenStatusResult[] res = await utc.GetTokenStatusAsync(userId, channelId, includeFilter, cancellationToken);
        return res.Select(t => new TokenStatus
        {
            ConnectionName = t.ConnectionName,
            HasToken = t.HasToken,
            ServiceProviderDisplayName = t.ServiceProviderDisplayName,
        }).ToArray();
    }

    public async override Task<TokenResponse> GetUserTokenAsync(string userId, string connectionName, string channelId, string magicCode, CancellationToken cancellationToken)
    {
        IUserTokenClient.GetTokenResult res = await utc.GetTokenAsync(userId, connectionName, channelId, magicCode, cancellationToken);
        return new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = connectionName,
            Token = res.Token,
            //Expiration = res.Expiration,
        };
    }

    public async override Task SignOutUserAsync(string userId, string connectionName, string channelId, CancellationToken cancellationToken)
    {
        await utc.SignOutUserAsync(userId, connectionName, channelId, cancellationToken);
    }
}
