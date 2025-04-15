using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Auth;

public class JsonWebToken : IToken
{
    [JsonPropertyName("appid")]
    public string? AppId
    {
        get => (string?)Token.Payload.GetValueOrDefault("appid");
    }

    [JsonPropertyName("app_displayname")]
    public string? AppDisplayName
    {
        get => (string?)Token.Payload.GetValueOrDefault("app_displayname");
    }

    [JsonPropertyName("tid")]
    public string? TenantId
    {
        get => (string?)Token.Payload.GetValueOrDefault("tid");
    }

    [JsonPropertyName("serviceurl")]
    public string ServiceUrl
    {
        get
        {
            var value = ((string?)Token.Payload.GetValueOrDefault("serviceurl")) ?? "https://smba.trafficmanager.net/teams";

            if (!value.EndsWith('/'))
            {
                value += '/';
            }

            return value;
        }
    }

    [JsonPropertyName("from")]
    public CallerType From
    {
        get => AppId == null ? CallerType.Azure : CallerType.Bot;
    }

    [JsonPropertyName("fromId")]
    public string FromId
    {
        get => From.IsBot ? $"urn:botframework:aadappid:{AppId}" : "urn:botframework:azure";
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

    public override string ToString() => _tokenAsString;
}