// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Reflection;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Common.Extensions;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Apps;

public partial class App
{
    internal IRouter Router { get; } = new Router();

    [Obsolete("Use Minimal APIs instead.")]
    public App AddController<T>(T controller) where T : class
    {
        var type = controller.GetType();
        var attribute = type.GetCustomAttribute<TeamsControllerAttribute>(true) ?? throw new Exception($"type '{type.Name}' is not a controller");
        var name = attribute.Name ?? type.Name;
        var methods = type.GetMethods();

        foreach (var method in methods)
        {
            var attrs = method.GetCustomAttributes<ActivityAttribute>(true);

            foreach (var attr in attrs)
            {
                var route = new AttributeRoute() { Attr = attr, Method = method, Object = controller };
                var activityType = attr.Name?.ToString() ?? "activity";
                var result = route.Validate();

                if (!result.Valid)
                {
                    throw new InvalidOperationException(result.ToString());
                }

                Router.Register(route);
                Logger.Debug($"'{activityType}' route '{name}.{method.Name}' registered");
            }
        }

        foreach (var method in methods)
        {
            var attrs = method.GetCustomAttributes<EventAttribute>(true);

            foreach (var attr in attrs)
            {
                this.OnEvent(attr.Name, async (plugin, @event, token) =>
                {
                    await method.InvokeAsync(controller, [plugin, @event]).ConfigureAwait(false);
                });

                Logger.Debug($"'{attr.Name}' event route '{name}.{method.Name}' registered");
            }
        }

        Logger.Debug($"controller '{name}' registered");
        return this;
    }

    protected async Task<Response> OnTokenExchangeActivity(IContext<Api.Activities.Invokes.SignIn.TokenExchangeActivity> context)
    {
        var connectionName = context.Activity.Value.ConnectionName;

        if (OAuth.DefaultConnectionName != connectionName)
        {
            Logger.Warn($"`default connection name \"{OAuth.DefaultConnectionName}\" does not match activity connection name \"{connectionName}\"");
        }

        try
        {
            var res = await context.Api.Users.Token.ExchangeAsync(new()
            {
                ChannelId = context.Activity.ChannelId,
                ConnectionName = context.Activity.Value.ConnectionName,
                UserId = context.Activity.From.Id,
                ExchangeRequest = new() { Token = context.Activity.Value.Token },
            }).ConfigureAwait(false);

            context.UserGraphToken = new JsonWebToken(res);

            await Events.Emit(
                context.Sender,
                EventType.SignIn,
                new SignInEvent()
                {
                    Context = context.ToActivityType<Api.Activities.Invokes.SignInActivity>(),
                    Token = res
                }
            ).ConfigureAwait(false);

            return new Response(HttpStatusCode.OK);
        }
        catch (HttpException ex)
        {
            await Events.Emit(
                context.Sender,
                EventType.Error,
                new ErrorEvent()
                {
                    Exception = ex,
                    Context = context.ToActivityType<IActivity>()
                },
                context.CancellationToken
            ).ConfigureAwait(false);

            if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.BadRequest && ex.StatusCode != HttpStatusCode.PreconditionFailed)
            {
                return new Response(ex.StatusCode);
            }

            return new Response(HttpStatusCode.PreconditionFailed, new Api.TokenExchange.InvokeResponse()
            {
                Id = context.Activity.Value.Id,
                ConnectionName = context.Activity.Value.ConnectionName,
                FailureDetail = ex.ToString(),
            });
        }
    }

    protected async Task<object?> OnVerifyStateActivity(IContext<Api.Activities.Invokes.SignIn.VerifyStateActivity> context)
    {
        try
        {
            if (context.Activity.Value.State is null)
            {
                context.Log.Warn($"auth state not found for conversation '{context.Ref.Conversation.Id}' and user '{context.Activity.From.Id}'");
                return new Response(HttpStatusCode.NotFound);
            }

            var res = await context.Api.Users.Token.GetAsync(new()
            {
                ChannelId = context.Activity.ChannelId,
                UserId = context.Activity.From.Id,
                ConnectionName = OAuth.DefaultConnectionName,
                Code = context.Activity.Value.State
            }).ConfigureAwait(false);

            context.UserGraphToken = new JsonWebToken(res);

            await Events.Emit(
                context.Sender,
                EventType.SignIn,
                new SignInEvent()
                {
                    Context = context.ToActivityType<Api.Activities.Invokes.SignInActivity>(),
                    Token = res
                }
            ).ConfigureAwait(false);
            return new Response(HttpStatusCode.OK);
        }
        catch (HttpException ex)
        {
            await Events.Emit(
                context.Sender,
                EventType.Error,
                new ErrorEvent()
                {
                    Exception = ex,
                    Context = context.ToActivityType<IActivity>()
                },
                context.CancellationToken
            ).ConfigureAwait(false);

            if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.BadRequest && ex.StatusCode != HttpStatusCode.PreconditionFailed)
            {
                return new Response(ex.StatusCode);
            }

            return new Response(HttpStatusCode.PreconditionFailed);
        }
    }

    /// <summary>
    /// Default handler for signin/failure invoke activities.
    /// Teams sends this when SSO token exchange fails (e.g., due to a
    /// misconfigured Entra app registration). Logs the failure details
    /// and emits an error event.
    ///
    /// Known failure codes (sent by the Teams client):
    /// <list type="bullet">
    /// <item><term>installappfailed</term><description>Failed to install the app in the user's personal scope (non-silent).</description></item>
    /// <item><term>authrequestfailed</term><description>The SSO auth request failed after app installation (non-silent).</description></item>
    /// <item><term>installedappnotfound</term><description>The bot app is not installed for the user or group chat.</description></item>
    /// <item><term>invokeerror</term><description>A generic error occurred during the SSO invoke flow.</description></item>
    /// <item><term>resourcematchfailed</term><description>The token exchange resource URI on the OAuthCard does not match the Application ID URI in the Entra app registration's "Expose an API" section.</description></item>
    /// <item><term>oauthcardnotvalid</term><description>The bot's OAuthCard could not be parsed.</description></item>
    /// <item><term>tokenmissing</term><description>AAD token acquisition failed.</description></item>
    /// <item><term>userconsentrequired</term><description>The user needs to consent (handled via OAuth card fallback, does not typically reach the bot).</description></item>
    /// <item><term>interactionrequired</term><description>User interaction is required (handled via OAuth card fallback, does not typically reach the bot).</description></item>
    /// </list>
    /// </summary>
    protected async Task<object?> OnSignInFailureActivity(IContext<Api.Activities.Invokes.SignIn.FailureActivity> context)
    {
        var failure = context.Activity.Value;

        Logger.Warn(
            $"sign-in failed for user \"{context.Activity.From.Id}\" in conversation " +
            $"\"{context.Ref.Conversation.Id}\": {failure.Code} — {failure.Message}. " +
            $"If the code is 'resourcematchfailed', verify that your Entra app registration " +
            $"has 'Expose an API' configured with the correct Application ID URI matching " +
            $"your OAuth connection's Token Exchange URL."
        );

        await Events.Emit(
            context.Sender,
            EventType.Error,
            new ErrorEvent()
            {
                Exception = new Exception($"Sign-in failure: {failure.Code} — {failure.Message}"),
                Context = context.ToActivityType<IActivity>()
            },
            context.CancellationToken
        ).ConfigureAwait(false);

        return new Response(HttpStatusCode.OK);
    }

    /// <summary>
    /// Register a middleware.
    /// </summary>
    /// <param name="handler">Callback to invoke.</param>
    /// <returns></returns>
    public App Use(Func<IContext<IActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Name = "middleware",
            Type = RouteType.User,
            Selector = _ => true,
            Handler = handler
        });
        return this;
    }

    /// <summary>
    /// Register a middleware.
    /// </summary>
    /// <param name="handler">Callback to invoke.</param>
    /// <returns></returns>
    public App Use(Func<IContext<IActivity>, Task> handler)
    {
        return Use(async (context) =>
        {
            await handler(context).ConfigureAwait(false);
            return null;
        });
    }
}