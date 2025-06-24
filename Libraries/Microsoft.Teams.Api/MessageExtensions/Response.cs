// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// Messaging extension response
/// </summary>
public class Response
{
    /// <summary>
    /// the message extension result
    /// </summary>
    [JsonPropertyName("composeExtension")]
    [JsonPropertyOrder(0)]
    public Result? ComposeExtension { get; set; }

    /// <summary>
    /// The cache info for this response
    /// </summary>
    [JsonPropertyName("cacheInfo")]
    [JsonPropertyOrder(1)]
    public CacheInfo? CacheInfo { get; set; }
}