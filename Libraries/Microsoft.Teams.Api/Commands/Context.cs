// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Commands;

/// <summary>
/// The context from which the command originates.
//  Possible values include: 'message', 'compose', 'commandbox'
/// </summary>
[JsonConverter(typeof(JsonConverter<Context>))]
public class Context(string value) : StringEnum(value)
{
    public static readonly Context Message = new("message");
    public bool IsMessage => Message.Equals(Value);

    public static readonly Context Compose = new("compose");
    public bool IsCompose => Compose.Equals(Value);

    public static readonly Context CommandBox = new("commandBox");
    public bool IsCommandBox => CommandBox.Equals(Value);
}