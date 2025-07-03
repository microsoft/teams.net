// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// Messaging extension query
/// </summary>
public class Query
{
    /// <summary>
    /// Id of the command assigned by Bot
    /// </summary>
    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(0)]
    public string? CommandId { get; set; }

    /// <summary>
    /// Parameters for the query
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonPropertyOrder(1)]
    public IList<Parameter>? Parameters { get; set; }

    /// <summary>
    /// Query options
    /// </summary>
    [JsonPropertyName("queryOptions")]
    [JsonPropertyOrder(2)]
    public Options? QueryOptions { get; set; }

    /// <summary>
    /// State parameter passed back to the bot after
    /// authentication/configuration flow
    /// </summary>
    [JsonPropertyName("state")]
    [JsonPropertyOrder(3)]
    public string? State { get; set; }

    /// <summary>
    /// Messaging extension query options
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Number of entities to skip
        /// </summary>
        [JsonPropertyName("skip")]
        [JsonPropertyOrder(0)]
        public int? Skip { get; set; }

        /// <summary>
        /// Number of entities to fetch
        /// </summary>
        [JsonPropertyName("count")]
        [JsonPropertyOrder(1)]
        public int? Count { get; set; }
    }
}