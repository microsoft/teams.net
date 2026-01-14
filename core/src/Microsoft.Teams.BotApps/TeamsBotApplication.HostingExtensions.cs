// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Extension methods for <see cref="TeamsBotApplication"/>.
/// </summary>
public static class TeamsBotApplicationHostingExtensions
{
    /// <summary>
    /// Adds TeamsBotApplication to the service collection.
    /// </summary>
    /// <param name="services">The WebApplicationBuilder instance.</param>
    /// <returns>The updated WebApplicationBuilder instance.</returns>
    public static IServiceCollection AddTeamsBotApplication(this IServiceCollection services)
    {
        services.AddHttpClient<TeamsAPXClient>();
        services.AddSingleton<TeamsAPXClient>();
        services.AddBotApplication<TeamsBotApplication>();
        return services;
    }
}
