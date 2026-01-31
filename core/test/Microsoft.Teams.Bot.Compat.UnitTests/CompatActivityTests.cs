// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Core.Schema;
using Newtonsoft.Json;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    public class CompatActivityTests
    {
        [Fact]
        public void FromCompatActivity()
        {
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(compatActivityJson)!;
            Assert.NotNull(botActivity);

            CoreActivity coreActivity = botActivity.FromCompatActivity();
            Assert.NotNull(coreActivity);
            Assert.NotNull(coreActivity.Attachments);
            Assert.Single(coreActivity.Attachments);

            var attachmentNode = coreActivity.Attachments[0];
            Assert.NotNull(attachmentNode);
            var attachmentObj = attachmentNode.AsObject();

            var contentType = attachmentObj["contentType"]?.GetValue<string>();
            Assert.Equal("application/vnd.microsoft.card.adaptive", contentType);

            var content = attachmentObj["content"];
            Assert.NotNull(content);
            var card = AdaptiveCard.FromJson(content.ToJsonString()).Card;
            Assert.Equal(2, card.Body.Count);
            var firstTextBlock = card.Body[0] as AdaptiveTextBlock;
            Assert.NotNull(firstTextBlock);
            Assert.Equal("Mention a user by User Principle Name: Hello <at>Rido UPN</at>", firstTextBlock.Text);
        }

        string compatActivityJson = """
            {
                "type": "message",
                "serviceUrl": "https://smba.trafficmanager.net/amer/9a9b49fd-1dc5-4217-88b3-ecf855e91b0e/",
                "channelId": "msteams",
                "from": {
                    "id": "28:fa45fe59-200c-493c-aa4c-80c17ad6f307",
                    "name": "ridodev-local"
                },
                "conversation": {
                    "conversationType": "personal",
                    "id": "a:188cfPEO2ZNiFxoCSq-2QwCkQTBywkMID0Y2704RpFR2QjMx8217cpDunnnI-rx95Qn_1ce11juGEelMnscuyEQvHTh_wRRRKR_WxbV8ZS4-1qFwb0l8T0Zrd9uiTCtLX",
                    "tenantId": "9a9b49fd-1dc5-4217-88b3-ecf855e91b0e"
                },
                "recipient": {
                    "id": "29:1zIP3NcdoJbnv2Rp-x-7ukmDhrgy6JqXcDgYB4mFxGCtBRvVT7V0Iwu0obPlWlBd14M2qEa4p5qqJde0HTYy4cw",
                    "name": "Rido",
                    "aadObjectId": "16de8f24-f65d-4f6b-a837-3a7e638ab6e1"
                },
                "attachmentLayout": "list",
                "locale": "en-US",
                "inputHint": "acceptingInput",
                "attachments": [
                    {
                        "contentType": "application/vnd.microsoft.card.adaptive",
                        "content": {
                            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                            "type": "AdaptiveCard",
                            "version": "1.5",
                            "speak": "This card mentions a user by User Principle Name: Hello Rido",
                            "body": [
                                {
                                    "type": "TextBlock",
                                    "text": "Mention a user by User Principle Name: Hello <at>Rido UPN</at>"
                                },
                                {
                                    "type": "TextBlock",
                                    "text": "Mention a user by AAD Object Id: Hello <at>Rido AAD</at>"
                                }
                            ],
                            "msteams": {
                                "entities": [
                                    {
                                        "type": "mention",
                                        "text": "<at>Rido UPN</at>",
                                        "mentioned": {
                                            "id": "rido@tsdk1.onmicrosoft.com",
                                            "name": "Rido"
                                        }
                                    },
                                    {
                                        "type": "mention",
                                        "text": "<at>Rido AAD</at>",
                                        "mentioned": {
                                            "id": "16de8f24-f65d-4f6b-a837-3a7e638ab6e1",
                                            "name": "Rido"
                                        }
                                    }
                                ]
                            }
                        }
                    }
                ],
                "entities": [
                    {
                        "type": "https://schema.org/Message",
                        "@context": "https://schema.org",
                        "@type": "Message",
                        "additionalType": [
                            "AIGeneratedContent"
                        ]
                    },
                ],
                "replyToId": "f:d1c5de53-9e8b-b5c3-c24d-07c2823079cf"
            }
            """;
    }
}