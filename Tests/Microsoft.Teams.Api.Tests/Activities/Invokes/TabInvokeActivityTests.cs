

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Tabs;
using static Microsoft.Teams.Api.Activities.Invokes.Tabs;

namespace Microsoft.Teams.Api.Tests.Activities;

public class TabInvokeActivityTests
{
    [Fact]
    public void TabsFetchActivity_Props()
    {
        var activity = new FetchActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var expectedSubmitException = "Unable to cast object of type 'FetchActivity' to type 'SubmitActivity'.";

        Assert.NotNull(activity.ToFetch());

        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSubmit());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void TabsFetchActivity_JsonSerialize()
    {
        var activity = new FetchActivity()
        {
            Value = new Tabs.Request() {
                TabContext= new EntityContext() { 
                    TabEntityId="tabEntityIdString" 
                },
            },
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TabsFetchActivity.json"
        ), json);
    }

    [Fact]
    public void TabsFetchActivity_JsonSerialize_Derived()
    {
        TabActivity activity = new FetchActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TabsFetchActivity.json"
        ), json);
    }

    [Fact]
    public void TabsFetchActivity_JsonSerialize_Interface_Derived()
    {
        Activity activity = new FetchActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TabsFetchActivity.json"
        ), json);
    }


    [Fact]
    public void TabsFetchActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TabsFetchActivity.json");
        var activity = JsonSerializer.Deserialize<FetchActivity>(json);
        var expected = new FetchActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void TabsFetchActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TabsFetchActivity.json");
        var activity = JsonSerializer.Deserialize<TabActivity>(json);
        var expected = new FetchActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void TabsSubmitActivity_Props()
    {
        var activity = new SubmitActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var expectedSubmitException = "Unable to cast object of type 'SubmitActivity' to type 'FetchActivity'.";

        Assert.NotNull(activity. ToSubmit());

        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToFetch());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void TabsSubmitActivity_JsonSerialize()
    {
        var activity = new SubmitActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TabsSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void TabsSubmitActivity_JsonSerialize_Derived()
    {
        TabActivity activity = new SubmitActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TabsSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void TabsSubmitActivity_JsonSerialize_Interface_Derived()
    {
        Activity activity = new SubmitActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/TabsSubmitActivity.json"
        ), json);
    }


    [Fact]
    public void TabsSubmitActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TabsSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<SubmitActivity>(json);
        var expected = new SubmitActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void TabsSubmitActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/TabsSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<TabActivity>(json);
        var expected = new SubmitActivity()
        {
            Value = new Tabs.Request()
            {
                TabContext = new EntityContext()
                {
                    TabEntityId = "tabEntityIdString"
                },
            },
        };

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}