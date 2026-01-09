using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Core.Compat;

internal sealed class CompatUserTokenClient(Core.UserTokenClient utc) : Connector.Authentication.UserTokenClient
{
    public async override Task<TokenStatus[]> GetTokenStatusAsync(string userId, string channelId, string includeFilter, CancellationToken cancellationToken)
    {
        GetTokenStatusResult[] res = await utc.GetTokenStatusAsync(userId, channelId, includeFilter, cancellationToken).ConfigureAwait(false);
        return res.Select(t => new TokenStatus
        {
            ChannelId = channelId,
            ConnectionName = t.ConnectionName,
            HasToken = t.HasToken,
            ServiceProviderDisplayName = t.ServiceProviderDisplayName,
        }).ToArray();
    }

    public async override Task<TokenResponse> GetUserTokenAsync(string userId, string connectionName, string channelId, string magicCode, CancellationToken cancellationToken)
    {
        GetTokenResult res = await utc.GetTokenAsync(userId, connectionName, channelId, magicCode, cancellationToken).ConfigureAwait(false);
        return new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = res.ConnectionName,
            Token = res.Token,
        };
    }

    public async override Task<SignInResource> GetSignInResourceAsync(string connectionName, Activity activity, string finalRedirect, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        GetSignInResourceResult res = await utc.GetSignInResource(activity.From.Id, connectionName, activity.ChannelId, finalRedirect, cancellationToken).ConfigureAwait(false);
        return new SignInResource
        {
            SignInLink = res!.SignInLink,
            TokenExchangeResource = null,
            TokenPostResource = null,
        };
    }

    public async override Task<TokenResponse> ExchangeTokenAsync(string userId, string connectionName, string channelId,
     TokenExchangeRequest exchangeRequest, CancellationToken cancellationToken)
    {
        GetTokenResult resp = await utc.ExchangeTokenAsync(userId, connectionName, channelId, exchangeRequest.Token,
        cancellationToken).ConfigureAwait(false);
        return new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = resp.ConnectionName,
            Token = resp.Token
        };
    }

    public async override Task SignOutUserAsync(string userId, string connectionName, string channelId, CancellationToken cancellationToken)
    {
        await utc.SignOutUserAsync(userId, connectionName, channelId, cancellationToken).ConfigureAwait(false);
    }

    public async override Task<Dictionary<string, TokenResponse>> GetAadTokensAsync(string userId, string connectionName, string[] resourceUrls, string channelId, CancellationToken cancellationToken)
    {
        IDictionary<string, GetTokenResult> res = await utc.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls, cancellationToken).ConfigureAwait(false);
        return res?.ToDictionary(kvp => kvp.Key, kvp => new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = kvp.Value.ConnectionName,
            Token = kvp.Value.Token
        }) ?? new Dictionary<string, TokenResponse>();
    }
}
