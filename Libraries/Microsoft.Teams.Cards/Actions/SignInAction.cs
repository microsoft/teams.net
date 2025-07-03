// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Cards;

public class SignInAction : SubmitAction
{
    public SignInAction(string value)
    {
        Data = new()
        {
            MsTeams = new SigninSubmitActionData(value)
        };
    }
}