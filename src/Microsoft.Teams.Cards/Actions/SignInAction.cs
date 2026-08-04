// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// This class is deprecated. Please use <see cref="SigninSubmitActionData"/> instead. This will be removed in a future version of the SDK.
/// </summary>
[Obsolete("This class is deprecated. Use SigninSubmitActionData instead. This will be removed in a future version of the SDK.")]
public class SignInAction : SubmitAction
{
    public SignInAction(string value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            Msteams = new SigninSubmitActionData(value)
        });
    }
}