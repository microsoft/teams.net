// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an adaptive card invoke activity.
/// </summary>
public class AdaptiveCardActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveCardActivity"/> class.
    /// </summary>
    public AdaptiveCardActivity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveCardActivity"/> class with the specified name.
    /// </summary>
    /// <param name="name">The invoke operation name.</param>
    public AdaptiveCardActivity(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Represents an adaptive card action invoke activity.
/// </summary>
public class AdaptiveCardActionActivity : AdaptiveCardActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveCardActionActivity"/> class.
    /// </summary>
    public AdaptiveCardActionActivity() : base("adaptiveCard/action")
    {
    }
}
