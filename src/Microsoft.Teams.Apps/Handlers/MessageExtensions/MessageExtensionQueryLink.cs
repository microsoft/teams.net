// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.MessageExtensions;

/// <summary>
/// App-based query link payload for link unfurling.
/// </summary>
public class MessageExtensionQueryLink
{
    /// <summary>
    /// URL queried by user.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; set; }
}
