// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Result from getting paged members of a conversation, with Teams-specific account information.
/// </summary>
public class PagedTeamsMembersResult
{
    /// <summary>
    /// Gets or sets the continuation token that can be used to get the next page of results.
    /// Null when there are no more pages.
    /// </summary>
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Gets or sets the list of members in this page.
    /// </summary>
    public IList<TeamsChannelAccount?> Members { get; set; } = [];
}
