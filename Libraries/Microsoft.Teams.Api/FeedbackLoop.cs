// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

/// <summary>
/// The type of feedback loop.
/// Use <c>Custom</c> to trigger a <c>message/fetchTask</c> invoke so the bot
/// can return its own task module dialog.
/// Use <c>Default</c> for the standard Teams thumbs up/down UI.
/// </summary>
[JsonConverter(typeof(JsonConverter<FeedbackType>))]
public partial class FeedbackType(string value) : StringEnum(value)
{
    public static readonly FeedbackType Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly FeedbackType Custom = new("custom");
    public bool IsCustom => Custom.Equals(Value);
}

/// <summary>
/// Configuration for a feedback loop on a message.
/// </summary>
public class FeedbackLoop
{
    /// <summary>
    /// The type of feedback loop.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public FeedbackType Type { get; set; } = FeedbackType.Default;

    public FeedbackLoop() { }

    public FeedbackLoop(FeedbackType type)
    {
        Type = type;
    }
}
