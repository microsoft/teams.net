// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// Options for configuring bot client HTTP clients.
/// </summary>
internal sealed class BotClientOptions
{
    /// <summary>
    /// Gets or sets the scope for bot authentication.
    /// </summary>
    public string Scope { get; set; } = "https://api.botframework.com/.default";

    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public string SectionName { get; set; } = "AzureAd";
}
