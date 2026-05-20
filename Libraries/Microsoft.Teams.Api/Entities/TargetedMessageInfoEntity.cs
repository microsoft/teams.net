// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

[Experimental("ExperimentalTeamsTargeted")]
public class TargetedMessageInfoEntity : Entity
{
    [JsonPropertyName("messageId")]
    [JsonPropertyOrder(3)]
    public required string MessageId { get; set; }

    public TargetedMessageInfoEntity() : base("targetedMessageInfo") { }
}
