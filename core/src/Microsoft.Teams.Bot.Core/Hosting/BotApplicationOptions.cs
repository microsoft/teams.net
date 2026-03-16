// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// Options for configuring a bot application instance.
/// </summary>
public sealed class BotApplicationOptions
{
    /// <summary>
    /// Gets or sets the application (client) ID, used for logging and diagnostics.
    /// </summary>
    public string AppId { get; set; } = string.Empty;
}
