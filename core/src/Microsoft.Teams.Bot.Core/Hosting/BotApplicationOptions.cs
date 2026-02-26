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

    /// <summary>
    /// Gets or sets the Teams bot service URL used for proactive messaging.
    /// Defaults to <c>https://smba.trafficmanager.net/teams</c> if not configured via the
    /// <c>SERVICE_URL</c> environment variable or app settings.
    /// </summary>
    public Uri ServiceUrl { get; set; } = new Uri("https://smba.trafficmanager.net/teams");
}
