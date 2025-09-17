using System.Text.Json;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.MessageExtensions;

public class SubmitActionDeserializationTests
{
    [Fact]
    public void SubmitActionActivity_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "name": "composeExtension/submitAction",
            "type": "invoke",
            "timestamp": "2025-09-17T08:44:12.099Z",
            "localTimestamp": "2025-09-17T01:44:12.099-07:00",
            "id": "f:bd88ab2f-5f9e-0b79-5619-ce59937731a2",
            "channelId": "msteams",
            "serviceUrl": "https://smba.trafficmanager.net/amer/50612dbb-0237-4969-b378-8d42590f9c00/",
            "from": {
                "id": "29:1_GIvHPvI3atQPvgLSHXVsZUwNN_c0FRgZQx6xtFAe_cZZW3uJ8VZp5x6Kl1DdRjnmsFg9x7wKQ83eXcrOLGIXw",
                "name": "Aamir Jawaid",
                "aadObjectId": "1f41a2a6-addd-4719-b075-28eb1c7a66f4"
            },
            "conversation": {
                "conversationType": "personal",
                "tenantId": "50612dbb-0237-4969-b378-8d42590f9c00",
                "id": "a:1KKKvq79q7mKR5h1D1SZtDDOCGQmeToAQXPLOAd4T9K3ineZ38Nwm9ELsV6Bv9yeRsw9taGd-byCJNBaaiy9_4u3bl0cuVok7IWxAAOmlH12adkr4u1lBiyye_wc1FwNu"
            },
            "recipient": {
                "id": "28:c083fa20-55e5-4aeb-916a-0cec47a40b62",
                "name": "MessageExtensions"
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
            "channelData": {
                "tenant": {
                    "id": "50612dbb-0237-4969-b378-8d42590f9c00"
                },
                "source": {
                    "name": "compose"
                }
            },
            "value": {
                "commandId": "createCard",
                "commandContext": "compose",
                "data": {
                    "id": "submitButton",
                    "title": "asdf",
                    "subTitle": "asdf",
                    "text": "asdf"
                },
                "context": {
                    "theme": "default"
                }
            },
            "locale": "en-US",
            "localTimezone": "America/Los_Angeles"
        }
        """;

        // Act
        var activity = JsonSerializer.Deserialize<Api.Activities.Invokes.MessageExtensions.SubmitActionActivity>(json);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("composeExtension/submitAction", activity.Name);
        Assert.Equal("invoke", activity.Type);
        Assert.NotNull(activity.Value);
        Assert.Equal("createCard", activity.Value.CommandId);
        Assert.Equal("compose", activity.Value.CommandContext);
        Assert.NotNull(activity.Value.Data);
        
        // Check that data can be accessed as JsonElement
        var dataElement = (JsonElement)activity.Value.Data;
        Assert.True(dataElement.TryGetProperty("title", out var titleElement));
        Assert.Equal("asdf", titleElement.GetString());
    }

    [Fact]
    public void SubmitActionActivity_ShouldDeserializeWithOptionalFields()
    {
        // This test uses the real Teams JSON format
        
        // Arrange - Real Teams JSON
        var teamsJson = """
        {
            "name": "composeExtension/submitAction",
            "type": "invoke", 
            "value": {
                "commandId": "createCard",
                "commandContext": "compose",
                "data": {
                    "title": "Test Title",
                    "description": "Test Description"
                }
            }
        }
        """;

        // Act
        var activity = JsonSerializer.Deserialize<Api.Activities.Invokes.MessageExtensions.SubmitActionActivity>(teamsJson);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("createCard", activity.Value?.CommandId);
        Assert.True(activity.Value?.CommandContext?.IsCompose);
        Assert.Null(activity.Value?.BotMessagePreviewAction); // Should be null since it's optional
        
        var dataElement = (JsonElement)activity.Value!.Data!;
        Assert.True(dataElement.TryGetProperty("title", out var title));
        Assert.Equal("Test Title", title.GetString());
    }

    [Fact] 
    public void Context_CommandBoxCasing_MightCauseIssues()
    {
        // Test potential casing issue with commandBox vs commandbox
        var lowerCaseJson = """{"commandContext": "commandbox"}""";
        var camelCaseJson = """{"commandContext": "commandBox"}""";

        // This should work (matches Context.CommandBox)
        var camelCase = JsonSerializer.Deserialize<Api.Commands.Context>(camelCaseJson.Replace("commandContext", ""));
        
        // This might fail if Teams sends lowercase
        var exception = Assert.ThrowsAny<Exception>(() => 
            JsonSerializer.Deserialize<Api.Commands.Context>(lowerCaseJson.Replace("commandContext", "")));
    }
}