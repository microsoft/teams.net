// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for bot token operations.
/// </summary>
/// <remarks>
/// In the core SDK, bot authentication is handled transparently by <c>BotAuthenticationHandler</c>,
/// which automatically acquires and attaches tokens to HTTP requests. This client exposes the
/// well-known token scopes for scenarios that need explicit scope references.
/// </remarks>
public static class BotTokenClient
{
    /// <summary>
    /// The default Bot Framework API scope.
    /// </summary>
    public static readonly string BotScope = "https://api.botframework.com/.default";

    /// <summary>
    /// The Microsoft Graph API scope.
    /// </summary>
    public static readonly string GraphScope = "https://graph.microsoft.com/.default";
}
