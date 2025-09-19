// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Defines an invoke action. This action is used to trigger a bot action.
/// </summary>
public class InvokeAction : SubmitAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeAction"/> class.
    /// </summary>
    /// <param name="value">The value to submit when the action is invoked.</param>
    public InvokeAction(object value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            Msteams = new InvokeSubmitActionData(value)
        });
    }
}