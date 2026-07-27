// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.MessageExtension;

/// <summary>
/// Messaging extension query payload.
/// </summary>
public class MessageExtensionQuery
{
    /// <summary>
    /// Id of the command assigned by the bot.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Parameters for the query.
    /// </summary>
    [JsonPropertyName("parameters")]
    public IList<QueryParameter> Parameters { get; set; } = [];

    /// <summary>
    /// Query options for pagination.
    /// </summary>
    [JsonPropertyName("queryOptions")]
    public QueryOptions? QueryOptions { get; set; }

    //TODO : check how to use this ? auth ?
    /*
    /// <summary>
    /// State parameter passed back to the bot after authentication/configuration flow.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
    */
}

/// <summary>
/// Query parameter.
/// </summary>
public class QueryParameter
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Value of the parameter.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}


/// <summary>
/// Query options for pagination.
/// </summary>
public class QueryOptions
{
    /// <summary>
    /// Number of entities to skip.
    /// </summary>
    [JsonPropertyName("skip")]
    public int? Skip { get; set; }

    /// <summary>
    /// Number of entities to fetch.
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}
