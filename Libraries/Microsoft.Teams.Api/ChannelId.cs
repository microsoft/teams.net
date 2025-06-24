// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(JsonConverter<ChannelId>))]
public class ChannelId(string value) : StringEnum(value)
{
    public static readonly ChannelId MsTeams = new("msteams");
    public bool IsMsTeams => MsTeams.Equals(Value);

    public static readonly ChannelId WebChat = new("webchat");
    public bool IsWebChat => WebChat.Equals(Value);
}