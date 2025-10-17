
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Common.Http;

namespace Samples.Agentic;

public class AgenticCredentials(IConfiguration config) : IHttpCredentials
{
    private readonly string tenantId = config["TeamsAG:TenantId"] ?? throw new ArgumentNullException("TeamsAG:TenantId");
    private readonly string clientId = config["TeamsAG:ClientId"] ?? throw new ArgumentNullException("TeamsAG:ClientId");
    private readonly string fmiPath = config["TeamsAG:FmiPath"] ?? throw new ArgumentNullException("TeamsAG:FmiPath");
    private readonly string secret = config["TeamsAG:ClientSecret"] ?? throw new ArgumentNullException("TeamsAG:ClientSecret");

    //public async Task<IHttpCredentials> AcquireAgenticTokenAsync(string tenantId, string clientId, string fmiPath, string secret, IEnumerable<string> scopes)
    public async Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, CancellationToken cancellationToken = default)
    {
        string authority = $"https://login.microsoftonline.com/{tenantId}";

        scopes = new[] { "api://AzureADTokenExchange/.default" };


        var agentAppClient = ConfidentialClientApplicationBuilder
           .Create(clientId)
           .WithAuthority(authority)
           .WithClientSecret(secret) // Use the managed identity token as the client assertion
           .Build();

        var result = await agentAppClient.AcquireTokenForClient(scopes)
            .WithFmiPath(fmiPath)
            .ExecuteAsync();

#pragma warning disable CS0618 // Type or member is obsolete
        IConfidentialClientApplication agentIdentityClient =
            ConfidentialClientApplicationBuilder.Create(fmiPath)
                        .WithClientAssertion(result.AccessToken)
                        .Build();
#pragma warning restore CS0618 // Type or member is obsolete

        AuthenticationResult agentAuthResult = await agentIdentityClient.AcquireTokenForClient(scopes)
                        .WithTenantId(tenantId)
                        .ExecuteAsync();

#pragma warning disable CS0618 // Type or member is obsolete
        IByUsernameAndPassword cca = (IByUsernameAndPassword)ConfidentialClientApplicationBuilder
                                .Create(fmiPath)
                                .WithTenantId(tenantId)
                                .WithClientAssertion(result.AccessToken)
                                .Build();
#pragma warning restore CS0618 // Type or member is obsolete

        string[] graphScope = new string[] { "https://graph.microsoft.com/.default" };
        AuthenticationResult userToken = await cca.AcquireTokenByUsernamePassword(graphScope,"A365-agentic-user4@testcsaaa.onmicrosoft.com","no_password")
                        .OnBeforeTokenRequest(async request =>
                        {
                            string userFicAssertion = agentAuthResult.AccessToken;
                            request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                            request.BodyParameters["grant_type"] = "user_fic";
                            request.BodyParameters["user_id"] = "f077912b-353d-4f93-b242-49726421e628";

                            // remove the password
                            request.BodyParameters.Remove("password");
                            request.BodyParameters.Remove("username");

                            if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                                    && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                            {
                                request.BodyParameters.Remove("client_secret");
                            }
                            await Task.CompletedTask;
                        }
                        )
                        .ExecuteAsync()
                        .ConfigureAwait(false);

        return new TokenResponse
        {
            AccessToken = userToken.AccessToken,
            TokenType = userToken.TokenType,
        };

    }


}
