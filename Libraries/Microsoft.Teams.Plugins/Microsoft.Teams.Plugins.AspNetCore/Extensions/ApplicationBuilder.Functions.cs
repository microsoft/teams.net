// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static partial class ApplicationBuilderExtensions
{
    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction(this IApplicationBuilder builder, string name, Action<IFunctionContext<object?>> handler)
    {
        return builder.AddFunction<object?>(name, context =>
        {
            handler(context);
            return Task.FromResult<object?>(null);
        });
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <typeparam name="TBody">The body (data) type</typeparam>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction<TBody>(this IApplicationBuilder builder, string name, Action<IFunctionContext<TBody>> handler)
    {
        return builder.AddFunction<TBody>(name, context =>
        {
            handler(context);
            return Task.FromResult<object?>(null);
        });
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction(this IApplicationBuilder builder, string name, Func<IFunctionContext<object?>, Task> handler)
    {
        return builder.AddFunction<object?>(name, context =>
        {
            handler(context).ConfigureAwait(false).GetAwaiter();
            return Task.FromResult<object?>(null);
        });
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <typeparam name="TBody">The body (data) type</typeparam>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction<TBody>(this IApplicationBuilder builder, string name, Func<IFunctionContext<TBody>, Task> handler)
    {
        return builder.AddFunction<TBody>(name, context =>
        {
            handler(context).ConfigureAwait(false).GetAwaiter();
            return Task.FromResult<object?>(null);
        });
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction(this IApplicationBuilder builder, string name, Func<IFunctionContext<object?>, object?> handler)
    {
        return builder.AddFunction<object?>(name, context => handler(context));
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction(this IApplicationBuilder builder, string name, Func<IFunctionContext<object?>, Task<object?>> handler)
    {
        return builder.AddFunction<object?>(name, context => handler(context));
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <typeparam name="TBody">The body (data) type</typeparam>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction<TBody>(this IApplicationBuilder builder, string name, Func<IFunctionContext<TBody>, Task<object?>> handler)
    {
        return builder.AddFunction<TBody>(name, context => handler(context).ConfigureAwait(false).GetAwaiter().GetResult());
    }

    /// <summary>
    /// add/update a function that can be called remotely
    /// </summary>
    /// <typeparam name="TBody">The body (data) type</typeparam>
    /// <param name="name">The unique function name</param>
    /// <param name="handler">The callback to handle the function</param>
    public static IApplicationBuilder AddFunction<TBody>(this IApplicationBuilder builder, string name, Func<IFunctionContext<TBody>, object?> handler)
    {
        builder.UseEndpoints(endpoints =>
        {
            endpoints.MapPost($"/api/functions/{name}", async context =>
            {
                context.Request.EnableBuffering();
                var app = context.RequestServices.GetRequiredService<App>();
                var log = app.Logger.Child("functions").Child(name);

                if (context.Request.Headers.Authorization.First() is null)
                {
                    await Results.Unauthorized().ExecuteAsync(context);
                    return;
                }

                if (!context.Request.Headers.TryGetValue("X-Teams-App-Session-Id", out var appSessionId))
                {
                    await Results.Unauthorized().ExecuteAsync(context);
                    return;
                }

                if (!context.Request.Headers.TryGetValue("X-Teams-Page-Id", out var pageId))
                {
                    await Results.Unauthorized().ExecuteAsync(context);
                    return;
                }

                var token = new JwtSecurityTokenHandler().ReadJwtToken(
                    context.Request.Headers.Authorization
                        .FirstOrDefault()?
                        .Replace("bearer ", string.Empty)
                        .Replace("Bearer ", string.Empty)
                );

                var ctx = new FunctionContext<TBody>(app)
                {
                    Api = new(app.Api),
                    Log = log,
                    AppSessionId = appSessionId,
                    TenantId = token.Claims.First(c => c.Type == "tid").Value,
                    UserId = token.Claims.First(c => c.Type == "oid").Value,
                    UserName = token.Claims.First(c => c.Type == "name").Value,
                    PageId = pageId,
                    AuthToken = token.ToString(),
                    Data = await context.Request.ReadFromJsonAsync<TBody>(),
                };

                if (context.Request.Headers.TryGetValue("X-Teams-Channel-Id", out var channelId))
                {
                    ctx.ChannelId = channelId;
                }

                if (context.Request.Headers.TryGetValue("X-Teams-Chat-Id", out var chatId))
                {
                    ctx.ChatId = chatId;
                }

                if (context.Request.Headers.TryGetValue("X-Teams-Meeting-Id", out var meetingId))
                {
                    ctx.MeetingId = meetingId;
                }

                if (context.Request.Headers.TryGetValue("X-Teams-Message-Id", out var messageId))
                {
                    ctx.MessageId = messageId;
                }

                if (context.Request.Headers.TryGetValue("X-Teams-Sub-Page-Id", out var subPageId))
                {
                    ctx.SubPageId = subPageId;
                }

                if (context.Request.Headers.TryGetValue("X-Teams-Team-Id", out var teamId))
                {
                    ctx.TeamId = teamId;
                }

                log.Debug(ctx.Data?.ToString());
                var res = handler(ctx);
                log.Debug(res?.ToString());
                await Results.Json(res).ExecuteAsync(context);
            });
        });

        return builder;
    }
}