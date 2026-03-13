// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an execute action invoke activity.
/// </summary>
public class ExecuteActionActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteActionActivity"/> class.
    /// </summary>
    public ExecuteActionActivity() : base("actionableMessage/executeAction")
    {
    }
}
