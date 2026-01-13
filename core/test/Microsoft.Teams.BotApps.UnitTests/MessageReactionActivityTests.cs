// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Handlers;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps.UnitTests;

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
        MessageReactionArgs? mra = new(act);

        Assert.NotNull(mra);
        Assert.NotNull(mra!.ReactionsAdded);
        Assert.Equal(2, mra!.ReactionsAdded!.Count);
        Assert.Equal("like", mra!.ReactionsAdded[0].Type);
        Assert.Equal("heart", mra!.ReactionsAdded[1].Type);
    }
}
