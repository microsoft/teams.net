// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

namespace Microsoft.Teams.BotApps.UnitTests;

public class TeamsActivityTests
{

    [Fact]
    public void DeserializeActivityWithTeamsChannelData()
    {
        TeamsActivity activityWithTeamsChannelData = TeamsActivity.FromJsonString(json);
        TeamsChannelData tcd = activityWithTeamsChannelData.ChannelData!;
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2", tcd.TeamsChannelId);
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2", tcd.Channel!.Id);
        // Assert.Equal("b15a9416-0ad3-4172-9210-7beb711d3f70", activity.From.AadObjectId);
    }

    [Fact]
    public void DeserializeTeamsActivityWithTeamsChannelData()
    {
        TeamsActivity activity = TeamsActivity.FromJsonString(json);
        TeamsChannelData tcd = activity.ChannelData!;
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2", tcd.TeamsChannelId);
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2", tcd.Channel!.Id);
        Assert.Equal("b15a9416-0ad3-4172-9210-7beb711d3f70", activity.From.AadObjectId);
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856", activity.Conversation.Id);

        Assert.NotNull(activity.Attachments);
        Assert.Single(activity.Attachments);
        Assert.Equal("text/html", activity.Attachments[0].ContentType);

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);

    }

    [Fact]
    public void DownCastTeamsActivity_To_CoreActivity()
    {
        CoreActivity activity = CoreActivity.FromJsonString(json);
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856", activity.Conversation!.Id);
        TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
        Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856", teamsActivity.Conversation!.Id);

        static void AssertCid(CoreActivity a)
        {
            Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856", a.Conversation!.Id);
        }
        AssertCid(teamsActivity);

    }

    [Fact]
    public void AddMentionEntity_To_TeamsActivity()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityTypes.Message));
        activity
            .AddMention(new ConversationAccount
            {
                Id = "user-id-01",
                Name = "rido"
            }, "ridotest");
            


        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.Equal("mention", activity.Entities[0].Type);
        MentionEntity? mention = activity.Entities[0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("user-id-01", mention.Mentioned?.Id);
        Assert.Equal("rido", mention.Mentioned?.Name);
        Assert.Equal("<at>ridotest</at>", mention.Text);

        string json = activity.ToJson();
        Assert.Contains("user-id-01", json);
    }

    [Fact]
    public void AddMentionEntity_Serialize_From_CoreActivity()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityTypes.Message));
        activity.AddMention(new ConversationAccount
            {
                Id = "user-id-01",
                Name = "rido"
            }, "ridotest");



        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.Equal("mention", activity.Entities[0].Type);
        MentionEntity? mention = activity.Entities[0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("user-id-01", mention.Mentioned?.Id);
        Assert.Equal("rido", mention.Mentioned?.Name);
        Assert.Equal("<at>ridotest</at>", mention.Text);

        static void SerializeAndAssert(CoreActivity a)
        {
            string json = a.ToJson();
            Assert.Contains("user-id-01", json);
        }

        SerializeAndAssert(activity);
    }


    [Fact]
    public void TeamsActivityBuilder_FluentAPI()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(ActivityTypes.Message)
            .WithText("Hello World")
            .WithChannelId("msteams")
            .AddMention(new ConversationAccount
            {
                Id = "user-123",
                Name = "TestUser"
            })
            .Build();

        Assert.Equal(ActivityTypes.Message, activity.Type);
        Assert.Equal("<at>TestUser</at> Hello World", activity.Text);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        MentionEntity? mention = activity.Entities[0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("user-123", mention.Mentioned?.Id);
        Assert.Equal("TestUser", mention.Mentioned?.Name);
    }

    [Fact]
    public void Deserialize_With_Entities()
    {
        TeamsActivity activity = TeamsActivity.FromJsonString(json);
        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);

        List<Entity> mentions = [.. activity.Entities.Where(e => e is MentionEntity)];
        Assert.Single(mentions);
        MentionEntity? m1 = mentions[0] as MentionEntity;
        Assert.NotNull(m1);
        Assert.NotNull(m1.Mentioned);
        Assert.Equal("28:0b6fe6d1-fece-44f7-9a48-56465e2d5ab8", m1.Mentioned.Id);
        Assert.Equal("ridotest", m1.Mentioned.Name);
        Assert.Equal("<at>ridotest</at>", m1.Text);

        List<Entity> clientInfos = [.. activity.Entities.Where(e => e is ClientInfoEntity)];
        Assert.Single(clientInfos);
        ClientInfoEntity? c1 = clientInfos[0] as ClientInfoEntity;
        Assert.NotNull(c1);
        Assert.Equal("en-US", c1.Locale);
        Assert.Equal("US", c1.Country);
        Assert.Equal("Web", c1.Platform);
        Assert.Equal("America/Los_Angeles", c1.Timezone);

    }



    private const string json = """
            {
              "type": "message",
              "channelId": "msteams",
              "text": "\u003Cat\u003Eridotest\u003C/at\u003E reply to thread",
              "id": "1759944781430",
              "serviceUrl": "https://smba.trafficmanager.net/amer/50612dbb-0237-4969-b378-8d42590f9c00/",
              "channelData": {
                "teamsChannelId": "19:6848757105754c8981c67612732d9aa7@thread.tacv2",
                "teamsTeamId": "19:66P469zibfbsGI-_a0aN_toLTZpyzS6u7CT3TsXdgPw1@thread.tacv2",
                "channel": {
                  "id": "19:6848757105754c8981c67612732d9aa7@thread.tacv2"
                },
                "team": {
                  "id": "19:66P469zibfbsGI-_a0aN_toLTZpyzS6u7CT3TsXdgPw1@thread.tacv2"
                },
                "tenant": {
                  "id": "50612dbb-0237-4969-b378-8d42590f9c00"
                }
              },
              "from": {
                "id": "29:17bUvCasIPKfQIXHvNzcPjD86fwm6GkWc1PvCGP2-NSkNb7AyGYpjQ7Xw-XgTwaHW5JxZ4KMNDxn1kcL8fwX1Nw",
                "name": "rido",
                "aadObjectId": "b15a9416-0ad3-4172-9210-7beb711d3f70"
              },
              "recipient": {
                "id": "28:0b6fe6d1-fece-44f7-9a48-56465e2d5ab8",
                "name": "ridotest"
              },
              "conversation": {
                "id": "19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856",
                "isGroup": true,
                "conversationType": "channel",
                "tenantId": "50612dbb-0237-4969-b378-8d42590f9c00"
              },
              "entities": [
                {
                  "mentioned": {
                    "id": "28:0b6fe6d1-fece-44f7-9a48-56465e2d5ab8",
                    "name": "ridotest"
                  },
                  "text": "\u003Cat\u003Eridotest\u003C/at\u003E",
                  "type": "mention"
                },
                {
                  "locale": "en-US",
                  "country": "US",
                  "platform": "Web",
                  "timezone": "America/Los_Angeles",
                  "type": "clientInfo"
                }
              ],
              "textFormat": "plain",
              "attachments": [
                {
                  "contentType": "text/html",
                  "content": "\u003Cp\u003E\u003Cspan itemtype=\u0022http://schema.skype.com/Mention\u0022 itemscope=\u0022\u0022 itemid=\u00220\u0022\u003Eridotest\u003C/span\u003E\u0026nbsp;reply to thread\u003C/p\u003E"
                }
              ],
              "timestamp": "2025-10-08T17:33:01.4953744Z",
              "localTimestamp": "2025-10-08T10:33:01.4953744-07:00",
              "locale": "en-US",
              "localTimezone": "America/Los_Angeles"
            }
            """;
}
