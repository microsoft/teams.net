// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// The context from which the command originates.
//  Possible values include: 'message', 'compose', 'commandbox'
/// </summary>
[JsonConverter(typeof(JsonConverter<MessagePreviewAction>))]
public class MessagePreviewAction(string value) : StringEnum(value)
{
    public static readonly MessagePreviewAction Edit = new("edit");
    public bool IsEdit => Edit.Equals(Value);

    public static readonly MessagePreviewAction Send = new("send");
    public bool IsSend => Send.Equals(Value);
}

/// <summary>
/// Messaging extension action
/// </summary>
public class Action : TaskModules.Request
{
    /// <summary>
    /// Id of the command assigned by Bot
    /// </summary>
    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(2)]
    public string? CommandId { get; set; }

    /// <summary>
    /// The context from which the command originates.
    //  Possible values include: 'message', 'compose', 'commandbox'
    /// </summary>
    [JsonPropertyName("commandContext")]
    [JsonPropertyOrder(3)]
    public required Commands.Context CommandContext { get; set; }

    /// <summary>
    /// Bot message preview action taken by user. Possible values include: 'edit', 'send'
    /// </summary>
    [JsonPropertyName("botMessagePreviewAction")]
    [JsonPropertyOrder(4)]
    public MessagePreviewAction? BotMessagePreviewAction { get; set; }

    /// <summary>
    /// the activities to preview
    /// </summary>
    [JsonPropertyName("botActivityPreview")]
    [JsonPropertyOrder(5)]
    public IList<Activities.Activity>? BotActivityPreview { get; set; }

    /// <summary>
    /// Message content sent as part of the command request.
    /// </summary>
    [JsonPropertyName("messagePayload")]
    [JsonPropertyOrder(6)]
    public Messages.Message? MessagePayload { get; set; }
}