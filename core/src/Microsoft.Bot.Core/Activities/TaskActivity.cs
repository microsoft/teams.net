// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a task fetch invoke activity.
/// </summary>
public class TaskFetchActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskFetchActivity"/> class.
    /// </summary>
    public TaskFetchActivity() : base("task/fetch")
    {
    }
}

/// <summary>
/// Represents a task submit invoke activity.
/// </summary>
public class TaskSubmitActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskSubmitActivity"/> class.
    /// </summary>
    public TaskSubmitActivity() : base("task/submit")
    {
    }
}
