// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Cards;

public class InvokeAction : SubmitAction
{
    public InvokeAction(object value)
    {
        Data = new()
        {
            MsTeams = new InvokeSubmitActionData(value)
        };
    }
}