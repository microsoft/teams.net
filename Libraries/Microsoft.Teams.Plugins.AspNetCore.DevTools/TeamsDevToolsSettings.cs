using Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public class TeamsDevToolsSettings
{
    public IList<Page> Pages { get; init; } = [];
}