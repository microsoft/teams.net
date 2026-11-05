// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.HtmlWidget;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

#pragma warning disable ExperimentalTeamsHtmlWidget

public class HtmlWidgetHelpersTests
{
    // --- BuildHtmlWidgetMarkdown ---

    [Fact]
    public void BuildHtmlWidgetMarkdown_WrapsInCodeFence()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body><p>Hi</p></body>",
            Domain = "https://example.com"
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.StartsWith("```html-widget\n", result);
        Assert.EndsWith("\n```", result);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_AppliesDefaultSecurityPolicy()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body><p>Hi</p></body>",
            Domain = "https://example.com"
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.Contains("securityPolicy", result);
        Assert.Contains("resourceDomains", result);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_IncludesBeforeAndAfter()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body><p>Hi</p></body>",
            Domain = "https://example.com"
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload, new HtmlWidgetMarkdownOptions
        {
            Before = "Hello before",
            After = "Hello after"
        });
        Assert.StartsWith("Hello before\n", result);
        Assert.EndsWith("\nHello after", result);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_ThrowsOnEmptyName()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "",
            Html = "<body>Hi</body>",
            Domain = "https://example.com"
        };
        var ex = Assert.Throws<ArgumentException>(() => HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_ThrowsOnEmptyHtml()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "",
            Domain = "https://example.com"
        };
        var ex = Assert.Throws<ArgumentException>(() => HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload));
        Assert.Contains("html", ex.Message);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_ThrowsOnInvalidDomain()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body>Hi</body>",
            Domain = "http://example.com"
        };
        var ex = Assert.Throws<ArgumentException>(() => HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload));
        Assert.Contains("domain", ex.Message);
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("https:// not a url")]
    [InlineData("example.com")]
    public void BuildHtmlWidgetMarkdown_ThrowsOnMalformedDomain(string domain)
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body>Hi</body>",
            Domain = domain
        };
        var ex = Assert.Throws<ArgumentException>(() => HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload));
        Assert.Contains("domain", ex.Message);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_AcceptsValidHttpsDomain()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body>Hi</body>",
            Domain = "https://teams.microsoft.com"
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.Contains("teams.microsoft.com", result);
    }

    [Fact]
    public void BuildHtmlWidgetMessage_SetsExtendedMarkdownFormat()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body><p>Hi</p></body>",
            Domain = "https://example.com"
        };
        var message = HtmlWidgetHelpers.BuildHtmlWidgetMessage(payload);
        Assert.Equal(TextFormats.ExtendedMarkdown, message.TextFormat);
        Assert.Contains("```html-widget", message.Text);
    }

    // --- ValidateSecurityPolicy ---

    [Fact]
    public void ValidateSecurityPolicy_DetectsExternalScriptSrc()
    {
        var html = "<script src=\"https://cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<script src>", warnings[0].Source);
        Assert.Equal("resourceDomains", warnings[0].PolicyField);
    }

    [Fact]
    public void ValidateSecurityPolicy_AllowsMatchingDomains()
    {
        var html = "<script src=\"https://cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = ["https://cdn.example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_WarnsWhenSubdomainUsesDifferentSchemeThanPinnedEntry()
    {
        var html = "<script src=\"http://cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = ["https://example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("http://cdn.example.com/lib.js", warnings[0].Url);
    }

    [Fact]
    public void ValidateSecurityPolicy_AllowsAnySchemeForHostOnlyEntry()
    {
        var html = "<script src=\"http://cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = ["example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsBaseHref()
    {
        var html = "<base href=\"https://evil.example.com/\">";
        var policy = new HtmlWidgetSecurityPolicy { BaseUriDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("baseUriDomains", warnings[0].PolicyField);
        Assert.Equal("<base href>", warnings[0].Source);
        Assert.Equal("https://evil.example.com/", warnings[0].Url);
    }

    [Fact]
    public void ValidateSecurityPolicy_AllowsBaseHrefInPolicy()
    {
        var html = "<base href=\"https://cdn.example.com/\">";
        var policy = new HtmlWidgetSecurityPolicy { BaseUriDomains = ["https://cdn.example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsFetch()
    {
        var html = "<script>fetch('https://api.example.com/data')</script>";
        var policy = new HtmlWidgetSecurityPolicy { ConnectDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("fetch()", warnings[0].Source);
        Assert.Equal("connectDomains", warnings[0].PolicyField);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsIframeSrc()
    {
        var html = "<iframe src=\"https://embed.example.com/video\"></iframe>";
        var policy = new HtmlWidgetSecurityPolicy { FrameDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<iframe src>", warnings[0].Source);
        Assert.Equal("frameDomains", warnings[0].PolicyField);
    }

    [Fact]
    public void ValidateSecurityPolicy_AllowsWildcard()
    {
        var html = "<script src=\"https://anything.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = ["*"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_IgnoresRelativeUrls()
    {
        var html = "<script src=\"./local.js\"></script><img src=\"/images/logo.png\">";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_IgnoresDataUrls()
    {
        var html = "<img src=\"data:image/png;base64,abc\">";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsCssUrl()
    {
        var html = "<style>body { background: url('https://cdn.example.com/bg.png'); }</style>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("CSS url()", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsFormAction()
    {
        var html = "<form action=\"https://evil.com/steal\"></form>";
        var policy = new HtmlWidgetSecurityPolicy { ConnectDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<form action>", warnings[0].Source);
    }

    // --- InvokeNames ---

    [Fact]
    public void InvokeNames_HasHtmlWidgetCallTool()
    {
        Assert.Equal("htmlwidget/calltool", InvokeNames.HtmlWidgetCallTool);
    }

    // --- TextFormats ---

    [Fact]
    public void TextFormats_HasExtendedMarkdown()
    {
        Assert.Equal("extendedmarkdown", TextFormats.ExtendedMarkdown);
    }

    // --- CallToolResponse helpers ---

    [Fact]
    public void HtmlWidgetCallToolResponse_FromText_CreatesCorrectShape()
    {
        var response = HtmlWidgetCallToolResponse.FromText("hello");
        Assert.Equal("htmlwidget/calltoolresult", response.ResponseType);
        Assert.Single(response.CallToolResult.Content);
        Assert.Equal("text", response.CallToolResult.Content[0].Type);
        Assert.Equal("hello", response.CallToolResult.Content[0].Text);
        Assert.False(response.CallToolResult.IsError);
    }

    [Fact]
    public void HtmlWidgetCallToolResponse_FromError_SetsIsError()
    {
        var response = HtmlWidgetCallToolResponse.FromError("something failed");
        Assert.Equal("htmlwidget/calltoolresult", response.ResponseType);
        Assert.True(response.CallToolResult.IsError);
        Assert.Equal("something failed", response.CallToolResult.Content[0].Text);
    }

    [Fact]
    public void HtmlWidgetCallToolResponse_SerializesCorrectly()
    {
        var response = HtmlWidgetCallToolResponse.FromText("test");
        var json = JsonSerializer.Serialize(response);
        Assert.Contains("\"responseType\":\"htmlwidget/calltoolresult\"", json);
        Assert.Contains("\"callToolResult\"", json);
    }

    [Fact]
    public void TextContent_SerializesTypeAndText()
    {
        var content = new McpUiCallToolResultContent { Type = "text", Text = "hi" };
        var json = JsonSerializer.Serialize(content);
        Assert.Contains("\"type\":\"text\"", json);
        Assert.Contains("\"text\":\"hi\"", json);
        Assert.DoesNotContain("\"data\"", json);
        Assert.DoesNotContain("\"resource\"", json);
    }

    [Fact]
    public void ImageContent_SerializesDataAndMimeType()
    {
        var content = new McpUiCallToolResultContent
        {
            Type = "image",
            Data = "iVBORw0KGgo=",
            MimeType = "image/png"
        };
        var json = JsonSerializer.Serialize(content);
        Assert.Contains("\"type\":\"image\"", json);
        Assert.Contains("\"data\":\"iVBORw0KGgo=\"", json);
        Assert.Contains("\"mimeType\":\"image/png\"", json);
        Assert.DoesNotContain("\"text\"", json);
    }

    [Fact]
    public void AudioContent_SerializesDataAndMimeType()
    {
        var content = new McpUiCallToolResultContent
        {
            Type = "audio",
            Data = "UklGRg==",
            MimeType = "audio/wav"
        };
        var json = JsonSerializer.Serialize(content);
        Assert.Contains("\"type\":\"audio\"", json);
        Assert.Contains("\"data\":\"UklGRg==\"", json);
        Assert.Contains("\"mimeType\":\"audio/wav\"", json);
    }

    [Fact]
    public void ResourceContent_SerializesNestedResource()
    {
        var content = new McpUiCallToolResultContent
        {
            Type = "resource",
            Resource = new McpUiResource { Uri = "widget://data", MimeType = "application/json", Text = "{}" }
        };
        var json = JsonSerializer.Serialize(content);
        Assert.Contains("\"type\":\"resource\"", json);
        Assert.Contains("\"resource\":{", json);
        Assert.Contains("\"uri\":\"widget://data\"", json);
        Assert.Contains("\"mimeType\":\"application/json\"", json);
        Assert.DoesNotContain("\"blob\"", json);
    }

    [Fact]
    public void ContentTypes_RoundTripThroughDeserialization()
    {
        var image = new McpUiCallToolResultContent
        {
            Type = "image",
            Data = "abc",
            MimeType = "image/jpeg"
        };
        var json = JsonSerializer.Serialize(image);
        var parsed = JsonSerializer.Deserialize<McpUiCallToolResultContent>(json);
        Assert.NotNull(parsed);
        Assert.Equal("image", parsed!.Type);
        Assert.Equal("abc", parsed.Data);
        Assert.Equal("image/jpeg", parsed.MimeType);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_DoesNotModifyHtml()
    {
        var html = "<body><p>Hi</p></body>";
        var payload = new HtmlWidgetPayload { Name = "test", Html = html, Domain = "https://example.com" };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.DoesNotContain("ui/initialize", result);
        Assert.DoesNotContain("size-changed", result);
        Assert.Contains(JsonSerializer.Serialize(html), result);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_OmitsMcpUrlAndCorrelationIdWhenUnset()
    {
        var payload = new HtmlWidgetPayload { Name = "test", Html = "<body>hi</body>", Domain = "https://example.com" };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.DoesNotContain("mcpUrl", result);
        Assert.DoesNotContain("correlationId", result);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_IncludesMcpUrlAndCorrelationIdWhenSet()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body>hi</body>",
            Domain = "https://example.com",
            McpUrl = "https://mcp.example.com",
            CorrelationId = "abc.1",
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.Contains("\"mcpUrl\":\"https://mcp.example.com\"", result);
        Assert.Contains("\"correlationId\":\"abc.1\"", result);
    }

    [Fact]
    public void CallToolResult_AlwaysSerializesDefaults()
    {
        var json = JsonSerializer.Serialize(new McpUiCallToolResult());
        Assert.Equal("{\"content\":[],\"structuredContent\":{},\"isError\":false}", json);
    }

    [Fact]
    public void CallToolResponse_FromText_SerializesStructuredContentAndIsError()
    {
        var json = JsonSerializer.Serialize(HtmlWidgetCallToolResponse.FromText("hi"));
        Assert.Contains("\"structuredContent\":{}", json);
        Assert.Contains("\"isError\":false", json);
    }

    [Fact]
    public void CallToolResult_ExplicitNullAssignment_KeepsNonNullWireShape()
    {
        var result = new McpUiCallToolResult { Content = null!, StructuredContent = null! };
        Assert.Equal("{\"content\":[],\"structuredContent\":{},\"isError\":false}", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void CallToolResult_BoxedJsonNullStructuredContent_KeepsNonNullWireShape()
    {
        var result = new McpUiCallToolResult { StructuredContent = JsonDocument.Parse("null").RootElement };
        Assert.Equal("{\"content\":[],\"structuredContent\":{},\"isError\":false}", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void CallToolResult_JsonDocumentWithNullRoot_KeepsNonNullWireShape()
    {
        var result = new McpUiCallToolResult { StructuredContent = JsonDocument.Parse("null") };
        Assert.Equal("{\"content\":[],\"structuredContent\":{},\"isError\":false}", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void CallToolResult_DeserializingNulls_KeepsNonNullWireShape()
    {
        var result = JsonSerializer.Deserialize<McpUiCallToolResult>("{\"content\":null,\"structuredContent\":null}")!;
        Assert.Equal("{\"content\":[],\"structuredContent\":{},\"isError\":false}", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void ValidateSecurityPolicy_AllowsSubdomainWhenParentInPolicy()
    {
        var html = "<script src=\"https://cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = ["https://example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_DoesNotAllowUnrelatedDomainSharingSuffix()
    {
        var html = "<script src=\"https://notexample.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = ["https://example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_DoesNotOverwriteUserSecurityPolicy()
    {
        var customPolicy = new HtmlWidgetSecurityPolicy
        {
            ConnectDomains = ["https://api.custom.com"],
            ResourceDomains = ["https://cdn.custom.com"],
            FrameDomains = ["https://embed.custom.com"],
            BaseUriDomains = [],
        };
        var payload = new HtmlWidgetPayload
        {
            Name = "test",
            Html = "<body>hi</body>",
            Domain = "https://example.com",
            SecurityPolicy = customPolicy,
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.Contains("api.custom.com", result);
        Assert.Contains("cdn.custom.com", result);
        Assert.Contains("embed.custom.com", result);
    }

    [Fact]
    public void BuildHtmlWidgetMarkdown_SerializesFullPayload()
    {
        var payload = new HtmlWidgetPayload
        {
            Name = "Weather Widget",
            Description = "Current weather",
            Html = "<body><div>72F</div></body>",
            Domain = "https://weather.example.com",
            SecurityPolicy = new HtmlWidgetSecurityPolicy
            {
                ConnectDomains = ["https://api.example.com"],
                ResourceDomains = ["'self'", "data:"],
                FrameDomains = [],
                BaseUriDomains = [],
            },
            ToolInput = JsonSerializer.SerializeToElement(new { location = "Seattle" }),
            Permissions = new HtmlWidgetPermissions { ClipboardWrite = new Dictionary<string, object>() },
        };
        var result = HtmlWidgetHelpers.BuildHtmlWidgetMarkdown(payload);
        Assert.StartsWith("```html-widget\n", result);
        Assert.EndsWith("\n```", result);
        Assert.Contains("Weather Widget", result);
        Assert.Contains("Current weather", result);
        Assert.Contains("weather.example.com", result);
        Assert.Contains("api.example.com", result);
        Assert.Contains("Seattle", result);
        Assert.Contains("clipboardWrite", result);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsImgSrc()
    {
        var html = "<img src=\"https://cdn.example.com/logo.png\">";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<img src>", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsAudioSrc()
    {
        var html = "<audio src=\"https://cdn.example.com/audio.mp3\"></audio>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<audio src>", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsVideoSrc()
    {
        var html = "<video src=\"https://cdn.example.com/video.mp4\"></video>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<video src>", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsLinkHref()
    {
        var html = "<link href=\"https://cdn.example.com/style.css\" rel=\"stylesheet\">";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("<link href>", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsWebSocket()
    {
        var html = "<script>new WebSocket('wss://ws.example.com/feed')</script>";
        var policy = new HtmlWidgetSecurityPolicy { ConnectDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("new WebSocket()", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsXhr()
    {
        var html = "<script>xhr.open('GET', 'https://api.example.com/data')</script>";
        var policy = new HtmlWidgetSecurityPolicy { ConnectDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("XMLHttpRequest.open()", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsEventSource()
    {
        var html = "<script>new EventSource('https://sse.example.com/stream')</script>";
        var policy = new HtmlWidgetSecurityPolicy { ConnectDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
        Assert.Equal("new EventSource()", warnings[0].Source);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsMultipleViolations()
    {
        var html = "<script src=\"https://a.com/x.js\"></script><img src=\"https://b.com/y.png\"><script>fetch('https://c.com/api')</script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [], ConnectDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Equal(3, warnings.Count);
    }

    [Fact]
    public void ValidateSecurityPolicy_NoWarningsForNoExternalRefs()
    {
        var html = "<div><p>Just text</p></div>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_HandlesProtocolRelativeUrls()
    {
        var html = "<script src=\"//cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_NoWarnWhenFetchInConnectDomains()
    {
        var html = "<script>fetch('https://api.example.com/data')</script>";
        var policy = new HtmlWidgetSecurityPolicy { ConnectDomains = ["https://api.example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_NoWarnWhenIframeInFrameDomains()
    {
        var html = "<iframe src=\"https://embed.example.com/video\"></iframe>";
        var policy = new HtmlWidgetSecurityPolicy { FrameDomains = ["https://embed.example.com"] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_DetectsCssImport()
    {
        var html = "<style>@import url('https://fonts.example.com/font.css');</style>";
        var policy = new HtmlWidgetSecurityPolicy { ResourceDomains = [] };
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
    }

    [Fact]
    public void ValidateSecurityPolicy_HandlesUndefinedPolicyFields()
    {
        var html = "<script src=\"https://cdn.example.com/lib.js\"></script>";
        var policy = new HtmlWidgetSecurityPolicy();
        var warnings = HtmlWidgetHelpers.ValidateSecurityPolicy(html, policy);
        Assert.Single(warnings);
    }

    // --- TryGetWidgetModelContext ---

    private static MessageActivity MessageActivityWithValue(object? value)
    {
        CoreActivity core = new()
        {
            Type = TeamsActivityTypes.Message
        };
        if (value is not null)
        {
            core.Properties["value"] = JsonSerializer.SerializeToElement(value);
        }
        return MessageActivity.FromActivity(core);
    }

    [Fact]
    public void TryGetWidgetModelContext_ParsesRawRequest()
    {
        var activity = MessageActivityWithValue(new
        {
            method = "ui/update-model-context",
            @params = new { content = new[] { new { type = "text", text = "hello" } } }
        });

        var result = HtmlWidgetHelpers.TryGetWidgetModelContext(activity);

        Assert.NotNull(result);
        Assert.Equal("ui/update-model-context", result!.Method);
        Assert.NotNull(result.Params.Content);
        Assert.Single(result.Params.Content!);
        Assert.Equal("hello", result.Params.Content[0].Text);
    }

    [Fact]
    public void TryGetWidgetModelContext_ParsesStructuredContentOnly()
    {
        var activity = MessageActivityWithValue(new
        {
            method = "ui/update-model-context",
            @params = new { structuredContent = new { count = 5 } }
        });

        var result = HtmlWidgetHelpers.TryGetWidgetModelContext(activity);

        Assert.NotNull(result);
        Assert.NotNull(result!.Params.StructuredContent);
    }

    [Fact]
    public void TryGetWidgetModelContext_UnwrapsEnvelope()
    {
        var activity = MessageActivityWithValue(new
        {
            type = "widgetModelContext",
            data = new
            {
                method = "ui/update-model-context",
                @params = new { content = new[] { new { type = "text", text = "wrapped" } } }
            }
        });

        var result = HtmlWidgetHelpers.TryGetWidgetModelContext(activity);

        Assert.NotNull(result);
        Assert.Equal("wrapped", result!.Params.Content[0].Text);
    }

    [Fact]
    public void TryGetWidgetModelContext_ReturnsNullWhenValueMissing()
    {
        var activity = MessageActivityWithValue(null);
        Assert.Null(HtmlWidgetHelpers.TryGetWidgetModelContext(activity));
    }

    [Fact]
    public void TryGetWidgetModelContext_ReturnsNullWhenValueNotObject()
    {
        var activity = MessageActivityWithValue("hello");
        Assert.Null(HtmlWidgetHelpers.TryGetWidgetModelContext(activity));
    }

    [Fact]
    public void TryGetWidgetModelContext_ReturnsNullForNonMatchingMethod()
    {
        var activity = MessageActivityWithValue(new
        {
            method = "something/else",
            @params = new { }
        });
        Assert.Null(HtmlWidgetHelpers.TryGetWidgetModelContext(activity));
    }

    [Fact]
    public void TryGetWidgetModelContext_ReturnsNullWhenParamsMissing()
    {
        var activity = MessageActivityWithValue(new
        {
            method = "ui/update-model-context"
        });
        Assert.Null(HtmlWidgetHelpers.TryGetWidgetModelContext(activity));
    }

    [Fact]
    public void TryGetWidgetModelContext_ReturnsNullForWrongEnvelopeType()
    {
        var activity = MessageActivityWithValue(new
        {
            type = "other",
            data = new { method = "ui/update-model-context", @params = new { } }
        });
        Assert.Null(HtmlWidgetHelpers.TryGetWidgetModelContext(activity));
    }
}

#pragma warning restore ExperimentalTeamsHtmlWidget
