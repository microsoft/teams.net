// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public static readonly Name Handoff = new("handoff/action");
    public bool IsHandoff => Handoff.Equals(Value);
}

public class HandoffActivity() : InvokeActivity(Name.Handoff)
{
    /// <summary>
    /// A value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(32)]
    public new required HandoffActivityValue Value
    {
        get => (HandoffActivityValue)base.Value!;
        set => base.Value = value;
    }
}

public class HandoffActivityValue
{
    /// <summary>
    /// Continuation token used to get the conversation reference.
    /// </summary>
    [JsonPropertyName("continuation")]
    [JsonPropertyOrder(0)]
    public required string Continuation { get; set; }
}