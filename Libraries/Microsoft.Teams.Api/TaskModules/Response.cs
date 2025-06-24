// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TaskModules;

/// <summary>
/// Envelope for Task Module Response.
/// </summary>
public class Response
{
    /// <summary>
    /// The JSON for the response to appear in the task module.
    /// </summary>
    [JsonPropertyName("task")]
    [JsonPropertyOrder(0)]
    public Task? Task { get; set; }

    /// <summary>
    /// The cache info for this response
    /// </summary>
    [JsonPropertyName("cacheInfo")]
    [JsonPropertyOrder(1)]
    public CacheInfo? CacheInfo { get; set; }

    public Response()
    {

    }

    public Response(Task? task)
    {
        Task = task;
    }
}