// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Api.Auth;

/// <summary>
/// Bundles all cloud-specific service endpoints for a given Azure environment.
/// Use predefined instances (<see cref="Public"/>, <see cref="USGov"/>, <see cref="USGovDoD"/>, <see cref="China"/>)
/// or construct a custom one.
/// </summary>
public class CloudEnvironment
{
    /// <summary>
    /// The Azure AD login endpoint (e.g. "https://login.microsoftonline.com").
    /// </summary>
    public string LoginEndpoint { get; }

    /// <summary>
    /// The default multi-tenant login tenant (e.g. "botframework.com").
    /// </summary>
    public string LoginTenant { get; }

    /// <summary>
    /// The Bot Framework OAuth scope (e.g. "https://api.botframework.com/.default").
    /// </summary>
    public string BotScope { get; }

    /// <summary>
    /// The Bot Framework token service base URL (e.g. "https://token.botframework.com").
    /// </summary>
    public string TokenServiceUrl { get; }

    /// <summary>
    /// The OpenID metadata URL for token validation (e.g. "https://login.botframework.com/v1/.well-known/openidconfiguration").
    /// </summary>
    public string OpenIdMetadataUrl { get; }

    /// <summary>
    /// The token issuer for Bot Framework tokens (e.g. "https://api.botframework.com").
    /// </summary>
    public string TokenIssuer { get; }

    /// <summary>
    /// The channel service URL. Empty for public cloud; set for sovereign clouds
    /// (e.g. "https://botframework.azure.us").
    /// </summary>
    public string ChannelService { get; }

    /// <summary>
    /// The OAuth redirect URL (e.g. "https://token.botframework.com/.auth/web/redirect").
    /// </summary>
    public string OAuthRedirectUrl { get; }

    public CloudEnvironment(
        string loginEndpoint,
        string loginTenant,
        string botScope,
        string tokenServiceUrl,
        string openIdMetadataUrl,
        string tokenIssuer,
        string channelService,
        string oauthRedirectUrl)
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
    /// Microsoft public (commercial) cloud.
    /// </summary>
    public static readonly CloudEnvironment Public = new(
        loginEndpoint: "https://login.microsoftonline.com",
        loginTenant: "botframework.com",
        botScope: "https://api.botframework.com/.default",
        tokenServiceUrl: "https://token.botframework.com",
        openIdMetadataUrl: "https://login.botframework.com/v1/.well-known/openidconfiguration",
        tokenIssuer: "https://api.botframework.com",
        channelService: "",
        oauthRedirectUrl: "https://token.botframework.com/.auth/web/redirect"
    );

    /// <summary>
    /// US Government Community Cloud High (GCCH).
    /// </summary>
    public static readonly CloudEnvironment USGov = new(
        loginEndpoint: "https://login.microsoftonline.us",
        loginTenant: "MicrosoftServices.onmicrosoft.us",
        botScope: "https://api.botframework.us/.default",
        tokenServiceUrl: "https://tokengcch.botframework.azure.us",
        openIdMetadataUrl: "https://login.botframework.azure.us/v1/.well-known/openidconfiguration",
        tokenIssuer: "https://api.botframework.us",
        channelService: "https://botframework.azure.us",
        oauthRedirectUrl: "https://tokengcch.botframework.azure.us/.auth/web/redirect"
    );

    /// <summary>
    /// US Government Department of Defense (DoD).
    /// </summary>
    public static readonly CloudEnvironment USGovDoD = new(
        loginEndpoint: "https://login.microsoftonline.us",
        loginTenant: "MicrosoftServices.onmicrosoft.us",
        botScope: "https://api.botframework.us/.default",
        tokenServiceUrl: "https://apiDoD.botframework.azure.us",
        openIdMetadataUrl: "https://login.botframework.azure.us/v1/.well-known/openidconfiguration",
        tokenIssuer: "https://api.botframework.us",
        channelService: "https://botframework.azure.us",
        oauthRedirectUrl: "https://apiDoD.botframework.azure.us/.auth/web/redirect"
    );

    /// <summary>
    /// China cloud (21Vianet).
    /// </summary>
    public static readonly CloudEnvironment China = new(
        loginEndpoint: "https://login.partner.microsoftonline.cn",
        loginTenant: "microsoftservices.partner.onmschina.cn",
        botScope: "https://api.botframework.azure.cn/.default",
        tokenServiceUrl: "https://token.botframework.azure.cn",
        openIdMetadataUrl: "https://login.botframework.azure.cn/v1/.well-known/openidconfiguration",
        tokenIssuer: "https://api.botframework.azure.cn",
        channelService: "https://botframework.azure.cn",
        oauthRedirectUrl: "https://token.botframework.azure.cn/.auth/web/redirect"
    );

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

    /// <summary>
    /// Resolves a cloud environment name (case-insensitive) to its corresponding instance.
    /// Valid names: "Public", "USGov", "USGovDoD", "China".
    /// </summary>
    public static CloudEnvironment FromName(string name) => name.ToLowerInvariant() switch
    {
        "public" => Public,
        "usgov" => USGov,
        "usgovdod" => USGovDoD,
        "china" => China,
        _ => throw new ArgumentException($"Unknown cloud environment: '{name}'. Valid values are: Public, USGov, USGovDoD, China.", nameof(name))
    };
}
