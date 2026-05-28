// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;

namespace McpServer;

// App-only Microsoft Graph client. Reuses the bot's AzureAd:TenantId / ClientId /
// ClientCredentials[0]:ClientSecret to acquire a token for graph.microsoft.com,
// then calls /users with $search. Requires User.ReadBasic.All (Application) consent.
public sealed class GraphClient
{
    private static readonly TokenRequestContext TokenContext = new(["https://graph.microsoft.com/.default"]);
    private readonly TokenCredential _credential;
    private readonly HttpClient _http;

    public GraphClient(IConfiguration config, HttpClient http)
    {
        string tenantId = config["AzureAd:TenantId"]
            ?? throw new InvalidOperationException("AzureAd:TenantId is not configured.");
        string clientId = config["AzureAd:ClientId"]
            ?? throw new InvalidOperationException("AzureAd:ClientId is not configured.");
        string clientSecret = config["AzureAd:ClientCredentials:0:ClientSecret"]
            ?? throw new InvalidOperationException("AzureAd:ClientCredentials:0:ClientSecret is not configured.");

        _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _http = http;
    }

    public async Task<IReadOnlyList<UserMatch>> SearchUsersAsync(
        string query, int top, CancellationToken cancellationToken)
    {
        AccessToken token = await _credential.GetTokenAsync(TokenContext, cancellationToken);

        string search = $"\"displayName:{query}\" OR \"userPrincipalName:{query}\"";
        string url = "https://graph.microsoft.com/v1.0/users"
            + $"?$search={Uri.EscapeDataString(search)}"
            + "&$select=id,displayName,userPrincipalName"
            + $"&$top={top}";

        using HttpRequestMessage req = new(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        req.Headers.Add("ConsistencyLevel", "eventual");

        using HttpResponseMessage resp = await _http.SendAsync(req, cancellationToken);
        resp.EnsureSuccessStatusCode();

        GraphUsersResponse? body = await resp.Content.ReadFromJsonAsync<GraphUsersResponse>(
            cancellationToken: cancellationToken);

        return body?.Value
            .Select(u => new UserMatch(u.Id, u.DisplayName, u.UserPrincipalName))
            .ToArray() ?? [];
    }

    private sealed record GraphUser(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("userPrincipalName")] string? UserPrincipalName);

    private sealed record GraphUsersResponse(
        [property: JsonPropertyName("value")] List<GraphUser> Value);
}
