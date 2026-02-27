// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Api.Auth;

/// <summary>
/// Bundles all cloud-specific service endpoints for a given Azure environment.
/// All properties default to Microsoft public (commercial) cloud values.
/// Configure endpoints via appsettings.json or programmatically for sovereign clouds.
/// </summary>
public class CloudEnvironment
{
    /// <summary>
    /// The Azure AD login endpoint.
    /// </summary>
    public string LoginEndpoint { get; }

    /// <summary>
    /// The default login tenant.
    /// </summary>
    public string LoginTenant { get; }

    /// <summary>
    /// The Bot Framework OAuth scope.
    /// </summary>
    public string BotScope { get; }

    /// <summary>
    /// The Bot Framework token service base URL.
    /// </summary>
    public string TokenServiceUrl { get; }

    /// <summary>
    /// The OpenID metadata URL for token validation.
    /// </summary>
    public string OpenIdMetadataUrl { get; }

    /// <summary>
    /// The token issuer for Bot Framework tokens.
    /// </summary>
    public string TokenIssuer { get; }

    /// <summary>
    /// The channel service URL. Empty for public cloud.
    /// </summary>
    public string ChannelService { get; }

    /// <summary>
    /// The OAuth redirect URL.
    /// </summary>
    public string OAuthRedirectUrl { get; }

    public CloudEnvironment(
        string loginEndpoint = "https://login.microsoftonline.com",
        string loginTenant = "botframework.com",
        string botScope = "https://api.botframework.com/.default",
        string tokenServiceUrl = "https://token.botframework.com",
        string openIdMetadataUrl = "https://login.botframework.com/v1/.well-known/openidconfiguration",
        string tokenIssuer = "https://api.botframework.com",
        string channelService = "",
        string oauthRedirectUrl = "https://token.botframework.com/.auth/web/redirect")
    {
        LoginEndpoint = loginEndpoint;
        LoginTenant = loginTenant;
        BotScope = botScope;
        TokenServiceUrl = tokenServiceUrl;
        OpenIdMetadataUrl = openIdMetadataUrl;
        TokenIssuer = tokenIssuer;
        ChannelService = channelService;
        OAuthRedirectUrl = oauthRedirectUrl;
    }

    /// <summary>
    /// Creates a new <see cref="CloudEnvironment"/> by applying non-null overrides on top of this instance.
    /// Returns the same instance if all overrides are null (no allocation).
    /// </summary>
    public CloudEnvironment WithOverrides(
        string? loginEndpoint = null,
        string? loginTenant = null,
        string? botScope = null,
        string? tokenServiceUrl = null,
        string? openIdMetadataUrl = null,
        string? tokenIssuer = null,
        string? channelService = null,
        string? oauthRedirectUrl = null)
    {
        if (loginEndpoint is null && loginTenant is null && botScope is null &&
            tokenServiceUrl is null && openIdMetadataUrl is null && tokenIssuer is null &&
            channelService is null && oauthRedirectUrl is null)
        {
            return this;
        }

        return new CloudEnvironment(
            loginEndpoint ?? LoginEndpoint,
            loginTenant ?? LoginTenant,
            botScope ?? BotScope,
            tokenServiceUrl ?? TokenServiceUrl,
            openIdMetadataUrl ?? OpenIdMetadataUrl,
            tokenIssuer ?? TokenIssuer,
            channelService ?? ChannelService,
            oauthRedirectUrl ?? OAuthRedirectUrl
        );
    }
}
