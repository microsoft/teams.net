// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.O365;

/// <summary>
/// An interface representing O365ConnectorCardActionQuery.
/// O365 connector card HttpPOST invoke query
/// </summary>
public class ConnectorCardActionQuery
{
    /// <summary>
    /// The results of body string defined in
    /// IO365ConnectorCardHttpPOST with substituted input values
    /// </summary>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(0)]
    public string? Body { get; set; }

    /// <summary>
    /// Action Id associated with the HttpPOST action
    /// button triggered, defined in O365ConnectorCardActionBase.
    /// </summary>
    [JsonPropertyName("actionId")]
    [JsonPropertyOrder(1)]
    public string? ActionId { get; set; }
}