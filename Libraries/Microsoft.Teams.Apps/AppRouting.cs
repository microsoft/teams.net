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
                    await method.InvokeAsync(controller, [plugin, @event]);
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
            });

            context.UserGraphToken = new JsonWebToken(res);

            await Events.Emit(
                context.Sender,
                EventType.SignIn,
                new SignInEvent()
                {
                    Context = context.ToActivityType<Api.Activities.Invokes.SignInActivity>(),
                    Token = res
                }
            );

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
            );

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
            });

            context.UserGraphToken = new JsonWebToken(res);

            await Events.Emit(
                context.Sender,
                EventType.SignIn,
                new SignInEvent()
                {
                    Context = context.ToActivityType<Api.Activities.Invokes.SignInActivity>(),
                    Token = res
                }
            );
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
            );

            if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.BadRequest && ex.StatusCode != HttpStatusCode.PreconditionFailed)
            {
                return new Response(ex.StatusCode);
            }

            return new Response(HttpStatusCode.PreconditionFailed);
        }
    }

    /// <summary>
    /// Register a middleware.
    /// </summary>
    /// <param name="handler">Callback to invoke.</param>
    /// <returns></returns>
    public App Use(Func<IContext<IActivity>, Task<object?>> handler)
    {
        Router.Register(handler);
        return this;
    }
}