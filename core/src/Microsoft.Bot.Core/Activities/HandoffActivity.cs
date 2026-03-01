// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a handoff invoke activity.
/// </summary>
public class HandoffActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HandoffActivity"/> class.
    /// </summary>
    public HandoffActivity() : base("handoff/action")
    {
    }
}
