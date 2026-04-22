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
    public string Scope { get; set; } = CloudEnvironment.Public.BotScope;

    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public string SectionName { get; set; } = "AzureAd";

    /// <summary>
    /// Gets or sets the resolved cloud environment. Defaults to <see cref="CloudEnvironment.Public"/>.
    /// </summary>
    public CloudEnvironment Cloud { get; set; } = CloudEnvironment.Public;
}
