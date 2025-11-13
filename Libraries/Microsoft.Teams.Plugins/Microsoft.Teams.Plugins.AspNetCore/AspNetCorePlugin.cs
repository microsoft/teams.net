// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Extensions.Logging;

using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace Microsoft.Teams.Plugins.AspNetCore;

[Plugin]
public partial class AspNetCorePlugin : ISenderPlugin, IAspNetCorePlugin
{
    private readonly ILogger<AspNetCorePlugin> _logger;

    [Dependency("Token", optional: true)]
    public IToken? Token { get; set; }

    [Dependency]
    public IHttpClient Client { get; set; }

    public event EventFunction Events;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AspNetCorePlugin(ILogger<AspNetCorePlugin>? logger = null)
    {
        _logger = logger ?? LoggerFactory.Create(builder => { }).CreateLogger<AspNetCorePlugin>();
    }

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
        _logger.LogDebug("OnStart");
        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OnError");
        return Task.CompletedTask;
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OnActivity");
        return Task.CompletedTask;
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OnActivitySent");
        return Task.CompletedTask;
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OnActivityResponse");
        return Task.CompletedTask;
    }

    public Task<IActivity> Send(IActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Send<IActivity>(activity, reference, isTargeted: false, cancellationToken);
    }

    public Task<IActivity> Send(IActivity activity, Api.ConversationReference reference, bool isTargeted, CancellationToken cancellationToken = default)
    {
        return Send<IActivity>(activity, reference, isTargeted, cancellationToken);
    }

    public Task<TActivity> Send<TActivity>(TActivity activity, Api.ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return Send<TActivity>(activity, reference, isTargeted: false, cancellationToken);
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity, Api.ConversationReference reference, bool isTargeted, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var client = new ApiClient(reference.ServiceUrl, Client, cancellationToken);

        activity.Conversation = reference.Conversation;
        activity.From = reference.Bot;
        activity.Recipient = reference.User;
        activity.ChannelId = reference.ChannelId;

        if (activity.Id is not null && !activity.IsStreaming)
        {
            await client
                .Conversations
                .Activities
                .UpdateAsync(reference.Conversation.Id, activity.Id, activity, isTargeted);

            return activity;
        }

        var res = await client
            .Conversations
            .Activities
            .CreateAsync(reference.Conversation.Id, activity, isTargeted);

        activity.Id = res?.Id;
        return activity;
    }

    public IStreamer CreateStream(Api.ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return new Stream()
        {
            Send = async activity =>
            {
                var res = await Send(activity, reference, false, cancellationToken);
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
            _logger.LogDebug("res: {Response}", res);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Activity event error");
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
            var activity = await ParseActivity(request);

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
            _logger.LogError(ex, "HTTP activity error");
            await Events(
                this,
                "error",
                new ErrorEvent() { Exception = ex },
                cancellationToken
            );

            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    public JsonWebToken ExtractToken(HttpRequest httpRequest)
    {
        var authHeader = httpRequest.Headers.Authorization.FirstOrDefault() ?? throw new UnauthorizedAccessException();
        return new JsonWebToken(authHeader.Replace("Bearer ", ""));
    }

    public async Task<Activity?> ParseActivity(HttpRequest httpRequest)
    {
        httpRequest.EnableBuffering();

        if (httpRequest.Body.CanSeek)
        {
            // reset the stream position to the beginning in case it was read before
            httpRequest.Body.Position = 0;
        }

        using StreamReader sr = new(httpRequest.Body);
        var body = await sr.ReadToEndAsync();
        Activity? activity = JsonSerializer.Deserialize<Activity>(body);

        return activity;
    }
}
