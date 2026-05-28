// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
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

    /// <summary>
    /// Typed accessor over an already-validated JWT payload. These constructors
    /// perform no signature verification, no issuer/audience checks, and no
    /// expiry enforcement. Constructing this class from an untrusted token does
    /// NOT establish trust in the contained claims.
    /// </summary>
    /// <remarks>
    /// Signature verification happens at the HTTP trust boundary via the
    /// ASP.NET Core JwtBearer middleware configured by
    /// <c>TokenValidator.ConfigureValidation</c>
    /// (<c>Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore/Extensions/TokenValidator.cs</c>),
    /// applied to endpoints via <c>.RequireAuthorization(...)</c>. Internal
    /// callers may also construct from tokens sourced from trusted identity
    /// infrastructure (MSAL, Bot Framework API responses).
    /// <para>
    /// Callers must not construct this class from raw network input.
    /// </para>
    /// </remarks>
    public JsonWebToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        Token = handler.ReadJwtToken(token);
        _tokenAsString = token;
    }

    /// <summary>
    /// Typed accessor over a token returned by Teams identity infrastructure.
    /// Same trust-boundary contract as the string constructor; see its remarks.
    /// </summary>
    public JsonWebToken(Token.Response response)
    {
        var handler = new JwtSecurityTokenHandler();
        Token = handler.ReadJwtToken(response.Token);
        _tokenAsString = response.Token;
    }

    public override string ToString() => _tokenAsString;
}