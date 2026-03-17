// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Teams.Bot.Core.UnitTests;

public class HttpRequestExtensionsTests
{
    [Fact]
    public void GetCorrelationVector_WithValidValue_ReturnsValue()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = "valid-correlation-vector";

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("valid-correlation-vector", result);
    }

    [Fact]
    public void GetCorrelationVector_WithNewlineCharacters_SanitizesValue()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = $"correlation{Environment.NewLine}vector{Environment.NewLine}with{Environment.NewLine}newlines";

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("correlationvectorwithnewlines", result);
        Assert.DoesNotContain(Environment.NewLine, result);
    }

    [Fact]
    public void GetCorrelationVector_WithCarriageReturnCharacters_SanitizesValue()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = $"correlation{Environment.NewLine}vector{Environment.NewLine}with{Environment.NewLine}carriage{Environment.NewLine}returns";

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("correlationvectorwithcarriagereturns", result);
        Assert.DoesNotContain(Environment.NewLine, result);
    }

    [Fact]
    public void GetCorrelationVector_WithCRLF_SanitizesValue()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = $"correlation{Environment.NewLine}vector{Environment.NewLine}with{Environment.NewLine}CRLF";

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("correlationvectorwithCRLF", result);
        Assert.DoesNotContain(Environment.NewLine, result);
    }

    [Fact]
    public void GetCorrelationVector_WithLogForgingAttempt_PreventsInjection()
    {
        // Simulates a malicious attempt to inject fake log entries
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = $"legitimate-value{Environment.NewLine}FAKE_LOG_ENTRY: Unauthorized access granted";

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("legitimate-valueFAKE_LOG_ENTRY: Unauthorized access granted", result);
        Assert.DoesNotContain(Environment.NewLine, result);
        // Verify that the newline that would allow log forging is removed
    }

    [Fact]
    public void GetCorrelationVector_WithNullRequest_ReturnsEmptyString()
    {
        HttpRequest? request = null;

        string? result = request!.GetCorrelationVector();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetCorrelationVector_WithMissingHeader_ReturnsNull()
    {
        DefaultHttpContext httpContext = new();

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Null(result);
    }

    [Fact]
    public void GetCorrelationVector_WithEmptyHeader_ReturnsEmptyString()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = string.Empty;

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetCorrelationVector_WithMultipleHeaderValues_ReturnsFirstValue()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = new[] { "first-value", "second-value" };

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("first-value", result);
    }

    [Fact]
    public void GetCorrelationVector_WithNewlineInMultipleValues_SanitizesFirstValue()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["MS-CV"] = new[] { $"first{Environment.NewLine}value", "second-value" };

        string? result = httpContext.Request.GetCorrelationVector();

        Assert.Equal("firstvalue", result);
        Assert.DoesNotContain(Environment.NewLine, result);
    }
}
