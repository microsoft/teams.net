// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class TeamsActivityTests
{
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
    public void DownCastTeamsActivity_To_CoreActivity_FromBuilder()
    {

        TeamsActivity teamsActivity = TeamsActivity
            .CreateBuilder()
            .WithConversation(new Conversation() { Id = "19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856" })
            .Build();

        static void AssertCid(CoreActivity a)
        {
            Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856", a.Conversation!.Id);
        }
        AssertCid(teamsActivity);
    }

    [Fact]
    public void DownCastTeamsActivity_To_CoreActivity_WithoutRebase()
    {
        TeamsActivity teamsActivity = new TeamsActivity() { 
            Conversation = new TeamsConversation()
            {
                Id = "19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856"
            }
        };
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
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
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

        string jsonResult = activity.ToJson();
        Assert.Contains("user-id-01", jsonResult);
    }

    [Fact]
    public void AddMentionEntity_Serialize_From_CoreActivity()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
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
            .WithType(TeamsActivityType.Message)
            .WithText("Hello World")
            .WithChannelId("msteams")
            .AddMention(new ConversationAccount
            {
                Id = "user-123",
                Name = "TestUser"
            })
            .Build();

        Assert.Equal(ActivityType.Message, activity.Type);
        Assert.Equal("<at>TestUser</at> Hello World", activity.Properties["text"]);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        MentionEntity? mention = activity.Entities[0] as MentionEntity;
        Assert.NotNull(mention);
        Assert.Equal("user-123", mention.Mentioned?.Id);
        Assert.Equal("TestUser", mention.Mentioned?.Name);
    }

    [Fact]
    public void Serialize_TeamsActivity_WithEntities()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithText("Hello World")
            .WithChannelId("msteams")
            .Build();

        activity.AddClientInfo("Web", "US", "America/Los_Angeles", "en-US");

        string jsonResult = activity.ToJson();
        Assert.Contains("clientInfo", jsonResult);
        Assert.Contains("Web", jsonResult);
        Assert.Contains("Hello World", jsonResult);
    }

    [Fact]
    public void Deserialize_TeamsActivity_Invoke_WithValue()
    {
        //TeamsActivity activity = CoreActivity.FromJsonString<TeamsActivity>(jsonInvoke);
        TeamsActivity activity = TeamsActivity.FromActivity(CoreActivity.FromJsonString(jsonInvoke));
        Assert.NotNull(activity.Value);
        string feedback = activity.Value?["action"]?["data"]?["feedback"]?.ToString()!;
        Assert.Equal("test invokes", feedback);
    }

    private const string jsonInvoke = """
          {
          "type": "invoke",
          "channelId": "msteams",
          "id": "f:17b96347-e8b4-f340-10bc-eb52fc1a6ad4",
          "serviceUrl": "https://smba.trafficmanager.net/amer/56653e9d-2158-46ee-90d7-675c39642038/",
          "channelData": {
            "tenant": {
              "id": "56653e9d-2158-46ee-90d7-675c39642038"
            },
            "source": {
              "name": "message"
            },
            "legacy": {
              "replyToId": "1:12SWreU4430kJA9eZCb1kXDuo6A8KdDEGB6d9TkjuDYM"
            }
          },
          "from": {
            "id": "29:1uMVvhoAyfTqdMsyvHL0qlJTTfQF9MOUSI8_cQts2kdSWEZVDyJO2jz-CsNOhQcdYq1Bw4cHT0__O6XDj4AZ-Jw",
            "name": "Rido",
            "aadObjectId": "c5e99701-2a32-49c1-a660-4629ceeb8c61"
          },
          "recipient": {
            "id": "28:aabdbd62-bc97-4afb-83ee-575594577de5",
            "name": "ridobotlocal"
          },
          "conversation": {
            "id": "a:17vxw6pGQOb3Zfh8acXT8m_PqHycYpaFgzu2mFMUfkT-h0UskMctq5ZPPc7FIQxn2bx7rBSm5yE_HeUXsCcKZBrv77RgorB3_1_pAdvMhi39ClxQgawzyQ9GBFkdiwOxT",
            "conversationType": "personal",
            "tenantId": "56653e9d-2158-46ee-90d7-675c39642038"
          },
          "entities": [
            {
              "locale": "en-US",
              "country": "US",
              "platform": "Web",
              "timezone": "America/Los_Angeles",
              "type": "clientInfo"
            }
          ],
          "value": {
            "action": {
              "type": "Action.Execute",
              "title": "Submit Feedback",
              "data": {
                "feedback": "test invokes"
              }
            },
            "trigger": "manual"
          },
          "name": "adaptiveCard/action",
          "timestamp": "2026-01-07T06:04:59.89Z",
          "localTimestamp": "2026-01-06T22:04:59.89-08:00",
          "replyToId": "1767765488332",
          "locale": "en-US",
          "localTimezone": "America/Los_Angeles"
        }
        """;

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

    [Fact]
    public void FromActivity_ReturnsDerivedType_WhenRegistered()
    {
        CoreActivity coreActivity = new CoreActivity(ActivityType.Message);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.IsType<MessageActivity>(activity);
    }

    [Fact]
    public void FromActivity_ReturnsBaseType_WhenNotRegistered()
    {
        CoreActivity coreActivity = new CoreActivity("unknownType");
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.Equal(typeof(TeamsActivity), activity.GetType());
        Assert.Equal("unknownType", activity.Type);
    }
}
