// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// Messaging Extension Attachment Layout. Possible values include: 'list', 'grid'.
/// </summary>
[JsonConverter(typeof(StringEnum.JsonConverter<AttachmentLayout>))]
public class AttachmentLayout(string value) : StringEnum(value)
{
    public static readonly AttachmentLayout List = new("list");
    [JsonIgnore]
    public bool IsList => List.Equals(Value);

    public static readonly AttachmentLayout Grid = new("grid");
    [JsonIgnore]
    public bool IsGrid => Grid.Equals(Value);
}
