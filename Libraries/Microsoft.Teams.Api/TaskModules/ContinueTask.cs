// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.TaskModules;

public partial class TaskType : StringEnum
{
    public static readonly TaskType Continue = new("continue");
    public bool IsContinue => Continue.Equals(Value);
}

/// <summary>
/// Task Module Response with continue action.
/// </summary>
public class ContinueTask(TaskInfo? value) : Task(TaskType.Continue)
{
    /// <summary>
    /// The JSON for the Adaptive card to
    /// appear in the task module.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public TaskInfo? Value { get; set; } = value;
}