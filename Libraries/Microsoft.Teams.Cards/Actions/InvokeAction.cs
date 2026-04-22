// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// This class is deprecated. Please use <see cref="InvokeSubmitActionData"/> instead. This will be removed in a future version of the SDK.
/// </summary>
[Obsolete("This class is deprecated. Use InvokeSubmitActionData instead. This will be removed in a future version of the SDK.")]
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
            Msteams = new InvokeSubmitActionData(new Union<object, CollabStageInvokeDataValue>(value))
        });
    }
}