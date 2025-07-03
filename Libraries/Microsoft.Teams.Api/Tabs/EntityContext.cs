// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Tabs;

/// <summary>
/// Current TabRequest entity context, or 'tabEntityId'.
/// </summary>
public class EntityContext
{
    /// <summary>
    /// The entity id of the tab.
    /// </summary>
    [JsonPropertyName("tabEntityId")]
    [JsonPropertyOrder(0)]
    public string? TabEntityId { get; set; }
}