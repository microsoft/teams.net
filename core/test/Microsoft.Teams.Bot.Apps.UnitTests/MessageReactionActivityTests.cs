// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class MessageReactionActivityTests
{
    [Fact]
    public void AsMessageReaction()
    {
        string json = """
        {
            "type": "messageReaction",
            "conversation": {
                "id": "19"
            },
            "reactionsAdded": [
                {
                    "type": "like"
                },
                {
                    "type": "heart"
                }
            ]
        }
        """;
        TeamsActivity act = TeamsActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("messageReaction", act.Type);

        // MessageReactionActivity? mra = MessageReactionActivity.FromActivity(act);
        /*MessageReactionArgs? mra = new(act);

        Assert.NotNull(mra);
        Assert.NotNull(mra!.ReactionsAdded);
        Assert.Equal(2, mra!.ReactionsAdded!.Count);
        Assert.Equal("like", mra!.ReactionsAdded[0].Type);
        Assert.Equal("heart", mra!.ReactionsAdded[1].Type);*/
    }
}
