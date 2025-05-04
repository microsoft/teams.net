using System.Net;
using System.Reflection;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Apps;

public partial interface IApp : IRoutingModule;

public partial class App : RoutingModule
{
    protected void RegisterAttributeRoutes()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        foreach (Type type in assembly.GetTypes())
        {
            var methods = type.GetMethods();

            foreach (MethodInfo method in methods)
            {
                var attrs = method.GetCustomAttributes(typeof(ActivityAttribute), true);

                if (attrs.Length == 0) continue;

                foreach (object attr in attrs)
                {
                    var attribute = (ActivityAttribute)attr;
                    var route = new AttributeRoute() { Attr = attribute, Method = method };
                    var result = route.Validate();

                    if (!result.Valid) throw new InvalidOperationException(result.ToString());
                    Router.Register(route);
                }
            }
        }
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
            if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.BadRequest && ex.StatusCode != HttpStatusCode.PreconditionFailed)
            {
                await ErrorEvent(this, context.Sender, ex, (IContext<IActivity>)context);
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
            if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.BadRequest && ex.StatusCode != HttpStatusCode.PreconditionFailed)
            {
                await ErrorEvent(this, context.Sender, ex, (IContext<IActivity>)context);
                return new Response(ex.StatusCode);
            }

            return new Response(HttpStatusCode.PreconditionFailed);
        }
    }
}