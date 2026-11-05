// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>Access to typed plan metadata on incoming activities.</summary>
[Experimental("ExperimentalTeamsPlan")]
public static class PlanInfoEntityExtensions
{
    /// <summary>Gets the first plan info entity, if present.</summary>
    public static PlanInfoEntity? GetPlanInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity.Entities?.OfType<PlanInfoEntity>().FirstOrDefault();
    }
}
