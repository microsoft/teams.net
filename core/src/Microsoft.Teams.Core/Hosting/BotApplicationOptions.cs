// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Hosting;

/// <summary>
/// Options for configuring a bot application instance.
/// </summary>
public class BotApplicationOptions
{
    /// <summary>
    /// Gets or sets the application (client) ID, used for logging and diagnostics.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum time allowed for processing an incoming activity.
    /// This timeout replaces the HTTP request's cancellation token so that handlers
    /// (especially streaming handlers) are not canceled when the incoming HTTP connection closes.
    /// Defaults to 5 minutes. Set to <see cref="Timeout.InfiniteTimeSpan"/> to disable the timeout.
    /// </summary>
    public TimeSpan ProcessActivityTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
