// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// This class is deprecated. Please use <see cref="ImBackSubmitActionData"/> instead. This will be removed in a future version of the SDK.
/// </summary>
[Obsolete("This class is deprecated. Use ImBackSubmitActionData instead. This will be removed in a future version of the SDK.")]
public class IMBackAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    /// 
    public IMBackAction(string value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            Msteams = new ImBackSubmitActionData(value)
        });
    }
}