// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

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