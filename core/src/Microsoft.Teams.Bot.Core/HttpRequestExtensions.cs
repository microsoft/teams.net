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
    /// </summary>
    public static string? GetCorrelationVector(this HttpRequest request)
        => request != null ? request.Headers["MS-CV"].FirstOrDefault() : string.Empty;
}
