// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace MigrationBot;

/// <summary>
/// Sample-local registration helpers for the Teams SDK side of the migration.
/// </summary>
internal static class MigrationExtensions
{
    /// <summary>
    /// Registers <see cref="NewSdkBot"/> as a separate singleton alongside the
    /// CompatAdapter's own <see cref="TeamsBotApplication"/> instance.
    ///
    /// The three underlying bot clients (<see cref="ConversationClient"/>,
    /// <see cref="UserTokenClient"/>, <see cref="TeamsApiClient"/>) are already
    /// registered by <c>AddCompatAdapter()</c> and are shared here — both instances
    /// use the same bot identity and talk to the same Teams endpoints.  Only the
    /// <see cref="TeamsBotApplication"/> itself is separate, giving <see cref="NewSdkBot"/>
    /// its own isolated router and OnActivity delegate.
    /// </summary>
    public static IHostApplicationBuilder AddNewSdkBot(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<NewSdkBot>(sp => new NewSdkBot(
            sp.GetRequiredService<ConversationClient>(),
            sp.GetRequiredService<UserTokenClient>(),
            sp.GetRequiredService<TeamsApiClient>(),
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<ILogger<TeamsBotApplication>>()
        ));

        return builder;
    }
}
