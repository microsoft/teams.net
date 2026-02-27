// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public class ClientCredentials : IHttpCredentials
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string? TenantId { get; set; }

    /// <summary>
    /// The Entra ID login endpoint, following the Microsoft Identity Web configuration schema.
    /// Override this for sovereign clouds (e.g. "https://login.microsoftonline.us" for US Gov).
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com";

    public ClientCredentials(string clientId, string clientSecret)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public ClientCredentials(string clientId, string clientSecret, string? tenantId)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        TenantId = tenantId;
    }

    public async Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantId ?? "botframework.com";
        var instance = Instance.TrimEnd('/');
        var request = HttpRequest.Post(
            $"{instance}/{tenantId}/oauth2/v2.0/token"
        );

        request.Headers.Add("Content-Type", ["application/x-www-form-urlencoded"]);
        request.Body = new Dictionary<string, string>()
        {
            { "grant_type", "client_credentials" },
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "scope", string.Join(",", scopes) }
        };

        var res = await client.SendAsync<TokenResponse>(request, cancellationToken);
        return res.Body;
    }
}