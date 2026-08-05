# Sample: Meetings

This sample demonstrates how to handle real-time updates for meeting events and meeting participant events.

## What it shows

- Meeting and participant lifecycle callbacks in Teams.
- Required manifest permissions for meeting event delivery.
- Practical trigger paths for validating each event type.

## Prerequisites

- Bot registered and installed in Teams.
- Meeting event subscriptions enabled for the bot in Teams Developer Portal.

## Manifest requirements

Include these manifest settings to support meetings events.

1) The `scopes` section must include `team`, and `groupChat`:

```json
 "bots": [
        {
            "botId": "",
            "scopes": [
                "team",
                "personal",
                "groupChat"
            ],
            "isNotificationOnly": false
        }
    ]
```

2) In the authorization section, make sure to specify the following resource-specific permissions:

```json
 "authorization":{
        "permissions":{
            "resourceSpecific":[
                {
                    "name":"OnlineMeetingParticipant.Read.Chat",
                    "type":"Application"
                },
                {
                    "name":"ChannelMeeting.ReadBasic.Group",
                    "type":"Application"
                },
                {
                    "name":"OnlineMeeting.ReadBasic.Chat",
                    "type":"Application"
                }
                ]
            }
        }
```

### Teams Developer Portal bot configuration

For your Bot, make sure the [Meeting Event Subscriptions](https://learn.microsoft.com/en-us/microsoftteams/platform/apps-in-teams-meetings/meeting-apps-apis?branch=pr-en-us-8455&tabs=channel-meeting%2Cguest-user%2Cone-on-one-call%2Cdotnet3%2Cdotnet2%2Cdotnet%2Cparticipant-join-event%2Cparticipant-join-event1#receive-meeting-participant-events) are checked.
This enables you to receive the Meeting Participant events.

## Running the Sample

~~~bash
dotnet run --project samples/MeetingsBot/MeetingsBot.csproj
~~~
