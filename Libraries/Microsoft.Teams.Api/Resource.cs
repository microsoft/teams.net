// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// A response containing a resource ID
/// </summary>
public class Resource
{
    /// <summary>
    /// Id of the resource
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }
}