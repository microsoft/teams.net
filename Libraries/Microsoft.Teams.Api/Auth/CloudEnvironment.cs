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
    /// The Microsoft Graph token scope (e.g. "https://graph.microsoft.com/.default").
    /// </summary>
    public string GraphScope { get; }

    public CloudEnvironment(
        string loginEndpoint,
        string loginTenant,
        string botScope,
        string tokenServiceUrl,
        string openIdMetadataUrl,
        string tokenIssuer,
        string graphScope)
    {
        LoginEndpoint = loginEndpoint.TrimEnd('/');
        LoginTenant = loginTenant;
        BotScope = botScope;
        TokenServiceUrl = tokenServiceUrl.TrimEnd('/');
        OpenIdMetadataUrl = openIdMetadataUrl;
        TokenIssuer = tokenIssuer;
        GraphScope = graphScope;
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
        graphScope: "https://graph.microsoft.com/.default"
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
        graphScope: "https://graph.microsoft.us/.default"
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
        graphScope: "https://dod-graph.microsoft.us/.default"
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
        graphScope: "https://microsoftgraph.chinacloudapi.cn/.default"
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
        string? graphScope = null)
    {
        if (loginEndpoint is null && loginTenant is null && botScope is null &&
            tokenServiceUrl is null && openIdMetadataUrl is null && tokenIssuer is null &&
            graphScope is null)
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
            graphScope ?? GraphScope
        );
    }

    /// <summary>
    /// Resolves a cloud environment name (case-insensitive) to its corresponding instance.
    /// Valid names: "Public", "USGov", "USGovDoD", "China".
    /// </summary>
    public static CloudEnvironment FromName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Cloud environment name cannot be empty or whitespace.", nameof(name));
        }

        return name.ToLowerInvariant() switch
        {
            "public" => Public,
            "usgov" => USGov,
            "usgovdod" => USGovDoD,
            "china" => China,
            _ => throw new ArgumentException($"Unknown cloud environment: '{name}'. Valid values are: Public, USGov, USGovDoD, China.", nameof(name))
        };
    }
}
