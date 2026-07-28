// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// This class is deprecated. Please use <see cref="MessageBackSubmitActionData"/> instead. This will be removed in a future version of the SDK.
/// </summary>
[Obsolete("This class is deprecated. Use MessageBackSubmitActionData instead. This will be removed in a future version of the SDK.")]
public class MessageBackAction : SubmitAction
{
    public MessageBackAction(string text, string value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            Msteams = new MessageBackSubmitActionData()
            {
                Text = text,
                Value = value
            }
        });
    }

    public MessageBackAction(string text, string displayText, string value)
    {
        Data = new Union<string, SubmitActionData>(new SubmitActionData
        {
            Msteams = new MessageBackSubmitActionData()
            {
                Text = text,
                DisplayText = displayText,
                Value = value
            }
        });
    }
}