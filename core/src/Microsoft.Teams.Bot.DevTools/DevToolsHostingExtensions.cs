    // Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.DevTools.Events;
using Microsoft.Teams.Bot.DevTools.Extensions;

namespace Microsoft.Teams.Bot.DevTools;

/// <summary>
/// Extension methods for registering DevTools services and endpoints.
/// </summary>
public static partial class DevToolsHostingExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "DevTools are not secure and should not be used in production environments")]
    private static partial void LogDevToolsSecurityWarning(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "DevTools available at {Address}/devtools")]
    private static partial void LogDevToolsAvailable(ILogger logger, string address);
    /// <summary>
    /// The named HttpClient name used by ConversationClient, duplicated here because the constant is internal.
    /// </summary>
    private const string ConversationHttpClientName = "BotConversationClient";

    /// <summary>
    /// Registers DevTools services: settings, shared service, middleware, and replaces
    /// ConversationClient with the DevTools decorator that emits "sent" events.
    /// Call this after <c>AddTeamsBotApplication()</c> or <c>AddBotApplication()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDevTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register settings from configuration
        services.AddSingleton<DevToolsSettings>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return config.GetSection("DevTools").Get<DevToolsSettings>() ?? new();
        });

        // Register shared service
        services.AddSingleton<DevToolsService>();

        // Register middleware
        services.AddSingleton<DevToolsMiddleware>();

        // Replace ConversationClient with DevToolsConversationClient.
        // AddBotApplication registers ConversationClient via AddHttpClient<ConversationClient>(...).
        // We remove that registration and re-register with our decorator.
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ConversationClient));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddHttpClient<ConversationClient, DevToolsConversationClient>(ConversationHttpClientName);

        return services;
    }

    /// <summary>
    /// Configures the DevTools middleware and endpoints. Enables WebSockets, serves the embedded React UI,
    /// maps WebSocket and test activity injection endpoints, and registers the DevTools middleware on the bot.
    /// Call this after <c>UseBotApplication()</c> or <c>UseTeamsBotApplication()</c>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder (typically <c>WebApplication</c>).</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder UseDevTools(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Enable WebSockets
        if (endpoints is IApplicationBuilder app)
        {
            app.UseWebSockets(new WebSocketOptions()
            {
                AllowedOrigins = { "*" }
            });

            // Serve embedded static files at /devtools
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "web"),
                ServeUnknownFileTypes = true,
                RequestPath = "/devtools"
            });
        }

        // Resolve services
        var service = endpoints.ServiceProvider.GetRequiredService<DevToolsService>();
        var middleware = endpoints.ServiceProvider.GetRequiredService<DevToolsMiddleware>();
        var lifetime = endpoints.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var files = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "web");
        var logger = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevTools");

        // Register middleware on the bot application
        var botApp = endpoints.ServiceProvider.GetRequiredService<BotApplication>();
        botApp.UseMiddleware(middleware);

        // Populate AppId/AppName from BotApplicationOptions
        var options = endpoints.ServiceProvider.GetService<BotApplicationOptions>();
        service.AppId = options?.AppId;
        service.AppName ??= options?.AppId ?? "DevTools";

        // Log DevTools URLs on application start
        lifetime.ApplicationStarted.Register(() =>
        {
            var server = endpoints.ServiceProvider.GetRequiredService<IServer>();
            var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;
            LogDevToolsSecurityWarning(logger);
            foreach (var address in addresses)
            {
                LogDevToolsAvailable(logger, address);
            }
        });

        // Map endpoints
        MapDevToolsEndpoints(endpoints, service, lifetime, files, botApp);

        return endpoints;
    }

    /// <summary>
    /// Configures DevTools and returns the bot application for chaining.
    /// </summary>
    /// <typeparam name="TApp">The bot application type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The bot application instance.</returns>
    public static TApp UseDevTools<TApp>(this IEndpointRouteBuilder endpoints) where TApp : BotApplication
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.UseDevTools();
        return endpoints.ServiceProvider.GetRequiredService<TApp>();
    }

    private static void MapDevToolsEndpoints(
        IEndpointRouteBuilder endpoints,
        DevToolsService service,
        IHostApplicationLifetime lifetime,
        ManifestEmbeddedFileProvider files,
        BotApplication botApp)
    {
        // Serve React UI — SPA fallback to index.html
        var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        endpoints.MapGet("/devtools/{*path}", (string? path) =>
        {
            var file = files.GetFileInfo(path ?? "index.html");
            if (!file.Exists)
            {
                file = files.GetFileInfo("index.html");
            }

            if (!contentTypeProvider.TryGetContentType(file.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Results.File(file.CreateReadStream(), contentType: contentType);
        });

        endpoints.MapGet("/devtools", () =>
        {
            var file = files.GetFileInfo("index.html");
            return Results.File(file.CreateReadStream(), contentType: "text/html");
        });

        // WebSocket endpoint
        endpoints.MapGet("/devtools/sockets", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var id = Guid.NewGuid().ToString();
            var buffer = new byte[1024];

            service.Sockets.Add(id, socket);
            await service.Sockets.Emit(id, new MetaDataEvent(service.MetaData), lifetime.ApplicationStopping).ConfigureAwait(false);

            try
            {
                while (socket.State.HasFlag(WebSocketState.Open))
                {
                    await socket.ReceiveAsync(buffer, lifetime.ApplicationStopping).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Server shutting down — expected
            }
            catch (WebSocketException)
            {
                // Connection closed unexpectedly — expected
            }
            finally
            {
                if (socket.IsCloseable())
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, lifetime.ApplicationStopping).ConfigureAwait(false);
                }
            }

            service.Sockets.Remove(id);
        });

        // Test activity injection endpoint (replaces ActivityController from main)
        endpoints.MapPost("/v3/conversations/{conversationId}/activities", async (
            string conversationId,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Read body as JsonNode
            var body = await JsonNode.ParseAsync(context.Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (body is null)
            {
                return Results.BadRequest();
            }

            var isDevTools = context.Request.Headers.TryGetValue("x-teams-devtools", out var headerValues)
                && headerValues.Any(h => h == "true");

            body["id"] ??= Guid.NewGuid().ToString();

            // If not from DevTools client, return 201 (passthrough for outgoing activity responses)
            if (!isDevTools)
            {
                return Results.Json(new { id = body["id"]?.ToString() }, statusCode: 201);
            }

            // Set default from/conversation/recipient for DevTools test messages
            body["from"] ??= JsonSerializer.SerializeToNode(new ConversationAccount
            {
                Id = "devtools",
                Name = "devtools"
            });

            body["conversation"] = JsonSerializer.SerializeToNode(new
            {
                id = conversationId,
                type = "personal",
                name = "default"
            });

            body["recipient"] = JsonSerializer.SerializeToNode(new ConversationAccount
            {
                Id = service.AppId ?? string.Empty,
                Name = service.AppName
            });

            // Set serviceUrl and channelId so replies route back here
            body["serviceUrl"] ??= $"{context.Request.Scheme}://{context.Request.Host}";
            body["channelId"] ??= "devtools";

            // Create a test HttpContext and route through ProcessAsync
            var activityJson = body.ToJsonString();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(activityJson));

            var testContext = new DefaultHttpContext
            {
                RequestServices = context.RequestServices
            };
            testContext.Request.Body = stream;
            testContext.Request.ContentType = "application/json";

            await botApp.ProcessAsync(testContext, cancellationToken).ConfigureAwait(false);

            return Results.Json(new { id = body["id"]?.ToString() }, statusCode: 201);
        });

        // Reply endpoint — bot sends replies to /v3/conversations/{id}/activities/{replyToId}
        endpoints.MapPost("/v3/conversations/{conversationId}/activities/{activityId}", async (
            string conversationId,
            string activityId,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var body = await JsonNode.ParseAsync(context.Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Results.Json(new { id = body?["id"]?.ToString() ?? Guid.NewGuid().ToString() }, statusCode: 201);
        });
    }
}
