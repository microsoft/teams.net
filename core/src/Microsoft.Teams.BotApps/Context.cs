// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Context for a bot turn.
/// </summary>
/// <param name="botApplication"></param>
/// <param name="activity"></param>
public class Context(TeamsBotApplication botApplication, TeamsActivity activity)
{
    /// <summary>
    /// Base bot application.
    /// </summary>
    public TeamsBotApplication BotApplication { get; } = botApplication;

    /// <summary>
    /// Current activity.
    /// </summary>
    public TeamsActivity Activity { get; } = activity;
}
