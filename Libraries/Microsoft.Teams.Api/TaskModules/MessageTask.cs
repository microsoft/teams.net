// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.TaskModules;

public partial class TaskType : StringEnum
{
    public static readonly TaskType Message = new("message");
    public bool IsMessage => Message.Equals(Value);
}

/// <summary>
/// Task Module response with message action.
/// </summary>
public class MessageTask(string? value) : Task(TaskType.Message)
{
    /// <summary>
    /// Teams will display the value of value in a popup
    /// message box.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public string? Value { get; set; } = value;
}