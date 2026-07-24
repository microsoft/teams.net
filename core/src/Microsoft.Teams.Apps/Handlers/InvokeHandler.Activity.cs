// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Utils;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

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
    public InvokeName? Name { get; internal set; }

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
        Name = new InvokeName(name);
    }

    /// <summary>
    /// Initializes a new instance of the InvokeActivity class with the specified core activity.
    /// </summary>
    /// <param name="activity">The core activity to be invoked. Cannot be null.</param>
    internal InvokeActivity(CoreActivity activity) : base(activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        Name = Properties.Extract<InvokeName>("name");
        Value = activity is InvokeActivity invoke
            ? invoke.Value
            : Properties.Extract<JsonNode>("value");
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
[JsonConverter(typeof(StringEnumJsonConverter<InvokeName>))]
public class InvokeName(string value) : StringEnum(value)
{
    /// <summary>File consent invoke name.</summary>
    public static readonly InvokeName FileConsent = new("fileConsent/invoke");
    /// <summary>Adaptive card action invoke name.</summary>
    public static readonly InvokeName AdaptiveCardAction = new("adaptiveCard/action");
    /// <summary>Search invoke name.</summary>
    public static readonly InvokeName Search = new("application/search");
    /// <summary>Task fetch invoke name.</summary>
    public static readonly InvokeName TaskFetch = new("task/fetch");
    /// <summary>Task submit invoke name.</summary>
    public static readonly InvokeName TaskSubmit = new("task/submit");
    /// <summary>Sign-in token exchange invoke name.</summary>
    public static readonly InvokeName SignInTokenExchange = new("signin/tokenExchange");
    /// <summary>Sign-in verify state invoke name.</summary>
    public static readonly InvokeName SignInVerifyState = new("signin/verifyState");
    /// <summary>Sign-in failure invoke name.</summary>
    public static readonly InvokeName SignInFailure = new("signin/failure");
    /// <summary>Message extension anonymous query link invoke name.</summary>
    public static readonly InvokeName MessageExtensionAnonQueryLink = new("composeExtension/anonymousQueryLink");
    /// <summary>Message extension fetch task invoke name.</summary>
    public static readonly InvokeName MessageExtensionFetchTask = new("composeExtension/fetchTask");
    /// <summary>Message extension query invoke name.</summary>
    public static readonly InvokeName MessageExtensionQuery = new("composeExtension/query");
    /// <summary>Message extension query link invoke name.</summary>
    public static readonly InvokeName MessageExtensionQueryLink = new("composeExtension/queryLink");
    /// <summary>Message extension query setting URL invoke name.</summary>
    public static readonly InvokeName MessageExtensionQuerySettingUrl = new("composeExtension/querySettingUrl");
    /// <summary>Message extension select item invoke name.</summary>
    public static readonly InvokeName MessageExtensionSelectItem = new("composeExtension/selectItem");
    /// <summary>Message extension submit action invoke name.</summary>
    public static readonly InvokeName MessageExtensionSubmitAction = new("composeExtension/submitAction");
    /// <summary>Message fetch task invoke name.</summary>
    public static readonly InvokeName MessageFetchTask = new("message/fetchTask");
    /// <summary>Message submit action invoke name.</summary>
    public static readonly InvokeName MessageSubmitAction = new("message/submitAction");
    /// <summary>Suggested action submit invoke name.</summary>
    public static readonly InvokeName SuggestedActionSubmit = new("suggestedActions/submit");
}

/// <summary>
/// String constants for invoke activity names.
/// </summary>
public static class InvokeNames
{
    /// <summary>
    /// File consent invoke name.
    /// </summary>
    public static InvokeName FileConsent => InvokeName.FileConsent;

    /// <summary>
    /// Adaptive card action invoke name.
    /// </summary>
    public static InvokeName AdaptiveCardAction => InvokeName.AdaptiveCardAction;

    /// <summary>
    /// Search invoke name. Sent by Adaptive Card dynamic typeahead 'Input.ChoiceSet' inputs.
    /// </summary>
    public static InvokeName Search => InvokeName.Search;

    /// <summary>
    /// Task fetch invoke name.
    /// </summary>
    public static InvokeName TaskFetch => InvokeName.TaskFetch;

    /// <summary>
    /// Task submit invoke name.
    /// </summary>
    public static InvokeName TaskSubmit => InvokeName.TaskSubmit;

    /// <summary>
    /// Sign-in token exchange invoke name.
    /// </summary>
    public static InvokeName SignInTokenExchange => InvokeName.SignInTokenExchange;

    /// <summary>
    /// Sign-in verify state invoke name.
    /// </summary>
    public static InvokeName SignInVerifyState => InvokeName.SignInVerifyState;

    /// <summary>
    /// Sign-in failure invoke name. Sent by the Teams client when SSO token exchange
    /// fails client-side (e.g., misconfigured Entra app registration).
    /// </summary>
    public static InvokeName SignInFailure => InvokeName.SignInFailure;

    /// <summary>
    /// Message extension anonymous query link invoke name.
    /// </summary>
    public static InvokeName MessageExtensionAnonQueryLink => InvokeName.MessageExtensionAnonQueryLink;

    /// <summary>
    /// Message extension fetch task invoke name.
    /// </summary>
    public static InvokeName MessageExtensionFetchTask => InvokeName.MessageExtensionFetchTask;

    /// <summary>
    /// Message extension query invoke name.
    /// </summary>
    public static InvokeName MessageExtensionQuery => InvokeName.MessageExtensionQuery;

    /// <summary>
    /// Message extension query link invoke name.
    /// </summary>
    public static InvokeName MessageExtensionQueryLink => InvokeName.MessageExtensionQueryLink;

    /// <summary>
    /// Message extension query setting URL invoke name.
    /// </summary>
    public static InvokeName MessageExtensionQuerySettingUrl => InvokeName.MessageExtensionQuerySettingUrl;

    /// <summary>
    /// Message extension select item invoke name.
    /// </summary>
    public static InvokeName MessageExtensionSelectItem => InvokeName.MessageExtensionSelectItem;

    /// <summary>
    /// Message extension submit action invoke name.
    /// </summary>
    public static InvokeName MessageExtensionSubmitAction => InvokeName.MessageExtensionSubmitAction;

    /// <summary>
    /// Message fetch task invoke name. Sent when the user clicks a feedback button on an AI-generated message.
    /// </summary>
    public static InvokeName MessageFetchTask => InvokeName.MessageFetchTask;

    /// <summary>
    /// Message submit action invoke name.
    /// </summary>
    public static InvokeName MessageSubmitAction => InvokeName.MessageSubmitAction;

    /// <summary>
    /// Suggested action submit invoke name.
    /// Sent when the user clicks a suggested action of type <c>Action.Submit</c>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.Experimental("ExperimentalTeamsSuggestedAction")]
    public static InvokeName SuggestedActionSubmit => InvokeName.SuggestedActionSubmit;
}
