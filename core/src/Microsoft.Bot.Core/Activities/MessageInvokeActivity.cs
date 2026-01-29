// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a message submit action invoke activity.
/// </summary>
public class MessageSubmitActionActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSubmitActionActivity"/> class.
    /// </summary>
    public MessageSubmitActionActivity() : base("message/submitAction")
    {
    }
}
