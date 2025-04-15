using System.Text.Json;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps;

public partial interface IContext<TActivity>
{
    /// <summary>
    /// is the activity sender signed in?
    /// </summary>
    public bool IsSignedIn { get; set; }

    /// <summary>
    /// trigger user signin flow for the activity sender
    /// </summary>
    /// <param name="options">option overrides</param>
    /// <returns>the existing user token if found</returns>
    public Task<string?> SignIn(SignInOptions? options = null);

    /// <summary>
    /// trigger user signin flow for the activity sender
    /// </summary>
    /// <param name="connectionName">the connection name</param>
    public Task SignOut(string? connectionName = null);
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public bool IsSignedIn { get; set; } = false;

    public async Task<string?> SignIn(SignInOptions? options = null)
    {
        options ??= new SignInOptions();
        var reference = Ref.Copy();

        try
        {
            var tokenResponse = await Api.Users.Token.GetAsync(new()
            {
                UserId = Activity.From.Id,
                ChannelId = Activity.ChannelId,
                ConnectionName = options.ConnectionName,
            });

            return tokenResponse.Token;
        }
        catch { }

        // create new 1:1 conversation with user to do SSO
        // because groupchats don't support it.
        if (Activity.Conversation.IsGroup == true)
        {
            var (id, _, _) = await Api.Conversations.CreateAsync(new()
            {
                TenantId = Ref.Conversation.TenantId,
                IsGroup = false,
                Bot = Ref.Bot,
                Members = [Activity.From]
            });

            reference.Conversation.Id = id;
            reference.Conversation.IsGroup = false;

            var oauthCardActivity = await Sender.Send(new MessageActivity(options.OAuthCardText), reference, CancellationToken);
            await OnActivitySent(oauthCardActivity, (IContext<IActivity>)this);
        }

        var tokenExchangeState = new Api.TokenExchange.State()
        {
            ConnectionName = options.ConnectionName,
            Conversation = reference,
            RelatesTo = Activity.RelatesTo,
            MsAppId = AppId
        };

        var state = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(tokenExchangeState));
        var resource = await Api.Bots.SignIn.GetResourceAsync(new() { State = state });
        var activity = new MessageActivity();

        activity.InputHint = InputHint.AcceptingInput;
        activity.Recipient = Activity.From;
        activity.Conversation = reference.Conversation;
        activity.AddAttachment(new Api.Cards.OAuthCard()
        {
            Text = options.OAuthCardText,
            ConnectionName = options.ConnectionName,
            TokenExchangeResource = resource.TokenExchangeResource,
            TokenPostResource = resource.TokenPostResource,
            Buttons = [
                new(Teams.Api.Cards.ActionType.SignIn)
                {
                    Title = options.SignInButtonText,
                    Value = resource.SignInLink
                }
            ]
        });

        var res = await Sender.Send(activity, reference, CancellationToken);
        await OnActivitySent(res, (IContext<IActivity>)this);
        return null;
    }

    public async Task SignOut(string? connectionName = null)
    {
        await Api.Users.Token.SignOutAsync(new()
        {
            ChannelId = Ref.ChannelId,
            UserId = Activity.From.Id,
            ConnectionName = connectionName ?? "graph",
        });
    }
}

public class SignInOptions
{
    /// <summary>
    /// the connection name
    /// </summary>
    public string ConnectionName { get; set; } = "graph";

    /// <summary>
    /// the oauth card text
    /// </summary>
    public string OAuthCardText { get; set; } = "Please Sign In...";

    /// <summary>
    /// the sign in button text
    /// </summary>
    public string SignInButtonText { get; set; } = "Sign In";
}