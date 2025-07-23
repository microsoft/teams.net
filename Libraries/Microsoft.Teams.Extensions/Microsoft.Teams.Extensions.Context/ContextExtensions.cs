using Microsoft.Teams.Apps;
using Microsoft.Teams.Api.Activities;
using Microsoft.Graph;

namespace Microsoft.Teams.Extensions.Context;

public static class ContextExtensions
{
    /// <summary>
    /// Get the user's graph client from the context.
    /// </summary>
    public static GraphServiceClient? GetUserGraphClient<TActivity>(this IContext<TActivity> context) where TActivity : IActivity
    {
        var userToken = context.UserGraphToken;

        if (userToken is null)
        {
            return null;
        }

        if (context.Extra.TryGetValue("UserGraphClient", out var client) && client is GraphServiceClient graphClient)
        {
            return graphClient;
        }

        var userGraphTokenProvider = Azure.Core.DelegatedTokenCredential.Create((context, _) =>
        {
            return new Azure.Core.AccessToken(userToken.ToString(), userToken.Token.ValidTo);
        });

        graphClient = new GraphServiceClient(userGraphTokenProvider);
        context.Extra["UserGraphClient"] = graphClient;

        return graphClient;
    }
}