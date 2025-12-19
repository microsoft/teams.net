// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a message extension anonymous query link invoke activity.
/// </summary>
public class MessageExtensionAnonQueryLinkActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionAnonQueryLinkActivity"/> class.
    /// </summary>
    public MessageExtensionAnonQueryLinkActivity() : base("composeExtension/anonymousQueryLink")
    {
    }
}

/// <summary>
/// Represents a message extension card button clicked invoke activity.
/// </summary>
public class MessageExtensionCardButtonClickedActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionCardButtonClickedActivity"/> class.
    /// </summary>
    public MessageExtensionCardButtonClickedActivity() : base("composeExtension/onCardButtonClicked")
    {
    }
}

/// <summary>
/// Represents a message extension fetch task invoke activity.
/// </summary>
public class MessageExtensionFetchTaskActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionFetchTaskActivity"/> class.
    /// </summary>
    public MessageExtensionFetchTaskActivity() : base("composeExtension/fetchTask")
    {
    }
}

/// <summary>
/// Represents a message extension query invoke activity.
/// </summary>
public class MessageExtensionQueryActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionQueryActivity"/> class.
    /// </summary>
    public MessageExtensionQueryActivity() : base("composeExtension/query")
    {
    }
}

/// <summary>
/// Represents a message extension query link invoke activity.
/// </summary>
public class MessageExtensionQueryLinkActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionQueryLinkActivity"/> class.
    /// </summary>
    public MessageExtensionQueryLinkActivity() : base("composeExtension/queryLink")
    {
    }
}

/// <summary>
/// Represents a message extension query setting URL invoke activity.
/// </summary>
public class MessageExtensionQuerySettingUrlActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionQuerySettingUrlActivity"/> class.
    /// </summary>
    public MessageExtensionQuerySettingUrlActivity() : base("composeExtension/querySettingUrl")
    {
    }
}

/// <summary>
/// Represents a message extension select item invoke activity.
/// </summary>
public class MessageExtensionSelectItemActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionSelectItemActivity"/> class.
    /// </summary>
    public MessageExtensionSelectItemActivity() : base("composeExtension/selectItem")
    {
    }
}

/// <summary>
/// Represents a message extension setting invoke activity.
/// </summary>
public class MessageExtensionSettingActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionSettingActivity"/> class.
    /// </summary>
    public MessageExtensionSettingActivity() : base("composeExtension/setting")
    {
    }
}

/// <summary>
/// Represents a message extension submit action invoke activity.
/// </summary>
public class MessageExtensionSubmitActionActivity : InvokeActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageExtensionSubmitActionActivity"/> class.
    /// </summary>
    public MessageExtensionSubmitActionActivity() : base("composeExtension/submitAction")
    {
    }
}
