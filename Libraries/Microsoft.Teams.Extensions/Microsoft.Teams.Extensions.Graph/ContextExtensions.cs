using Microsoft.Teams.Apps;
using Microsoft.Teams.Api.Activities;
using Microsoft.Graph;

namespace Microsoft.Teams.Extensions.Graph;

public static class ContextExtensions
{
    /// <summary>
    /// Get user's graph client from the context.
    /// </summary>
    /// <typeparam name="TActivity">The activity type</typeparam>
    /// <param name="context">The context object</param>
    /// <returns>The graph client scoped to the user's token</returns>
    /// <exception cref="InvalidOperationException">If the user token doesn't exist on the context. That is, if the user is not signed in.</exception>
    public static GraphServiceClient GetUserGraphClient<TActivity>(this IContext<TActivity> context) where TActivity : IActivity
    {
        var userToken = context.UserGraphToken;

        if (userToken is null)
        {
            throw new InvalidOperationException("context.UserGraphToken is null. Ensure the user is signed in and the token is available in the context.");
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