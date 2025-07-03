// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType MessageUpdate = new("messageUpdate");
    public bool IsMessageUpdate => MessageUpdate.Equals(Value);
}

public class MessageUpdateActivity : MessageActivity
{
    public MessageUpdateActivity() : base()
    {
        Type = ActivityType.MessageUpdate;
    }

    public MessageUpdateActivity(string text) : base(text)
    {
        Type = ActivityType.MessageUpdate;
    }
}