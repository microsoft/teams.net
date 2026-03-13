// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a tab fetch invoke activity.
/// </summary>
public class TabFetchActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabFetchActivity"/> class.
    /// </summary>
    public TabFetchActivity() : base("tab/fetch")
    {
    }
}

/// <summary>
/// Represents a tab submit invoke activity.
/// </summary>
public class TabSubmitActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabSubmitActivity"/> class.
    /// </summary>
    public TabSubmitActivity() : base("tab/submit")
    {
    }
}
