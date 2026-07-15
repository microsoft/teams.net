// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.HtmlWidget;
using Microsoft.Teams.Apps.Schema;

using HtmlWidgetBot;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// ==================== MESSAGE COMMANDS ====================

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    var text = context.Activity.Text?.Trim().ToLowerInvariant() ?? "";

    switch (text)
    {
        case "/simple":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "Simple Widget",
                    Description = "A static HTML widget with no callbacks.",
                    Html = Widgets.SimpleHtml,
                    Domain = "https://teams.microsoft.com",
                    SecurityPolicy = new HtmlWidgetSecurityPolicy
                    {
                        ConnectDomains = [],
                        ResourceDomains = ["'self'", "data:"],
                        FrameDomains = [],
                        BaseUriDomains = [],
                    },
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "Here is a simple static widget:",
                    After = "No callbacks needed for static content.",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/calltool":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "CallTool Widget",
                    Description = "Widget that calls tools on the bot.",
                    Html = Widgets.CallToolHtml,
                    Domain = "https://teams.microsoft.com",
                    SecurityPolicy = new HtmlWidgetSecurityPolicy
                    {
                        ConnectDomains = ["https://teams.microsoft.com", "https://teams.cloud.microsoft.com"],
                        ResourceDomains = ["'self'", "data:"],
                        FrameDomains = [],
                        BaseUriDomains = [],
                    },
                    ToolInput = new { demo = true }, // Passed to the widget as initial context (available via toolInput in ui/initialize)
                    ToolOutput = new
                    {
                        content = new[] { new { type = "text", text = "Initial data loaded." } },
                        structuredContent = new { counter = 0, lastAction = "init" },
                        isError = false,
                    },
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "Here is a widget with callTool support (click Refresh):",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/messageback":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "MessageBack Widget",
                    Description = "Widget that sends messageBack to the bot.",
                    Html = Widgets.MessageBackHtml,
                    Domain = "https://teams.microsoft.com",
                    SecurityPolicy = new HtmlWidgetSecurityPolicy
                    {
                        ConnectDomains = [],
                        ResourceDomains = ["'self'", "data:"],
                        FrameDomains = [],
                        BaseUriDomains = [],
                    },
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "This widget tests the onMessage (messageBack) callback:",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/fullscreen":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "Fullscreen Widget",
                    Description = "Widget that requests fullscreen mode.",
                    Html = Widgets.FullscreenHtml,
                    Domain = "https://teams.microsoft.com",
                    SecurityPolicy = new HtmlWidgetSecurityPolicy
                    {
                        ConnectDomains = [],
                        ResourceDomains = ["'self'", "data:"],
                        FrameDomains = [],
                        BaseUriDomains = [],
                    },
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "This widget will request fullscreen mode:",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/multi":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "Multi-Tool Widget",
                    Description = "Widget that calls multiple different tools.",
                    Html = Widgets.MultiToolHtml,
                    Domain = "https://teams.microsoft.com",
                    SecurityPolicy = new HtmlWidgetSecurityPolicy
                    {
                        ConnectDomains = ["https://teams.microsoft.com"],
                        ResourceDomains = ["'self'", "data:"],
                        FrameDomains = [],
                        BaseUriDomains = [],
                    },
                    ToolInput = new { }, // Passed to the widget as initial context (available via toolInput in ui/initialize)
                    ToolOutput = new
                    {
                        content = new[] { new { type = "text", text = "Ready." } },
                        structuredContent = new { tools = new[] { "getTime", "roll", "echo" } },
                        isError = false,
                    },
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "This widget has multiple tools to test dispatch:",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/openlink":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "open-link-test",
                    Html = Widgets.OpenLinkHtml,
                    Domain = "https://teams.microsoft.com",
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "Widget with ui/open-link support (click a button to open a URL):",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/context":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "update-context-test",
                    Html = Widgets.UpdateContextHtml,
                    Domain = "https://teams.microsoft.com",
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "Widget with ui/update-model-context support:",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/hostcontext":
        {
            var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(
                new HtmlWidgetPayload
                {
                    Name = "host-context-inspector",
                    Html = Widgets.HostContextHtml,
                    Domain = "https://teams.microsoft.com",
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = "Widget that inspects hostContext from ui/initialize:",
                });
            await context.SendActivityAsync(message, cancellationToken);
            break;
        }

        case "/validate":
        {
            var htmlWithExternalRefs = """
                <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Roboto">
                <div style="font-family: Roboto, sans-serif; padding: 16px;">
                  <h2>Validation Demo</h2>
                  <p>This widget was validated before sending.</p>
                </div>
                """;

            var strictPolicy = new HtmlWidgetSecurityPolicy
            {
                ConnectDomains = [],
                ResourceDomains = ["'self'", "data:"],
                FrameDomains = [],
                BaseUriDomains = [],
            };
            var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(htmlWithExternalRefs, strictPolicy);

            var correctedPolicy = new HtmlWidgetSecurityPolicy
            {
                ConnectDomains = [],
                ResourceDomains = ["'self'", "data:", "https://fonts.googleapis.com"],
                FrameDomains = [],
                BaseUriDomains = [],
            };
            var warningText = string.Join("\n", warnings.Select(w => $"- **{w.Source}**: `{w.Url}` not in `{w.PolicyField}`"));
            var markdown = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(
                new HtmlWidgetPayload
                {
                    Name = "Validated Widget",
                    Description = "Widget built after security policy validation.",
                    Html = htmlWithExternalRefs,
                    Domain = "https://teams.microsoft.com",
                    SecurityPolicy = correctedPolicy,
                },
                new HtmlWidgetMarkdownOptions
                {
                    Before = $"**Validation found {warnings.Count} warning(s):**\n\n{warningText}\n\nPolicy was corrected before sending:",
                });
            await context.SendActivityAsync(
                new MessageActivity(markdown) { TextFormat = TextFormats.ExtendedMarkdown },
                cancellationToken);
            break;
        }

        case "/help":
        {
            await context.SendActivityAsync(
                new MessageActivity(
                    "**HTML Widget Test Commands:**\n\n" +
                    "- `/simple` - Static widget (no callbacks)\n" +
                    "- `/calltool` - Widget with onCallTool\n" +
                    "- `/messageback` - Widget with onMessage\n" +
                    "- `/fullscreen` - Widget requesting fullscreen\n" +
                    "- `/multi` - Widget with multiple tools\n" +
                    "- `/openlink` - Widget with ui/open-link\n" +
                    "- `/context` - Widget with ui/update-model-context\n" +
                    "- `/hostcontext` - Inspect hostContext from initialize\n" +
                    "- `/validate` - Security policy validation demo\n" +
                    "- `/help` - This message")
                { TextFormat = TextFormats.Markdown },
                cancellationToken);
            break;
        }

        default:
        {
            await context.SendActivityAsync(
                "Send `/help` for available widget test commands.",
                cancellationToken);

            break;
        }
    }
});

// ==================== WIDGET CALL TOOL HANDLER ====================

teamsApp.OnWidgetCallTool(async (context, cancellationToken) =>
{
    var request = context.Activity.Value;
    var toolName = request?.Name ?? "unknown";
    var args = request?.Arguments;

    Console.WriteLine($"[widget.callTool] tool={toolName} args={JsonSerializer.Serialize(args)}");

    var response = toolName switch
    {
        "refresh" => new HtmlWidgetCallToolResponse
        {
            CallToolResult = new McpUiCallToolResult
            {
                Content = [new McpUiCallToolResultContent { Text = "Refreshed!" }],
                StructuredContent = new
                {
                    counter = GetCounter(args) + 1,
                    lastAction = "refresh",
                    timestamp = DateTime.UtcNow.ToString("o"),
                },
            }
        },
        "getTime" => new HtmlWidgetCallToolResponse
        {
            CallToolResult = new McpUiCallToolResult
            {
                Content = [new McpUiCallToolResultContent { Text = DateTime.UtcNow.ToString("HH:mm:ss") }],
                StructuredContent = new { time = DateTime.UtcNow.ToString("o") },
            }
        },
        "roll" => CreateRollResponse(args),
        "echo" => new HtmlWidgetCallToolResponse
        {
            CallToolResult = new McpUiCallToolResult
            {
                Content = [new McpUiCallToolResultContent { Text = JsonSerializer.Serialize(args) }],
                StructuredContent = args,
            }
        },
        _ => HtmlWidgetCallToolResponse.FromError($"Unknown tool: {toolName}"),
    };

    Console.WriteLine($"[widget.callTool] result={JsonSerializer.Serialize(response)}");

    await Task.CompletedTask;
    return InvokeResponse.Ok(response);
});

webApp.Run();

static int GetCounter(object? args)
{
    if (args is JsonElement je && je.TryGetProperty("counter", out var c) && c.TryGetInt32(out var val))
        return val;
    return 0;
}

static HtmlWidgetCallToolResponse CreateRollResponse(object? args)
{
    int sides = 6;
    if (args is JsonElement je && je.TryGetProperty("sides", out var s) && s.TryGetInt32(out var val))
        sides = val;

    int result = Random.Shared.Next(1, sides + 1);
    return new HtmlWidgetCallToolResponse
    {
        CallToolResult = new McpUiCallToolResult
        {
            Content = [new McpUiCallToolResultContent { Text = $"Rolled a {result} (d{sides})" }],
            StructuredContent = new { result, sides },
        }
    };
}
