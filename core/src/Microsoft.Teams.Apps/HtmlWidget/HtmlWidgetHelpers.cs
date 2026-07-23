// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.HtmlWidget;

/// <summary>
/// Options for injecting the MCP Apps protocol into widget HTML.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class InjectWidgetProtocolOptions
{
    /// <summary>
    /// The widget app name sent during ui/initialize.
    /// </summary>
    public string Name { get; set; } = "widget";

    /// <summary>
    /// The widget app version sent during ui/initialize.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Display modes this widget supports.
    /// </summary>
    public IList<string>? AvailableDisplayModes { get; set; }

    /// <summary>
    /// Host notifications to listen for.
    /// Known values: "tool-result", "tool-input", "tool-input-partial",
    /// "tool-cancelled", "host-context-changed", "resource-teardown".
    /// </summary>
    public IList<string>? Notifications { get; set; }

    /// <summary>
    /// When true, injects a CSP violation listener for debugging.
    /// </summary>
    public bool DebugCspViolations { get; set; }
}

/// <summary>
/// Options for building an HTML widget markdown string.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class HtmlWidgetMarkdownOptions
{
    /// <summary>
    /// Text to include before the widget code block.
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Text to include after the widget code block.
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Options forwarded to <see cref="HtmlWidgetHelpers.InjectWidgetProtocol"/> when the protocol
    /// is auto-injected. The Name field is always set from the payload's Name.
    /// </summary>
    public InjectWidgetProtocolOptions? ProtocolOptions { get; set; }
}

/// <summary>
/// A warning produced by <see cref="HtmlWidgetHelpers.ValidateSecurityPolicy"/> when the widget HTML
/// references an external origin not present in the declared security policy.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public class SecurityPolicyWarning
{
    /// <summary>
    /// The URL or origin found in the HTML.
    /// </summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "May contain relative URLs, fragments, or unparseable values")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The HTML element or API where the reference was found.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The securityPolicy field that should include this origin.
    /// </summary>
    public string PolicyField { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Helper methods for building and validating HTML widget messages.
/// </summary>
[Experimental("ExperimentalTeamsHtmlWidget")]
public static class HtmlWidgetHelpers
{
    private const string McpProtocolVersion = "2026-01-26";

    private static readonly Dictionary<string, string> NotificationCallbacks = new()
    {
        ["tool-result"] = "onToolResult",
        ["tool-input"] = "onToolInput",
        ["tool-input-partial"] = "onToolInputPartial",
        ["tool-cancelled"] = "onToolCancelled",
        ["host-context-changed"] = "onHostContextChanged",
        ["resource-teardown"] = "onResourceTeardown",
    };

    private static readonly HtmlWidgetSecurityPolicy DefaultSecurityPolicy = new()
    {
        ConnectDomains = [],
        ResourceDomains = ["'self'", "data:"],
        FrameDomains = [],
        BaseUriDomains = [],
    };

    /// <summary>
    /// Injects the MCP Apps protocol script into widget HTML.
    /// If the HTML already contains the protocol (detected by "ui/initialize"), it is returned unchanged.
    /// </summary>
    /// <param name="html">The raw HTML content for the widget.</param>
    /// <param name="options">Optional configuration for the protocol setup.</param>
    /// <returns>The HTML with the protocol script injected.</returns>
    public static string InjectWidgetProtocol(string html, InjectWidgetProtocolOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(html);

        // Skip injection only if the HTML already performs the ui/initialize
        // handshake (a `method: 'ui/initialize'` postMessage), not if it merely
        // mentions the string somewhere (e.g. in a comment or visible text).
        if (Regex.IsMatch(html, "[\"']?method[\"']?\\s*:\\s*[\"']ui/initialize[\"']"))
        {
            return html;
        }

        var name = EscapeForInlineScript(options?.Name ?? "widget");
        var version = EscapeForInlineScript(options?.Version ?? "1.0.0");

        var capsJson = options?.AvailableDisplayModes is { Count: > 0 } modes
            ? $"{{availableDisplayModes:{JsonSerializer.Serialize(modes)}}}"
            : "{}";

        var hookLines = new StringBuilder();
        if (options?.Notifications is { Count: > 0 } notifications)
        {
            foreach (var n in notifications)
            {
                if (NotificationCallbacks.TryGetValue(n, out var cb))
                {
                    hookLines.Append(CultureInfo.InvariantCulture, $"if(d.method==='ui/notifications/{n}'&&window.{cb}){{window.{cb}(d.params);}}");
                }
            }
        }

        var cspDebug = options?.DebugCspViolations == true
            ? "document.addEventListener('securitypolicyviolation',function(e){"
              + "console.warn('[widget CSP violation]',{"
              + "blockedURI:e.blockedURI,"
              + "violatedDirective:e.violatedDirective,"
              + "originalPolicy:e.originalPolicy"
              + "});});"
            : "";

        var script = "<script>(function(){"
            + cspDebug
            + "var id='init-'+Math.random().toString(36).slice(2);"
            + "function notifySize(){window.parent.postMessage({jsonrpc:'2.0',method:'ui/notifications/size-changed',params:{height:document.body.scrollHeight}},'*');}"
            + "window.addEventListener('message',function(e){var d=e.data;if(!d||d.jsonrpc!=='2.0')return;"
            + "if(d.id===id&&d.result){window.parent.postMessage({jsonrpc:'2.0',method:'ui/notifications/initialized'},'*');setTimeout(notifySize,100);}"
            + hookLines
            + "});"
            + $"window.parent.postMessage({{jsonrpc:'2.0',id:id,method:'ui/initialize',params:{{protocolVersion:'{McpProtocolVersion}',appInfo:{{name:'{name}',version:'{version}'}},appCapabilities:{capsJson}}}}},'*');"
            + "document.addEventListener('DOMContentLoaded',notifySize);"
            + "})()</script>";

        if (html.Contains("</body>", StringComparison.Ordinal))
        {
            return html.Replace("</body>", script + "</body>", StringComparison.Ordinal);
        }

        return html + script;
    }

    /// <summary>
    /// Wraps an HTML widget payload in the ```html-widget markdown code fence
    /// format required by Teams to render the widget in a message.
    /// </summary>
    /// <param name="payload">The widget payload to serialize.</param>
    /// <param name="options">Optional text to include before/after the widget block.</param>
    /// <returns>The markdown string containing the widget code block.</returns>
    public static string BuildHtmlWidgetMarkdown(HtmlWidgetPayload payload, HtmlWidgetMarkdownOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ValidatePayload(payload);

        var protocolOpts = new InjectWidgetProtocolOptions
        {
            Name = payload.Name,
            Version = options?.ProtocolOptions?.Version ?? "1.0.0",
            AvailableDisplayModes = options?.ProtocolOptions?.AvailableDisplayModes,
            Notifications = options?.ProtocolOptions?.Notifications,
            DebugCspViolations = options?.ProtocolOptions?.DebugCspViolations ?? false,
        };

        var injectedPayload = new HtmlWidgetPayload
        {
            Type = payload.Type,
            Name = payload.Name,
            Description = payload.Description,
            Html = InjectWidgetProtocol(payload.Html, protocolOpts),
            Domain = payload.Domain,
            SecurityPolicy = payload.SecurityPolicy ?? DefaultSecurityPolicy,
            ToolInput = payload.ToolInput,
            ToolOutput = payload.ToolOutput,
            Permissions = payload.Permissions,
        };

        var json = JsonSerializer.Serialize(injectedPayload);
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(options?.Before))
        {
            parts.Add(options.Before);
            parts.Add("");
        }

        parts.Add("```html-widget");
        parts.Add(json);
        parts.Add("```");

        if (!string.IsNullOrEmpty(options?.After))
        {
            parts.Add("");
            parts.Add(options.After);
        }

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Builds a message activity containing an HTML widget, ready to be sent.
    /// </summary>
    /// <param name="payload">The widget payload to include in the message.</param>
    /// <param name="options">Optional text to include before/after the widget block.</param>
    /// <returns>A MessageActivityInput with TextFormat set to "extendedmarkdown".</returns>
    public static MessageActivityInput BuildHtmlWidgetMessage(HtmlWidgetPayload payload, HtmlWidgetMarkdownOptions? options = null)
    {
        return MessageActivityInput.CreateBuilder()
            .WithText(BuildHtmlWidgetMarkdown(payload, options), TextFormats.ExtendedMarkdown)
            .Build();
    }

    /// <summary>
    /// Attempts to extract an MCP UI update-model-context request from a message
    /// activity's <c>value</c>. A widget can request that content be added to the
    /// model context by reusing the messageBack mechanism (like <c>Action.Submit</c>
    /// for adaptive cards).
    /// Such a request arrives as a normal message activity whose <c>value</c> carries
    /// the <see cref="McpUiUpdateModelContextRequest"/> payload.
    /// This is fire-and-forget: the bot does not respond.
    /// The helper is tolerant of two wire shapes: the raw request object, or an
    /// envelope of the form <c>{ "type": "widgetModelContext", "data": &lt;request&gt; }</c>.
    /// </summary>
    /// <param name="activity">The received message activity.</param>
    /// <returns>The parsed request, or <c>null</c> if the activity value is not a valid update-model-context request.</returns>
    [Experimental("ExperimentalTeamsHtmlWidget")]
    public static McpUiUpdateModelContextRequest? TryGetWidgetModelContext(MessageActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (!activity.Properties.TryGetValue("value", out object? raw) || raw is null)
        {
            return null;
        }

        JsonElement value = raw is JsonElement je ? je : JsonSerializer.SerializeToElement(raw);
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Unwrap the { "type": "widgetModelContext", "data": ... } envelope if present.
        JsonElement candidate = value;
        if (value.TryGetProperty("type", out JsonElement typeEl)
            && typeEl.ValueKind == JsonValueKind.String
            && typeEl.GetString() == "widgetModelContext"
            && value.TryGetProperty("data", out JsonElement dataEl)
            && dataEl.ValueKind == JsonValueKind.Object)
        {
            candidate = dataEl;
        }

        if (!candidate.TryGetProperty("method", out JsonElement methodEl)
            || methodEl.ValueKind != JsonValueKind.String
            || methodEl.GetString() != "ui/update-model-context")
        {
            return null;
        }

        if (!candidate.TryGetProperty("params", out JsonElement paramsEl)
            || paramsEl.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<McpUiUpdateModelContextRequest>(candidate.GetRawText());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Validates that external references in widget HTML are covered by the
    /// declared security policy. Returns a list of warnings for any
    /// references to origins not present in the appropriate policy field.
    /// </summary>
    /// <param name="html">The raw HTML content of the widget.</param>
    /// <param name="policy">The security policy to validate against.</param>
    /// <returns>A list of warnings. Empty list means no issues found.</returns>
    public static IList<SecurityPolicyWarning> ValidateSecurityPolicy(string html, HtmlWidgetSecurityPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(policy);

        var warnings = new List<SecurityPolicyWarning>();

        // resourceDomains: <script src>, <link href>, <img src>, <source src>, <audio src>, <video src>
        CheckTagAttribute(html, "script", "src", "resourceDomains", "<script src>", policy.ResourceDomains, warnings);
        CheckTagAttribute(html, "link", "href", "resourceDomains", "<link href>", policy.ResourceDomains, warnings);
        CheckTagAttribute(html, "img", "src", "resourceDomains", "<img src>", policy.ResourceDomains, warnings);
        CheckTagAttribute(html, "source", "src", "resourceDomains", "<source src>", policy.ResourceDomains, warnings);
        CheckTagAttribute(html, "audio", "src", "resourceDomains", "<audio src>", policy.ResourceDomains, warnings);
        CheckTagAttribute(html, "video", "src", "resourceDomains", "<video src>", policy.ResourceDomains, warnings);

        // CSS url() and @import
        CheckCssPatterns(html, policy.ResourceDomains, warnings);

        // connectDomains: fetch(), XMLHttpRequest.open(), new WebSocket(), new EventSource()
        CheckConnectPatterns(html, policy.ConnectDomains, warnings);

        // frameDomains: <iframe src>
        CheckTagAttribute(html, "iframe", "src", "frameDomains", "<iframe src>", policy.FrameDomains, warnings);

        // connectDomains: <form action>
        CheckTagAttribute(html, "form", "action", "connectDomains", "<form action>", policy.ConnectDomains, warnings);

        // baseUriDomains: <base href>
        CheckTagAttribute(html, "base", "href", "baseUriDomains", "<base href>", policy.BaseUriDomains, warnings);

        return warnings;
    }

    private static void ValidatePayload(HtmlWidgetPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            throw new ArgumentException("HTML widget payload requires a non-empty \"name\" field.");
        }

        if (string.IsNullOrWhiteSpace(payload.Html))
        {
            throw new ArgumentException("HTML widget payload requires a non-empty \"html\" field.");
        }

        if (string.IsNullOrWhiteSpace(payload.Domain)
            || !Uri.TryCreate(payload.Domain.Trim(), UriKind.Absolute, out var domainUri)
            || domainUri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrEmpty(domainUri.Host))
        {
            throw new ArgumentException("HTML widget payload requires \"domain\" to be a valid URL starting with \"https://\".");
        }
    }

    private static string EscapeForInlineScript(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("</", "<\\/", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }

    private static string? ExtractOrigin(string url)
    {
        var trimmed = url.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("data:", StringComparison.Ordinal) || trimmed.StartsWith('#') || trimmed.StartsWith("blob:", StringComparison.Ordinal))
        {
            return null;
        }

        if (!trimmed.Contains("://", StringComparison.Ordinal) && !trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var fullUrl = trimmed.StartsWith("//", StringComparison.Ordinal) ? $"https:{trimmed}" : trimmed;
            var uri = new Uri(fullUrl);
            return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static bool IsOriginAllowed(string origin, IList<string>? allowedDomains)
    {
        if (allowedDomains is null || allowedDomains.Count == 0) return false;
        if (allowedDomains.Contains("*")) return true;

        foreach (var domain in allowedDomains)
        {
            var cleaned = domain.Trim('\'', '"');
            if (cleaned == "*") return true;
            if (origin == cleaned) return true;

            // If the policy entry pins a scheme (e.g. "https://example.com"), the
            // origin must use that same scheme; otherwise fall back to host-only match.
            var schemeMatch = Regex.Match(cleaned, @"^(https?)://");
            if (schemeMatch.Success && !origin.StartsWith($"{schemeMatch.Groups[1].Value}://", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var domainHost = Regex.Replace(cleaned, @"^https?://", "");
            if (origin.EndsWith($".{domainHost}", StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static void CheckTagAttribute(
        string html,
        string tagName,
        string attrName,
        string policyField,
        string source,
        IList<string>? allowedDomains,
        List<SecurityPolicyWarning> warnings)
    {
        var upperHtml = html.ToUpperInvariant();
        var needle = $"<{tagName}".ToUpperInvariant();
        var pos = 0;

        while (pos < upperHtml.Length)
        {
            var start = upperHtml.IndexOf(needle, pos, StringComparison.Ordinal);
            if (start == -1) break;

            var afterTag = start + needle.Length;
            if (afterTag < upperHtml.Length && upperHtml[afterTag] != ' ' && upperHtml[afterTag] != '\t'
                && upperHtml[afterTag] != '\n' && upperHtml[afterTag] != '\r' && upperHtml[afterTag] != '>'
                && upperHtml[afterTag] != '/')
            {
                pos = afterTag;
                continue;
            }

            var end = html.IndexOf('>', start);
            if (end == -1) break;

            var tagStr = html.Substring(start, end - start + 1);
            var attrMatch = Regex.Match(tagStr, $@"{attrName}=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (attrMatch.Success)
            {
                var url = attrMatch.Groups[1].Value;
                var origin = ExtractOrigin(url);
                if (origin != null && !IsOriginAllowed(origin, allowedDomains))
                {
                    warnings.Add(new SecurityPolicyWarning
                    {
                        Url = url,
                        Source = source,
                        PolicyField = policyField,
                        Message = $"{source} references \"{url}\" but origin \"{origin}\" is not in {policyField}.",
                    });
                }
            }

            pos = end + 1;
        }
    }

    private static void CheckCssPatterns(string html, IList<string>? allowedDomains, List<SecurityPolicyWarning> warnings)
    {
        var cssUrlRegex = new Regex(@"url\(\s*[""']([^""')]+)[""']\s*\)", RegexOptions.IgnoreCase);
        foreach (Match match in cssUrlRegex.Matches(html))
        {
            var url = match.Groups[1].Value;
            var origin = ExtractOrigin(url);
            if (origin != null && !IsOriginAllowed(origin, allowedDomains))
            {
                warnings.Add(new SecurityPolicyWarning
                {
                    Url = url,
                    Source = "CSS url()",
                    PolicyField = "resourceDomains",
                    Message = $"CSS url() references \"{url}\" but origin \"{origin}\" is not in resourceDomains.",
                });
            }
        }

        var importRegex = new Regex(@"@import\s+[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        foreach (Match match in importRegex.Matches(html))
        {
            var url = match.Groups[1].Value;
            var origin = ExtractOrigin(url);
            if (origin != null && !IsOriginAllowed(origin, allowedDomains))
            {
                warnings.Add(new SecurityPolicyWarning
                {
                    Url = url,
                    Source = "CSS @import",
                    PolicyField = "resourceDomains",
                    Message = $"CSS @import references \"{url}\" but origin \"{origin}\" is not in resourceDomains.",
                });
            }
        }
    }

    private static void CheckConnectPatterns(string html, IList<string>? allowedDomains, List<SecurityPolicyWarning> warnings)
    {
        var patterns = new (Regex Regex, string Source)[]
        {
            (new Regex(@"fetch\(\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase), "fetch()"),
            (new Regex(@"\.open\(\s*[""'][A-Za-z]+[""']\s*,\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase), "XMLHttpRequest.open()"),
            (new Regex(@"new\s+WebSocket\(\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase), "new WebSocket()"),
            (new Regex(@"new\s+EventSource\(\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase), "new EventSource()"),
        };

        foreach (var (regex, source) in patterns)
        {
            foreach (Match match in regex.Matches(html))
            {
                var url = match.Groups[1].Value;
                var origin = ExtractOrigin(url);
                if (origin != null && !IsOriginAllowed(origin, allowedDomains))
                {
                    warnings.Add(new SecurityPolicyWarning
                    {
                        Url = url,
                        Source = source,
                        PolicyField = "connectDomains",
                        Message = $"{source} references \"{url}\" but origin \"{origin}\" is not in connectDomains.",
                    });
                }
            }
        }
    }
}
