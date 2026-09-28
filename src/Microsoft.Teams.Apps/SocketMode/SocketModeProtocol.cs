// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Defines constants used by the Socket Mode protocol.
/// </summary>
internal static class SocketModeProtocol
{
    /// <summary>
    /// The protocol version emitted by this SDK.
    /// </summary>
    internal const int CurrentVersion = 1;

    /// <summary>
    /// The default base URL for Socket Mode negotiation.
    /// </summary>
    /// <remarks>
    /// Socket Mode negotiate is only available on the canary ring for now; production (<c>https://botapi.skype.com</c>)
    /// returns 503. Switch back once Socket Mode is enabled in production.
    /// </remarks>
    internal const string DefaultNegotiateBaseUrl = "https://canary.botapi.skype.com";

    /// <summary>
    /// The default geographies for Socket Mode connections.
    /// </summary>
    internal static IReadOnlyList<string> DefaultGeos { get; } =
        Array.AsReadOnly(["amer", "emea", "apac"]);

    /// <summary>
    /// The negotiate path appended after the geography segment.
    /// </summary>
    internal const string NegotiatePath = "/v3/websockets/connect";
}
