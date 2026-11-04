// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.HtmlWidget;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class HtmlWidgetCallToolHandlerTests
{
    [Fact]
    public async Task OnWidgetCallTool_PassesArgumentsIncludingMsTeamsToHandlerUntouched()
    {
        TeamsBotApplication app = CreateApp();
        CallToolRequest? received = null;
        app.OnWidgetCallTool((ctx, _) =>
        {
            received = ctx.Activity.Value;
            return Task.FromResult(new InvokeResponse(200, HtmlWidgetCallToolResponse.FromText("ok")));
        });

        InvokeActivity activity = new(InvokeNames.HtmlWidgetCallTool)
        {
            Value = JsonNode.Parse("""
                {
                  "name": "echo",
                  "arguments": {
                    "text": "hello",
                    "n": 3,
                    "__msteams__": { "mcpUrl": "https://widgets.example.com/mcp", "correlationId": "cv-123.4" }
                  }
                }
                """)
        };

        InvokeResponse response = await app.Router.DispatchWithReturnAsync(new Context<TeamsActivity>(app, activity));

        Assert.Equal(200, response.Status);
        Assert.NotNull(received);
        Assert.Equal("echo", received.Name);

        JsonNode? expected = JsonNode.Parse("""
            {
              "text": "hello",
              "n": 3,
              "__msteams__": { "mcpUrl": "https://widgets.example.com/mcp", "correlationId": "cv-123.4" }
            }
            """);
        JsonNode? actual = JsonSerializer.SerializeToNode(received.Arguments);
        Assert.True(JsonNode.DeepEquals(expected, actual), actual?.ToJsonString());
    }

    private static TeamsBotApplication CreateApp()
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);

        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        return new TeamsBotApplication(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
    }
}
