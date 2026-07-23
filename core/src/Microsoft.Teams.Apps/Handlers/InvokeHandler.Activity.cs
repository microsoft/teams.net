// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Represents an invoke activity.
/// </summary>
public class InvokeActivity : TeamsActivity
{
    /// <summary>
    /// Creates an InvokeActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The core activity to convert.</param>
    /// <returns>An <see cref="InvokeActivity"/> instance.</returns>
    public static new InvokeActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new InvokeActivity(activity);
    }

    /// <summary>
    /// Gets or sets the name of the operation. See <see cref="InvokeNames"/> for common values.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets or sets the value payload of the invoke activity.
    /// </summary>
    [JsonPropertyName("value")]
    public JsonNode? Value { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal InvokeActivity() : base(TeamsActivityTypes.Invoke)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity"/> class with the specified name.
    /// </summary>
    /// <param name="name">The invoke operation name.</param>
    internal InvokeActivity(string name) : base(TeamsActivityTypes.Invoke)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the InvokeActivity class with the specified core activity.
    /// </summary>
    /// <param name="activity">The core activity to be invoked. Cannot be null.</param>
    internal InvokeActivity(CoreActivity activity) : base(activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        Name = activity.Properties.Extract<string>("name");
        Value = activity is InvokeActivity invoke
            ? invoke.Value
            : activity.Properties.Extract<JsonNode>("value");
    }
}

/// <summary>
/// Represents an invoke activity with a strongly-typed value.
/// </summary>
/// <remarks>
/// The strongly-typed Value property provides compile-time type safety while maintaining a single storage location
/// through the base class. Both the typed and untyped Value properties access the same underlying JsonNode value.
/// </remarks>
/// <typeparam name="TValue">The type of the value payload.</typeparam>
public class InvokeActivity<TValue> : InvokeActivity
{
    /// <summary>
    /// Gets or sets the strongly-typed value associated with the invoke activity.
    /// This property shadows the base class Value property but uses the same underlying storage,
    /// ensuring no synchronization issues between typed and untyped access.
    /// </summary>
    public new TValue? Value
    {
        get => base.Value != null ? JsonSerializer.Deserialize<TValue>(base.Value.ToJsonString()) : default;
        set => base.Value = value != null ? JsonSerializer.SerializeToNode(value) : null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeActivity{TValue}"/> class from an InvokeActivity.
    /// </summary>
    /// <param name="activity">The invoke activity.</param>
    internal InvokeActivity(InvokeActivity activity) : base(activity)
    {
    }
}

/// <summary>
/// String constants for invoke activity names.
/// </summary>
public static class InvokeNames
{
    /// <summary>
    /// File consent invoke name.
    /// </summary>
    public const string FileConsent = "fileConsent/invoke";

    /// <summary>
    /// Adaptive card action invoke name.
    /// </summary>
    public const string AdaptiveCardAction = "adaptiveCard/action";

    /// <summary>
    /// Search invoke name. Sent by Adaptive Card dynamic typeahead 'Input.ChoiceSet' inputs.
    /// </summary>
    public const string Search = "application/search";

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
    /// Sign-in failure invoke name. Sent by the Teams client when SSO token exchange
    /// fails client-side (e.g., misconfigured Entra app registration).
    /// </summary>
    public const string SignInFailure = "signin/failure";

    /// <summary>
    /// Message extension anonymous query link invoke name.
    /// </summary>
    public const string MessageExtensionAnonQueryLink = "composeExtension/anonymousQueryLink";

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
    /// Message extension submit action invoke name.
    /// </summary>
    public const string MessageExtensionSubmitAction = "composeExtension/submitAction";

    /// <summary>
    /// Message fetch task invoke name. Sent when the user clicks a feedback button on an AI-generated message.
    /// </summary>
    public const string MessageFetchTask = "message/fetchTask";

    /// <summary>
    /// Message submit action invoke name.
    /// </summary>
    public const string MessageSubmitAction = "message/submitAction";

    /// <summary>
    /// Suggested action submit invoke name.
    /// Sent when the user clicks a suggested action of type <c>Action.Submit</c>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.Experimental("ExperimentalTeamsSuggestedAction")]
    public const string SuggestedActionSubmit = "suggestedActions/submit";
}
