// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HtmlWidgetBot;

namespace Microsoft.Teams.Apps.UnitTests;

public class HtmlWidgetSampleScriptTests
{
    [Theory]
    [InlineData("ui/initialize")]
    [InlineData("ui/notifications/initialized")]
    [InlineData("ui/notifications/size-changed")]
    [InlineData("2026-01-26")]
    [InlineData("ResizeObserver")]
    public void HostScript_ContainsProtocolMethods(string expected)
    {
        Assert.Contains(expected, Widgets.HostScript);
    }

    [Fact]
    public void HostScript_MeasuresUnroundedBoundingClientRectHeight()
    {
        Assert.Contains("getBoundingClientRect().height", Widgets.HostScript);
        Assert.DoesNotContain("Math.round", Widgets.HostScript);
        Assert.DoesNotContain("Math.ceil", Widgets.HostScript);
        Assert.DoesNotContain("Math.floor", Widgets.HostScript);
        Assert.DoesNotContain("scrollHeight", Widgets.HostScript);
    }

    [Fact]
    public void HostScript_SendsInitializedAfterInitializeResponse()
    {
        string script = Widgets.HostScript;
        int initialize = script.IndexOf("'ui/initialize'", StringComparison.Ordinal);
        int initialized = script.IndexOf("'ui/notifications/initialized'", StringComparison.Ordinal);
        Assert.True(initialize >= 0 && initialized >= 0);
        Assert.True(script.IndexOf("addEventListener('message'", StringComparison.Ordinal) < initialize);
    }

    [Fact]
    public void EveryWidgetIncludesHostScriptAndRootElement()
    {
        string[] widgets =
        [
            Widgets.SimpleHtml,
            Widgets.CallToolHtml,
            Widgets.MessageBackHtml,
            Widgets.FullscreenHtml,
            Widgets.MultiToolHtml,
            Widgets.OpenLinkHtml,
            Widgets.UpdateContextHtml,
            Widgets.HostContextHtml,
        ];

        foreach (string html in widgets)
        {
            Assert.Contains("ui/notifications/size-changed", html);
            Assert.Contains("id=\"root\"", html);
        }
    }
}
