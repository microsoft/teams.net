// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a config fetch invoke activity.
/// </summary>
public class ConfigFetchActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigFetchActivity"/> class.
    /// </summary>
    public ConfigFetchActivity() : base("config/fetch")
    {
    }
}

/// <summary>
/// Represents a config submit invoke activity.
/// </summary>
public class ConfigSubmitActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigSubmitActivity"/> class.
    /// </summary>
    public ConfigSubmitActivity() : base("config/submit")
    {
    }
}
