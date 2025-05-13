

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.Tasks;

namespace Microsoft.Teams.Api.Tests.Activities;

public class TaskInvokeActivityTests
{
    private FetchActivity setupFetchTaskActivity()
    {
        return new FetchActivity()
        {
            Value = new TaskModules.Request()
            {
                Data = new Dictionary<string, object>()
                {
                    { "key", "value" },
                },
                Context = new TaskModules.RequestContext()
                {
                    Theme = "default",
                },
            },
            Conversation = new Conversation()
            {
                Id = "conversationIdString",
                Type = ConversationType.GroupChat,
            },
            From = new Account()
            {
                Id = "fromIdString",
                Name = "fromNameString",
                AadObjectId = "fromAadObjectIdString",
                Role = Role.Bot,
            },
            Recipient = new Account()
            {
                Id = "recipientIdString",
                Name = "recipientNameString",
            },
            ChannelData = new ChannelData()
            {
                Channel = new Channel()
                {
                    Id = "channelIdString",
                    Name = "channelNameString",
                },
                Team = new Team()
                {
                    Id = "teamIdString",
                    Name = "teamNameString",
                },
            },
            Locale = "en-en",
            ServiceUrl = "fakeServiceUrl",
            Properties = new Dictionary<string, object?>()
            {
                { "key", "value" },
            },
        };
    }

    private SubmitActivity setupSubmitTaskActivity()
    {
        return new SubmitActivity()
        {
            Value = new TaskModules.Request()
            {
                Data = new Dictionary<string, object>()
                {
                    { "key", "value" },
                },
                Context = new TaskModules.RequestContext()
                {
                    Theme = "default",
                },
            },
            Conversation = new Conversation()
            {
                Id = "conversationIdString",
                Type = ConversationType.GroupChat,
            },
            From = new Account()
            {
                Id = "fromIdString",
                Name = "fromNameString",
                AadObjectId = "fromAadObjectIdString",
                Role = Role.Bot,
            },
            Recipient = new Account()
            {
                Id = "recipientIdString",
                Name = "recipientNameString",
            },
            ChannelData = new ChannelData()
            {
                Channel = new Channel()
                {
                    Id = "channelIdString",
                    Name = "channelNameString",
                },
                Team = new Team()
                {
                    Id = "teamIdString",
                    Name = "teamNameString",
                },
            },
            Locale = "en-en",
            ServiceUrl = "fakeServiceUrl",
            Properties = new Dictionary<string, object?>()
            {
                { "key", "value" },
            },
        };
    }

    [Fact]
    public void TaskFetchActivity_Props()
    {
        var activity = new FetchActivity()
        {
            Value = new TaskModules.Request()
            {
                Data = new Dictionary<string, object>()
                {
                    { "key", "value" },
                },
                Context = new TaskModules.RequestContext()
                {
                    Theme = "default",
                },
            },

        };

        var expectedSubmitException = "Unable to cast object of type 'FetchActivity' to type 'SubmitActivity'.";

        Assert.NotNull(activity.ToFetch());
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSubmit());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void TaskFetchActivity_JsonSerialize()
    {
        var activity = setupFetchTaskActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TaskFetchActivity.json"
        ), json);
    }

    [Fact]
    public void TaskFetchActivity_JsonSerialize_Derived()
    {
        TaskActivity activity = setupFetchTaskActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TaskFetchActivity.json"
        ), json);
    }

    [Fact]
    public void TaskFetchActivity_JsonSerialize_Interface_Derived()
    {
        Activity activity = setupFetchTaskActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TaskFetchActivity.json"
        ), json);
    }

    [Fact]
    public void TaskFetchActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TaskFetchActivity.json");
        var activity = JsonSerializer.Deserialize<FetchActivity>(json);
        var expected = setupFetchTaskActivity();
        Assert.Equal(expected.ToString(), activity?.ToString());
    }

    [Fact]
    public void TaskFetchActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TaskFetchActivity.json");
        var activity = JsonSerializer.Deserialize<TaskActivity>(json);
        var expected = setupFetchTaskActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void TaskSubmitActivity_Props()
    {
        var activity = new SubmitActivity()
        {
            Value = new TaskModules.Request()
            {
                Data = new Dictionary<string, object>()
                {
                    { "key", "value" },
                },
                Context = new TaskModules.RequestContext()
                {
                    Theme = "default",
                },
            },

        };

        var expectedSubmitException = "Unable to cast object of type 'SubmitActivity' to type 'FetchActivity'.";

        Assert.NotNull(activity.ToSubmit());
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToFetch());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void TaskSubmitActivity_JsonSerialize()
    {
        var activity = setupSubmitTaskActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TaskSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void TaskSubmitActivity_JsonSerialize_Derived()
    {
        TaskActivity activity = setupSubmitTaskActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TaskSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void TaskSubmitActivity_JsonSerialize_Interface_Derived()
    {
        Activity activity = setupSubmitTaskActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TaskSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void TaskSubmitActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TaskSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<FetchActivity>(json);
        var expected = setupSubmitTaskActivity();
        Assert.Equal(expected.ToString(), activity?.ToString());
    }

    [Fact]
    public void TaskSubmitActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TaskSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<TaskActivity>(json);
        var expected = setupSubmitTaskActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

}