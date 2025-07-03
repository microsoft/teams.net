// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class MessageExtensions : StringEnum
    {
        public static readonly MessageExtensions CardButtonClicked = new("composeExtension/onCardButtonClicked");
        public bool IsCardButtonClicked => CardButtonClicked.Equals(Value);
    }
}

public static partial class MessageExtensions
{
    public class CardButtonClickedActivity() : MessageExtensionActivity(Name.MessageExtensions.CardButtonClicked)
    {

    }
}