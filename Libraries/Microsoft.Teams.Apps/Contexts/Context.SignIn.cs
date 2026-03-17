// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Clients;

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
    /// <param name="cancellationToken">optional cancellation token</param>
    /// <returns>the existing user token if found</returns>
    public Task<string?> SignIn(OAuthOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// trigger user SSO signin flow for the activity sender
    /// </summary>
    /// <param name="options">option overrides</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task SignIn(SSOOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// trigger user signin flow for the activity sender
    /// </summary>
    /// <param name="connectionName">the connection name</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task SignOut(string? connectionName = null, CancellationToken cancellationToken = default);
}

public partial class Context<TActivity> : IContext<TActivity>
{
    public bool IsSignedIn { get; set; } = false;
    public required string ConnectionName { get; set; }

    public async Task<string?> SignIn(OAuthOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new OAuthOptions();
        var reference = Ref.Copy();
        var token = cancellationToken == default ? CancellationToken : cancellationToken;
        var api = new ApiClient(Api, token);

        try
        {
            var tokenResponse = await api.Users.Token.GetAsync(new()
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
            var (id, _, _) = await api.Conversations.CreateAsync(new()
            {
                TenantId = Ref.Conversation.TenantId,
                IsGroup = false,
                Bot = Ref.Bot,
                Members = [Activity.From]
            });

            reference.Conversation.Id = id;
            reference.Conversation.IsGroup = false;

            var oauthCardActivity = await Sender.Send(new MessageActivity(options.OAuthCardText), reference, token);
            await OnActivitySent(oauthCardActivity, ToActivityType());
        }

        var state = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(tokenExchangeState));
        var resource = await api.Bots.SignIn.GetResourceAsync(new() { State = state });
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

        var res = await Sender.Send(activity, reference, token);
        await OnActivitySent(res, ToActivityType());
        return null;
    }

    public async Task SignIn(SSOOptions options, CancellationToken cancellationToken = default)
    {
        var token = cancellationToken == default ? CancellationToken : cancellationToken;
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

            var oauthCardActivity = await Sender.Send(new MessageActivity(options.OAuthCardText), reference, token);
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

        var res = await Sender.Send(activity, reference, token);
        await OnActivitySent(res, ToActivityType());
    }

    public async Task SignOut(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        var token = cancellationToken == default ? CancellationToken : cancellationToken;
        var api = new ApiClient(Api, token);
        await api.Users.Token.SignOutAsync(new()
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