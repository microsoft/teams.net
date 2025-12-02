using Microsoft.Teams.Schema;

namespace Microsoft.Teams.Handlers;

//public Func<InstallationUpdateWrapper, CancellationToken, Task>? OnInstallationUpdate { get; set; }
public delegate Task InstallationUpdateHandler(InstallationUpdateArgs installationUpdateActivity, CancellationToken cancellationToken = default);

public class InstallationUpdateArgs(TeamsActivity act)
{
    public TeamsActivity Activity { get; set; } = act;

    public string? Action { get; set; } = act.Properties.TryGetValue("action", out object? value) && value is string s ? s : null;
    public string? SelectedChannelId { get; set; } = act.ChannelData?.Settings?.SelectedChannel?.Id;
    public bool IsAdd => Action == "add";
    public bool IsRemove => Action == "remove";
}
