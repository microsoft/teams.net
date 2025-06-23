// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// Possible values include: 'result', 'auth', 'config', 'message', 'botMessagePreview', 'silentAuth'.
/// </summary>
[JsonConverter(typeof(JsonConverter<ResultType>))]
public class ResultType(string value) : StringEnum(value)
{
    public static readonly ResultType Result = new("result");
    public bool IsResult => Result.Equals(Value);

    public static readonly ResultType Auth = new("auth");
    public bool IsAuth => Auth.Equals(Value);

    public static readonly ResultType Config = new("config");
    public bool IsConfig => Config.Equals(Value);

    public static readonly ResultType Message = new("message");
    public bool IsMessage => Message.Equals(Value);

    public static readonly ResultType BotMessagePreview = new("botMessagePreview");
    public bool IsBotMessagePreview => BotMessagePreview.Equals(Value);

    public static readonly ResultType SilentAuth = new("silentAuth");
    public bool IsSilentAuth => SilentAuth.Equals(Value);
}

/// <summary>
/// Messaging extension result
/// </summary>
public class Result
{
    /// <summary>
    /// Hint for how to deal with multiple attachments. Possible values include: 'list', 'grid'
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    [JsonPropertyOrder(0)]
    public Api.Attachment.Layout? AttachmentLayout { get; set; }

    /// <summary>
    /// The type of the result. Possible values include:
    /// 'result', 'auth', 'config', 'message', 'botMessagePreview'
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public ResultType? Type { get; set; }

    /// <summary>
    /// (Only when type is result) Attachments
    /// </summary>
    [JsonPropertyName("attachments")]
    [JsonPropertyOrder(2)]
    public IList<Attachment>? Attachments { get; set; }

    /// <summary>
    /// suggested actions
    /// </summary>
    [JsonPropertyName("suggestedActions")]
    [JsonPropertyOrder(3)]
    public SuggestedActions? SuggestedActions { get; set; }

    /// <summary>
    /// (Only when type is message) Text
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(4)]
    public string? Text { get; set; }

    /// <summary>
    /// (Only when type is botMessagePreview) Message activity to preview
    /// </summary>
    [JsonPropertyName("activityPreview")]
    [JsonPropertyOrder(5)]
    public Activities.Activity? ActivityPreview { get; set; }
}