// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Teams channel data settings.
/// </summary>
public class TeamsChannelDataSettings
{
    /// <summary>
    /// Selected channel information.
    /// </summary>
    [JsonPropertyName("selectedChannel")] public required TeamsChannel SelectedChannel { get; set; }

    /// <summary>
    /// Gets or sets the collection of additional properties not explicitly defined by the type.
    /// </summary>
    /// <remarks>This property stores extra JSON fields encountered during deserialization that do not map to
    /// known properties. It enables round-tripping of unknown or custom data without loss. The dictionary keys
    /// correspond to the property names in the JSON payload.</remarks>
#pragma warning disable CA2227 // Collection properties should be read only
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
