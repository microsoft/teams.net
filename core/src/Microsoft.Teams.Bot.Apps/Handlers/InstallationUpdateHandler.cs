// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling installation update activities when the bot is installed or uninstalled in a Teams scope.
/// </summary>
/// <param name="installationUpdateActivity">The installation update arguments containing action details (add/remove) and selected channel information.</param>
/// <param name="context">The turn context for sending responses and accessing conversation information.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the asynchronous handler operation.</returns>
public delegate Task InstallationUpdateHandler(InstallationUpdateArgs installationUpdateActivity, Context context, CancellationToken cancellationToken = default);

/// <summary>
/// Provides arguments for installation update activities including installation action and selected channel.
/// </summary>
/// <param name="act">The Teams activity containing the installation update information.</param>
public class InstallationUpdateArgs(TeamsActivity act)
{
    /// <summary>
    /// Activity for the installation update.
    /// </summary>
    public TeamsActivity Activity { get; set; } = act;

    /// <summary>
    /// Installation action: "add" or "remove".
    /// </summary>
    public string? Action { get; set; } = act.Properties.TryGetValue("action", out object? value) && value is string s ? s : null;

    /// <summary>
    /// Gets or sets the identifier of the currently selected channel.
    /// </summary>
    public string? SelectedChannelId { get; set; } = act.ChannelData?.Settings?.SelectedChannel?.Id;

    /// <summary>
    /// Gets a value indicating whether the current action is an add operation.
    /// </summary>
    public bool IsAdd => Action == "add";

    /// <summary>
    /// Gets a value indicating whether the current action is a remove operation.
    /// </summary>
    public bool IsRemove => Action == "remove";
}
