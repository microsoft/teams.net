// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a search invoke activity.
/// </summary>
public class SearchActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchActivity"/> class.
    /// </summary>
    public SearchActivity() : base("search")
    {
    }
}
