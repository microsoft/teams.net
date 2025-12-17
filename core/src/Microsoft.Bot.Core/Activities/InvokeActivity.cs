// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Invoke name constants.
/// </summary>
public static class InvokeNames
{
    /// <summary>
    /// Execute action invoke name.
    /// </summary>
    public const string ExecuteAction = "actionableMessage/executeAction";

    /// <summary>
    /// File consent invoke name.
    /// </summary>
    public const string FileConsent = "fileConsent/invoke";

    /// <summary>
    /// Handoff invoke name.
    /// </summary>
    public const string Handoff = "handoff/action";

    /// <summary>
    /// Search invoke name.
    /// </summary>
    public const string Search = "application/search";

    /// <summary>
    /// Adaptive card invoke name.
    /// </summary>
    public const string AdaptiveCard = "adaptiveCard/action";

    /// <summary>
    /// Config fetch invoke name.
    /// </summary>
    public const string ConfigFetch = "config/fetch";

    /// <summary>
    /// Config submit invoke name.
    /// </summary>
    public const string ConfigSubmit = "config/submit";

    /// <summary>
    /// Message extension query invoke name.
    /// </summary>
    public const string ComposeExtensionQuery = "composeExtension/query";

    /// <summary>
    /// Message extension query link invoke name.
    /// </summary>
    public const string ComposeExtensionQueryLink = "composeExtension/queryLink";

    /// <summary>
    /// Message extension anonymous query link invoke name.
    /// </summary>
    public const string ComposeExtensionAnonymousQueryLink = "composeExtension/anonymousQueryLink";

    /// <summary>
    /// Message extension fetch task invoke name.
    /// </summary>
    public const string ComposeExtensionFetchTask = "composeExtension/fetchTask";

    /// <summary>
    /// Message extension query setting URL invoke name.
    /// </summary>
    public const string ComposeExtensionQuerySettingUrl = "composeExtension/querySettingUrl";

    /// <summary>
    /// Message extension setting invoke name.
    /// </summary>
    public const string ComposeExtensionSetting = "composeExtension/setting";

    /// <summary>
    /// Message extension select item invoke name.
    /// </summary>
    public const string ComposeExtensionSelectItem = "composeExtension/selectItem";

    /// <summary>
    /// Message extension submit action invoke name.
    /// </summary>
    public const string ComposeExtensionSubmitAction = "composeExtension/submitAction";

    /// <summary>
    /// Message extension card button clicked invoke name.
    /// </summary>
    public const string ComposeExtensionCardButtonClicked = "composeExtension/onCardButtonClicked";

    /// <summary>
    /// Sign in token exchange invoke name.
    /// </summary>
    public const string SignInTokenExchange = "signin/tokenExchange";

    /// <summary>
    /// Sign in verify state invoke name.
    /// </summary>
    public const string SignInVerifyState = "signin/verifyState";

    /// <summary>
    /// Tab fetch invoke name.
    /// </summary>
    public const string TabFetch = "tab/fetch";

    /// <summary>
    /// Tab submit invoke name.
    /// </summary>
    public const string TabSubmit = "tab/submit";

    /// <summary>
    /// Task fetch invoke name.
    /// </summary>
    public const string TaskFetch = "task/fetch";

    /// <summary>
    /// Task submit invoke name.
    /// </summary>
    public const string TaskSubmit = "task/submit";
}

/// <summary>
/// Represents an invoke activity.
/// </summary>
public class InvokeActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the invoke operation.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value associated with the activity.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity"/> class.
    /// </summary>
    public InvokeActivity() : base(ActivityTypes.Invoke)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity"/> class with the specified invoke name.
    /// </summary>
    /// <param name="name">The invoke operation name.</param>
    public InvokeActivity(string? name) : base(ActivityTypes.Invoke)
    {
        Name = name;
    }
}
