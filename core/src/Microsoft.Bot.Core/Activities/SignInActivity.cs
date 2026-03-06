// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a sign-in token exchange invoke activity.
/// </summary>
public class SignInTokenExchangeActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignInTokenExchangeActivity"/> class.
    /// </summary>
    public SignInTokenExchangeActivity() : base("signin/tokenExchange")
    {
    }
}

/// <summary>
/// Represents a sign-in verify state invoke activity.
/// </summary>
public class SignInVerifyStateActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignInVerifyStateActivity"/> class.
    /// </summary>
    public SignInVerifyStateActivity() : base("signin/verifyState")
    {
    }
}
