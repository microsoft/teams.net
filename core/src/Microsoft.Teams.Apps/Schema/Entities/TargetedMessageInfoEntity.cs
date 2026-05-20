// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Represents a targeted message info entity in a Teams activity, used to identify
/// the original targeted message being responded to in a Prompt Preview reactive send.
/// </summary>
[Experimental("ExperimentalTeamsTargeted")]
public class TargetedMessageInfoEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="TargetedMessageInfoEntity"/>.
    /// </summary>
    public TargetedMessageInfoEntity() : base("targetedMessageInfo") { }

    /// <summary>
    /// Gets or sets the ID of the targeted message being referenced.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when read after deserialization from JSON that did not include a "messageId" field.
    /// The <c>required</c> modifier enforces initialization in C# code but does not gate
    /// deserialization through the underlying extension-properties dictionary.
    /// </exception>
    [JsonPropertyName("messageId")]
    public required string MessageId
    {
        get => base.Properties.Get<string>("messageId")
            ?? throw new InvalidOperationException(
                "messageId is required on TargetedMessageInfoEntity but was not present after deserialization.");
        set => base.Properties["messageId"] = value;
    }
}
