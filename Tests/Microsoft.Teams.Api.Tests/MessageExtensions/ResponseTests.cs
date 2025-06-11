using System.Text.Json;

using Microsoft.Teams.Api.MessageExtensions;
using Microsoft.Teams.Cards;



namespace Microsoft.Teams.Api.Tests.MessageExtensions;

public class ResponseTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string s_queryLinkMEResponseJson = File.ReadAllText(
        @"../../../Json/MessageExtensions/QueryLinkMEResponse.json"
    );

    private static Response SetupQueryLinkResponse()
    {
        var card = new AdaptiveCard
        {
            Body =
            [
                new TextBlock("Hello from Samples.Agent!")
            ]
        };

        return new Response
        {
            ComposeExtension = new Result
            {
                Type = ResultType.Result,
                Attachments = new List<Api.MessageExtensions.Attachment>
                {
                    new Api.MessageExtensions.Attachment(ContentType.AdaptiveCard)
                    {
                        Content= card,
                        Preview =  new Api.Attachment(ContentType.ThumbnailCard)
                        {
                            ThumbnailUrl = "https://github.com/microsoft/teams-agent-accelerator-samples/raw/main/python/memory-sample-agent/docs/images/memory-thumbnail.png"
                        }
                    }
                },
                AttachmentLayout = Attachment.Layout.List,
            }
        };
    }

    [Fact]
    public void QueryLinkMEResponse_JsonSerialize()
    {
        var meResponse = SetupQueryLinkResponse();

        var json = JsonSerializer.Serialize(meResponse, CachedJsonSerializerOptions);

        Assert.Equal(typeof(Response), meResponse.GetType());
        Assert.Equal(s_queryLinkMEResponseJson, json);
    }
}