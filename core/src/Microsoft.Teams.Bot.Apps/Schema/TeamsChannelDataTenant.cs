// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Tenant information for Teams channel data.
/// </summary>
public class TeamsChannelDataTenant
{
    /// <summary>
    /// Unique identifier of the tenant.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }
}
