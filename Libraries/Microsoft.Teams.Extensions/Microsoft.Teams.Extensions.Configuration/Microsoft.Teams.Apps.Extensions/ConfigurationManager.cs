// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Apps.Extensions;

public static class ConfigurationManagerExtensions
{
    public static TeamsSettings GetTeams(this IConfigurationManager manager)
    {
        return manager.GetSection("Teams").Get<TeamsSettings>() ?? new();
    }

    public static LoggingSettings GetTeamsLogging(this IConfigurationManager manager)
    {
        return manager.GetSection("Logging").GetSection("Microsoft.Teams").Get<LoggingSettings>() ?? new();
    }
}