// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.SocketMode;

internal static class SocketModeProtocol
{
    internal const int CurrentVersion = 1;
    internal const string DefaultNegotiateBaseUrl = "https://botapi.skype.com";
    internal const string NegotiatePath = "/v3/websockets/connect";
}
