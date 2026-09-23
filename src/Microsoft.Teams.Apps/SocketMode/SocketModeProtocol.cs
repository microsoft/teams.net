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
    internal const string DefaultNegotiateBaseUrl = "https://botapi.skype.com";

    /// <summary>
    /// The path for the Socket Mode negotiate endpoint.
    /// </summary>
    internal const string NegotiatePath = "/amer/v3/websockets/connect";
}
