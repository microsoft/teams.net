// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps;

public partial interface IContext<TActivity>
{
    /// <summary>
    /// is the activity sender signed in?
    /// </summary>
    public bool IsSignedIn { get; set; }

    /// <summary>
    /// the default connection name to use for the app.
    /// by default it is  "graph".
    /// </summary>
    public string ConnectionName { get; set; }

    /// <summary>
    /// trigger user OAuth signin flow for the activity sender
    /// </summary>
    /// <param name="options">option overrides</param>
    /// <returns>the existing user token if found</returns>
    public Task<string?> SignIn(OAuthOptions? options = null);

    /// <summary>
    /// trigger user SSO signin flow for the activity sender
    /// </summary>
    /// <param name="options">option overrides</param>
    public Task SignIn(SSOOptions options);

    /// <summary>
    /// trigger user signin flow for the activity sender
    /// </summary>
    /// <param name="connectionName">the connection name</param>
    public Task SignOut(string? connectionName = null);
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public bool IsSignedIn { get; set; } = false;
    public required string ConnectionName { get; set; }

    public async Task<string?> SignIn(OAuthOptions? options = null)
    {
        options ??= new OAuthOptions();
        var reference = Ref.Copy();

        AgenticIdentity? aid = AgenticIdentity.FromProperties(this.Activity.Recipient.Properties!);
        Api.Users.Token.AgenticIdentity = aid;
        
        try
        {
            var tokenResponse = await Api.Users.Token.GetAsync(new()
            {
                UserId = Activity.From.Id,
                ChannelId = Activity.ChannelId,
                ConnectionName = options.ConnectionName ?? ConnectionName,
            });

            return tokenResponse.Token;
        }
        catch { }

        var tokenExchangeState = new Api.TokenExchange.State()
        {
            ConnectionName = options.ConnectionName ?? ConnectionName,
            Conversation = reference,
            RelatesTo = Activity.RelatesTo,
            MsAppId = AppId
        };

        if (Activity.Conversation.IsGroup == true)
        {
            // create new 1:1 conversation with user to do SSO
            // because groupchats don't support it.
            var (id, _, _) = await Api.Conversations.CreateAsync(new()
            {
                TenantId = Ref.Conversation.TenantId,
                IsGroup = false,
                Bot = Ref.Bot,
                Members = [Activity.From]
            });

            reference.Conversation.Id = id;
            reference.Conversation.IsGroup = false;

            var oauthCardActivity = await Sender.Send(new MessageActivity(options.OAuthCardText), reference, false, CancellationToken);
            await OnActivitySent(oauthCardActivity, ToActivityType());
        }

        var state = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(tokenExchangeState));
        Api.Bots.SignIn.AgenticIdentity = aid;
        var resource = await Api.Bots.SignIn.GetResourceAsync(new() { State = state });
        var activity = new MessageActivity();

        activity.InputHint = InputHint.AcceptingInput;
        activity.Recipient = Activity.From;
        activity.Conversation = reference.Conversation;
        activity.AddAttachment(new Api.Cards.OAuthCard()
        {
            Text = options.OAuthCardText,
            ConnectionName = options.ConnectionName ?? ConnectionName,
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

        var res = await Sender.Send(activity, reference, false, CancellationToken);
        await OnActivitySent(res, ToActivityType());
        return null;
    }

    public async Task SignIn(SSOOptions options)
    {
        var signInLink = $"{options.SignInLink}?scope={Uri.EscapeDataString(string.Join(" ", options.Scopes))}&clientId={AppId}&tenantId={TenantId}";
        var reference = Ref.Copy();

        if (Activity.Conversation.IsGroup == true)
        {
            // create new 1:1 conversation with user to do SSO
            // because groupchats don't support it.
            var (id, _, _) = await Api.Conversations.CreateAsync(new()
            {
                TenantId = Ref.Conversation.TenantId,
                IsGroup = false,
                Bot = Ref.Bot,
                Members = [Activity.From]
            });

            reference.Conversation.Id = id;
            reference.Conversation.IsGroup = false;

            var oauthCardActivity = await Sender.Send(new MessageActivity(options.OAuthCardText), reference, false, CancellationToken);
            await OnActivitySent(oauthCardActivity, ToActivityType());
        }

        var activity = new MessageActivity();

        activity.InputHint = InputHint.AcceptingInput;
        activity.Recipient = Activity.From;
        activity.Conversation = reference.Conversation;
        activity.AddAttachment(new Api.Cards.OAuthCard()
        {
            Text = options.OAuthCardText,
            TokenExchangeResource = new()
            {
                Id = Guid.NewGuid().ToString()
            },
            Buttons = [
                new(Teams.Api.Cards.ActionType.SignIn)
                {
                    Title = options.SignInButtonText,
                    Value = options.SignInLink
                }
            ]
        });

        var res = await Sender.Send(activity, reference, false, CancellationToken);
        await OnActivitySent(res, ToActivityType());
    }

    public async Task SignOut(string? connectionName = null)
    {
        await Api.Users.Token.SignOutAsync(new()
        {
            ChannelId = Ref.ChannelId,
            UserId = Activity.From.Id,
            ConnectionName = connectionName ?? ConnectionName,
        });
    }
}

/// <summary>
/// base sign in options type
/// </summary>
public abstract class SignInOptions
{
    /// <summary>
    /// the oauth card text
    /// </summary>
    public string OAuthCardText { get; set; } = "Please Sign In...";

    /// <summary>
    /// the sign in button text
    /// </summary>
    public string SignInButtonText { get; set; } = "Sign In";
}

/// <summary>
/// OAuth sign in options
/// </summary>
public class OAuthOptions : SignInOptions
{
    /// <summary>
    /// the auth connection name to use, defaults
    /// to the default connection name of the app
    /// </summary>
    public string? ConnectionName { get; set; }
}

/// <summary>
/// SSO sign in options
/// </summary>
public class SSOOptions : SignInOptions
{
    /// <summary>
    /// the scopes to request consent for
    /// </summary>
    public required string[] Scopes { get; set; }

    /// <summary>
    /// the sign in link to use, defaults to
    /// the link returned by the sign in resource
    /// </summary>
    public required string SignInLink { get; set; }
}