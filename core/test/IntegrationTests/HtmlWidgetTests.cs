// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.HtmlWidget;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for HTML widget messages — verifies Teams accepts widget payloads via the Bot API.
/// These tests require a canary service URL to pass (widgets are canary-only).
/// </summary>
[SuppressMessage("Usage", "ExperimentalTeamsHtmlWidget:Type is for evaluation purposes only", Justification = "Integration tests for experimental feature")]
public class HtmlWidgetTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public HtmlWidgetTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    private bool IsCanary => _f.ServiceUrl.ToString().Contains("canary", StringComparison.OrdinalIgnoreCase);

    private CoreActivity CreateWidgetActivity(string markdown) =>
        CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithFrom(IntegrationTestFixture.GetChannelAccountWithAgenticProperties())
            .WithProperty("text", markdown)
            .WithProperty("textFormat", TextFormats.ExtendedMarkdown)
            .Build();

    [SkippableFact(Timeout = 5000)]
    public async Task SendWidgetMessage()
    {
        Skip.IfNot(IsCanary, "Widgets require canary service");

        var markdown = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(
            new HtmlWidgetPayload
            {
                Name = "Integration Test Widget",
                Description = "Verifies Teams accepts widget payload.",
                Html = "<body><p>Integration test widget</p></body>",
                Domain = "https://teams.microsoft.com",
                SecurityPolicy = new HtmlWidgetSecurityPolicy
                {
                    ConnectDomains = [],
                    ResourceDomains = ["'self'", "data:"],
                    FrameDomains = [],
                    BaseUriDomains = [],
                },
                Permissions = new HtmlWidgetPermissions(),
            },
            new HtmlWidgetMarkdownOptions { Before = "[.NET Integration] HTML widget send test" });

        CoreActivity activity = CreateWidgetActivity(markdown);
        SendActivityResponse? res = await _f.ScopedApiClient.Conversations.Activities.CreateAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Sent widget activity: {res.Id}");
    }

    [SkippableFact(Timeout = 5000)]
    public async Task SendWidgetWithToolData()
    {
        Skip.IfNot(IsCanary, "Widgets require canary service");

        var markdown = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(
            new HtmlWidgetPayload
            {
                Name = "ToolOutput Widget",
                Description = "Widget with initial tool data.",
                Html = "<body><p>Widget with tool data</p></body>",
                Domain = "https://teams.microsoft.com",
                SecurityPolicy = new HtmlWidgetSecurityPolicy
                {
                    ConnectDomains = [],
                    ResourceDomains = ["'self'"],
                    FrameDomains = [],
                    BaseUriDomains = [],
                },
                ToolInput = System.Text.Json.JsonSerializer.SerializeToElement(new { query = "test" }),
                ToolOutput = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    content = new[] { new { type = "text", text = "Result data" } },
                    structuredContent = new { key = "value" },
                    isError = false,
                }),
                Permissions = new HtmlWidgetPermissions { ClipboardWrite = new() },
            });

        CoreActivity activity = CreateWidgetActivity(markdown);
        SendActivityResponse? res = await _f.ScopedApiClient.Conversations.Activities.CreateAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Sent widget with tool data: {res.Id}");
    }

    [SkippableFact(Timeout = 5000)]
    public async Task UpdateWidgetMessage()
    {
        Skip.IfNot(IsCanary, "Widgets require canary service");

        var markdown = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(
            new HtmlWidgetPayload
            {
                Name = "Update Test Widget",
                Html = "<body><p>Original content</p></body>",
                Domain = "https://teams.microsoft.com",
            },
            new HtmlWidgetMarkdownOptions { Before = "[.NET Integration] Widget update test - original" });

        CoreActivity activity = CreateWidgetActivity(markdown);
        SendActivityResponse? sent = await _f.ScopedApiClient.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        var updatedMarkdown = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(
            new HtmlWidgetPayload
            {
                Name = "Update Test Widget",
                Html = "<body><p>Updated content</p></body>",
                Domain = "https://teams.microsoft.com",
            },
            new HtmlWidgetMarkdownOptions { Before = "[.NET Integration] Widget update test - updated" });

        CoreActivity updatedActivity = CreateWidgetActivity(updatedMarkdown);
        UpdateActivityResponse? res = await _f.ScopedApiClient.Conversations.Activities.UpdateAsync(
            _f.ConversationId, sent.Id, updatedActivity);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated widget activity: {res.Id}");
    }

    [SkippableFact(Timeout = 10000)]
    public async Task DeleteWidgetMessage()
    {
        Skip.IfNot(IsCanary, "Widgets require canary service");

        var markdown = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(
            new HtmlWidgetPayload
            {
                Name = "Delete Test Widget",
                Html = "<body><p>Will be deleted</p></body>",
                Domain = "https://teams.microsoft.com",
            });

        CoreActivity activity = CreateWidgetActivity(markdown);
        SendActivityResponse? sent = await _f.ScopedApiClient.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _f.ScopedApiClient.Conversations.Activities.DeleteAsync(_f.ConversationId, sent.Id, _f.AgenticIdentity);
        _output.WriteLine($"Deleted widget activity: {sent.Id}");
    }
}
