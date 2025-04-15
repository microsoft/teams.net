namespace Microsoft.Teams.Common.Http;

public interface IHttpCredentialsFactory
{
    public IHttpCredentials? GetCredentials();
    public Task<IHttpCredentials?> GetCredentialsAsync();
}