// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public static readonly Name ExecuteAction = new("actionableMessage/executeAction");
    public bool IsExecuteAction => ExecuteAction.Equals(Value);
}

/// <summary>
/// The name of the operation associated with an invoke or event activity.
/// </summary>
public class ExecuteActionActivity(O365.ConnectorCardActionQuery value) : InvokeActivity(Name.ExecuteAction)
{
    /// <summary>
    /// A value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(32)]
    public new O365.ConnectorCardActionQuery Value { get; set; } = value;
}