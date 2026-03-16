// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Teams.Bot.Core;

/// <summary>
/// Extension methods for <see cref="HttpRequest"/>.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Gets the Microsoft Correlation Vector (MS-CV) from the request headers, if present.
    /// The value is sanitized to prevent log forging attacks by removing newline characters.
    /// </summary>
    public static string? GetCorrelationVector(this HttpRequest request)
    {
        if (request == null)
        {
            return string.Empty;
        }

        string? correlationVector = request.Headers["MS-CV"].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationVector))
        {
            return correlationVector;
        }

        // Sanitize to prevent log forging: remove newline characters
        return correlationVector
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "", StringComparison.Ordinal);
    }
}
