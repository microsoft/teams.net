// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }

    /// <summary>The Azure AD login endpoint.</summary>
    public string? LoginEndpoint { get; set; }

    /// <summary>The default login tenant.</summary>
    public string? LoginTenant { get; set; }

    /// <summary>The Bot Framework OAuth scope.</summary>
    public string? BotScope { get; set; }

    /// <summary>The Bot Framework token service URL.</summary>
    public string? TokenServiceUrl { get; set; }

    /// <summary>The OpenID metadata URL for token validation.</summary>
    public string? OpenIdMetadataUrl { get; set; }

    /// <summary>The token issuer for Bot Framework tokens.</summary>
    public string? TokenIssuer { get; set; }

    /// <summary>The channel service URL.</summary>
    public string? ChannelService { get; set; }

    /// <summary>The OAuth redirect URL.</summary>
    public string? OAuthRedirectUrl { get; set; }

    public bool Empty
    {
        get { return ClientId == "" || ClientSecret == ""; }
    }

    /// <summary>
    /// Resolves the <see cref="CloudEnvironment"/> by starting from <paramref name="programmaticCloud"/>
    /// (or a new default instance), then applying any per-endpoint overrides from settings.
    /// </summary>
    public CloudEnvironment ResolveCloud(CloudEnvironment? programmaticCloud = null)
    {
        var baseCloud = programmaticCloud ?? new CloudEnvironment();

        return baseCloud.WithOverrides(
            loginEndpoint: LoginEndpoint,
            loginTenant: LoginTenant,
            botScope: BotScope,
            tokenServiceUrl: TokenServiceUrl,
            openIdMetadataUrl: OpenIdMetadataUrl,
            tokenIssuer: TokenIssuer,
            channelService: ChannelService,
            oauthRedirectUrl: OAuthRedirectUrl
        );
    }

    public AppOptions Apply(AppOptions? options = null)
    {
        options ??= new AppOptions();

        var cloud = ResolveCloud(options.Cloud);
        options.Cloud = cloud;

        if (ClientId is not null && ClientSecret is not null && !Empty)
        {
            var credentials = new ClientCredentials(ClientId, ClientSecret, TenantId)
            {
                Cloud = cloud
            };
            options.Credentials = credentials;
        }

        return options;
    }
}