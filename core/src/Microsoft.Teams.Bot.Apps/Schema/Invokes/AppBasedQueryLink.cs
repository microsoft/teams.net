// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// App-based query link payload for link unfurling.
/// </summary>
public class AppBasedQueryLink
{
    /// <summary>
    /// URL queried by user.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; set; }

    //TODO : review
    /*
    /// <summary>
    /// State parameter for OAuth flow.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
    */
}
