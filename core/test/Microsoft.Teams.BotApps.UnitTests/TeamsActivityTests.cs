using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema;

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

        void AssertCid(CoreActivity a)
        {
            Assert.Equal("19:6848757105754c8981c67612732d9aa7@thread.tacv2;messageid=1759881511856", a.Conversation!.Id);
        }
        AssertCid(teamsActivity);

    }


    const string json = """
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
