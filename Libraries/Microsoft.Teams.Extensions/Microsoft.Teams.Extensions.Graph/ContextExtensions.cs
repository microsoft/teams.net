using System.Text.RegularExpressions;

using Microsoft.Graph;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace Microsoft.Teams.Extensions.Graph;

public static class ContextExtensions
{
    // Extracts scheme + host (+ optional port) from a URL-like scope such as
    // "https://graph.microsoft.us/.default" -> "https://graph.microsoft.us".
    private static readonly Regex _graphBaseUrlRegex = new(@"^(https?://[^/]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        // Derive per-cloud Graph base URL from the configured cloud's graphScope.
        // Falls back to the public Graph endpoint if the scope isn't a URL.
        var graphScope = context.Cloud?.GraphScope?.Trim();
        string? baseUrl = null;
        if (!string.IsNullOrEmpty(graphScope))
        {
            var match = _graphBaseUrlRegex.Match(graphScope);
            if (match.Success)
            {
                baseUrl = match.Groups[1].Value;
            }
            else
            {
                context.Log.Warn($"graphScope \"{graphScope}\" is not a URL; Graph calls will route to the public cloud. " +
                    "Set graphScope to an \"https://<host>/.default\" value to route to the correct Graph endpoint.");
            }
        }

        graphClient = baseUrl is null
            ? new GraphServiceClient(userGraphTokenProvider)
            : new GraphServiceClient(userGraphTokenProvider, scopes: null, baseUrl: baseUrl);
        context.Extra["UserGraphClient"] = graphClient;

        return graphClient;
    }
}