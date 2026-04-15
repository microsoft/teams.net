// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Stub for backward compatibility. In the new SDK, bot authentication is handled
/// automatically by the DI pipeline via <c>BotAuthenticationHandler</c>.
/// </summary>
[Obsolete("Bot token acquisition is handled by the DI pipeline in the new SDK. This class is provided for structural compatibility only.")]
public sealed class BotTokenClient
{
    public static readonly string BotScope = "https://api.botframework.com/.default";
    public static readonly string GraphScope = "https://graph.microsoft.com/.default";

    // Instance field to prevent CA1052 (static holder type)
    internal bool IsInitialized { get; } = true;

    internal BotTokenClient() { }
}
