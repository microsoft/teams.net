﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Plugins.AspNetCore;

[Plugin]
public partial class AspNetCorePlugin : ISenderPlugin, IAspNetCorePlugin
{
    [Dependency]
    public ILogger Logger { get; set; }

    [Dependency("Token", optional: true)]
    public IToken? Token { get; set; }

    [Dependency]
    public IHttpClient Client { get; set; }

    public event EventFunction Events;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        return builder;
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnStart");
        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnError");
        return Task.CompletedTask;
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivity");
        return Task.CompletedTask;
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivitySent");
        return Task.CompletedTask;
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivityResponse");
        return Task.CompletedTask;
    }

    public Task<IActivity> Send(IActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Send<IActivity>(activity, reference, cancellationToken);
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var client = new ApiClient(reference.ServiceUrl, Client, cancellationToken);

        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;

        if (activity.Id is not null && !activity.IsStreaming)
        {
            await client
                .Conversations
                .Activities
                .UpdateAsync(reference.Conversation.Id, activity.Id, activity);

            return activity;
        }

        var res = await client
            .Conversations
            .Activities
            .CreateAsync(reference.Conversation.Id, activity);

        activity.Id = res?.Id;
        return activity;
    }

    public IStreamer CreateStream(Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return new Stream()
        {
            Send = async activity =>
            {
                var res = await Send(activity, reference, cancellationToken);
                return res;
            }
        };
    }

    public async Task<Response> Do(ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var @out = await Events(
                this,
                "activity",
                @event,
                cancellationToken
            );

            var res = (Response?)@out ?? throw new Exception("expected activity response");
            Logger.Debug(res);
            return res;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await Events(
                this,
                "error",
                new ErrorEvent() { Exception = ex },
                cancellationToken
            );

            return new Response(System.Net.HttpStatusCode.InternalServerError, ex.ToString());
        }
    }

    public async Task<IResult> Do(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = httpContext.Request;
            var token = ExtractToken(request);
            var activity = await ExtractActivity(request);

            if (activity is null)
            {
                return Results.BadRequest("Missing activity");
            }

            var data = new Dictionary<string, object?>
            {
                ["Request.TraceId"] = httpContext.TraceIdentifier
            };

            foreach (var pair in httpContext.Items)
            {
                var key = pair.Key.ToString();

                if (key is null) continue;

                data[key] = pair.Value;
            }

            var res = await Do(new ActivityEvent()
            {
                Token = token,
                Activity = activity,
                Extra = data,
                Services = httpContext.RequestServices
            }, cancellationToken);

            // convert response metadata to headers
            foreach (var (key, value) in res.Meta)
            {
                var str = value?.ToString();
                if (string.IsNullOrEmpty(str)) continue;
                httpContext.Response.Headers.Append($"X-Teams-{char.ToUpper(key[0]) + key[1..]}", str);
            }

            return Results.Json(
                res.Body,
                _jsonSerializerOptions,
                contentType: null,
                statusCode: (int)res.Status
            );
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await Events(
                this,
                "error",
                new ErrorEvent() { Exception = ex },
                cancellationToken
            );

            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    public JsonWebToken ExtractToken(Microsoft.AspNetCore.Http.HttpRequest httpRequest)
    {
        var authHeader = httpRequest.Headers.Authorization.FirstOrDefault() ?? throw new UnauthorizedAccessException();
        return new JsonWebToken(authHeader.Replace("Bearer ", ""));
    }

    public async Task<Activity?> ExtractActivity(Microsoft.AspNetCore.Http.HttpRequest httpRequest)
    {
        // Fallback logic
        httpRequest.EnableBuffering();
        var body = await new StreamReader(httpRequest.Body).ReadToEndAsync();
        Activity? activity = JsonSerializer.Deserialize<Activity>(body);
        httpRequest.Body.Position = 0;

        return activity;
    }
}