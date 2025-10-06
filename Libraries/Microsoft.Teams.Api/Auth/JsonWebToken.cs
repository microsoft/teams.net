// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Auth;

public class JsonWebToken : IToken
{
    [JsonPropertyName("appid")]
    public string? AppId => Token.Payload.TryGetValue("appid", out var value) ? (string?)value : null;

    [JsonPropertyName("app_displayname")]
    public string? AppDisplayName => Token.Payload.TryGetValue("app_displayname", out var value) ? (string?)value : null;

    [JsonPropertyName("tid")]
    public string? TenantId => Token.Payload.TryGetValue("tid", out var value) ? (string?)value : null;

    [JsonPropertyName("serviceurl")]
    public string ServiceUrl
    {
        get
        {
            var serviceUrl = Token.Payload.TryGetValue("serviceurl", out var value) ? (string?)value : null;

            if (serviceUrl is null)
            {
                serviceUrl = "https://smba.trafficmanager.net/teams";
            }

            if (!serviceUrl.EndsWith("/"))
            {
                serviceUrl += '/';
            }

            return serviceUrl;
        }
    }

    [JsonPropertyName("from")]
    public CallerType From
    {
        get => AppId is null ? CallerType.Azure : CallerType.Bot;
    }

    [JsonPropertyName("fromId")]
    public string FromId
    {
        get => From.IsBot ? $"urn:botframework:aadappid:{AppId}" : "urn:botframework:azure";
    }

    [JsonPropertyName("expiration")]
    public DateTime? Expiration
    {
        get => Token.ValidTo;
    }

    [JsonIgnore]
    public bool IsExpired
    {
        get => Token.ValidTo <= DateTime.UtcNow.AddMilliseconds(1000 * 60 * 5);
    }

    [JsonPropertyName("scopes")]
    public IEnumerable<string> Scopes
    {
        get
        {
            var claim = Token.Claims.FirstOrDefault(c => c.Type == "scope" || c.Type == "scp");
            if (claim is null) return [];
            return claim.Value.Split(' ');
        }
    }

    public JwtSecurityToken Token { get; }
    private readonly string _tokenAsString;

    public JsonWebToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        Token = handler.ReadJwtToken(token);
        _tokenAsString = token;
    }

    public JsonWebToken(Token.Response response)
    {
        var handler = new JwtSecurityTokenHandler();
        Token = handler.ReadJwtToken(response.Token);
        _tokenAsString = response.Token;
    }

    public JsonWebToken(ClaimsIdentity identity)
    {
        Token = new JwtSecurityToken(claims: identity.Claims);
        _tokenAsString = Token.ToString();
    }

    public override string ToString() => _tokenAsString;
}