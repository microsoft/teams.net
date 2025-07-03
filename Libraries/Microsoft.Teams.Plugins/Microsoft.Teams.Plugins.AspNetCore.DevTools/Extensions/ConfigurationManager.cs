// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public static class ConfigurationManagerExtensions
{
    public static TeamsDevToolsSettings GetTeamsDevTools(this IConfigurationManager manager)
    {
        return manager.GetSection("Teams").GetSection("Plugins.DevTools").Get<TeamsDevToolsSettings>() ?? new();
    }
}