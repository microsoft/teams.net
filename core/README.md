# Microsoft.Bot.Core

Bot Core implements the Activity Protocol, including schema, conversation client, user token client, and support for Bot and Agentic Identities.

## Design Principles

- Loose schema. `CoreActivity` contains only the strictly required fields for Conversation Client, additional fields are captured as a Dictitionary with JsonExtensionData attributes.
- Simple Serialization. `CoreActivity` can be serialized/deserialized without any custom logic, and trying to avoid custom converters as much as possible. 
- Extensible schema. Fields subject to extension, such as `ChannelData` must define their own `Properties` to allow serialization of unknown fields. Use of generics to allow additional types that are not defined in the Core Library.
- Auth based on MSAL. Token acquisition done on top of MSAL
- Respect ASP.NET DI. `BotApplication` dependencies are configured based on .NET ServiceCollection extensions, reusing the existing `HttpClient`
- Respect ILogger and IConfiguration.

## Samples

### Extensible Activity

```cs
public class MyChannelData : ChannelData
{
    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }

    [JsonPropertyName("myChannelId")]
    public string? MyChannelId { get; set; }
}

public class MyCustomChannelDataActivity : CoreActivity
{
    [JsonPropertyName("channelData")]
    public new MyChannelData? ChannelData { get; set; }
}

[Fact]
public void Deserialize_CustomChannelDataActivity()
{
    string json = """
    {
        "type": "message",
        "channelData": {
            "customField": "customFieldValue",
            "myChannelId": "12345"
        }
    }
    """;
    var deserializedActivity = CoreActivity.FromJsonString<MyCustomChannelDataActivity>(json);
    Assert.NotNull(deserializedActivity);
    Assert.NotNull(deserializedActivity.ChannelData);
    Assert.Equal("customFieldValue", deserializedActivity.ChannelData.CustomField);
    Assert.Equal("12345", deserializedActivity.ChannelData.MyChannelId);
}
```

> Note `FromJsonString` lives in `CoreActivity`, and there is no need to override.


### Basic Bot Application Usage

```cs
var webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication<BotApplication>();
var webApp = webAppBuilder.Build();
var botApp = webApp.UseBotApplication<BotApplication>();

botApp.OnActivity = async (activity, cancellationToken) =>
{
    var replyText = $"CoreBot running on SDK {BotApplication.Version}.";
    replyText += $"\r\nYou sent: `{activity.Text}` in activity of type `{activity.Type}`.";
    replyText += $"\r\n to Conversation ID: `{activity.Conversation.Id}` type: `{activity.Conversation.Properties["conversationType"]}`";
    var replyActivity = activity.CreateReplyActivity(ActivityType.Message, replyText);
    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();
```

## Testing in Teams

Need to create a Teams Application, configure it in ABS and capture `TenantId`, `ClientId` and `ClientSecret`. Provide those values as

```json
{
  "AzureAd" : {
    "Instance" : "https://login.microsoftonline.com/",
    "TenantId" : "<your-tenant-id>",
    "ClientId" : "<your-client-id>",
    "Scope" : "https://api.botframework.com/.default",
    "ClientCredentials" : [
        {
            "SourceType" : "ClientSecret",
            "ClientSecret" : "<your-entra-app-secret>"
        }
    ]
  }   
}
```

or as env vars, using the IConfiguration Environment Configuration Provider:

```env
 AzureAd__Instance=https://login.microsoftonline.com/
 AzureAd__TenantId=<your-tenant-id>
 AzureAd__ClientId=<your-client-id>
 AzureAd__Scope=https://api.botframework.com/.default
 AzureAd__ClientCredentials__0__SourceType=ClientSecret
 AzureAd__ClientCredentials__0__ClientSecret=<your-entra-app-secret>
```



## Testing in localhost (anonymous)

When not providing MSAL configuration all the communication will happen as anonymous REST calls, suitable for localhost testing.

### Install Playground

Linux
```
curl -s https://raw.githubusercontent.com/OfficeDev/microsoft-365-agents-toolkit/dev/.github/scripts/install-agentsplayground-linux.sh | bash
```

Windows
```
winget install m365agentsplayground
```


### Run Scenarios

```
dotnet samples/scenarios/middleware.cs -- --urls "http://localhost:3978"
```
