using System.Text.Json;
using Microsoft.Teams.Api.MessageExtensions;
using Microsoft.Teams.Cards;



namespace Microsoft.Teams.Api.Tests.MessageExtensions;

public class ResponseTests
{
    private Response SetupQueryLinkResponse()
    {
        var card = new Teams.Cards.AdaptiveCard
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

        var json = JsonSerializer.Serialize(meResponse, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });


        Assert.Equal(typeof(Response), meResponse.GetType());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/MessageExtensions/QueryLinkMEResponse.json"
        ), json);
    }

    //[Fact]
    //public void QueryLinkMEActivity_JsonSerialize_Derived()
    //{
    //    MessageExtensionActivity activity = SetupQueryLinkActivity();

    //    var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
    //    {
    //        WriteIndented = true,
    //        IndentSize = 2,
    //        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    //    });

    //    string expectedPath = "Activity.Invoke.ComposeExtension/queryLink";
    //    Assert.Equal(expectedPath, activity.GetPath());
    //    Assert.Equal(File.ReadAllText(
    //        @"../../../Json/Activity/Invokes/QueryLinkMEActivity.json"
    //    ), json);
    //}

    //[Fact]
    //public void QueryLinkMEActivity_JsonSerialize_Derived_Interface()
    //{
    //    InvokeActivity activity = SetupQueryLinkActivity();

    //    var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
    //    {
    //        WriteIndented = true,
    //        IndentSize = 2,
    //        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    //    });

    //    string expectedPath = "Activity.Invoke.ComposeExtension/queryLink";
    //    Assert.Equal(expectedPath, activity.GetPath());
    //    Assert.Equal(File.ReadAllText(
    //        @"../../../Json/Activity/Invokes/QueryLinkMEActivity.json"
    //    ), json);
    //}

    //[Fact]
    //public void QueryLinkMEActivity_JsonDeserialize()
    //{
    //    var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
    //    var activity = JsonSerializer.Deserialize<QueryLinkActivity>(json);
    //    var expected = SetupQueryLinkActivity();

    //    Assert.Equal(expected.ToString(), activity!.ToString());
    //    Assert.NotNull(activity.ToMessageExtension());
    //}

    //[Fact]
    //public void QueryLinkMEActivity_JsonDeserialize_Derived()
    //{
    //    var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
    //    var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
    //    var expected = SetupQueryLinkActivity();

    //    Assert.Equal(expected.ToString(), activity!.ToString());
    //    Assert.NotNull(activity.ToMessageExtension());
    //    var expectedSubmitException = "Unable to cast object of type 'QueryLinkActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
    //    var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
    //    Assert.Equal(expectedSubmitException, ex.Message);
    //}

    //[Fact]
    //public void QueryLinkMEActivity_JsonDeserialize_Derived_Interface()
    //{
    //    var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
    //    var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
    //    var expected = SetupQueryLinkActivity();

    //    Assert.NotNull(activity);
    //    Assert.Equal(expected.ToString(), activity.ToString());
    //    Assert.NotNull(activity.ToMessageExtension());

    //    var expectedSubmitException = "Unable to cast object of type 'QueryLinkActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.SignInActivity'.";
    //    var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSignIn());
    //    Assert.Equal(expectedSubmitException, ex.Message);
    //}

    //[Fact]
    //public void QueryLinkMEActivity_JsonDeserialize_Derived_Activity_Interface()
    //{
    //    var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
    //    var activity = JsonSerializer.Deserialize<Activity>(json);
    //    var expected = SetupQueryLinkActivity();

    //    Assert.NotNull(activity);
    //    Assert.Equal(expected.ToString(), activity.ToString());
    //}
}