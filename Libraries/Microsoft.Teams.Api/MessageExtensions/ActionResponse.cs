// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// Response of messaging extension action
/// </summary>
public class ActionResponse : TaskModules.Response
{
    /// <summary>
    /// the message extension result
    /// </summary>
    [JsonPropertyName("composeExtension")]
    [JsonPropertyOrder(2)]
    public Result? ComposeExtension { get; set; }
}