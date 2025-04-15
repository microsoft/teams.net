namespace Microsoft.Teams.Common.Http;

public interface ITokenResponse
{
    public string TokenType { get; }
    public int? ExpiresIn { get; }
    public string AccessToken { get; }
}