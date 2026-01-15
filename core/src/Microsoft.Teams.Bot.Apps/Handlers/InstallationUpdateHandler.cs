// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling installation update activities.
/// </summary>
/// <param name="installationUpdateActivity"></param>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task InstallationUpdateHandler(InstallationUpdateArgs installationUpdateActivity, Context context, CancellationToken cancellationToken = default);


/// <summary>
/// Installation update activity arguments.
/// </summary>
/// <param name="act"></param>
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
