using System.Net;
using System.Reflection;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Common.Extensions;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Apps;

public partial class App : RoutingModule
{
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
            var attrs = method.GetCustomAttributes<Events.EventAttribute>(true);

            foreach (var attr in attrs)
            {
                OnEvent(attr.Name, async (plugin, @event, token) =>
                {
                    await method.InvokeAsync(controller, [plugin, @event]);
                });

                Logger.Debug($"'{attr.Name}' event route '{name}.{method.Name}' registered");
            }
        }

        Logger.Debug($"controller '{name}' registered");
        return this;
    }

    protected async Task<object?> OnTokenExchangeActivity(IContext<Api.Activities.Invokes.SignIn.TokenExchangeActivity> context)
    {
        var key = $"auth/{context.Ref.Conversation.Id}/{context.Activity.From.Id}";

        try
        {
            await Storage.SetAsync(key, context.Activity.Value.ConnectionName);
            var res = await context.Api.Users.Token.ExchangeAsync(new()
            {
                ChannelId = context.Activity.ChannelId,
                ConnectionName = context.Activity.Value.ConnectionName,
                UserId = context.Activity.From.Id,
                ExchangeRequest = new() { Token = context.Activity.Value.Token },
            });

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
                    Context = (IContext<IActivity>)context
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
        var key = $"auth/{context.Ref.Conversation.Id}/{context.Activity.From.Id}";

        try
        {
            var connectionName = (string?)await Storage.GetAsync(key);

            if (connectionName is null || context.Activity.Value.State is null)
            {
                context.Log.Warn($"auth state not found for conversation '{context.Ref.Conversation.Id}' and user '{context.Activity.From.Id}'");
                return new Response(HttpStatusCode.NotFound);
            }

            var res = await context.Api.Users.Token.GetAsync(new()
            {
                ChannelId = context.Activity.ChannelId,
                UserId = context.Activity.From.Id,
                ConnectionName = connectionName,
                Code = context.Activity.Value.State
            });

            await Storage.DeleteAsync(key);
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
                    Context = (IContext<IActivity>)context
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
}