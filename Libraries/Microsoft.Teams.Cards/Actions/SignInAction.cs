// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public class SignInAction : SubmitAction
{
    public SignInAction(string value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            MsTeams = new SigninSubmitActionData(value)
        });
    }
}