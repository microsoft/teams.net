// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an invoke activity.
/// </summary>
public class InvokeActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the operation. See <see cref="InvokeNames"/> for common values.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity"/> class.
    /// </summary>
    public InvokeActivity() : base(ActivityTypes.Invoke)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity"/> class with the specified name.
    /// </summary>
    /// <param name="name">The invoke operation name.</param>
    public InvokeActivity(string name) : base(ActivityTypes.Invoke)
    {
        Name = name;
    }
}

/// <summary>
/// String constants for invoke activity names.
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
    public const string Search = "search";

    /// <summary>
    /// Adaptive card action invoke name.
    /// </summary>
    public const string AdaptiveCardAction = "adaptiveCard/action";

    /// <summary>
    /// Config fetch invoke name.
    /// </summary>
    public const string ConfigFetch = "config/fetch";

    /// <summary>
    /// Config submit invoke name.
    /// </summary>
    public const string ConfigSubmit = "config/submit";

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

    /// <summary>
    /// Sign-in token exchange invoke name.
    /// </summary>
    public const string SignInTokenExchange = "signin/tokenExchange";

    /// <summary>
    /// Sign-in verify state invoke name.
    /// </summary>
    public const string SignInVerifyState = "signin/verifyState";

    /// <summary>
    /// Message submit action invoke name.
    /// </summary>
    public const string MessageSubmitAction = "message/submitAction";

    /// <summary>
    /// Message extension anonymous query link invoke name.
    /// </summary>
    public const string MessageExtensionAnonQueryLink = "composeExtension/anonymousQueryLink";

    /// <summary>
    /// Message extension card button clicked invoke name.
    /// </summary>
    public const string MessageExtensionCardButtonClicked = "composeExtension/onCardButtonClicked";

    /// <summary>
    /// Message extension fetch task invoke name.
    /// </summary>
    public const string MessageExtensionFetchTask = "composeExtension/fetchTask";

    /// <summary>
    /// Message extension query invoke name.
    /// </summary>
    public const string MessageExtensionQuery = "composeExtension/query";

    /// <summary>
    /// Message extension query link invoke name.
    /// </summary>
    public const string MessageExtensionQueryLink = "composeExtension/queryLink";

    /// <summary>
    /// Message extension query setting URL invoke name.
    /// </summary>
    public const string MessageExtensionQuerySettingUrl = "composeExtension/querySettingUrl";

    /// <summary>
    /// Message extension select item invoke name.
    /// </summary>
    public const string MessageExtensionSelectItem = "composeExtension/selectItem";

    /// <summary>
    /// Message extension setting invoke name.
    /// </summary>
    public const string MessageExtensionSetting = "composeExtension/setting";

    /// <summary>
    /// Message extension submit action invoke name.
    /// </summary>
    public const string MessageExtensionSubmitAction = "composeExtension/submitAction";
}
